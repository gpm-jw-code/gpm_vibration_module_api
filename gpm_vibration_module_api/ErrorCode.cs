using System;
using System.Collections.Generic;
using System.Text;

namespace gpm_module_api
{

    public enum CodeError
    {
        TrialTimeEnd = 401, KEYPRO_NOT_MATCH = 403,
        KEYPRO_NOT_FOUND = 404,

        IPIllegal = 601,
        PortIllegal = 602,
        CONNECT_FAIL = 603,
        NoConnection = 604,
        DATA_GET_TIMEOUT = 605,
        PARAM_SET_TIMEOUT = 606,
        SelfTestFail = 1506,
        ConnectFail_HostNoReply = 16606,
        ModuleIsBusy = 16607,
        ERROR_PARAM_RETURN_FROM_CONTROLLER = 16608,
        SYSTEM_ERROR = 144444,
        ParticleSensorConvertError_Source_Data_Insufficient = 52110,
        LicenseFileNoExist = 704,
        LicenseExpired = 705,
        LicenseCheckFail = 706,
    }

}
