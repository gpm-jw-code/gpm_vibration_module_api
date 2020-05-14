using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace gpm_vibration_module_api.sys
{
    public class Settings_Ctrl
    {
        private static bool Check_Config_Exist()
        {
            return File.Exists("settings.xml");
        }

        public static int Creat_Config()
        {
            try
            {
                var ModelSavePath = "";
                var filepath = Path.Combine(ModelSavePath, "settings.xml");
                if (!File.Exists(filepath))
                    File.Create(filepath).Close();
                FileStream fs = new FileStream(filepath, FileMode.Create);
                XmlSerializer xs = new XmlSerializer(typeof(Settings_Items));
                xs.Serialize(fs, new Settings_Items());
                fs.Close();
                return 0;
            }
            catch (IOException exp)
            {
                return -1;
            }

        }

        public static Settings_Items Log_Config()
        {
            try
            {
                if (Check_Config_Exist() == false)
                {
                    Creat_Config();
                    return new Settings_Items();
                }
                FileStream fs = new FileStream("settings.xml", FileMode.Open);
                XmlSerializer xs = new XmlSerializer(typeof(Settings_Items));
                Settings_Items setting = (Settings_Items)xs.Deserialize(fs);
                fs.Flush();
                fs.Close();
                return setting;
            }
            catch (Exception ex)
            {
                Task.Run(() => MessageBox.Show($"settings.xml 讀取失敗\r\n{ex.Message}"));
                Creat_Config();
                return new Settings_Items();
            }
        }
    }
}
