using gpm_module_api.ParticalSensor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace gpm_vibration_module_api
{

    public class DataSet
    {
        private const string DataSaveDir = "./Log/Data/";
        public DataSet(double samplingRate)
        {
            this.FFTData.SamplingRate = samplingRate;
        }
        public DataSet() { }
        internal void AddWindow(double[] w)
        {
            for (int i = 0; i < AccData.X.Count; i++)
            {
                AccData.acc_x_with_window.Add(AccData.X[i] * w[i]);
                AccData.acc_y_with_window.Add(AccData.Y[i] * w[i]);
                AccData.acc_z_with_window.Add(AccData.Z[i] * w[i]);
            }
        }


        [NonSerialized]
        internal bool IsReady = false;
        public int ID = -1;

        [NonSerialized]
        public int ErrorCode = 0;
        public clsAcc AccData = new clsAcc();

        [NonSerialized]
        public clsAcc AccData_Filtered = new clsAcc();
        public clsFFTData FFTData = new clsFFTData();

        [NonSerialized]
        public clsFFTData PSDData = new clsFFTData();

        [NonSerialized]
        public clsFFTData FFTData_Filtered = new clsFFTData();

        [NonSerialized]
        public clsFFTData MelBankData = new clsFFTData();
        public clsOtherFeatures Features = new clsOtherFeatures();
        [NonSerialized]
        public clsOtherFeatures Features_Filtered = new clsOtherFeatures();

        public clsPhysicalQuantity PhysicalQuantity = new clsPhysicalQuantity();

        [NonSerialized]
        public ADCError DATAERROR = new ADCError();
        public long TimeSpend;
        public DateTime RecieveTime;


        internal void AddData(DataSet NewData)
        {

            this.AccData.X.AddRange(NewData.AccData.X);
            this.AccData.Y.AddRange(NewData.AccData.Y);
            this.AccData.Z.AddRange(NewData.AccData.Z);
            this.AccData_Filtered.X.AddRange(NewData.AccData_Filtered.X);
            this.AccData_Filtered.Y.AddRange(NewData.AccData_Filtered.Y);
            this.AccData_Filtered.Z.AddRange(NewData.AccData_Filtered.Z);
            //this.FFTData.X.AddRange(NewData.FFTData.X);
            //this.FFTData.Y.AddRange(NewData.FFTData.Y);
            //this.FFTData.Z.AddRange(NewData.FFTData.Z);
            this.DATAERROR.X.AddRange(NewData.DATAERROR.X);
            this.DATAERROR.Y.AddRange(NewData.DATAERROR.Y);
            this.DATAERROR.Z.AddRange(NewData.DATAERROR.Z);
        }

        internal static void AutoDelete()
        {
            Task.Run(() =>
            {
                int delete_cnt = 0;
                foreach (var filePath in Directory.GetFiles(DataSaveDir))
                {
                    if ((DateTime.Now - new FileInfo(filePath).LastWriteTime).TotalDays > 7)
                    { File.Delete(filePath); delete_cnt++; }
                }
            });
        }

        internal void AddData(DataSet NewData, double[] _window)
        {
            AddData(NewData);
            NewData.AddWindow(_window);
            this.AccData.acc_x_with_window.AddRange(NewData.AccData.acc_x_with_window);
            this.AccData.acc_y_with_window.AddRange(NewData.AccData.acc_y_with_window);
            this.AccData.acc_z_with_window.AddRange(NewData.AccData.acc_z_with_window);
        }


        public class clsPhysicalQuantity
        {
            public DataSets.PhysicalQuantityDataSet X = new DataSets.PhysicalQuantityDataSet();
            public DataSets.PhysicalQuantityDataSet Y = new DataSets.PhysicalQuantityDataSet();
            public DataSets.PhysicalQuantityDataSet Z = new DataSets.PhysicalQuantityDataSet();
        }



        public class clsAcc : AxisListValue
        {
            internal List<double> acc_x_with_window = new List<double>();
            internal List<double> acc_y_with_window = new List<double>();
            internal List<double> acc_z_with_window = new List<double>();

            internal List<double> acc_x_For_FFT = new List<double>();
            internal List<double> acc_y_For_FFT = new List<double>();
            internal List<double> acc_z_For_FFT = new List<double>();



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

        public class ADCError
        {

            public int ErrorSumCount
            {
                get
                {
                    return X.Count + Y.Count + Z.Count;
                }
            }

            public List<ErrorValue> X = new List<ErrorValue>();
            public List<ErrorValue> Y = new List<ErrorValue>();
            public List<ErrorValue> Z = new List<ErrorValue>();
        }

        public class ErrorValue
        {
            public int Index;
        }

    }

    public class ParticleDataSet
    {
        public double Temperature;
        public double Humidity;
        public double Illuminance;
        public double TypicalParticleSize;
        public Dictionary<PARTICLE_SIZE, Concentration> ParticalValueDict = new Dictionary<PARTICLE_SIZE, Concentration>();
        public double Res1;
        public double Res2;
        internal long TimeSpend;

        public int ErrorCode { get; internal set; }
        public ParticleDataSet()
        {

        }
        public ParticleDataSet(int ErrorCode)
        {
            this.ErrorCode = ErrorCode;
            Temperature = Humidity = Illuminance = Res1 = Res2 = ErrorCode;
        }

    }


    public class UVDataSet : ParticleDataSet
    {
        public double UVValue = -1;
        public int ErrorCode = 0;
        public DateTime RecieveTime;

        public UVDataSet(int ErrorCode) : base(ErrorCode)
        {
            this.ErrorCode = ErrorCode;
        }
    }

}