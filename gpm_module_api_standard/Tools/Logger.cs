using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace gpm_vibration_module_api.Tools
{
    /// <summary>
    /// 用來記錄LOG
    /// </summary>
    public static class Logger
    {
        public static EventLog Event_Log = new EventLog();
        public static CodeErrorLog Code_Error_Log = new CodeErrorLog();


        public class EventLog : LogFun
        {
            public EventLog()
            {
                Console.WriteLine("log initialize");
                SaveDir = "Log/Event_Log/";
                //DirCreation();
                //LoadGlobalSetting();
            }
            public override void Log(string content)
            {
                Console.WriteLine("***EVENT LOG >>>" + content);
                //base.Log(content);
            }
        }

        public class CodeErrorLog : LogFun
        {
            public CodeErrorLog()
            {
                SaveDir = "Log/Coode_Error_Log/";
                //DirCreation();
                //LoadGlobalSetting();
            }

            public override void Log(string content)
            {
                Console.WriteLine("***CODE ERROR >>>" + content);
                //base.Log(content);
            }
            public void Log(Exception ex)
            {
                //base.Log($"{ex.Message} { ex.StackTrace}");
            }
        }
    }


    public class LogFun
    {
        public string SaveDir;
        public bool is_log_enable = true;
        internal NET.UDPServer udpServer = new NET.UDPServer();

        internal void LoadGlobalSetting()
        {
            var settings =sys.Settings_Ctrl.Log_Config();
            this.is_log_enable = settings.is_write_log_to_HardDisk;
        }

        public virtual void Log(string content)
        {
            var log = $"{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")} {content}";
            udpServer.Send(Encoding.Unicode.GetBytes(log));
            if (is_log_enable == false) return;
            try
            {
                var fileName = DateTime.Now.ToString("yyyyMMdd_HH") + ".txt";
                using (StreamWriter sw = new StreamWriter(SaveDir + fileName, true))
                {
                    sw.WriteLine(log);
                };
            }
            catch (Exception)
            {

            }
        }
        public void DirCreation()
        {

            Console.WriteLine("log 創建資料夾");
            try
            {
                if (Directory.Exists(SaveDir) == false)
                {
                    Console.WriteLine("創建資料夾:" + SaveDir);
                    Directory.CreateDirectory(SaveDir);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
           
        }
    }
}
