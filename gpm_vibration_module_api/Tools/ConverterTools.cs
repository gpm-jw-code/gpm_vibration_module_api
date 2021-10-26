using gpm_module_api.ParticalSensor;
using System;
using System.Collections.Generic;
using System.Text;
using static gpm_vibration_module_api.DataSet;

namespace gpm_vibration_module_api.Tools
{
    public static class ConverterTools
    {
        /// <summary>
        /// 將兩個Byte組合成Double
        /// </summary>
        /// <param name="HB"></param>
        /// <param name="LB"></param>
        /// <returns></returns>
        internal static double bytesToDouble(byte LB, byte HB)
        {
            return (sbyte)HB * 256 + LB;
        }
        public static List<List<double>> AccPacketToListDouble(byte[] AccPacket, clsEnum.Module_Setting_Enum.MEASURE_RANGE measureRange, DAQMode dAQMode, bool Noise_remove=false)
        {
            ADCError _ADCError = null;
            return AccPacketToListDouble(AccPacket, out _ADCError, measureRange, dAQMode, Noise_remove);
        }
        /// <summary>
        /// 將加速度封包轉換成List<double></double>
        /// </summary>
        /// <param name="AccPacket"></param>
        /// <returns></returns>
        public static List<List<double>> AccPacketToListDouble(byte[] AccPacket, out ADCError _ADCError, clsEnum.Module_Setting_Enum.MEASURE_RANGE measureRange, DAQMode dAQMode, bool noise_remove=false)
        {
            _ADCError = new ADCError();
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
            else if (dAQMode == DAQMode.Low_Sampling)
            {
                for (int i = 0; i < N; i++)
                {
                    var XHB = AccPacket[(6 * i) + 1];
                    var XLB = AccPacket[(6 * i) + 0];
                    var YHB = AccPacket[6 * i + 3];
                    var YLB = AccPacket[6 * i + 2];
                    var ZHB = AccPacket[(6 * i) + 5];
                    var ZLB = AccPacket[(6 * i) + 4];

                    if (XHB == 0xFF)
                        _ADCError.X.Add(new ErrorValue() { Index = i });
                    if (YHB == 0xFF)
                        _ADCError.Y.Add(new ErrorValue() { Index = i });
                    if (ZHB == 0xFF)
                        _ADCError.Z.Add(new ErrorValue() { Index = i });

                    Gx.Add(bytesToDouble(XLB, XHB) / LSB);
                    Gy.Add(bytesToDouble(YLB, YHB) / LSB);
                    Gz.Add(bytesToDouble(ZLB, ZHB) / LSB);
                }
            }

            if (noise_remove)
            {
                for (int i = 0; i < Gx.Count; i++)
                {
                    if (i == 0 | i==Gx.Count-1)
                    {
                        int index = i == 0 ? 1 : -1;
                        Gx[i] = Math.Abs((Gx[i] - Gx[i + index])) > 0.2 ? Gx[i + index] : Gx[i];
                        Gy[i] = Math.Abs((Gy[i] - Gy[i + index])) > 0.2 ? Gy[i + index] : Gy[i];
                        Gz[i] = Math.Abs((Gz[i] - Gz[i + index])) > 0.2 ? Gz[i + index] : Gz[i];
                    }
                    else
                    {
                        Gx[i] = Math.Abs((Gx[i] - Gx[i + 1])) > 0.2 && Math.Abs((Gx[i] - Gx[i - 1])) > 0.2 ? Gx[i - 1] : Gx[i];
                        Gy[i] = Math.Abs((Gy[i] - Gy[i + 1])) > 0.2 && Math.Abs((Gy[i] - Gy[i - 1])) > 0.2 ? Gy[i - 1] : Gy[i];
                        Gz[i] = Math.Abs((Gz[i] - Gz[i + 1])) > 0.2 && Math.Abs((Gz[i] - Gz[i - 1])) > 0.2 ? Gz[i - 1] : Gz[i];
                    }
                }
            }
            return new List<List<double>> { Gx, Gy, Gz };
        }


