﻿using System;
using System.Collections.Generic;
using System.Text;

namespace gpm_vibration_module_api
{
    public class DataSet
    {
        internal bool IsReady = false;
        public int ErrorCode = 0;
        public clsAcc AccData = new clsAcc();
        public clsFFTData FFTData = new clsFFTData();
        public clsOtherFeatures Features = new clsOtherFeatures();
        public long TimeSpend;
        public class clsAcc : AxisListValue
        {
            public enum States
            {

            }
        }
        public class clsFFTData : AxisListValue
        {
            internal const double SamplingRate = 5000;
            public List<double> FreqsVec = new List<double>();
        }

        public class clsOtherFeatures
        {
            public clsAccRMS AccRMS = new clsAccRMS();
            public clsEnergy VibrationEnergy = new clsEnergy();
            public clsP2p AccP2P = new clsP2p();
            public class clsAccRMS : AxisDoubleValue
            {
            }
            public class clsEnergy : AxisDoubleValue
            {
            }
            public class clsP2p : AxisDoubleValue
            {

            }
        }

        public class AxisListValue
        {
            public List<double> X = new List<double>();
            public List<double> Y = new List<double>();
            public List<double> Z = new List<double>();
        }

        public class AxisDoubleValue
        {
            public double X = 0;
            public double Y = 0;
            public double Z = 0;
        }
    }
}