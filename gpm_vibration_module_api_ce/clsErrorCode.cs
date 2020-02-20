using System;
using System.Collections.Generic;
using System.Text;

namespace gpm_vibration_module_api
{
    public static class clsErrorCode
    {
        public enum Error
        {
            ConnectFail,
            Timeout,
            KeyproNotFound,
            TrialTimeEnd
        }
    }
}
