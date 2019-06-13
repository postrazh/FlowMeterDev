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

namespace FlowMeter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ALDSerialPort serial;

        public MainWindow()
        {
            InitializeComponent();

            lblPortName.Content = SettingsManager.getInstance().CurrentSettings.SerialConfig.PortName;

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
            string strReceived = serial.Serial.ReadExisting();

            this.Dispatcher.Invoke(new Action(delegate
            {
                if (serial.LastSent.Substring(0, 3) == "@10")
                {
                    txtMaxPressure.Text = strReceived.Substring(3);
                }

                this.richTxtConsole.AppendText(strReceived); // "\n" + 
                this.richTxtConsole.ScrollToEnd();
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

        private void BtnCalculateExternal_Click(object sender, RoutedEventArgs e)
        {
            PasswordInput passwordInput = new PasswordInput();
            passwordInput.Owner = this;
            if (passwordInput.ShowDialog() == true)
            {
                Debug.WriteLine("Changed the serial port configuration.");
            }
        }

        private void BtnCalculateFlow_Click(object sender, RoutedEventArgs e)
        {
            if (serial == null || serial.Serial.IsOpen == false)
            {
                return;
            }

            sendToSerial("Hello\r\n");
            sendToSerial("@01?\r\n");
        }

        private void sendToSerial(string strData)
        {
            if (serial == null || serial.Serial.IsOpen == false)
            {
                showSnackBarMessage("Serial Port is not opened!");
                return;
            }
            if (!serial.SendData(strData))
            {
                showSnackBarMessage("Can not send to the Serial Port!");
                return;
            }

            string append;
            append = ">> ";
            append += strData;
            richTxtConsole.AppendText(append);
        }

        private void BtnReadStray_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnWriteStray_Click(object sender, RoutedEventArgs e)
        {

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

        }

        private void BtnReadMinPressure_Click(object sender, RoutedEventArgs e)
        {
            sendToSerial("@14?\r\n");
        }

        private void BtnWriteMinPressure_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnReadPurgeCycles_Click(object sender, RoutedEventArgs e)
        {
            sendToSerial("@17?\r\n");
        }

        private void BtnWritePurgeCycles_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnReadStabilizationTime_Click(object sender, RoutedEventArgs e)
        {
            sendToSerial("@18?\r\n");
        }

        private void BtnWriteStabilizationTime_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnReadBasePressure_Click(object sender, RoutedEventArgs e)
        {
            sendToSerial("@40?\r\n");
        }

        private void BtnWriteBasePressure_Click(object sender, RoutedEventArgs e)
        {

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
    }
}
