using Ald.SerialPort.Configuration;
using FlowMeter.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FlowMeter
{
    /// <summary>
    /// Interaction logic for SerialSettings.xaml
    /// </summary>
    public partial class SerialSettings : Window
    {
        private SerialConfiguration serialConfig;

        public SerialSettings()
        {
            InitializeComponent();

            serialConfig = SettingsManager.getInstance().CurrentSettings.SerialConfig;
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            serialConfig.PortName = (string)cmbPort.SelectedItem;
            serialConfig.ConfigBitsPerSecond = (int)cmbBaudRate.SelectedValue;
            serialConfig.ConfigDataBits = (SerialConfiguration.EDataBits)cmbDataBits.SelectedItem;
            serialConfig.ConfigParity = (System.IO.Ports.Parity)cmbParity.SelectedItem;
            serialConfig.ConfigStopBits = (System.IO.Ports.StopBits)cmbStopBits.SelectedItem;

            // save settings
            SettingsManager.getInstance().Save();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {

        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            // serial port list
            var res = ALDSerialPort.EnumeratePorts();
            var lstCom = res.Select(x => x.Port).ToList();

            cmbPort.ItemsSource = lstCom;
            cmbPort.SelectedItem = serialConfig.PortName;

            // serial port parameters
            Type type;

            type = typeof(SerialConfiguration.EBitsPerSecond);
            cmbBaudRate.ItemsSource = type.GetEnumValues().Cast<int>().ToArray();
            cmbBaudRate.SelectedItem = (int)serialConfig.ConfigBitsPerSecond;

            type = typeof(SerialConfiguration.EDataBits);
            cmbDataBits.ItemsSource = type.GetEnumValues().Cast<int>().ToArray();
            cmbDataBits.SelectedItem = (int)serialConfig.ConfigDataBits;

            type = typeof(System.IO.Ports.Parity);
            cmbParity.ItemsSource = type.GetEnumValues();
            cmbParity.SelectedItem = serialConfig.ConfigParity;

            type = typeof(System.IO.Ports.StopBits);
            cmbStopBits.ItemsSource = type.GetEnumValues();
            cmbStopBits.SelectedItem = serialConfig.ConfigStopBits;
        }
    }
}
