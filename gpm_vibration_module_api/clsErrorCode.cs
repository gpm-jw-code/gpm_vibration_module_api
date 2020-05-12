using System;
using System.Collections.Generic;
using System.Text;

namespace gpm_vibration_module_api
{
    public static class clsErrorCode
    {

        public enum Error
        {
            ConnectFail = 603,
            DataGetTimeout = 605,
            ParametersSettingTimeout = 606,
            KeyproNotFound = 404,
            TrialTimeEnd = 401,
            NoConnection = 604,
            ConnectFail_HostNoReply = 16606,
            PortIllegal = 602,
            IPIllegal = 601,
            SelfTestFail = 506,
            ModuleIsBusy = 16607
        }

    }
}