        /// <summary>
        /// </summary>
        /// <param name="AccPacket"></param>
        /// <param name="measureRange"></param>
        /// <param name="dAQMode"></param>
        /// <param name="min_axis_sample_num"></param>
        /// <param name="High_Rich_Data"></param>
        /// <returns></returns>
        public static List<List<double>> AccPacketToListDouble(byte[] AccPacket, out ADCError _ADCError, clsEnum.Module_Setting_Enum.MEASURE_RANGE measureRange, DAQMode dAQMode, int min_axis_sample_num = 100, bool High_Rich_Data = false)
        {
            _ADCError = new ADCError();
            //Console.WriteLine($"Algrium:{convertAlgrium.ToString()}");
            var min_single_axes_sample_num = AccPacket.Length / 6;
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
                for (int i = 0; i < min_single_axes_sample_num; i++)
                {
                    Gx.Add(bytesToDouble(AccPacket[(8 * i) + 0], AccPacket[(8 * i) + 1]) / LSB);
                    Gy.Add(bytesToDouble(AccPacket[(8 * i) + 2], AccPacket[(8 * i) + 3]) / LSB);
                    Gz.Add(bytesToDouble(AccPacket[(8 * i) + 4], AccPacket[(8 * i) + 5]) / LSB);
                }
            }
            else if (dAQMode == DAQMode.High_Sampling)
            {

                if (High_Rich_Data == false)
                    for (int i = 0; i < min_single_axes_sample_num; i++)
                    {
                        Gx.Add(bytesToDouble(AccPacket[min_single_axes_sample_num * 0 + i], AccPacket[min_single_axes_sample_num * 1 + i]) / LSB);
                        Gy.Add(bytesToDouble(AccPacket[min_single_axes_sample_num * 2 + i], AccPacket[min_single_axes_sample_num * 3 + i]) / LSB);
                        Gz.Add(bytesToDouble(AccPacket[min_single_axes_sample_num * 4 + i], AccPacket[min_single_axes_sample_num * 5 + i]) / LSB);
                    }
                else
                {
                    min_single_axes_sample_num = 512;
                    var pN = AccPacket.Length / 3072; //幾倍
                    for (int n = 0; n < pN; n++)
                    {
                        byte[] single_bytes = new byte[3072];
                        //copy
                        Array.Copy(AccPacket, (n * 3072), single_bytes, 0, 3072);
                        for (int i = 0; i < 512; i++)
                        {
                            Gx.Add(bytesToDouble(single_bytes[min_single_axes_sample_num * 0 + i], single_bytes[min_single_axes_sample_num * 1 + i]) / LSB);
                            Gy.Add(bytesToDouble(single_bytes[min_single_axes_sample_num * 2 + i], single_bytes[min_single_axes_sample_num * 3 + i]) / LSB);
                            Gz.Add(bytesToDouble(single_bytes[min_single_axes_sample_num * 4 + i], single_bytes[min_single_axes_sample_num * 5 + i]) / LSB);
                        }
                    }
                }
            }
            else if (dAQMode == DAQMode.Low_Sampling)
            {
                //for (int i = 0; i < N; i++)
                //{
                //    Gx.Add(bytesToDouble(AccPacket[(6 * i) + 0], AccPacket[(6 * i) + 1]) / LSB);
                //    Gy.Add(bytesToDouble(AccPacket[(6 * i) + 2], AccPacket[6 * i + 3]) / LSB);
                //    Gz.Add(bytesToDouble(AccPacket[(6 * i) + 4], AccPacket[(6 * i) + 5]) / LSB);
                //}

                min_single_axes_sample_num = min_axis_sample_num;
                var min_single_packet_len = min_single_axes_sample_num * 6;
                var pN = AccPacket.Length / (min_single_packet_len); //幾倍
                for (int n = 0; n < pN; n++)
                {
                    byte[] single_bytes = new byte[min_single_packet_len];
                    //copy
                    Array.Copy(AccPacket, (n * min_single_packet_len), single_bytes, 0, min_single_packet_len);
                    for (int i = 0; i < min_single_axes_sample_num; i++)
                    {
                        var XHB = single_bytes[min_single_axes_sample_num * 1 + i];
                        var XLB = single_bytes[min_single_axes_sample_num * 0 + i];
                        var YHB = single_bytes[min_single_axes_sample_num * 3 + i];
                        var YLB = single_bytes[min_single_axes_sample_num * 2 + i];
                        var ZHB = single_bytes[min_single_axes_sample_num * 5 + i];
                        var ZLB = single_bytes[min_single_axes_sample_num * 4 + i];

                        if (XHB == 0xFF)
                            _ADCError.X.Add(new ErrorValue() { Index = i });
                        if (YHB == 0xFF)
                            _ADCError.Y.Add(new ErrorValue() { Index = i });
                        if (ZHB == 0xFF)
                            _ADCError.Z.Add(new ErrorValue() { Index = i });
                        Gx.Add(bytesToDouble(XLB, XHB) / LSB);
                        Gy.Add(bytesToDouble(YLB, YHB) / LSB);
                        Gz.Add(bytesToDouble(ZLB, ZHB) / LSB);
                    }
                }
            }
            return new List<List<double>> { Gx, Gy, Gz };
        }


