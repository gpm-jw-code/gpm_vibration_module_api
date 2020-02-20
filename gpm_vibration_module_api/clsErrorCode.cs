using System;
using System.Collections.Generic;
using System.Text;

namespace gpm_vibration_module_api
{
    public static class clsErrorCode
    {

        public enum Error
        {
            ConnectFail=1404,
            Timeout,
            KeyproNotFound,
            TrialTimeEnd,
            ModelSaveError,
            ModelDeleteError,
            ModelNoExist = 2040,
            ConnectFail_HostNoReply = 16606
        }
    }
}
