using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ald.SerialPort.Configuration
{
    public class SerialPreviousInformation
    {
        public string Port { get; set; }
        public string Description { get; set; }

        public SerialPreviousInformation(string port, string description)
        {
            this.Port = port;
            this.Description = description;
        }
        public SerialPreviousInformation() { }
        public override string ToString()
        {
            return string.Format("[{0}] {1}", Port, Description == ""? "No information": Description);
        }
    }
}
