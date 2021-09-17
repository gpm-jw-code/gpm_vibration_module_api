namespace gpm_vibration_module_api
{
    public static class clsErrorCode
    {

        public enum Error
        {
            None = 0,
            TrialTimeEnd = 401,
            KEYPRO_NOT_FOUND = 404,
            IPIllegal = 601,
            PortIllegal = 602,
            CONNECT_FAIL = 603,
            SerialPortOpenFail = 2603,
            DATA_GET_INTERUPT = 144448,
            NoConnection = 604,
            DATA_GET_TIMEOUT = 605,
            PARAM_HS_TIMEOUT = 606,
            SensorNoConnection = 607,
            SensorBroken = 608,
            LicenseFileNoExist = 704,
            LicenseExpired = 705,
            LicenseCheckFail = 706,
            SelfTestFail = 1506,
            ConnectFail_HostNoReply = 16606,
            ModuleIsBusy = 16607,
            ERROR_PARAM_RETURN_FROM_CONTROLLER = 16608,
            SYSTEM_ERROR = 144444,
            DATA_LENGTH_SETTING_VALUE_ILLEGAL = 144445,
            Cancel = 144446,
            ParticleSensorConvertError_Source_Data_Insufficient = 52110,
            PostProcessingError = 144447,
            MRSettingOutOfRange = 144449,
            /// <summary>
            /// 量測範圍尚未進行設定,
            /// </summary>
            VibrationMeasureRangeNotSetYet = 144450,
            ChannelNotExist = 144451,
        }

    }
}
