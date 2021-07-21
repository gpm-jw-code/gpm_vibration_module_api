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
    [Ignore()]
    public class GPMAPITests
    {
        public GPMAPITests()
        {
            //KillSimulator();
        }

        GPMModuleAPI api = new GPMModuleAPI();
        //const string IP = "192.168.0.57";
        const string IP = "192.168.0.7";
        const int Port = 5000;

        private bool Connect()
        {
            var ret = api.Connect(IP, Port).Result;
            //var ret = api.Connect("COM4", slaveID, 115200);
            return ret == 0;
        }
        private void KillSimulator()
        {
            var pros = Process.GetProcessesByName("modbus_simulator");
            foreach (var item in pros)
            {
                item.Kill();
            }
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
            api.Disconnect();
            Assert.IsTrue(ret);
        }
        [TestMethod]
        public void DisconnectTest()
        {
            var ret = Connect();
            api.Disconnect();
        }

        [TestMethod]
        public void MeasureRangeSettingTest()
        {
            if (!Connect())
                Assert.Fail();
            var ret = api.Measure_Range_Setting(clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_2G).Result;
            Assert.AreEqual(0, ret);
        }
        [TestMethod]
        public void FullProcessTest()
        {
            if (!Connect())
                Assert.Fail();
            var daqModeRet = api.DAQModeSetting(DAQMode.Low_Sampling).Result;
            Assert.AreEqual(daqModeRet, 0);

            var measureRangeSetRet = api.Measure_Range_Setting(clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_2G).Result;
            Assert.AreEqual(measureRangeSetRet, 0);

            int ret = api.Data_Length_Setting(512).Result;
            Assert.AreEqual(0, ret);
            Assert.AreEqual("0x00,0x01,0x9F,0x00,0x00,0x00,0x00,0x00,", api.GetDeviceParams().Result);
            var dataset = api.GetData(true, true).Result;
            Assert.AreEqual(0, dataset.ErrorCode);
            Assert.AreEqual(512, dataset.AccData.X.Count);

            ret = api.Data_Length_Setting(1024).Result;
            Assert.AreEqual(0, ret);
            Assert.AreEqual("0x00,0x02,0x9F,0x00,0x00,0x00,0x00,0x00,", api.GetDeviceParams().Result);
            dataset = api.GetData(true, true).Result;
            Assert.AreEqual(0, dataset.ErrorCode);
            Assert.AreEqual(1024, dataset.AccData.X.Count);

            List<int> testLenList = new List<int>() { 8192, 16384 };
            foreach (var len in testLenList)
            {
                ret = api.Data_Length_Setting(len).Result;
                Assert.AreEqual(0, ret);
                Assert.AreEqual($"0x00,0x{(len / 512):X2},0x9F,0x00,0x00,0x00,0x00,0x00,", api.GetDeviceParams().Result);
                dataset = api.GetData(true, true).Result;
                Assert.AreEqual(0, dataset.ErrorCode);
                Assert.AreEqual(len, dataset.AccData.X.Count);
            }
        }

    }
}