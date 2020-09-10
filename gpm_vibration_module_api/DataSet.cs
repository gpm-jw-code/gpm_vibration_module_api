using gpm_vibration_module_api.GpmMath;
using System;
using System.Collections.Generic;
using System.Text;

namespace gpm_vibration_module_api
{
  
    public class DataSet
    {
        public DataSet(double samplingRate)
        {
            this.FFTData.SamplingRate = samplingRate;
        }
        internal void AddWindow(double[] w)
        {
            for (int i = 0; i < AccData.X.Count; i++)
            {
                AccData.acc_x_with_window.Add(AccData.X[i] * w[i]);
                AccData.acc_y_with_window.Add(AccData.Y[i] * w[i]);
                AccData.acc_z_with_window.Add(AccData.Z[i] * w[i]);
            }
        }
        internal bool IsReady = false;
        public int ErrorCode = 0;
        public clsAcc AccData = new clsAcc();
        public clsFFTData FFTData = new clsFFTData();
        public clsFFTData MelBankData = new clsFFTData();
        public clsOtherFeatures Features = new clsOtherFeatures();
        public long TimeSpend;
        public DateTime RecieveTime;

        internal void AddData(DataSet NewData)
        {

            this.AccData.X.AddRange(NewData.AccData.X);
            this.AccData.Y.AddRange(NewData.AccData.Y);
            this.AccData.Z.AddRange(NewData.AccData.Z);
            this.FFTData.X.AddRange(NewData.FFTData.X);
            this.FFTData.Y.AddRange(NewData.FFTData.Y);
            this.FFTData.Z.AddRange(NewData.FFTData.Z);
        }

        internal void AddData(DataSet NewData, double[] _window)
        {
            AddData(NewData);
            NewData.AddWindow(_window);
            this.AccData.acc_x_with_window.AddRange(NewData.AccData.acc_x_with_window);
            this.AccData.acc_y_with_window.AddRange(NewData.AccData.acc_y_with_window);
            this.AccData.acc_z_with_window.AddRange(NewData.AccData.acc_z_with_window);
        }
        public class clsAcc : AxisListValue
        {
            internal List<double> acc_x_with_window = new List<double>();
            internal List<double> acc_y_with_window = new List<double>();
            internal List<double> acc_z_with_window = new List<double>();
            public enum States
            {

            }


        }
        public class clsFFTData : AxisListValue
        {
            internal double SamplingRate = 1001;
            private List<double> _FreqVec = new List<double>();
            public double Freq_Resolution { get; private set; } = 0;
            public List<double> FreqVec
            {
                get
                {

                    return _FreqVec;
                }
                set
                {
                    Freq_Resolution = SamplingRate / 2.0 / value.Count;
                    _FreqVec = value;
                }
            }
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