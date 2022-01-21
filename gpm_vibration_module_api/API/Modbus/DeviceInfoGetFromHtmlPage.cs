using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static gpm_vibration_module_api.Modbus.ModbusClient;

namespace gpm_vibration_module_api.API.Modbus
{
    public class DeviceInfoGetFromHtmlPage
    {
        public static string GetSettingString(string controllerIP)
        {

            string result = string.Empty;
            string url = "http://" + controllerIP + "/w5500.js";
            HttpWebRequest webRequest = WebRequest.CreateHttp(url);
            webRequest.Method = "GET";
            try
            {
                var response = webRequest.GetResponse();
                using (StreamReader reader = new StreamReader((response as HttpWebResponse).GetResponseStream(), Encoding.UTF8))
                {
                    result = reader.ReadToEnd();
                    reader.Close();
                }
                if (string.IsNullOrEmpty(result))
                {
                    Console.WriteLine("請求地址錯誤");
                    return null;
                }
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public static CONNECTION_TYPE GET_Protocol_Type(string controllIP)
        {
            string htmlRepose = null;
            int retry = 0;
            while ((htmlRepose = GetSettingString(controllIP)) == null)
            {
                Thread.Sleep(100);
                retry += 1;
                if (retry == 3)
                    return CONNECTION_TYPE.UnKnow;
            }
            if (htmlRepose == null)
                return CONNECTION_TYPE.UnKnow;
            if (htmlRepose.Contains("\"datamode\":\"2\""))
                return CONNECTION_TYPE.TCP;
            else if (htmlRepose.Contains("\"datamode\":\"3\""))
                return CONNECTION_TYPE.RTU;
            else
                return CONNECTION_TYPE.UnKnow;
        }
        public static int GET_BaudRate(string controllIP)
        {
            string htmlRepose = null;
            int retry = 0;
            while ((htmlRepose = GetSettingString(controllIP)) == null)
            {
                Thread.Sleep(100);
                retry += 1;
                if (retry == 3)
                    return -999;
            }
            if (htmlRepose == null)
                return -1;
            if (htmlRepose.Contains("\"baud\":\"0\""))
                return 9600;
            if (htmlRepose.Contains("\"baud\":\"1\""))
                return 115200;
            if (htmlRepose.Contains("\"baud\":\"2\""))
                return 921600;
            if (htmlRepose.Contains("\"baud\":\"3\""))
                return 1152000;
            else
                return -1;
        }
    }
}
