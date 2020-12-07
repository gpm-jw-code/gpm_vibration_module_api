using Microsoft.VisualStudio.TestTools.UnitTesting;
using gpm_module_api.UVSensor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using gpm_vibration_module_api;
namespace gpm_module_api.UVSensor.Tests
{
    [TestClass()]
    public class UVSensorAPITests
    {
        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void ConnectTest()
        {
            UVSensorAPI uv_module = new UVSensorAPI();
            uv_module.AccDataRevTimeOut = 1000;
            var ret = uv_module.Connect("127.0.0.1", 5000, IsSelfTest: false);
            //Assert.AreEqual(  ,uv_module.SensorType);
            Assert.AreEqual(0, ret.Result);
            uv_module.Disconnect();
        }

        [TestMethod()]
        public async Task GetDataTestAsync()
        {
            UVSensorAPI uv_module = new UVSensorAPI();
            uv_module.AccDataRevTimeOut = 1000;
            var ret = await uv_module.Connect("192.168.0.12", 5000, IsSelfTest: false);
            if (ret != 0) return;
            var dataset = await uv_module.GetData();
             Assert.IsTrue(dataset.ErrorCode == 0);
        }

        [TestMethod()]
        public async Task ContiuneGetDataTestAsync()
        {
            UVSensorAPI uv_module = new UVSensorAPI();
            uv_module.AccDataRevTimeOut = 1000;
            var ret = await uv_module.Connect("127.0.0.1", 5000, IsSelfTest: false);
            if (ret != 0) return;
            List<UVDataSet> datals = new List<UVDataSet>();
            Task t = Task.Run(async () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    var dataset = await uv_module.GetData();
                    datals.Add(dataset);
                }
            });
            await t.ContinueWith(results =>
            {
                Assert.IsTrue(datals[0].RecieveTime != datals[1].RecieveTime);
                Assert.IsTrue(datals.Count == 10);
            });
        }
    }
}