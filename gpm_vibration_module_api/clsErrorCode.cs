namespace gpm_vibration_module_api
{
    public static class clsErrorCode
    {

        public enum Error
        {
            CONNECT_FAIL = 603,
            DATA_GET_TIMEOUT = 605,
            PARAM_SET_TIMEOUT = 606,
            KEYPRO_NOT_FOUND = 404,
            TrialTimeEnd = 401,
            NoConnection = 604,
            ConnectFail_HostNoReply = 16606,
            PortIllegal = 602,
            IPIllegal = 601,
            SelfTestFail = 1506,
            ModuleIsBusy = 16607,
            ERROR_PARAM_RETURN_FROM_CONTROLLER = 16608,
            SYSTEM_ERROR = 144444,
            DATA_LENGTH_SETTING_VALUE_ILLEGAL = 144445,
            ParticleSensorConvertError_Source_Data_Insufficient = 52110,
            LicenseFileNoExist = 704,
            LicenseExpired = 705,
            LicenseCheckFail = 706,
            Cancel = 144446,
        }

    }
}
