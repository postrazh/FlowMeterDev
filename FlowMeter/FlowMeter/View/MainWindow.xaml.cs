using Ald.SerialPort.Configuration;
using FlowMeter.Configuration;
using FlowMeter.View;
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
        private ALDSerialPort serial;

        private string strBuffer = "";

        public MainWindow()
        {
            InitializeComponent();

            lblPortName.Content = SettingsManager.getInstance().CurrentSettings.SerialConfig.PortName;
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

            // check the validaity
            if (strFull[0] != '@')
                return;

            string command = "";
            if (serial.LastSent.Length >= 3)
                command = serial.LastSent.Substring(0, 3);

            Debug.WriteLine("Received: " + strFull);

            if (command == "@10")
            {
                string strValue = strFull.Substring(3);
                strValue = strValue.TrimEnd('\r', '\n');

                Debug.WriteLine("Value: " + strValue);

                this.Dispatcher.Invoke(new Action(delegate
                {
                    txtMaxPressure.Text = strValue;
                }));
            }
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
            totalStray += Convert.ToDouble(lblAsset100.Content.ToString());
            totalStray += Convert.ToDouble(lblAsset105.Content.ToString());
            totalStray += Convert.ToDouble(lblAsset1498.Content.ToString());
            totalStray += Convert.ToDouble(lblAsset1500.Content.ToString());

            // Flow meter,CCM
            if (tglP5a.IsChecked ?? false)
            {
                totalStray += Convert.ToDouble(lblP5a.Content.ToString());
                lblPipeP5a.Content = "P5a";
            }

            if (tglP6a.IsChecked ?? false)
            {
                totalStray += Convert.ToDouble(lblP6a.Content.ToString());
                lblPipeP5a.Content = "P6a";
            }

            if (tgl1157.IsChecked ?? false)
            {
                totalStray += Convert.ToDouble(lbl1157.Content.ToString());
                lblPipeP5a.Content = "1157";
            }

            if (tglStrayExtra.IsChecked ?? false)
            {
                totalStray += Convert.ToDouble(txtStrayExtra.Text);

                stackExtra.Visibility = Visibility.Visible;
                Canvas.SetLeft(imgMfc, -386);
            }
            else
            {
                stackExtra.Visibility = Visibility.Hidden;
                Canvas.SetLeft(imgMfc, -335);
            }

            txtStrayTotal.Text = totalStray.ToString();

            // render image
            RenderStrayVolume();
        }


        private void RenderStrayVolume()
        {

            double offset = 0;

            //// pipe 101
            //if (tglPipe101.IsChecked ?? false)
            //{
            //    stackPipe101.Visibility = Visibility.Visible;
            //    offset -= 97;
            //}
            //else
            //{
            //    stackPipe101.Visibility = Visibility.Hidden;     
            //}
            //Canvas.SetLeft(stackPipe101, 12 + offset);

            //// pipe 102
            //if (tglPipe102.IsChecked ?? false)
            //{
            //    stackPipe102.Visibility = Visibility.Visible;
            //    offset -= 75;
            //}
            //else
            //{
            //    stackPipe102.Visibility = Visibility.Hidden;
            //}
            //Canvas.SetLeft(stackPipe102, 12 + offset);

            //// pipe 103
            //if (tglPipe103.IsChecked ?? false)
            //{
            //    stackPipe103.Visibility = Visibility.Visible;
            //    offset -= 75;
            //}
            //else
            //{
            //    stackPipe103.Visibility = Visibility.Hidden;
            //}
            //Canvas.SetLeft(stackPipe103, 12 + offset);

            // pipe extra
            //if (tglPipeExtra.IsChecked ?? false)
            //{
            //    stackPipeExtra.Visibility = Visibility.Visible;
            //    offset -= 48;
            //}
            //else
            //{
            //    stackPipeExtra.Visibility = Visibility.Hidden;
            //}
            //Canvas.SetLeft(stackPipeExtra, 12 + offset);

            //// image mfc
            //Canvas.SetLeft(imgMfc, -70 + offset);
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
                MessageBox.Show("Serial Port is not opened!", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            serial.SendData(strData);

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

        private void TglStrayExtra_Checked(object sender, RoutedEventArgs e)
        {
            SumupStrayVolume();
        }

        private void TglStrayExtra_Unchecked(object sender, RoutedEventArgs e)
        {
            SumupStrayVolume();
        }
    }
}
