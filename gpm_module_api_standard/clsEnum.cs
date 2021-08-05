namespace gpm_vibration_module_api
{
    public class clsEnum
    {
        public enum MODULE_TYPE
        {
            VIBRATION,
            UV,
            PARTICAL,
            UNKNOW
        }
        internal enum ControllerCommand
        {
            READVALUE, READSTVAL, BULKVALUE, BULKBREAK
        }
        public struct Module_Setting_Enum
        {
            public enum SENSOR_TYPE
            {
                High = 2, Genernal = 1
            }

            public enum DATA_LENGTH
            {
                none = 0, x1 = 512, x2 = 1024, x4 = 2048, x8 = 4096, x16 = 8192, Others
            }

            public enum ODR
            {
                _9F = 159, _87 = 135
            }

            public enum MEASURE_RANGE
            {
                MR_2G = 16384,
                MR_4G = 8192,
                MR_8G = 4096,
                MR_16G = 2048,
                MR_32G = 1024,
                MR_64G = 512,
            }
        }
        /// <summary>
        /// GPM專用韌體ENUM
        /// </summary>
        internal struct FWSetting_Enum
        {
            internal enum ACC_CONVERT_ALGRIUM
            {
                Old, New, Bulk
            }

        }

    }
}
