using Microsoft.VisualStudio.TestTools.UnitTesting;
using gpm_vibration_module_api.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace gpm_vibration_module_api.Tools.Tests
{
    [TestClass()]
    public class LogFunTests
    {
        [TestMethod()]
        public void LogTest()
        {
            Logger.Event_Log.Log("TEST");
            var _logcontent = File.ReadAllText(Logger.Event_Log.SaveDir + DateTime.Now.ToString("yyyyMMdd_HH") + ".txt");
            Assert.IsTrue(_logcontent.Contains("TEST"));
        }
    }
}