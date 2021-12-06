using Microsoft.VisualStudio.TestTools.UnitTesting;
using gpm_vibration_module_api.API.Modbus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace gpm_vibration_module_api.API.Modbus.Tests
{
    [TestClass()]
    public class DeviceInfoGetFromHtmlPageTests
    {
        [TestMethod()]
        public void EnterMainPageTest()
        {
            string ret = DeviceInfoGetFromHtmlPage.GetSettingString("192.168.0.100");
            Console.WriteLine(ret);
            Assert.IsTrue(ret.ToUpper().Contains("SETTINGSCALLBACK"));
        }



        [TestMethod()]
        public void GET_Protocol_TypeTest()
        {
            gpm_vibration_module_api.Modbus.ModbusClient.CONNECTION_TYPE type = DeviceInfoGetFromHtmlPage.GET_Protocol_Type("192.168.0.100");
            Assert.AreEqual(gpm_vibration_module_api.Modbus.ModbusClient.CONNECTION_TYPE.TCP, type);
        }

        [TestMethod()]
        public void GET_BaudRateTest()
        {
            int br = DeviceInfoGetFromHtmlPage.GET_BaudRate("192.168.0.100");
            Assert.AreEqual(115200, br);
        }
    }
}