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
        enum PROTOCOL_TYPE
        {
            TCP, RTU
        }
        GPMModbusAPI api = new GPMModbusAPI() { IsReadBaudRateWhenConnected = false };
        string PortName = "COM3";
        private int excepectedBaudRateForTest = 115200;
        string slaveID = "65";
        const string SensorFwVersion = "1.09";
        const string IP = "192.168.0.59";
        const int Port = 5000;
        PROTOCOL_TYPE pROTOCOL = PROTOCOL_TYPE.RTU;
        bool connected = false;


        private bool Connect()
        {
            if (this.connected)
                return this.connected;
            bool connected = pROTOCOL == PROTOCOL_TYPE.TCP ? api.Connect(IP, Port, slaveID) : api.Connect(PortName, slaveID, 115200);
            if (connected)
            {
                //刷新目前ID
                //slaveID = api.GetSlaveID();
                api.DisConnect();

            }
            connected = this.connected = pROTOCOL == PROTOCOL_TYPE.TCP ? api.Connect(IP, Port, slaveID) : api.Connect(PortName, slaveID, 115200);
            return connected;
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
            Assert.AreEqual(SensorFwVersion, version);
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
            api.DisConnect();
            Console.WriteLine(ID);
            Assert.AreEqual(slaveID, ID);
        }

        [TestMethod()]
        public void SlaveIDSettingTest()
        {
            if (!Connect())
                Assert.Fail();
            api.SlaveIDSetting(0x03);
            var ID = api.GetSlaveID();
            api.DisConnect();
            Console.WriteLine(ID);
            Assert.AreEqual("03", ID);
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
            if (pROTOCOL == PROTOCOL_TYPE.RTU)
                return;
            var buadRateToSetting = 9600;
            for (int i = 0; i < 1; i++)
            {
                if (!Connect())
                    Assert.Fail();
                //先讀
                var currentBaudRate = api.ReadBaudRateSetting();
                buadRateToSetting = currentBaudRate == 9600 ? 115200 : 9600; //變更設定
                bool success = api.BaudRateSetting(buadRateToSetting);
                if (success)
                {
                    api.DisConnect();
                    if (!Connect())
                        Assert.Fail();
                    Assert.AreEqual(buadRateToSetting, api.ReadBaudRateSetting());
                    api.DisConnect();
                }
            }

            if (excepectedBaudRateForTest != buadRateToSetting)
            {
                if (!Connect())
                    Assert.Fail();
                //先讀
                bool success = api.BaudRateSetting(excepectedBaudRateForTest);
                api.DisConnect();
                Assert.IsTrue(success);
            }

        }
    }
}