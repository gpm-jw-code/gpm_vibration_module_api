using System;
using System.Collections.Generic;
using System.Text;

namespace gpm_vibration_module_api
{
    public class MeasureOption
    {
        public int WindowSize = 512;
        public int TimePeriod = 10;

        public string _Description
        {
            get
            {
                return $"WindowSize:{WindowSize}, TimePeriod:{TimePeriod}";
            }
        }

    }
}
