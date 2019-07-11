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

            // load values
            LoadAssetsLabels();
            LoadToggleState();

        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // connect
            tglConnect.IsChecked = true;

            // read initial values
            ReadInitialValues();
        }

        private void ReadInitialValues()
        {
            int readingInterval = 200;

            bool isReading = true;
            if (!CheckSerialPort())
                isReading = false;

            if (isReading)
                _notifier.ShowInformation("Start reading values...");

            Task.Factory
                // 1
                .StartNew(() => Thread.Sleep(readingInterval))
                .ContinueWith((t) =>
                {
                    if (!CheckSerialPort())
                        isReading = false;
                    if (isReading)
                        SendToSerial("@10?\r\n");
                }, TaskScheduler.FromCurrentSynchronizationContext())
                // 2
                .ContinueWith((t) => Thread.Sleep(readingInterval))
                .ContinueWith((t) =>
                {
                    if (!CheckSerialPort())
                        isReading = false;
                    if (isReading)
                        SendToSerial("@11?\r\n");
                }, TaskScheduler.FromCurrentSynchronizationContext())
                // 3
                .ContinueWith((t) => Thread.Sleep(readingInterval))
                .ContinueWith((t) =>
                {
                    if (!CheckSerialPort())
                        isReading = false;
                    if (isReading)
                        SendToSerial("@14?\r\n");
                }, TaskScheduler.FromCurrentSynchronizationContext())
                // 4
                .ContinueWith((t) => Thread.Sleep(readingInterval))
                .ContinueWith((t) =>
                {
                    if (!CheckSerialPort())
                        isReading = false;
                    if (isReading)
                        SendToSerial("@17?\r\n");
                }, TaskScheduler.FromCurrentSynchronizationContext())
                // 5
                .ContinueWith((t) => Thread.Sleep(readingInterval))
                .ContinueWith((t) =>
                {
                    if (!CheckSerialPort())
                        isReading = false;
                    if (isReading)
                        SendToSerial("@18?\r\n");
                }, TaskScheduler.FromCurrentSynchronizationContext())
                // 6
                .ContinueWith((t) => Thread.Sleep(readingInterval))
                .ContinueWith((t) =>
                {
                    if (!CheckSerialPort())
                        isReading = false;
                    if (isReading)
                        SendToSerial("@40?\r\n");
                }, TaskScheduler.FromCurrentSynchronizationContext())
                // 7 : read external volume
                .ContinueWith((t) => Thread.Sleep(readingInterval))
                .ContinueWith((t) =>
                {
                    if (!CheckSerialPort())
                        isReading = false;
                    if (isReading)
                        SendToSerial("@23?\r\n");
                }, TaskScheduler.FromCurrentSynchronizationContext())
                // show finish toast
                .ContinueWith((t) => Thread.Sleep(readingInterval))
                .ContinueWith((t) =>
                {
                    if (!CheckSerialPort())
                        isReading = false;
                    if (isReading)
                        _notifier.ShowInformation("Finished reading values.");
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void OnRs232Settings(object sender, RoutedEventArgs e)
        {
            SerialSettings dlg = new SerialSettings();
            dlg.Owner = this;
            if (dlg.ShowDialog() == true)
            {
                lblPortName.Content = SettingsManager.getInstance().CurrentSettings.SerialConfig.PortName;
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
            var config = SettingsManager.getInstance().CurrentSettings.SerialConfig;
            serial = new ALDSerialPort(config);
            try
            {
                serial.OpenPort();

                serial.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(serial_DataReceived);
                serial.ErrorReceived += new System.IO.Ports.SerialErrorReceivedEventHandler(serial_ErrorReceived);

                _notifier.ShowSuccess($"Successfully connected to the serial port : {config.PortName}.");
            }
            catch
            {
                _notifier.ShowError("Can not open the serial port.");

                Close_SerialPort();
            }
        }

        private void TglConnect_Unchecked(object sender, RoutedEventArgs e)
        {
            Close_SerialPort();

            var tglSender = sender as ToggleButton;
            if (tglSender.IsFocused)
                _notifier.ShowWarning("Closed the the Serial Port.");
        }

        private void Close_SerialPort()
        {
            if (serial != null)
            {
                serial.ClosePort();
                serial = null;
            }
            tglConnect.IsChecked = false;
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

            // splite the received string
            string[] commandList = strFull.Split('\r');
            foreach (string command in commandList)
            {
                string trimmed = command.Trim('\r', '\n');

                if (string.IsNullOrEmpty(trimmed))
                    continue;

                // Execute one command
                ExecuteOneCommand(trimmed);
            }            
        }

        private void ExecuteOneCommand(string strOneCommand)
        {
            // log to rich textbox
            this.Dispatcher.Invoke(new Action(delegate
            {
                Log(strOneCommand);
            }));

            // extract the command
            string command = strOneCommand.Substring(0, 3);
            string strValue = strOneCommand.Substring(3);     // .TrimStart('+'); .TrimEnd('\r', '\n');

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
            if (!CheckSerialPort())
            {
                _notifier.ShowWarning("Serial Port is not opened.");
                return false;
            }   

            if (!serial.SendData(strData))
            {
                _notifier.ShowWarning("Can not send to the Serial Port.");

                Close_SerialPort();

                return false;
            }
            Log($">>{strData}");
            
            return true;
        }

        private bool CheckSerialPort()
        {
            if (serial != null && serial.Serial.IsOpen == true)
            {
                return true;
            }
            else
            {
                serial = null;
                tglConnect.IsChecked = false;
                return false;
            }
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
            if (!CheckSerialPort())
            {
                _notifier.ShowWarning("Serial Port is not opened.");
                return;
            }

            if (calcMode == CalcMode.NONE)
            {
                Double flowVerificationTime = double.Parse(txtTimeout.Text);
                if (flowVerificationTime < 600)
                {
                    System.Windows.Style style = new System.Windows.Style();
                    style.Setters.Add(new Setter(Xceed.Wpf.Toolkit.MessageBox.YesButtonContentProperty, "Continue"));
                    style.Setters.Add(new Setter(Xceed.Wpf.Toolkit.MessageBox.NoButtonContentProperty, "Abort"));
                    MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show("The flow verification time might be too low, recommend 600 seconds.", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Yes, style);
                    if (result == MessageBoxResult.No)
                    {
                        if (SendToSerial("@20?\r\n"))
                            _notifier.ShowInformation("Sent 'Report Status'(@20?) command.");
                        return;
                    }
                }

                PasswordInput passwordInput = new PasswordInput();
                passwordInput.Owner = this;
#if !DEBUG
            if (passwordInput.ShowDialog() != true)
            {
                return;
            }
#endif
                calcMode = CalcMode.WAITING_ACCEPTION_01;
                lblExternalStatus.Content = "Waiting...";
                lblExternalValue.Content = "---";
                progressExternal.Visibility = Visibility.Visible;
                btnExternalStart.IsEnabled = false;

                if (SendToSerial("@01\r\n"))
                    _notifier.ShowInformation("Sent 'Calculate Volume'(@01) command.");
                {
                    lblExternalStatus.Content = "---";
                    progressExternal.Visibility = Visibility.Hidden;
                    btnExternalStart.IsEnabled = true;
                }
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
                    _notifier.ShowSuccess($"Received 'External Volume Status' = 0x{status}.");

                    calcMode = CalcMode.WAITING_STOP_FLOW;

                    lblExternalStatus.Content = "Please stop the gas flow.";
                    btnExternalStop.Visibility = Visibility.Visible;
                    progressExternal.Visibility = Visibility.Hidden;
                }
            }
            // after external volume, require the report status
            else if (calcMode == CalcMode.WAITING_REPORT_STATUS_20)
            {
                _notifier.ShowSuccess("Received 'Report Status' = 0x{status}.");
                lblExternalStatus.Content = "After 2 secs, 'Report Volume'";

                Task.Factory.StartNew(() => Thread.Sleep(2000))
                    .ContinueWith((t) =>
                    {
                        calcMode = CalcMode.WAITING_REPORT_VOLUME_23;
                        lblExternalStatus.Content = "Waiting report volume...";

                        // send report volume
                        if (SendToSerial("@23?\r\n"))
                            _notifier.ShowInformation("Sent 'Report Volume'(@23?) command.");

                    }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            // flow rate status
            else if (calcMode == CalcMode.WAITING_FLOW_VERIFICATION_2000)
            {
                // if ready
                if (status == "00")
                {
                    _notifier.ShowSuccess($"Received 'Flow Rate Status' = 0x{status}.");
                    lblFlowStatus.Content = "After 2 secs, 'Report Flow'";

                    Task.Factory.StartNew(() => Thread.Sleep(2000))
                    .ContinueWith((t) =>
                    {
                        calcMode = CalcMode.WAITING_REPORT_FLOW_21;
                        lblFlowStatus.Content = "Waiting report flow...";

                        // send report flow
                        if (SendToSerial("@21?\r\n"))
                            _notifier.ShowInformation("Sent 'Report Flow'(@21?) command.");

                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
            }
            else
            {
                _notifier.ShowSuccess($"Received 'Report Status' = 0x{status}.");
            }

            // parse and show status to the UI
            ParseStatus(status);
        }

        private readonly string[] statusOperatingMode = { "Idle", "Verify flow", "Calculate volume", "Reserved", "Purge", "Setup", "Isolated Leak Check", "Connected Leak Check" };
        private readonly string[] statusCurrentOperation = { "Busy", "Waiting for Flow Off", "Busy", "Waiting for Flow On" };
        private readonly string[] statusLastOperation = { "No error", "Valve not responding", "Unstable pressure", "Pressure not rising", "Pressure not falling", "Flow not stable", "Operation aborted", "Volume unknown" };

        private void ParseStatus(string strStatus)
        {
            // convert to integer
            int status = Convert.ToInt32(strStatus, 16);
            if (status < 0 || status > 255)
            {
                _notifier.ShowError($"Invalid Status : {status}");
                return;
            }

            // convert to bits
            int D0, D1, D2, D3, D4, D5, D6, D7;
            D0 = status & 0x01;
            D1 = (status >> 1) & 0x01;
            D2 = (status >> 2) & 0x01;
            D3 = (status >> 3) & 0x01;
            D4 = (status >> 4) & 0x01;
            D5 = (status >> 5) & 0x01;
            D6 = (status >> 6) & 0x01;
            D7 = (status >> 7) & 0x01;

            // display to the status table
            lblD0.Content = D0.ToString();
            lblD1.Content = D1.ToString();
            lblD2.Content = D2.ToString();
            lblD3.Content = D3.ToString();
            lblD4.Content = D4.ToString();
            lblD5.Content = D5.ToString();
            lblD6.Content = D6.ToString();
            lblD7.Content = D7.ToString();

            // display status message
            lblStatusOperatingMode.Content = statusOperatingMode[D2 * 4 + D1 * 2 + D0];
            lblStatusCurrentOperation.Content = statusCurrentOperation[D7 * 2 + D3];
            lblStatusLastOperation.Content = statusLastOperation[D6 * 4 + D5 * 2 + D4];
        }

        private void BtnExternalStop_Click(object sender, RoutedEventArgs e)
        {
            if (calcMode == CalcMode.WAITING_STOP_FLOW)
            {
                // send @04
                if (SendToSerial("@04\r\n"))
                    _notifier.ShowInformation("Sent 'Continue'(@04) command.");

                // send @20? after 7 seconds
                lblExternalStatus.Content = "After 7 secs, 'Report Status'";
                Task.Factory.StartNew(() => Thread.Sleep(7000))
                    .ContinueWith((t) =>
                    {
                        calcMode = CalcMode.WAITING_REPORT_STATUS_20;
                        lblExternalStatus.Content = "Waiting report status...";

                        if (SendToSerial("@20?\r\n"))
                            _notifier.ShowInformation("Sent 'Report Status'(@20?) command.");
                        
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
            //if (calcMode == CalcMode.WAITING_REPORT_VOLUME_23)
            //{
                double value = -1;
                double.TryParse(strValue, out value);

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
            //}      
        }

        private void BtnFlowRate_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckSerialPort())
            {
                _notifier.ShowWarning("Serial Port is not opened.");
                return;
            }

            if (calcMode == CalcMode.NONE)
            {
                Double flowVerificationTime = double.Parse(txtTimeout.Text);
                if (flowVerificationTime > 60)
                {
                    MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show("The flow verification is over 60 seconds.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                calcMode = CalcMode.WAITING_ACCEPTION_00;
                lblFlowStatus.Content = "Waiting acception...";
                lblFlowValue.Content = "---";
                progressFlow.Visibility = Visibility.Visible;
                btnFlowStart.IsEnabled = false;

                if (SendToSerial("@00\r\n"))
                    _notifier.ShowInformation("Sent 'Flow Rate'(@00) command.");
                else
                {
                    calcMode = CalcMode.NONE;
                    lblFlowStatus.Content = "---";
                    progressFlow.Visibility = Visibility.Hidden;
                    btnFlowStart.IsEnabled = true;
                }
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
                    calcMode = CalcMode.WAITING_REPORT_VARIATION_22;
                    lblFlowStatus.Content = "Waiting report variation...";

                    // send report flow
                    if (SendToSerial("@22?\r\n"))
                        _notifier.ShowInformation("Sent 'Report Variation'(@22?) command.");

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
            if (SendToSerial("@04\r\n"))
                _notifier.ShowInformation("Sent 'Continue'(@04) command.");
        }

        private void BtnAbort_Click(object sender, RoutedEventArgs e)
        {
            calcMode = CalcMode.NONE;

            lblExternalStatus.Content = "---";
            btnExternalStop.Visibility = Visibility.Collapsed;
            btnExternalStart.IsEnabled = true;
            progressExternal.Visibility = Visibility.Hidden;

            lblFlowStatus.Content = "---";
            btnFlowStart.IsEnabled = true;
            progressFlow.Visibility = Visibility.Hidden;

            if (SendToSerial("@05\r\n"))
                _notifier.ShowInformation("Sent 'Abort'(@05) command.");
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
