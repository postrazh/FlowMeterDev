using Ald.SerialPort.Configuration;
using FlowMeter.Configuration;
using FlowMeter.Helpers;
using FlowMeter.View;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace FlowMeter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ALDSerialPort serial;

        private string strBuffer = "";

        private enum CalcMode
        {
            NONE,
            STARTED_EXTERNAL_VOLUME,
            STARTED_FLOW_RATE
        }
        private CalcMode calcMode = CalcMode.NONE;

        public MainWindow()
        {
            InitializeComponent();

            // initialize values of controls
            lblPortName.Content = SettingsManager.getInstance().CurrentSettings.SerialConfig.PortName;

            // start the timer
            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += TimerTick;
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Start();

            var myMessageQueue = new SnackbarMessageQueue(TimeSpan.FromMilliseconds(1000));
            MySnackbar.MessageQueue = myMessageQueue;

        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            // load values
            LoadAssetsLabels();
            LoadToggleState();

            // connect
            tglConnect.IsChecked = true;
        }

        public void showSnackBarMessage(string str)
        {
            MySnackbar.MessageQueue.Enqueue(str);
        }

        private void OnRs232Settings(object sender, RoutedEventArgs e)
        {
            SerialSettings dlg = new SerialSettings();
            dlg.Owner = this;
            if (dlg.ShowDialog() == true)
            {
                Debug.WriteLine("Changed the serial port configuration.");
            }
        }

        private void OnPipeSettings(object sender, RoutedEventArgs e)
        {
            PipeSettings dlg = new PipeSettings();
            dlg.Owner = this;
            if (dlg.ShowDialog() == true)
            {
                LoadAssetsLabels();

                Debug.WriteLine("Changed the pipe settings.");
            }
        }

        private void OnExternalVolumeSettings(object sender, RoutedEventArgs e)
        {
            PasswordInput passwordInput = new PasswordInput();
            passwordInput.Owner = this;
            if (passwordInput.ShowDialog() == true)
            {
                Debug.WriteLine("Changed the serial port configuration.");
            }
        }

        private void TglConnect_Checked(object sender, RoutedEventArgs e)
        {
            serial = new ALDSerialPort(SettingsManager.getInstance().CurrentSettings.SerialConfig);
            try
            {
                serial.OpenPort();

                serial.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(serial_DataReceived);
                serial.ErrorReceived += new System.IO.Ports.SerialErrorReceivedEventHandler(serial_ErrorReceived);
            }
            catch
            {
                MessageBox.Show("Cannot open port.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);

                serial = null;
                tglConnect.IsChecked = false;
            }
        }

        private void serial_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            MessageBox.Show(e.EventType.ToString());
        }

        private void serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // accumulate the recevied string
            string strReceived = serial.Serial.ReadExisting();
            string strFull = "";
            if (strReceived.Contains('\r'))
            {
                strFull = strBuffer + strReceived;
                strBuffer = "";
            }
            else
            {
                strBuffer += strReceived;
                return;
            }

            // log to rich textbox
            this.Dispatcher.Invoke(new Action(delegate
            {
                Log(strFull);
            }));


            // extract the command
            string command = strFull.Substring(0, 3);
            string strValue = strFull.Substring(3).TrimStart('+').TrimEnd('\r', '\n');

            this.Dispatcher.Invoke(new Action(delegate
            {
                switch (command)
                {
                    case "@10":
                        txtMaxPressure.Text = strValue;
                        break;
                    case "@11":
                        txtTimeout.Text = strValue;
                        break;
                    case "@14":
                        txtMinPressure.Text = strValue;
                        break;
                    case "@17":
                        txtPurgeCycles.Text = strValue;
                        break;
                    case "@18":
                        txtStabilizationTime.Text = strValue;
                        break;
                    case "@40":
                        txtBasePressure.Text = strValue;
                        break;
                    case "@16":
                        txtStrayTotal.Text = strValue;
                        break;
                    // calculate external volume
                    case "@01":
                        startedExternalVolume();
                        break;
                    case "=01":
                        failedToStartExternalVolume();
                        break;
                    case "@20":
                        if (strValue == "0A")
                            readyExternalVolume();
                        else if (strValue == "00")
                            completedFlowRate();
                        break;
                    case "@04":
                        continueExternalVolume();
                        break;
                    case "@23":
                        reportExternalVolume(strValue);
                        break;
                    // calculate flow rate
                    case "@00":
                        startedFlowRate();
                        break;
                    case "=00":
                        failedToStartFlowRate();
                        break;
                    case "@21":
                        lblFlowValue.Content = strValue;
                        break;
                }
            }));

        }

        private void TglConnect_Unchecked(object sender, RoutedEventArgs e)
        {
            if (serial != null)
            {
                serial.ClosePort();
                serial = null;
            }
        }

        private void TxtAssetExtra_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            // check whether the text is changed by the user
            if (textBox.IsFocused)
            {
                Settings.AssetExtraValue = txtAssetExtra.Text.ToString();

                SumupStrayVolume();
            }
        }

        private void SumupStrayVolume()
        {
            if (txtStrayTotal == null)
                return;

            double totalStray = 0;

            // Asset, CCM
            if (tglAsset01.IsChecked == true)
            {
                totalStray += Convert.ToDouble(lblAsset01Value.Content.ToString());
                stackAsset01.Visibility = Visibility.Visible;
            }
            else
                stackAsset01.Visibility = Visibility.Collapsed;

            if (tglAsset02.IsChecked == true)
            {
                totalStray += Convert.ToDouble(lblAsset02Value.Content.ToString());
                stackAsset02.Visibility = Visibility.Visible;
            }
            else
                stackAsset02.Visibility = Visibility.Collapsed;

            if (tglAsset03.IsChecked == true)
            {
                totalStray += Convert.ToDouble(lblAsset03Value.Content.ToString());
                stackAsset03.Visibility = Visibility.Visible;
            }
            else
                stackAsset03.Visibility = Visibility.Collapsed;

            if (tglAsset04.IsChecked == true)
            {
                totalStray += Convert.ToDouble(lblAsset04Value.Content.ToString());
                stackAsset04.Visibility = Visibility.Visible;
            }
            else
                stackAsset04.Visibility = Visibility.Collapsed;

            // Flow meter,CCM
            if (tglAsset05.IsChecked == true)
            {
                totalStray += Convert.ToDouble(lblAsset05Value.Content.ToString());
                lblAsset567.Content = Settings.Asset05Name.ToString();
            }

            if (tglAsset06.IsChecked == true)
            {
                totalStray += Convert.ToDouble(lblAsset06Value.Content.ToString());
                lblAsset567.Content = Settings.Asset06Name.ToString();
            }

            if (tglAsset07.IsChecked == true)
            {
                totalStray += Convert.ToDouble(lblAsset07Value.Content.ToString());
                lblAsset567.Content = Settings.Asset07Name.ToString();
            }

            // show p5a
            if (tglAsset05.IsChecked == false && tglAsset06.IsChecked == false && tglAsset07.IsChecked == false)
            {
                stackAsset567.Visibility = Visibility.Collapsed;
            }
            else
            {
                stackAsset567.Visibility = Visibility.Visible;
            }

            // extra
            if (tglAssetExtra.IsChecked == true)
            {
                double value = 0;
                double.TryParse(txtAssetExtra.Text, out value);
                totalStray += value;

                stackAssetExtra.Visibility = Visibility.Visible;
            }
            else
            {
                stackAssetExtra.Visibility = Visibility.Collapsed;
            }

            txtStrayTotal.Text = totalStray.ToString();

        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            SumupStrayVolume();
        }

        private bool SendToSerial(string strData)
        {
            if (serial == null || serial.Serial.IsOpen == false)
            {
                showSnackBarMessage("Serial Port is not opened!");
                return false;
            }
            if (!serial.SendData(strData))
            {
                showSnackBarMessage("Can not send to the Serial Port!");
                return false;
            }
            Log($">>{strData}");
            
            return true;
        }

        private void Log(string str)
        {
            richTxtConsole.AppendText($"{DateTime.Now: HH:mm:ss}  : {str.TrimEnd('\r', '\n')} \n");
            richTxtConsole.ScrollToEnd();
        }

        private void BtnReadStray_Click(object sender, RoutedEventArgs e)
        {
            SendToSerial("@16?\r\n");
        }

        private void BtnWriteStray_Click(object sender, RoutedEventArgs e)
        {
            SendToSerial("@16" + ToOneDecimal(txtStrayTotal.Text) + "\r\n");
        }

        private void BtnReadMaxPressure_Click(object sender, RoutedEventArgs e)
        {
            SendToSerial("@10?\r\n");
        }

        private void BtnWriteMaxPressure_Click(object sender, RoutedEventArgs e)
        {
            SendToSerial("@10" + txtMaxPressure.Text + "\r\n");
        }

        private void BtnReadTimeout_Click(object sender, RoutedEventArgs e)
        {
            SendToSerial("@11?\r\n");
        }

        private void BtnWriteTimeout_Click(object sender, RoutedEventArgs e)
        {
            SendToSerial("@11" + txtTimeout.Text + "\r\n");
        }

        private void BtnReadMinPressure_Click(object sender, RoutedEventArgs e)
        {
            SendToSerial("@14?\r\n");
        }

        private void BtnWriteMinPressure_Click(object sender, RoutedEventArgs e)
        {
            SendToSerial("@14" + txtMinPressure.Text + "\r\n");
        }

        private void BtnReadPurgeCycles_Click(object sender, RoutedEventArgs e)
        {
            SendToSerial("@17?\r\n");
        }

        private void BtnWritePurgeCycles_Click(object sender, RoutedEventArgs e)
        {
            SendToSerial("@17" + txtPurgeCycles.Text + "\r\n");
        }

        private void BtnReadStabilizationTime_Click(object sender, RoutedEventArgs e)
        {
            SendToSerial("@18?\r\n");
        }

        private void BtnWriteStabilizationTime_Click(object sender, RoutedEventArgs e)
        {
            SendToSerial("@18" + txtStabilizationTime.Text + "\r\n");
        }

        private void BtnReadBasePressure_Click(object sender, RoutedEventArgs e)
        {
            SendToSerial("@40?\r\n");
        }

        private void BtnWriteBasePressure_Click(object sender, RoutedEventArgs e)
        {
            SendToSerial("@40" + txtBasePressure.Text + "\r\n");
        }

        private string ToOneDecimal(string strValue)
        {
            double value = Convert.ToDouble(strValue);
            value = Math.Round(value, 1);
            string retVal = value.ToString("0.0");
            return retVal;
        }

        private void Toggle_Changed(object sender, RoutedEventArgs e)
        {
            ToggleButton tglSender = sender as ToggleButton;
            if (tglSender == tglAsset05 && tglSender.IsChecked == true)
            {
                tglAsset06.IsChecked = false;
                tglAsset07.IsChecked = false;
            }
            else if (tglSender == tglAsset06 && tglSender.IsChecked == true)
            {
                tglAsset05.IsChecked = false;
                tglAsset07.IsChecked = false;
            }
            else if (tglSender == tglAsset07 && tglSender.IsChecked == true)
            {
                tglAsset05.IsChecked = false;
                tglAsset06.IsChecked = false;
            }

            // save toggle
            SaveToggleState();

            // calculate
            SumupStrayVolume();
        }

		private void BtnCalculateFlow_Click(object sender, RoutedEventArgs e)
        {
            SendToSerial("@40?\r\n");
        }

        private void TimerTick(object sender, EventArgs e)
        {
            if (calcMode == CalcMode.STARTED_EXTERNAL_VOLUME)
            {
                SendToSerial("@20?\r\n");
            }
            else if (calcMode == CalcMode.STARTED_FLOW_RATE)
            {
                SendToSerial("@20?\r\n");
            }
        }

        private void BtnExternalStart_Click(object sender, RoutedEventArgs e)
        {
            PasswordInput passwordInput = new PasswordInput();
            passwordInput.Owner = this;
#if !DEBUG
            if (passwordInput.ShowDialog() != true)
            {
                return;
            }
#endif

            if (calcMode == CalcMode.NONE)
            {
                if (!SendToSerial("@01\r\n"))
                    return;

                lblExternalStatus.Content = "Starting...";
                progressExternal.Visibility = Visibility.Visible;
                btnExternalStart.IsEnabled = false;
            }
            else
            {
                MessageBox.Show("Can not start!", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void startedExternalVolume()
        {
            calcMode = CalcMode.STARTED_EXTERNAL_VOLUME;
            lblExternalStatus.Content = "Monitoring...";
            progressExternal.Visibility = Visibility.Visible;
        }

        private void failedToStartExternalVolume()
        {
            lblExternalStatus.Content = "---";
            progressExternal.Visibility = Visibility.Hidden;
            btnExternalStart.IsEnabled = true;

            MessageBox.Show("Command is inappropriate.\n" +
                            "The GBR3B is currently performing another operation.",
                            "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void readyExternalVolume()
        {
            calcMode = CalcMode.NONE;
            lblExternalStatus.Content = "Waiting to stop";
            btnExternalStop.Visibility = Visibility.Visible;
            progressExternal.Visibility = Visibility.Hidden;
        }

        private void BtnExternalStop_Click(object sender, RoutedEventArgs e)
        {
            lblExternalStatus.Content = "Continue";
            btnExternalStop.Visibility = Visibility.Collapsed;
            btnExternalStart.IsEnabled = true;

            SendToSerial("@04\r\n");
        }

        private void continueExternalVolume()
        {
            SendToSerial("@23?\r\n");
        }

        private void reportExternalVolume(string strValue)
        {
            double value = Convert.ToDouble(strValue);
            if (value < 0)
            {
                lblExternalStatus.Content = "Fail";
                lblExternalValue.Content = "---";
            }
            else
            {
                lblExternalStatus.Content = "Success";
                lblExternalValue.Content = strValue;
            }
        }

        private void BtnFlowStart_Click(object sender, RoutedEventArgs e)
        {
            if (calcMode == CalcMode.NONE)
            {
                if (!SendToSerial("@00\r\n"))
                    return;

                lblFlowStatus.Content = "Starting...";
                progressFlow.Visibility = Visibility.Visible;
                btnFlowStart.IsEnabled = false;
            }
            else
            {
                MessageBox.Show("Can not start!", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void startedFlowRate()
        {
            calcMode = CalcMode.STARTED_FLOW_RATE;
            lblFlowStatus.Content = "Monitoring...";
            progressFlow.Visibility = Visibility.Visible;
        }

        private void completedFlowRate()
        {
            calcMode = CalcMode.NONE;
            lblFlowStatus.Content = "Success";
            progressFlow.Visibility = Visibility.Hidden;
            btnFlowStart.IsEnabled = true;

            SendToSerial("@21?\r\n");                       // ask the flow rate
        }

        private void failedToStartFlowRate()
        {
            lblFlowStatus.Content = "---";
            progressFlow.Visibility = Visibility.Hidden;
            btnFlowStart.IsEnabled = true;

            MessageBox.Show("Command is inappropriate.\n" +
                            "The GBR3B is currently performing another operation or the external volume has not been calculated yet.",
                            "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void BtnContinue_Click(object sender, RoutedEventArgs e)
        {
            SendToSerial("@04\r\n");
        }

        private void BtnAbort_Click(object sender, RoutedEventArgs e)
        {
            SendToSerial("@05\r\n");

            calcMode = CalcMode.NONE;

            lblExternalStatus.Content = "---";
            btnExternalStop.Visibility = Visibility.Collapsed;
            btnExternalStart.IsEnabled = true;
            progressExternal.Visibility = Visibility.Hidden;

            lblFlowStatus.Content = "---";
            btnFlowStart.IsEnabled = true;
            progressFlow.Visibility = Visibility.Hidden;
        }

        // load asset labels
        private void LoadAssetsLabels()
        {
            // asset names
            lblAsset01Name.Content = Settings.Asset01Name;
            lblAsset02Name.Content = Settings.Asset02Name;
            lblAsset03Name.Content = Settings.Asset03Name;
            lblAsset04Name.Content = Settings.Asset04Name;
            lblAsset05Name.Content = Settings.Asset05Name;
            lblAsset06Name.Content = Settings.Asset06Name;
            lblAsset07Name.Content = Settings.Asset07Name;

            // asset values
            lblAsset01Value.Content = Settings.Asset01Value.ToString();
            lblAsset02Value.Content = Settings.Asset02Value.ToString();
            lblAsset03Value.Content = Settings.Asset03Value.ToString();
            lblAsset04Value.Content = Settings.Asset04Value.ToString();
            lblAsset05Value.Content = Settings.Asset05Value.ToString();
            lblAsset06Value.Content = Settings.Asset06Value.ToString();
            lblAsset07Value.Content = Settings.Asset07Value.ToString();

            // pipe values
            lblAsset01Pipe.Content = Settings.Asset01Name;
            lblAsset02Pipe.Content = Settings.Asset02Name;
            lblAsset03Pipe.Content = Settings.Asset03Name;
            lblAsset04Pipe.Content = Settings.Asset04Name;

            SumupStrayVolume();
        }

        // save toggle state
        private void SaveToggleState()
        {
            // asset toggle
            Settings.Asset01Toggle = tglAsset01.IsChecked == true;
            Settings.Asset02Toggle = tglAsset02.IsChecked == true;
            Settings.Asset03Toggle = tglAsset03.IsChecked == true;
            Settings.Asset04Toggle = tglAsset04.IsChecked == true;
            Settings.Asset05Toggle = tglAsset05.IsChecked == true;
            Settings.Asset06Toggle = tglAsset06.IsChecked == true;
            Settings.Asset07Toggle = tglAsset07.IsChecked == true;
            Settings.AssetExtraToggle = tglAssetExtra.IsChecked == true;
        }

        // load toggle state
        private void LoadToggleState()
        {
            // asset toggle
            tglAsset01.IsChecked = Settings.Asset01Toggle;
            tglAsset02.IsChecked = Settings.Asset02Toggle;
            tglAsset03.IsChecked = Settings.Asset03Toggle;
            tglAsset04.IsChecked = Settings.Asset04Toggle;
            tglAsset05.IsChecked = Settings.Asset05Toggle;
            tglAsset06.IsChecked = Settings.Asset06Toggle;
            tglAsset07.IsChecked = Settings.Asset07Toggle;
            tglAssetExtra.IsChecked = Settings.AssetExtraToggle;

            // asset extra value
            txtAssetExtra.Text = Settings.AssetExtraValue;
        }

        private void TextBox_DecimalOnly(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9]*(?:\.[0-9]*)?$");
        }

        private void TextBox_NumberOnly(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]*$");
        }
    }
}
