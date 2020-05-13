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
                SaveDir = "Log/Event_Log/";
                DirCreation();
            }
        }

        public class CodeErrorLog : LogFun
        {
            public CodeErrorLog()
            {
                SaveDir = "Log/Coode_Error_Log/";
                DirCreation();
            }
        }
    }


    public class LogFun
    {
        public string SaveDir;
        public void Log(string content)
        {
            try
            {
                var fileName = DateTime.Now.ToString("yyyyMMdd_HH") + ".txt";
                using (StreamWriter sw = new StreamWriter(SaveDir + fileName, true))
                {
                    sw.WriteLine($"{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")} {content}");
                };
            }
            catch (Exception)
            {

            }
        }
        public void DirCreation()
        {
            if (Directory.Exists(SaveDir) == false)
            {
                Directory.CreateDirectory(SaveDir);
            }
        }
    }
}
