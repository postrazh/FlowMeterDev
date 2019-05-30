using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;

namespace Ald.SerialPort.Configuration
{
    [Serializable]
    public class SerialConfiguration
    {
        public enum EBitsPerSecond
        {
            V75 = 75,
            V110 = 110,
            V134 = 134,
            V150 = 150,
            V300 = 300,
            V600 = 600,
            V1200 = 1200,
            V1800 = 1800,
            V2400 = 2400,
            V4800 = 4800,
            V7200 = 7200,
            V9600 = 9600,
            V14400 = 14400,
            V19200 = 19200,
            V38400 = 38400,
            V57600 = 57600,
            V115200 = 115200,
            V128000 = 128000
        };

        public enum EDataBits
        {
            V4 = 4,
            V5 = 5,
            V6 = 6,
            V7 = 7,
            V8 = 8
        };

        public String PortName { get; set; }

        public int ConfigBitsPerSecond { get; set; }
        public EDataBits ConfigDataBits { get; set; }
        public Parity ConfigParity { get; set; }
        public StopBits ConfigStopBits { get; set; }

        public SerialConfiguration()
        {
            PortName = "";

            ConfigBitsPerSecond = (int)EBitsPerSecond.V9600;
            ConfigDataBits = EDataBits.V8;
            ConfigParity = Parity.None;
            ConfigStopBits = StopBits.One;
        }
        public static int[] BitsPerSecondArray
        {
            get
            {
                return Enum.GetValues(typeof(EBitsPerSecond)).Cast<int>().ToArray();
            }
        }
    }
}
