using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gpm_vibration_module_api
{
    public class Enviroment
    {
        internal static bool IsNoNeedKey = false;
        public static string SECRET
        {
            set
            {
                IsNoNeedKey = (value == "30ffc644-d304-47e6-9924-5b1ce7e3ca03");
            }
        }
    }
}