        public static List<List<double>> AccPacketToListDouble_KX134(byte[] AccPacket, int LSB, int min_axis_sample_num = 128, bool noise_remove = false)
        {
            var min_single_axes_sample_num = AccPacket.Length / 6;
            List<double> Gx = new List<double>();
            List<double> Gy = new List<double>();
            List<double> Gz = new List<double>();

            min_single_axes_sample_num = min_axis_sample_num;
            var min_single_packet_len = min_single_axes_sample_num * 6;
            var pN = AccPacket.Length / (min_single_packet_len); //幾倍
            for (int n = 0; n < pN; n++)
            {
                byte[] single_bytes = new byte[min_single_packet_len];
                //copy
                Array.Copy(AccPacket, (n * min_single_packet_len), single_bytes, 0, min_single_packet_len);
                for (int i = 0; i < min_single_axes_sample_num; i++)
                {
                    var XHB = single_bytes[min_single_axes_sample_num * 1 + i];
                    var XLB = single_bytes[min_single_axes_sample_num * 0 + i];
                    var YHB = single_bytes[min_single_axes_sample_num * 3 + i];
                    var YLB = single_bytes[min_single_axes_sample_num * 2 + i];
                    var ZHB = single_bytes[min_single_axes_sample_num * 5 + i];
                    var ZLB = single_bytes[min_single_axes_sample_num * 4 + i];

                    Gx.Add(bytesToDouble(XLB, XHB) / LSB);
                    Gy.Add(bytesToDouble(YLB, YHB) / LSB);
                    Gz.Add(bytesToDouble(ZLB, ZHB) / LSB);
                }
            }
            if (noise_remove)
            {
                for (int i = 0; i < Gx.Count; i++)
                {
                    if (i == 0)
                    {
                        Gx[i] = Math.Abs((Gx[i] - Gx[i + 1])) > 0.2 ? Gx[i + 1] : Gx[i];
                        Gy[i] = Math.Abs((Gy[i] - Gy[i + 1])) > 0.2 ? Gy[i + 1] : Gy[i];
                        Gz[i] = Math.Abs((Gz[i] - Gz[i + 1])) > 0.2 ? Gz[i + 1] : Gz[i];
                    }
                    else
                    {
                        Gx[i] = Math.Abs((Gx[i] - Gx[i + 1])) > 0.2 && Math.Abs((Gx[i] - Gx[i - 1])) > 0.2 ? Gx[i - 1] : Gx[i];
                        Gy[i] = Math.Abs((Gy[i] - Gy[i + 1])) > 0.2 && Math.Abs((Gy[i] - Gy[i - 1])) > 0.2 ? Gy[i - 1] : Gy[i];
                        Gz[i] = Math.Abs((Gz[i] - Gz[i + 1])) > 0.2 && Math.Abs((Gz[i] - Gz[i - 1])) > 0.2 ? Gz[i - 1] : Gz[i];
                    }
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
                return new UVDataSet((int)clsErrorCode.Error.ParticleSensorConvertError_Source_Data_Insufficient);
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
