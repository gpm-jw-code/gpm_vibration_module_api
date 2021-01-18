﻿using System;
using static gpm_vibration_module_api.clsEnum.Module_Setting_Enum;

namespace gpm_vibration_module_api
{
    /// <summary>
    /// 擴充功能
    /// </summary>
    internal static class ExtendMethods
    {
        internal static string ToCommaString(this byte[] byteAry)
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
    }
}