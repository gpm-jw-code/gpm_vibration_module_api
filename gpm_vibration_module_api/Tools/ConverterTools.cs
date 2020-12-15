using gpm_module_api.ParticalSensor;
using System;
using System.Collections.Generic;
using System.Text;

namespace gpm_vibration_module_api.Tools
{
    internal static class ConverterTools
    {
        /// <summary>
        /// 將兩個Byte組合成Double
        /// </summary>
        /// <param name="HB"></param>
        /// <param name="LB"></param>
        /// <returns></returns>
        internal static double bytesToDouble(byte HB, byte LB)
        {
            return HB + (sbyte) LB * 256;
        }

        /// <summary>
        /// 將加速度封包轉換成List<double></double>
        /// </summary>
        /// <param name="AccPacket"></param>
        /// <returns></returns>
        public static List<List<double>> AccPacketToListDouble(byte[] AccPacket, clsEnum.Module_Setting_Enum.MEASURE_RANGE measureRange, DAQMode dAQMode)
        {
            //Console.WriteLine($"Algrium:{convertAlgrium.ToString()}");
            var N = AccPacket.Length / 6;
            var LSB = Convert.ToInt32(measureRange);
            List<double> Gx = new List<double>();
            List<double> Gy = new List<double>();
            List<double> Gz = new List<double>();
            int splitIndex = -1;

            if (dAQMode == DAQMode.BULK)
            {
                for (int i = 0; true; i++)
                {
                    if (AccPacket[i] == 13)
                    {
                        if (AccPacket[i + 1] == 10)
                        {
                            splitIndex = i;
                            break;
                        }
                    }
                }
                for (int i = 0; i < N; i++)
                {
                    Gx.Add(bytesToDouble(AccPacket[(8 * i) + 0], AccPacket[(8 * i) + 1]) / LSB);
                    Gy.Add(bytesToDouble(AccPacket[(8 * i) + 2], AccPacket[(8 * i) + 3]) / LSB);
                    Gz.Add(bytesToDouble(AccPacket[(8 * i) + 4], AccPacket[(8 * i) + 5]) / LSB);
                }
            }
            else if (dAQMode == DAQMode.High_Sampling)
            {
                for (int i = 0; i < N; i++)
                {
                    Gx.Add(bytesToDouble(AccPacket[N * 0 + i], AccPacket[N * 1 + i]) / LSB);
                    Gy.Add(bytesToDouble(AccPacket[N * 2 + i], AccPacket[N * 3 + i]) / LSB);
                    Gz.Add(bytesToDouble(AccPacket[N * 4 + i], AccPacket[N * 5 + i]) / LSB);
                }
            }
            else if (dAQMode ==  DAQMode.Low_Sampling)
            {
                for (int i = 0; i < N; i++)
                {
                    //0,1
                    //2,3
                    //4,5
                    Gx.Add(bytesToDouble(AccPacket[(6 * i) + 0], AccPacket[(6 * i) + 1]) / LSB);
                    Gy.Add(bytesToDouble(AccPacket[(6 * i) + 2], AccPacket[6 * i + 3]) / LSB);
                    Gz.Add(bytesToDouble(AccPacket[(6 * i) + 4], AccPacket[(6 * i) + 5]) / LSB);
                }
            }
            return new List<List<double>> { Gx, Gy, Gz };
        }
        internal static double[] PacketToVEVals(byte[] Packet)
        {

            if (Packet.Length != 24)
                throw new Exception("The lenth of input arrary should be '8' ");
            var _str = Encoding.ASCII.GetString(Packet);
            string[] split = _str.Split(',');

            try
            {
                var ve_x = Convert.ToDouble(split[0]);
                var ve_y = Convert.ToDouble(split[1]);
                var ve_z = Convert.ToDouble(split[2]);
                return new double[] { ve_x, ve_y, ve_z };
            }
            catch (Exception ex)
            {
                return new double[] { 0, 0, 0 };
            }
        }

