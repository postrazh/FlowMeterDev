using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowMeter.Configuration
{
    public class SettingsManager
    {
        #region Singletone
        private static SettingsManager instance = new SettingsManager();

        public static SettingsManager getInstance()
        {
            return instance;
        }
        #endregion

        private const string fileName = "app.settings.json";

        public ApplicationSettings CurrentSettings { get; set; } = new ApplicationSettings();

        public SettingsManager()
        {
            Load();
        }

        private void Load()
        {
            string json;
            if (File.Exists(fileName))
            {
                json = File.ReadAllText(fileName);
                this.CurrentSettings = JsonConvert.DeserializeObject<ApplicationSettings>(json);
            }
            else
            {
                this.CurrentSettings = new ApplicationSettings();
            }
        }

        public void Save()
        {
            string json = JsonConvert.SerializeObject(this.CurrentSettings, Formatting.Indented);
            File.WriteAllText(fileName, json);
        }
    }
}
