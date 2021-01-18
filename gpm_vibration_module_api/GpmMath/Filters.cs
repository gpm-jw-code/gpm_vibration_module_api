﻿using Accord.Audio.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gpm_vibration_module_api.GpmMath
{
    internal class Filters
    {
        /// <summary>
        /// 通過低通濾波器
        /// </summary>
        internal static List<List<double>> LPF(List<List<double>> datas, double CutoffFreq, double SamplingRate)
        {
            LowPassFilter passFilter = new LowPassFilter(CutoffFreq,SamplingRate);

            for (int i = 0; i < 3; i++)
            {
                List<double> real_data = datas[i];
                List<double> y = new List<double>() { real_data[0] };
                for (int t = 1; t < real_data.Count; t++)
                {
                    var _y = y[t - 1] + passFilter.Alpha * (real_data[t] - y[t - 1]);
                    y.Add(_y);
                }
                datas[i] = y;
            }

            return datas;
        }
    }
}