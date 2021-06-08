using Microsoft.VisualStudio.TestTools.UnitTesting;
using gpm_vibration_module_api.Modbus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gpm_vibration_module_api.Modbus.Tests
{
    [TestClass()]
    public class GPMModbusAPITests
    {
        GPMModbusAPI api = new GPMModbusAPI() { IsReadBaudRateWhenConnected = false };
        private bool Connect()
        {
            var ret = api.Connect("192.168.0.58", 5000, "1");
            return ret;
        }
        [TestMethod()]
        public void ConnectTest()
        {
            var ret = Connect();
            api.DisConnect();
            Assert.IsTrue(ret);
        }

        [TestMethod()]
        public void ReadVEValuesTest()
        {
            if (!Connect())
                Assert.Fail();
            var vevalues = api.ReadVEValues().Result;
            api.DisConnect();
            Assert.AreEqual(3, vevalues.Length);
        }

        [TestMethod()]
        public void GetVersionTest()
        {
            if (!Connect())
                Assert.Fail();
            string version = api.GetVersion();
            Console.WriteLine("ve rsion:" + version);
            api.DisConnect();
            Assert.AreEqual("1.07", version);
        }

        [TestMethod()]
        public void ReadRMSValuesTest()
        {
            if (!Connect())
                Assert.Fail();
            var rmsvalues = api.ReadRMSValues().Result;
            api.DisConnect();
            Assert.AreEqual(3, rmsvalues.Length);
        }

        [TestMethod()]
        public void ReadTotalVEValuesTest()
        {
            if (!Connect())
                Assert.Fail();
            var totalVe = api.ReadTotalVEValues().Result;//VEx+VEy+VEz
            api.DisConnect();
        }

        [TestMethod()]
        public void GetSlaveIDTest()
        {
            if (!Connect())
                Assert.Fail();
            var ID = api.GetSlaveID();
            api.DisConnect();
            Console.WriteLine(ID);
            Assert.AreEqual("01",ID);
        }
    }
}