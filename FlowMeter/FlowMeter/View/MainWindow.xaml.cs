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
using System.Threading;
using System.Threading.Tasks;
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

using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace FlowMeter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Notifier _notifier;

        private ALDSerialPort serial;

        private string strBuffer = "";

        private enum CalcMode
        {
            NONE,

            // external volumne
            WAITING_ACCEPTION_01,
            WAITING_READY_TO_STOP_FLOW_200A,
            WAITING_STOP_FLOW,
            WAITING_REPORT_STATUS_20,
            WAITING_REPORT_VOLUME_23,

            // flow rate
            WAITING_ACCEPTION_00,
            WAITING_FLOW_VERIFICATION_2000,
            WAITING_REPORT_FLOW_21,
            WAITING_REPORT_VARIATION_22,
        }
        private CalcMode calcMode = CalcMode.NONE;

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

        public MainWindow()
        {
            InitializeComponent();

            // initialize values of controls
            lblPortName.Content = SettingsManager.getInstance().CurrentSettings.SerialConfig.PortName;

            // start the timer
            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += Timer_Tick;
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Start();

            // toast notifier
            _notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new WindowPositionProvider(
                    parentWindow: Application.Current.MainWindow,
                    corner: Corner.TopLeft,
                    offsetX: 5,
                    offsetY: 0);

                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(5),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(6));

                cfg.Dispatcher = Application.Current.Dispatcher;

                cfg.DisplayOptions.TopMost = false;
                cfg.DisplayOptions.Width = 250;
            });

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
                        ExternalVolume_Response_01();
                        break;
                    case "=01":
                        ExternalVolume_Failed();
                        break;
                    case "@20":
                        Response_ReportStatus_20(strValue);
                        break;
                    case "@04":
                        Response_Continue_04();
                        break;
                    case "@23":
                        Response_Report_23(strValue);
                        break;
                    // calculate flow rate
                    case "@00":
                        FlowRate_Response_00();
                        break;
                    case "=00":
                        FlowRate_Failed();
                        break;
                    case "@21":
                        ReportFlow_21(strValue);
                        break;
                    case "@22":
                        ReportVariation_22(strValue);
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
                _notifier.ShowWarning("Serial Port is not opened.");
                return false;
            }
            if (!serial.SendData(strData))
            {
                _notifier.ShowWarning("Can not send to the Serial Port.");
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

        private void BtnExternalVolume_Click(object sender, RoutedEventArgs e)
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

                _notifier.ShowInformation("Sent 'Calculate Volume'(@01) command.");

                calcMode = CalcMode.WAITING_ACCEPTION_01;

                lblExternalStatus.Content = "Waiting acception...";
                progressExternal.Visibility = Visibility.Visible;
                btnExternalStart.IsEnabled = false;
            }
        }

        private void ExternalVolume_Response_01()
        {
            if (calcMode == CalcMode.WAITING_ACCEPTION_01)
            {
                _notifier.ShowSuccess($"Accepted 'Calculate Volume'(@01) command.");

                calcMode = CalcMode.WAITING_READY_TO_STOP_FLOW_200A;
                // sending command '@20?' is doing by timer.

                lblExternalStatus.Content = "Waiting ready to stop flow...";
                progressExternal.Visibility = Visibility.Visible;
            }
        }

        private void ExternalVolume_Failed()
        {
            calcMode = CalcMode.NONE;

            lblExternalStatus.Content = "---";
            progressExternal.Visibility = Visibility.Hidden;
            btnExternalStart.IsEnabled = true;

            _notifier.ShowError("External Volume Failed!");
        }


        private void Response_ReportStatus_20(string status)
        {
            // external volumne status
            if (calcMode == CalcMode.WAITING_READY_TO_STOP_FLOW_200A)
            {
                // ready status
                if (status == "0A")
                {
                    _notifier.ShowSuccess($"Received 'External Volume Status'={status}.");

                    calcMode = CalcMode.WAITING_STOP_FLOW;

                    lblExternalStatus.Content = "Please stop the gas flow.";
                    btnExternalStop.Visibility = Visibility.Visible;
                    progressExternal.Visibility = Visibility.Hidden;
                }
            }
            // after external volume, require the report status
            else if (calcMode == CalcMode.WAITING_REPORT_STATUS_20)
            {
                _notifier.ShowSuccess("Received 'Report Status'.");
                lblExternalStatus.Content = "After 2 secs, 'Report Volume'";

                Task.Factory.StartNew(() => Thread.Sleep(2000))
                    .ContinueWith((t) =>
                    {
                        // send report volume
                        SendToSerial("@23\r\n");
                        _notifier.ShowInformation("Sent 'Report Volume'(@23) command.");

                        // calc mode
                        calcMode = CalcMode.WAITING_REPORT_VOLUME_23;
                        lblExternalStatus.Content = "Waiting report volume...";

                    }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            // flow rate status
            else if (calcMode == CalcMode.WAITING_FLOW_VERIFICATION_2000)
            {
                // if ready
                if (status == "00")
                {
                    _notifier.ShowSuccess($"Received 'Flow Rate Status'={status}!");
                    lblFlowStatus.Content = "After 2 secs, 'Report Flow'";

                    Task.Factory.StartNew(() => Thread.Sleep(2000))
                    .ContinueWith((t) =>
                    {
                        // send report flow
                        SendToSerial("@21?\r\n");
                        _notifier.ShowInformation("Sent 'Report Flow'(@21?) command.");

                        // calc mode
                        calcMode = CalcMode.WAITING_REPORT_FLOW_21;
                        lblFlowStatus.Content = "Waiting report flow...";

                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
            }


            // TODO: show status to the UI
        }

        private void BtnExternalStop_Click(object sender, RoutedEventArgs e)
        {
            if (calcMode == CalcMode.WAITING_STOP_FLOW)
            {
                // send @04
                SendToSerial("@04\r\n");
                _notifier.ShowInformation("Sent 'Continue'(@04) command.");
                lblExternalStatus.Content = "After 7 secs, 'Report Status'";

                // send @20 after 7 seconds
                Task.Factory.StartNew(() => Thread.Sleep(7000))
                    .ContinueWith((t) =>
                    {
                        SendToSerial("@20?\r\n");
                        _notifier.ShowInformation("Sent 'Report Status'(@20?) command.");
                        lblExternalStatus.Content = "Waiting report status...";
                        
                        // calc mode
                        calcMode = CalcMode.WAITING_REPORT_STATUS_20;

                    }, TaskScheduler.FromCurrentSynchronizationContext());

                progressExternal.Visibility = Visibility.Visible;
                btnExternalStop.Visibility = Visibility.Collapsed;
            }
        }

        private void Response_Continue_04()
        {

        }

        private void Response_Report_23(string strValue)
        {
            if (calcMode == CalcMode.WAITING_REPORT_VOLUME_23)
            {
                double value = Convert.ToDouble(strValue);
                if (value < 0)
                {
                    _notifier.ShowWarning($"Invalid 'Report Flow'={strValue}");

                    lblExternalStatus.Content = "Fail";
                    lblExternalValue.Content = "---";
                }
                else
                {
                    _notifier.ShowSuccess($"Received 'Report Flow'={strValue}");

                    lblExternalStatus.Content = "Success";
                    lblExternalValue.Content = strValue;
                }

                // calc mode
                calcMode = CalcMode.NONE;
                progressExternal.Visibility = Visibility.Hidden;
                btnExternalStart.IsEnabled = true;
            }      
        }

        private void BtnFlowRate_Click(object sender, RoutedEventArgs e)
        {
            if (calcMode == CalcMode.NONE)
            {
                if (!SendToSerial("@00\r\n"))
                    return;

                _notifier.ShowInformation("Sent 'Flow Rate'(@00) command.");

                calcMode = CalcMode.WAITING_ACCEPTION_00;

                lblFlowStatus.Content = "Waiting acception...";
                progressFlow.Visibility = Visibility.Visible;
                btnFlowStart.IsEnabled = false;
            }
        }

        private void FlowRate_Response_00()
        {
            if (calcMode == CalcMode.WAITING_ACCEPTION_00)
            {
                _notifier.ShowSuccess($"Accepted 'Flow Rate'(@00) command.");

                calcMode = CalcMode.WAITING_FLOW_VERIFICATION_2000;
                // sending command '@20?' is doing by timer.

                lblFlowStatus.Content = "Waiting flow verification...";
                progressFlow.Visibility = Visibility.Visible;
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (calcMode == CalcMode.WAITING_READY_TO_STOP_FLOW_200A)
            {
                SendToSerial("@20?\r\n");
            }
            else if (calcMode == CalcMode.WAITING_FLOW_VERIFICATION_2000)
            {
                SendToSerial("@20?\r\n");
            }
        }

        private void FlowRate_Failed()
        {
            lblFlowStatus.Content = "---";
            progressFlow.Visibility = Visibility.Hidden;
            btnFlowStart.IsEnabled = true;

            _notifier.ShowError("Flow Rate Failed!");
        }

        private void ReportFlow_21(string strValue)
        {
            if (calcMode == CalcMode.WAITING_REPORT_FLOW_21)
            {
                _notifier.ShowSuccess($"Received 'Report Flow'={strValue}");
                lblFlowValue.Content = strValue;

                lblFlowStatus.Content = "After 2 secs, 'Report Variation'"; 

                Task.Factory.StartNew(() => Thread.Sleep(2000))
                .ContinueWith((t) =>
                {
                    // send report flow
                    SendToSerial("@22?\r\n");
                    _notifier.ShowInformation("Sent 'Report Variation'(@22?) command.");

                    // calc mode
                    calcMode = CalcMode.WAITING_REPORT_VARIATION_22;
                    lblFlowStatus.Content = "Waiting report variation...";

                }, TaskScheduler.FromCurrentSynchronizationContext());
            }            
        }

        private void ReportVariation_22(string strValue)
        {
            if (calcMode == CalcMode.WAITING_REPORT_VARIATION_22)
            {
                double value = Convert.ToDouble(strValue);
                if (value < 0)
                {
                    _notifier.ShowWarning($"Invalid 'Report Variation'={strValue}");

                    lblFlowStatus.Content = "Fail";
                    lblFlowVariation.Content = "---";
                }
                else
                {
                    _notifier.ShowSuccess($"Received 'Report Variation'={strValue}");

                    lblFlowStatus.Content = "Success";
                    lblFlowVariation.Content = strValue;
                }

                // finish
                calcMode = CalcMode.NONE;

                progressFlow.Visibility = Visibility.Hidden;
                btnFlowStart.IsEnabled = true;
            }
        }

        private void BtnContinue_Click(object sender, RoutedEventArgs e)
        {
            SendToSerial("@04\r\n");

            _notifier.ShowSuccess("Sent 'Continue'(@04) command.");
        }

        private void BtnAbort_Click(object sender, RoutedEventArgs e)
        {
            SendToSerial("@05\r\n");

            _notifier.ShowWarning("Sent 'Abort'(@05) command.");

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
    }
}
