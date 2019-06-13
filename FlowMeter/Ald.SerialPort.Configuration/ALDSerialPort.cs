using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ports = System.IO.Ports;
using System.Management;

namespace Ald.SerialPort.Configuration
{
    public class ALDSerialPort
    {
        private SerialConfiguration config;
        private Ports.SerialPort serial;

        public Ports.SerialPort Serial
        {
            get { return serial; }
        }

        public string LastSent { get; private set; }

        public event Ports.SerialErrorReceivedEventHandler ErrorReceived;
        public event Ports.SerialDataReceivedEventHandler DataReceived;

        public bool Kbhit()
        {
            if (!serial.IsOpen) return false;
            return serial.BytesToRead > 0;
        }
        public void ReadAllJunk()
        {
            if (!serial.IsOpen) return;
            while (serial.BytesToRead > 0)
                serial.ReadByte();
        }

        public static List<SerialPreviousInformation> EnumeratePorts()
        {
            string targetCaptionProperty = "Caption";

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity");
            List<string> Descriptions = new List<string>();
            foreach (ManagementObject queryObj in searcher.Get())
                if (queryObj.ContainsProperty(targetCaptionProperty) && queryObj[targetCaptionProperty] != null && queryObj[targetCaptionProperty].ToString().Contains("(COM"))
                    Descriptions.Add(queryObj[targetCaptionProperty].ToString());

            List<SerialPreviousInformation> list = Ports.SerialPort.GetPortNames().ToList().ConvertAll(x => new SerialPreviousInformation() { Port = x });
            string information;
            int previdx;
            foreach (var i in list)
            {
                information = "";
                previdx = int.MaxValue;
                foreach (var j in Descriptions.Where(x => x.Contains(i.Port)))
                {

                    int idx = j.IndexOf(i.Port);
                    if (idx == j.Length - 1 || !(j[idx + 1] >= '0' && j[idx + 1] <= '9') && idx < previdx)
                    {
                        information = j;
                        previdx = idx;
                    }
                }
                i.Description = information;
            }
            return list;
        }
        public ALDSerialPort(SerialConfiguration config)
        {
            this.config = config;
        }
        void serial_DataReceived(object sender, Ports.SerialDataReceivedEventArgs e)
        {
            if (DataReceived != null)
                DataReceived(this, e);
        }
        void serial_ErrorReceived(object sender, Ports.SerialErrorReceivedEventArgs e)
        {
            if (ErrorReceived != null)
                ErrorReceived(this, e);
        }
        public void OpenPort()
        {
            serial = new Ports.SerialPort(config.PortName, (int)config.ConfigBitsPerSecond, config.ConfigParity, (int)config.ConfigDataBits, config.ConfigStopBits);
            serial.ErrorReceived += new Ports.SerialErrorReceivedEventHandler(serial_ErrorReceived);
            serial.DataReceived += new Ports.SerialDataReceivedEventHandler(serial_DataReceived);
            serial.Encoding = Encoding.GetEncoding("Windows-1252");

            serial.Open();
            serial.Encoding = System.Text.ASCIIEncoding.ASCII;

            serial.WriteTimeout = 1000;
        }
        public void ClosePort()
        {

            serial.ErrorReceived -= new Ports.SerialErrorReceivedEventHandler(serial_ErrorReceived);
            serial.DataReceived -= new Ports.SerialDataReceivedEventHandler(serial_DataReceived);

            serial.DiscardInBuffer();
            serial.DiscardOutBuffer();
            var stream = serial.BaseStream;
            stream.Flush();
            stream.Close();
            stream.Dispose();

            serial.Close();

            serial.Dispose();

            serial = null;
            stream = null;
            GC.Collect();

        }
        public bool IsOpen
        {
            get { return serial != null && serial.IsOpen; }
        }
        public bool SendData(byte[] data)
        {
            try
            {
                serial.Write(data, 0, data.Length);
                return true;
            }
            catch { return false; }
        }
        public bool SendData(string data)
        {
            LastSent = data;
            try
            {
                if (serial.IsOpen)
                {
                    serial.Write(data);
                    return true;
                }
                else
                    return false;
            }
            catch (TimeoutException ex)
            {
                return false;
            }
            catch { return false; }

        }
    }
}
