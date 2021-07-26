using Microsoft.VisualStudio.TestTools.UnitTesting;
using gpm_vibration_module_api.ThreeInOne;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace gpm_vibration_module_api.ThreeInOne.Tests
{
    [TestClass()]
    public class ThreeInOneModuleAPITests
    {
        [TestMethod()]
        public void ConnectTest()
        {
            ThreeInOneModuleAPI api = new ThreeInOneModuleAPI();
            int errorCode = api.Connect("COM1");
            api.Close();
            Assert.AreEqual(0, errorCode);

        }

        [TestMethod()]
        public void GetDataTest()
        {
            ThreeInOne.Emulator emulator = new Emulator();
            emulator.Connect("COM2");

            ThreeInOneModuleAPI api = new ThreeInOneModuleAPI();
            api.Connect("COM1");
            for (int i = 0; i < 10; i++)
            {
                DataSets.ThreeInOneModuleDataSet data = api.GetData().Result;
                Assert.AreEqual(0, data.ErrorCode);
                Assert.AreEqual(32.299999237060547, data.Temperature1);
                Assert.AreEqual(34.0, data.Temperature2);
                Assert.AreEqual(60.200000762939453, data.Humidity1);
                Assert.AreEqual(94.0, data.Humidity2);
                Assert.AreEqual(123.30000305175781, data.Pressure1);
                Assert.AreEqual(200.30000305175781, data.Pressure2);
                Assert.AreEqual(3096, data.RawBytes.Count);

            }
            api.Close();
        }

        [TestMethod()]
        public void MeasureRangeSettingTest()
        {
            Emulator emulator = new Emulator();
            emulator.Connect("COM2");
            ThreeInOneModuleAPI api = new ThreeInOneModuleAPI();
            api.Connect("COM1");

            int ErrorCode = api.MeasureRangeSetting(clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_4G).Result;
            Assert.AreEqual(0,ErrorCode);
            Assert.AreEqual( clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_4G,api.MEASURE_RANGE);

            emulator.isParamReturErrorSimulate = false;
            ErrorCode = api.MeasureRangeSetting(clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_8G).Result;
            Assert.AreEqual(0, ErrorCode);
            Assert.AreEqual(clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_8G, api.MEASURE_RANGE);


            emulator.isParamReturErrorSimulate = true;
            ErrorCode = api.MeasureRangeSetting(clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_4G).Result;
            Assert.AreEqual(16608, ErrorCode);
            Assert.AreEqual(clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_8G, api.MEASURE_RANGE);


            api.Close();
        }
    }
}