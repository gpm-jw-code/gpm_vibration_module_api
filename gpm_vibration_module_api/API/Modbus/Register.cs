//#define v110

namespace gpm_vibration_module_api.Modbus
{
    public partial class GPMModbusAPI
    {
        /// <summary>
        /// 暫存器位址設定(HEX)
        /// </summary>
        internal struct Register 
        {
            public const int VEValuesRegStartIndex = 0;
            public const int VEValuesRegLen = 6;

            public const int TotalVEValueRegStartIndex = 6;
            public const int TotalVEValueRegLen = 2;

            public const int RMSValuesRegStartIndex = 8;
            public const int RMSValuesRegLen = 6;

            public const int P2PValuesRegStartIndex = 14;
            public const int P2PValuesRegLen = 6;

            public const int AllValuesRegStartIndex = 0;
            public const int AllValuesRegLen = 20;

            public const int Velocity_RMSRegStartIndex = 22;
            public const int Velocity_RMSRegLen = 6;

            public const int Displacement_RMSRegStartIndex = 28;
            public const int Displacement_RMSRegLen = 6;

            public const int Velocity_P2PRegStartIndex = 34;
            public const int Velocity_P2PRegLen = 6;

            public const int Displacement_P2PRegStartIndex = 40;
            public const int Displacement_P2PRegLen = 6;
            //ID
            public const int IDRegIndex = 144;

            public const int RangeRegStart = 129;
            public const int BaudRateSetRegIndex = 146;

            /// <summary>
            /// Sampling Rate 儲存位址(Support Function Code s03 & 06)
            /// </summary>
            public const int SamplingRateReg = 148; 

        }

    }
}