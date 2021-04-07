using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gpm_vibration_module_api.sys
{
    public class Settings_Items
    {
        public Settings_Items()
        {
            //Settings_Ctrl.Log_Config();
        }
        public bool is_write_log_to_HardDisk = false;
        public int sampling_rate_of_vibration_sensor = 4500;
        public int parma_write_fail_retry_time = 1;
    }
}