        /// <summary>
        /// 將封包數據轉為UV Sensor DATASET
        /// </summary>
        /// <param name="dataByteAry"></param>
        /// <returns></returns>
        public static UVDataSet UVPacketToDatatSet(byte[] dataByteAry)
        {
            if (dataByteAry.Length < 4)
                return new UVDataSet( (int)clsErrorCode.Error.ParticleSensorConvertError_Source_Data_Insufficient);
            UVDataSet dataSet = new UVDataSet(0);
            byte hb = dataByteAry[0];
            byte Lb = dataByteAry[1];
            double uv_val = hb * 256 + Lb;
            dataSet.UVValue = uv_val;
            return dataSet;
        }
        /// <summary>
        /// 將封包數據轉為Partical Sensor DataSet
        /// </summary>
        /// <param name="dataByteAry"></param>
        /// <returns></returns>
        public static ParticleDataSet ParticalPacketToDataSet(byte[] dataByteAry)
        {
            if (dataByteAry.Length < 62)
                return new ParticleDataSet((int)clsErrorCode.Error.ParticleSensorConvertError_Source_Data_Insufficient);
            ParticleDataSet dataSet = new ParticleDataSet();
            byte[] ParticleBytes = new byte[40];
            Array.Copy(dataByteAry, 0, ParticleBytes, 0, ParticleBytes.Length);
            List<double> ValList = new List<double>();
            for (int i = 0; i < 40; i += 4)
            {
                byte[] bytes = new byte[4] { ParticleBytes[i], ParticleBytes[i + 1], ParticleBytes[i + 2], ParticleBytes[i + 3] };
                ValList.Add(Math.Round(IEE764(bytes), 2));
            }
            dataSet.ParticalValueDict = new Dictionary<PARTICLE_SIZE, Concentration>()
            {
                { PARTICLE_SIZE.PM1 , new Concentration{ Mass=ValList[0] , Number =ValList[5], PARTICLE_TYPE= PARTICLE_SIZE.PM1 } },
                { PARTICLE_SIZE.PM2Dot5 , new Concentration{ Mass=ValList[1] , Number =ValList[6], PARTICLE_TYPE= PARTICLE_SIZE.PM2Dot5 } },
                { PARTICLE_SIZE.PM4 , new Concentration{ Mass=ValList[2] , Number =ValList[7], PARTICLE_TYPE= PARTICLE_SIZE.PM4 } },
                { PARTICLE_SIZE.PM10 , new Concentration{ Mass=ValList[3] , Number =ValList[8], PARTICLE_TYPE= PARTICLE_SIZE.PM10 } },
                { PARTICLE_SIZE.PM0Dot5 , new Concentration{ Mass=-999 , Number =ValList[4], PARTICLE_TYPE= PARTICLE_SIZE.PM0Dot5 } },
            };
            dataSet.TypicalParticleSize = ValList[9];
            dataSet.Temperature = ByteToDouble(new byte[] { dataByteAry[40], dataByteAry[41] }) / 100.0;
            dataSet.Humidity = ByteToDouble(new byte[] { dataByteAry[42], dataByteAry[43] }) / 100.0;
            dataSet.Illuminance = ByteToDouble(new byte[] { dataByteAry[44], dataByteAry[45] });
            //dataSet.Temperature = ByteToDouble(new byte[] { dataByteAry[40] ,dataByteAry[41] });
            //dataSet.Temperature = ByteToDouble(new byte[] { dataByteAry[40] ,dataByteAry[41] });
            return dataSet;
        }

        private static double ByteToDouble(byte[] _dataBytes)
        {
            byte hb = _dataBytes[0];
            byte Lb = _dataBytes[1];
            return hb * 256 + Lb;
            //Covert to double (count num)
        }


        public static float IEE764(byte[] bytes)
        {
            int data1 = bytes[0];
            int data2 = bytes[1];
            int data3 = bytes[2];
            int data4 = bytes[3];

            int data = data1 << 24 | data2 << 16 | data3 << 8 | data4;

            int nSign;
            if ((data & 0x80000000) > 0)
            {
                nSign = -1;
            }
            else
            {
                nSign = 1;
            }
            int nExp = data & (0x7F800000);
            nExp = nExp >> 23;
            float nMantissa = data & (0x7FFFFF);

            if (nMantissa != 0)
                nMantissa = 1 + nMantissa / 8388608;
            return nSign * nMantissa * (2 << (nExp - 128));
        }
        //internal static List<double> BytesToXYZAccData(byte[] XYZBytes, clsEnum.Module_Setting_Enum.MEASURE_RANGE measureRange)
        //{
        //    List<double> XYZData = new List<double>();
        //    var LSB = Convert.ToInt32(measureRange);
        //    XYZData.Add(bytesToDouble(XYZBytes[0], XYZBytes[1]) / LSB);
        //    XYZData.Add(bytesToDouble(XYZBytes[2], XYZBytes[3]) / LSB);
        //    XYZData.Add(bytesToDouble(XYZBytes[4], XYZBytes[5]) / LSB);
        //    return XYZData;
        //}
        //internal static List<double> BytesToXYZAccData(byte[] XYZBytes, clsEnum.Module_Setting_Enum.MEASURE_RANGE measureRange)
        //{
        //    List<double> XYZData = new List<double>();
        //    var LSB = Convert.ToInt32(measureRange);
        //    XYZData.Add(bytesToDouble(XYZBytes[0], XYZBytes[1]) / LSB);
        //    XYZData.Add(bytesToDouble(XYZBytes[2], XYZBytes[3]) / LSB);
        //    XYZData.Add(bytesToDouble(XYZBytes[4], XYZBytes[5]) / LSB);
        //    return XYZData;
        //}
    }
}
