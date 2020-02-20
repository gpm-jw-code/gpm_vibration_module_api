using System;
using System.Collections.Generic;
using System.Text;

namespace gpm_vibration_module_api
{
    public static class clsEnum
    {
        public enum ControllerCommand
        {
            READVALUE, READSTVAL
        }
        public struct Module_Setting_Enum
        {
            public enum SensorType
            {
                High = 2, Genernal = 1
            }

            public enum DataLength
            {
                x1 = 512, x2 = 1024, x4 = 2048, x8 = 4096
            }

            public enum ODR
            {
                _9F = 159, _87 = 135
            }

            public enum MeasureRange
            {
                MR_2G = 16384,
                MR_4G = 8192,
                MR_8G = 4096,
                MR_16G = 2048,
            }
        }
    }
}
