using Ald.SerialPort.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowMeter.Configuration
{
    public class ApplicationSettings
    {
        public string LastUsedPort { get; set; }

        public SerialConfiguration SerialConfig { get; set; } = new SerialConfiguration();

        public ApplicationSettings()
        {
            
        }
    }
}
