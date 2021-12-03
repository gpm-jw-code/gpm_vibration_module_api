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
        string PortName = "COM14";
        private int excepectedBaudRateForTest = 115200;
        string slaveID = "01";
        const string SensorFwVersion = "1.16";
        const string IP = "192.168.0.100";
        const int Port = 500;
        PROTOCOL_TYPE pROTOCOL = PROTOCOL_TYPE.RTU;
        bool connected = false;

        [TestMethod]
        public void TEST2()
        {
            GPMModbusAPI api1 = new GPMModbusAPI();
            api1.Connect("192.168.0.9", 5000, "9");

            for (int i = 0; i < 10; i++)
            {
                var data1 = api1.ReadRMSValues().Result;
                Console.WriteLine($"[65]RMS:{string.Join(",", data1)}");
            }
            api1.Disconnect();
        }

        private bool Connect()
        {
            if (this.connected)
                return this.connected;
            bool connected = pROTOCOL == PROTOCOL_TYPE.TCP ? api.Connect(IP, Port, slaveID) : api.Connect(PortName, slaveID, 115200);
            if (connected)
            {
                //刷新目前ID
                //slaveID = api.GetSlaveID();
                api.Disconnect();

            }
            connected = this.connected = pROTOCOL == PROTOCOL_TYPE.TCP ? api.Connect(IP, Port, slaveID) : api.Connect(PortName, slaveID, 115200);
            return connected;
        }

        [TestMethod()]
        public void ConnectTest()
        {
            var ret = Connect();
            api.Disconnect();
            Assert.IsTrue(ret);
        }

        [TestMethod()]
        public void ReadVEValuesTest()
        {
            if (!Connect())
                Assert.Fail();
            var vevalues = api.ReadVEValues().Result;
            Console.WriteLine("XYZ VE>" + string.Join(",", vevalues));
            api.Disconnect();
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
            api.Disconnect();
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
            api.Disconnect();
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
            api.Disconnect();
            //Assert.AreEqual(2, totalVe);
            Assert.IsTrue(totalVe > 0.0001);
        }

        [TestMethod()]
        public void GetSlaveIDTest()
        {
            if (!Connect())
                Assert.Fail();
            var ID = api.GetSlaveID();
            api.Disconnect();
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
            api.Disconnect();
            Console.WriteLine(ID);
            Assert.AreEqual("03", ID);
        }

        [TestMethod()]
        public void GetCurrentMeasureRangeTest()
        {
            if (!Connect())
                Assert.Fail();
            api.GetCurrentMeasureRange();
            api.Disconnect();
        }

        [TestMethod()]
        public void MeasureRangeSetTest()
        {
            if (!Connect())
                Assert.Fail();
            int RangeSet = 2;
            api.MeasureRangeSet(RangeSet);
            int mesRange = api.GetCurrentMeasureRange();
            api.Disconnect();
            Assert.AreEqual(RangeSet, mesRange);
        }

        [TestMethod()]
        public void ReadBaudRateSettingTest()
        {
            if (!Connect())
                Assert.Fail();
            int baud = api.ReadBaudRateSetting().Result;
            api.Disconnect();
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
                var currentBaudRate = api.ReadBaudRateSetting().Result;
                buadRateToSetting = currentBaudRate == 9600 ? 115200 : 9600; //變更設定
                bool success = api.BaudRateSetting(buadRateToSetting);
                if (success)
                {
                    api.Disconnect();
                    if (!Connect())
                        Assert.Fail();
                    Assert.AreEqual(buadRateToSetting, api.ReadBaudRateSetting());
                    api.Disconnect();
                }
            }

            if (excepectedBaudRateForTest != buadRateToSetting)
            {
                if (!Connect())
                    Assert.Fail();
                //先讀
                bool success = api.BaudRateSetting(excepectedBaudRateForTest);
                api.Disconnect();
                Assert.IsTrue(success);
            }

        }

        [TestMethod()]
        public void IntsToIntTest()
        {
        }

        [TestMethod()]
        public void GetSamplingRateTest()
        {
            Connect();
            Assert.AreEqual(5000, api.GetSamplingRate(false));
        }

        [TestMethod()]
        public void SetSamplingRateTest()
        {
            Task.Run(() =>
            {
                Connect();
                api.SetSamplingRate(3000);
            });
            while (true)
            {
                Thread.Sleep(1);
            }
        }
    }
    public static class Extension
    {
        internal static double[] ToIEEE754FloatAry(this int[] intAry)
        {
            if (intAry == null)
                return null;
            List<double> valuesList = new List<double>();
            if (intAry.Length == 1)
            {
                if (intAry[0] == -1)
                {
                    return new double[1] { -1 };
                }
                else if (intAry[0] == -2)
                {
                    return new double[1] { -2 };
                }
            }
            for (int i = 0; i < intAry.Length; i += 4)
            {
                var hexstring = intAry[i].ToString("X2") + intAry[i + 1].ToString("X2") + intAry[i + 2].ToString("X2") + intAry[i + 3].ToString("X2");
                double dVal = hexstring.ToFloat();
                valuesList.Add(dVal);
            }
            return valuesList.ToArray();
        }
        internal static byte[] ToByteAry(this int[] intAry)
        {
            List<byte> byteList = new List<byte>();
            for (int i = 0; i < intAry.Length; i++)
            {
                byteList.Add((byte)intAry[i]);
            }
            return byteList.ToArray();
        }

        static float ToFloat(this string Hex32Input)
        {
            double doubleout = 0.0;
            UInt64 bigendian;
            bool success = UInt64.TryParse(Hex32Input,
                System.Globalization.NumberStyles.HexNumber, null, out bigendian);
            if (success)
            {
                double fractionDivide = Math.Pow(2, 23);

                int sign = (bigendian & 0x80000000) == 0 ? 1 : -1;
                Int64 exponent = ((Int64)(bigendian & 0x7F800000) >> 23) - (Int64)127;
                UInt64 fraction = (bigendian & 0x007FFFFF);
                if (fraction == 0)
                    doubleout = sign * Math.Pow(2, exponent);
                else
                    doubleout = sign * (1 + (fraction / fractionDivide)) * Math.Pow(2, exponent);
            }
            return (float)doubleout;
        }
    }
}