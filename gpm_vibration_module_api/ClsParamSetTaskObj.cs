using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gpm_vibration_module_api
{
    internal class ClsParamSetTaskObj
    {
        public readonly DAQMode DAQMode;

        public ClsParamSetTaskObj(DAQMode dAQMode)
        {
            DAQMode = dAQMode;
        }
        public object SettingItem;
        public object SettingValue;
    }
}
