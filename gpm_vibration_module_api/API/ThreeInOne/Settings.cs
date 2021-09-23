using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Json;

namespace gpm_vibration_module_api.API.ThreeInOne
{
    public class Settings
    {
        private const string ConfigName = "three-in-one-device-config.json";
        public static Configs LoadConfig()
        {
            Configs obj;
            if (File.Exists(ConfigName))
            {
                string str = File.ReadAllText(ConfigName);
                 obj = str.FromJson<Configs>();
            }
            else
                obj = new Configs();

            File.WriteAllText(ConfigName, obj.ToJson());
            return obj;
        }
        public class Configs
        {
            public int record_raw_data { get; set; } = 0;
            public int single_data_mode { get; set; } = 1;
        }
    }
}
