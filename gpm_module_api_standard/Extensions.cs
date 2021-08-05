using System;
using System.Collections.Generic;
using static gpm_vibration_module_api.clsEnum.Module_Setting_Enum;

namespace gpm_vibration_module_api
{
    /// <summary>
    /// 擴充功能
    /// </summary>
    public static class ExtendMethods
    {
        internal static string To8BitString(this string string_leakZero)
        {
            var zero_to_add_num = 8 - string_leakZero.Length;
            string zerostr = "";
            for (int i = 0; i < zero_to_add_num; i++)
            {
                zerostr += "0";
            }
            return zerostr + string_leakZero;
        }
        internal static string ToCommaHexString(this byte[] byteAry)
        {
            string str = "";
            foreach (var byt in byteAry)
            {
                str += $"0x{byt.ToString("X2")},";
            }
            return str;
        }

        internal static byte[] ToHLBytes(this int _int)
        {
            byte[] bytes = BitConverter.GetBytes(_int);
            return new byte[2] { bytes[1], bytes[0] };
        }

        internal static byte ToKXByte(this MEASURE_RANGE range_set)
        {
            byte b = 0xC0;
            switch (range_set)
            {
                case MEASURE_RANGE.MR_8G:
                    b = 0xC0;
                    break;
                case MEASURE_RANGE.MR_16G:
                    b = 0XC8;
                    break;
                case MEASURE_RANGE.MR_32G:
                    b = 0XD0;
                    break;
                case MEASURE_RANGE.MR_64G:
                    b = 0XD8;
                    break;
                default:
                    b = 0XC0;
                    break;
            }
            return b;

        }

        internal static byte ToGENByte(this MEASURE_RANGE range_set)
        {
            byte b = 0xC0;
            switch (range_set)
            {
                case MEASURE_RANGE.MR_2G:
                    b = 0x00;
                    break;
                case MEASURE_RANGE.MR_4G:
                    b = 0X10;
                    break;
                case MEASURE_RANGE.MR_8G:
                    b = 0X20;
                    break;
                case MEASURE_RANGE.MR_16G:
                    b = 0X30;
                    break;
                default:
                    b = 0X00;
                    break;
            }
            return b;

        }

        internal static MEASURE_RANGE ToMeasureGENRange(this byte mrByte)
        {
            switch (mrByte)
            {
                case 0x00:
                    return MEASURE_RANGE.MR_2G;
                case 0x10:
                    return MEASURE_RANGE.MR_4G;
                case 0x20:
                    return MEASURE_RANGE.MR_8G;
                case 0x30:
                    return MEASURE_RANGE.MR_16G;
                default:
                    return MEASURE_RANGE.MR_2G;
            }
        }
        internal static double ToRMS(this List<double> data)
        {
            var sum = 0.0;
            for (int i = 0; i < data.Count; i++)
                sum += Math.Pow(data[i], 2);
            return Math.Sqrt(sum / (double)data.Count);
        }
        public static List<double> ToDoubleList(this List<float> floatList)
        {
            List<double> doubleList = new List<double>();
            foreach (var floatval in floatList)
                doubleList.Add(floatval);
            return doubleList;
        }

        public static List<float> ToFloatList(this List<double> doubleList)
        {
            List<float> floatList = new List<float>();
            foreach (var doubleval in doubleList)
                floatList.Add((float)doubleval);
            return floatList;
        }
    }
}
