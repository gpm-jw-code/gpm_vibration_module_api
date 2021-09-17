using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gpm_vibration_module_api.DataSets
{
    public class ThreeInOneModuleDataSet :IDisposable
    {
        private bool disposedValue;

        public int ErrorCode { get; internal set; }
        public double Temperature1 { get; internal set; }
        public double Temperature2 { get; internal set; }
        public double Humidity1 { get; internal set; }
        public double Humidity2 { get; internal set; }

        public double Pressure1 { get; internal set; }
        public double Pressure2 { get; internal set; }
        public DataSet.clsAcc VibrationData { get; internal set; } = new DataSet.clsAcc();
        public DataSet.clsFFTData FFTData { get; internal set; } = new DataSet.clsFFTData();
        public List<byte> RawBytes { get; internal set; }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 處置受控狀態 (受控物件)
                }
                RawBytes = null;
                VibrationData = null;
                FFTData = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
