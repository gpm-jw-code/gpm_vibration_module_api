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
            return HB + (sbyte)LB * 256;
        }

        /// <summary>
        /// 將加速度封包轉換成List<double></double>
        /// </summary>
        /// <param name="AccPacket"></param>
        /// <returns></returns>
        internal static List<List<double>> AccPacketToListDouble(byte[] AccPacket, clsEnum.Module_Setting_Enum.MEASURE_RANGE measureRange, clsEnum.FWSetting_Enum.ACC_CONVERT_ALGRIUM convertAlgrium)
        {
            Console.WriteLine($"Algrium:{convertAlgrium.ToString()}");
            var N = AccPacket.Length / 6;
            var LSB = Convert.ToInt32(measureRange);
            List<double> Gx = new List<double>();
            List<double> Gy = new List<double>();
            List<double> Gz = new List<double>();
            int splitIndex = -1;
            // 0 0 X X 0 0 0 0 0 0 X X 0 0 0 0 0 0 X X 
            // 2?   => 4 , 12 , 20, 28

            // 0 X X 0 0 0 0 0 0 X X 0 0 0 0 0 0 X X 
            // 1?   => 3, 11 , 19 
            // 0 8 16

            //var s = splitIndex < 6 ? :;
            if (convertAlgrium == clsEnum.FWSetting_Enum.ACC_CONVERT_ALGRIUM.Bulk)
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
            else
                for (int i = 0; i < N; i++)
                {
                    if (convertAlgrium == clsEnum.FWSetting_Enum.ACC_CONVERT_ALGRIUM.Old)
                    {
                        Gx.Add(bytesToDouble(AccPacket[N * 0 + i], AccPacket[N * 1 + i]) / LSB);
                        Gy.Add(bytesToDouble(AccPacket[N * 2 + i], AccPacket[N * 3 + i]) / LSB);
                        Gz.Add(bytesToDouble(AccPacket[N * 4 + i], AccPacket[N * 5 + i]) / LSB);
                    }
                    else
                    {
                        Gx.Add(bytesToDouble(AccPacket[(6 * i) + 0], AccPacket[(6 * i) + 1]) / LSB);
                        Gy.Add(bytesToDouble(AccPacket[(6 * i) + 2], AccPacket[6 * i + 3]) / LSB);
                        Gz.Add(bytesToDouble(AccPacket[(6 * i) + 4], AccPacket[(6 * i) + 5]) / LSB);
                    }
                }

            return new List<List<double>> { Gx, Gy, Gz };
        }

        internal static List<double> BytesToXYZAccData(byte[] XYZBytes, clsEnum.Module_Setting_Enum.MEASURE_RANGE measureRange)
        {
            List<double> XYZData = new List<double>();
            var LSB = Convert.ToInt32(measureRange);
            XYZData.Add(bytesToDouble(XYZBytes[0], XYZBytes[1]) / LSB);
            XYZData.Add(bytesToDouble(XYZBytes[2], XYZBytes[3]) / LSB);
            XYZData.Add(bytesToDouble(XYZBytes[4], XYZBytes[5]) / LSB);
            return XYZData;
        }
    }
}
