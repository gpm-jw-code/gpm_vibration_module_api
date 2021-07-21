using Microsoft.VisualStudio.TestTools.UnitTesting;
using gpm_vibration_module_api.Modbus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace gpm_vibration_module_api.Modbus.Tests
{
    [TestClass()]
    public class GPMModbusAPITests
    {
        GPMModbusAPI api = new GPMModbusAPI() { IsReadBaudRateWhenConnected = false };
        private int excepectedBaudRateForTest = 9600;
        const string slaveID = "01";
        const string Version = "1.08";
        const string IP = "192.168.0.3";
        const int Port = 5000;

        private bool Connect()
        {
            var ret = api.Connect(IP, Port, slaveID);
            //var ret = api.Connect("COM4", slaveID, 115200);
            return ret;
        }
        [TestMethod()]
        public void NullValueTest()
        {
            GPMModbusAPI api = new GPMModbusAPI();
            var ret = api.TestGetF03FloatValue().Result;
        }


        [Ignore()]
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
            Console.WriteLine("XYZ VE>" + string.Join(",", vevalues));
            api.DisConnect();
            Assert.AreEqual(3, vevalues.Length);
            // Assert.AreEqual("2,2,2", string.Join(",", vevalues));
            Assert.IsTrue(vevalues.FirstOrDefault(val => val < 0.00001) == default);
        }

        [TestMethod()]
        public void GetVersionTest()
        {
            if (!Connect())
                Assert.Fail();
            string version = api.GetVersion();
            Console.WriteLine("Verssion:" + version);
            api.DisConnect();
            Assert.AreEqual(Version, version);
        }

        [TestMethod()]
        public void ReadRMSValuesTest()
        {
            if (!Connect())
                Assert.Fail();
            var rmsvalues = api.ReadRMSValues().Result;
            List<double[]> rmsList = new List<double[]>();
            Console.WriteLine("XYZ RMS>" + string.Join(",", rmsvalues));
            api.DisConnect();
            Assert.AreEqual(3, rmsvalues.Length);
            //Assert.AreEqual("2,2,2", string.Join(",", rmsvalues));
            Assert.IsTrue(rmsvalues.FirstOrDefault(val => val < 0.00001) == default);
        }

        [TestMethod()]
        public void ReadTotalVEValuesTest()
        {
            if (!Connect())
                Assert.Fail();
            var totalVe = api.ReadTotalVEValues().Result;//VEx+VEy+VEz
            Console.WriteLine("Total VE>" + totalVe);
            api.DisConnect();
            //Assert.AreEqual(2, totalVe);
            Assert.IsTrue(totalVe > 0.0001);
        }

        [TestMethod()]
        public void GetSlaveIDTest()
        {
            if (!Connect())
                Assert.Fail();
            var ID = api.GetSlaveID();
            ID = api.GetSlaveID();
            api.DisConnect();
            Console.WriteLine(ID);
            Assert.AreEqual(slaveID, ID);
        }

        [TestMethod()]
        public void SlaveIDSettingTest()
        {
            if (!Connect())
                Assert.Fail();
            api.SlaveIDSetting(3);
            var ID = api.GetSlaveID();
            api.DisConnect();
            Console.WriteLine(ID);
            Assert.AreEqual(3, ID);
        }

        [TestMethod()]
        public void GetCurrentMeasureRangeTest()
        {
            if (!Connect())
                Assert.Fail();
            api.GetCurrentMeasureRange();
            api.DisConnect();
        }

        [TestMethod()]
        public void MeasureRangeSetTest()
        {
            if (!Connect())
                Assert.Fail();
            int RangeSet = 2;
            api.MeasureRangeSet(RangeSet);
            int mesRange = api.GetCurrentMeasureRange();
            api.DisConnect();
            Assert.AreEqual(RangeSet, mesRange);
        }

        [TestMethod()]
        public void ReadBaudRateSettingTest()
        {
            if (!Connect())
                Assert.Fail();
            int baud = api.ReadBaudRateSetting();
            api.DisConnect();
            Assert.AreEqual(excepectedBaudRateForTest, baud);
        }
        [TestMethod()]
        public void BaudRateSettingTest()
        {
            if (!Connect())
                Assert.Fail();
            bool success = api.BaudRateSetting(115200);
            if (!success)
                Assert.Fail();
            Assert.AreEqual(115200,  api.ReadBaudRateSetting());

             success = api.BaudRateSetting(115200);
            if (!success)
                Assert.Fail();
            Assert.AreEqual(115200, api.ReadBaudRateSetting());

            api.DisConnect();
            Assert.IsTrue(success);
        }
    }
}