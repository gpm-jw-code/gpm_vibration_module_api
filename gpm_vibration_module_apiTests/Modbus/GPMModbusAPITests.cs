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
        const string slaveID = "01";
        const string Version = "1.06";
        private bool Connect()
        {
            var ret = api.Connect("127.0.0.1", 5000, slaveID);
           // var ret = api.Connect("COM4", slaveID, 115200);
            return ret;
        }
        
        [TestMethod()]
        public void NullValueTest()
        {
            GPMModbusAPI api = new GPMModbusAPI();
            var ret = api.TestGetF03FloatValue().Result;
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
            Console.WriteLine("XYZ VE>" + string.Join(",", vevalues));
            api.DisConnect();
            Assert.AreEqual(3, vevalues.Length);
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
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 10; i++)
            {
                var _rmsvalues = api.ReadRMSValues().Result;
                rmsList.Add(_rmsvalues);
            }
            sw.Stop();
            Console.WriteLine("10 Times spend:" + sw.ElapsedMilliseconds + " ms");
            Console.WriteLine("One Times spend:" + sw.ElapsedMilliseconds / 10 + " ms");
            Console.WriteLine("XYZ RMS>" + string.Join(",", rmsvalues));
            api.DisConnect();
            Assert.AreEqual(3, rmsvalues.Length);
            Assert.AreEqual(10, rmsList.Count);
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
        public void DisconnectTest()
        {
            if (!Connect())
                Assert.Fail();
            api.DisConnect();
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
            Assert.AreEqual(9600, baud);
        }
    }
}