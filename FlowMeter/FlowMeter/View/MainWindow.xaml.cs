using Ald.SerialPort.Configuration;
using FlowMeter.Configuration;
using FlowMeter.View;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
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

        public void showSnackBarMessage(string str)
        {
            MySnackbar.MessageQueue.Enqueue(str);
        }

        private void OnRs232Settings(object sender, RoutedEventArgs e)
        {
            SerialSettings serialSettings = new SerialSettings();
            serialSettings.Owner = this;
            if (serialSettings.ShowDialog() == true)
            {
                Debug.WriteLine("Changed the serial port configuration.");
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
                this.richTxtConsole.AppendText(strFull);
                this.richTxtConsole.ScrollToEnd();
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

        private void TxtStrayExtra_TextChanged(object sender, TextChangedEventArgs e)
        {
            SumupStrayVolume();

        }

        private void SumupStrayVolume()
        {
            if (txtStrayTotal == null)
                return;

            double totalStray = 0;

            // Asset, CCM
            if (tgl101.IsChecked == true)
            {
                totalStray += Convert.ToDouble(lblAsset101.Content.ToString());
                stackAsset101.Visibility = Visibility.Visible;
            }
            else
                stackAsset101.Visibility = Visibility.Collapsed;

            if (tgl112.IsChecked == true)
            {
                totalStray += Convert.ToDouble(lblAsset112.Content.ToString());
                stackAsset112.Visibility = Visibility.Visible;
            }
            else
                stackAsset112.Visibility = Visibility.Collapsed;

            if (tgl136.IsChecked == true)
            {
                totalStray += Convert.ToDouble(lblAsset136.Content.ToString());
                stackAsset136.Visibility = Visibility.Visible;
            }
            else
                stackAsset136.Visibility = Visibility.Collapsed;

            if (tgl124.IsChecked == true)
            {
                totalStray += Convert.ToDouble(lblAsset124.Content.ToString());
                stackAsset124.Visibility = Visibility.Visible;
            }
            else
                stackAsset124.Visibility = Visibility.Collapsed;

            // Flow meter,CCM
            if (tglP5a.IsChecked == true)
            {
                totalStray += Convert.ToDouble(lblP5a.Content.ToString());
                lblPipeP5a.Content = "P5a";
            }

            if (tglP6a.IsChecked == true)
            {
                totalStray += Convert.ToDouble(lblP6a.Content.ToString());
                lblPipeP5a.Content = "P6a";
            }

            if (tgl1157.IsChecked == true)
            {
                totalStray += Convert.ToDouble(lbl1157.Content.ToString());
                lblPipeP5a.Content = "1157";
            }

            if (tglStrayExtra.IsChecked == true)
            {
                totalStray += Convert.ToDouble(txtStrayExtra.Text);

                stackExtra.Visibility = Visibility.Visible;
            }
            else
            {
                stackExtra.Visibility = Visibility.Collapsed;
            }

            txtStrayTotal.Text = totalStray.ToString();

        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            SumupStrayVolume();
        }

        private bool sendToSerial(string strData)
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

            string append;
            append = ">> ";
            append += strData;
            richTxtConsole.AppendText(append);
			return true;
        }

        private void BtnReadStray_Click(object sender, RoutedEventArgs e)
        {
            sendToSerial("@16?\r\n");
        }

        private void BtnWriteStray_Click(object sender, RoutedEventArgs e)
        {
            sendToSerial("@16" + ToOneDecimal(txtStrayTotal.Text) + "\r\n");
        }

        private void BtnReadMaxPressure_Click(object sender, RoutedEventArgs e)
        {
            sendToSerial("@10?\r\n");
        }

        private void BtnWriteMaxPressure_Click(object sender, RoutedEventArgs e)
        {
            sendToSerial("@10" + txtMaxPressure.Text + "\r\n");
        }

        private void BtnReadTimeout_Click(object sender, RoutedEventArgs e)
        {
            sendToSerial("@11?\r\n");
        }

        private void BtnWriteTimeout_Click(object sender, RoutedEventArgs e)
        {
            sendToSerial("@11" + txtTimeout.Text + "\r\n");
        }

        private void BtnReadMinPressure_Click(object sender, RoutedEventArgs e)
        {
            sendToSerial("@14?\r\n");
        }

        private void BtnWriteMinPressure_Click(object sender, RoutedEventArgs e)
        {
            sendToSerial("@14" + txtMinPressure.Text + "\r\n");
        }

        private void BtnReadPurgeCycles_Click(object sender, RoutedEventArgs e)
        {
            sendToSerial("@17?\r\n");
        }

        private void BtnWritePurgeCycles_Click(object sender, RoutedEventArgs e)
        {
            sendToSerial("@17" + txtPurgeCycles.Text + "\r\n");
        }

        private void BtnReadStabilizationTime_Click(object sender, RoutedEventArgs e)
        {
            sendToSerial("@18?\r\n");
        }

        private void BtnWriteStabilizationTime_Click(object sender, RoutedEventArgs e)
        {
            sendToSerial("@18" + txtStabilizationTime.Text + "\r\n");
        }

        private void BtnReadBasePressure_Click(object sender, RoutedEventArgs e)
        {
            sendToSerial("@40?\r\n");
        }

        private void BtnWriteBasePressure_Click(object sender, RoutedEventArgs e)
        {
            sendToSerial("@40" + txtBasePressure.Text + "\r\n");
        }

        private string ToOneDecimal(string strValue)
        {
            double value = Convert.ToDouble(strValue);
            value = Math.Round(value, 1);
            string retVal = value.ToString("0.0");
            return retVal;
        }

        private void TglP5a_Checked(object sender, RoutedEventArgs e)
        {
            if (tglP6a != null)
                tglP6a.IsChecked = false;
            if (tgl1157 != null)
                tgl1157.IsChecked = false;

            SumupStrayVolume();
        }

        private void TglP5a_Unchecked(object sender, RoutedEventArgs e)
        {
            SumupStrayVolume();
        }

        private void TglP6a_Checked(object sender, RoutedEventArgs e)
        {
            if (tglP5a != null)
                tglP5a.IsChecked = false;
            if (tgl1157 != null)
                tgl1157.IsChecked = false;

            SumupStrayVolume();
        }

        private void TglP6a_Unchecked(object sender, RoutedEventArgs e)
        {
            SumupStrayVolume();
        }

        private void Tgl1157_Checked(object sender, RoutedEventArgs e)
        {
            if (tglP5a != null)
                tglP5a.IsChecked = false;
            if (tglP6a != null)
                tglP6a.IsChecked = false;

            SumupStrayVolume();
        }

        private void Tgl1157_Unchecked(object sender, RoutedEventArgs e)
        {
            SumupStrayVolume();
        }

        private void Toggle_Changed(object sender, RoutedEventArgs e)
        {
            SumupStrayVolume();
        }
		private void BtnCalculateFlow_Click(object sender, RoutedEventArgs e)
        {
            sendToSerial("@40?\r\n");
        }

        


        private void TimerTick(object sender, EventArgs e)
        {
            if (calcMode == CalcMode.STARTED_EXTERNAL_VOLUME)
            {
                sendToSerial("@20?\r\n");
            }
            else if (calcMode == CalcMode.STARTED_FLOW_RATE)
            {
                sendToSerial("@20?\r\n");
            }
        }

        private void BtnExternalStart_Click(object sender, RoutedEventArgs e)
        {
            PasswordInput passwordInput = new PasswordInput();
            passwordInput.Owner = this;
            if (passwordInput.ShowDialog() != true)
            {
                return;
            }

            if (calcMode == CalcMode.NONE)
            {
                if (!sendToSerial("@01\r\n"))
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

            sendToSerial("@04\r\n");
        }

        private void continueExternalVolume()
        {
            sendToSerial("@23?\r\n");
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
                if (!sendToSerial("@00\r\n"))
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

            sendToSerial("@21?\r\n");                       // ask the flow rate
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

        private void BtnContinueGbror_Click(object sender, RoutedEventArgs e)
        {
            sendToSerial("@04\r\n");
        }

        private void BtnAbortGbror_Click(object sender, RoutedEventArgs e)
        {
            sendToSerial("@05\r\n");
        }
    }
}
