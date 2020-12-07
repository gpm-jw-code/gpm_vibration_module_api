using gpm_vibration_module_api;
using gpm_vibration_module_api.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gpm_module_api.UVSensor
{

    public class UVSensorAPI : GPMModuleAPI
    {
        internal UVDataSet PreDataSet = new UVDataSet(0);
        private UVDataSet uv_dataset = new UVDataSet(0);
        public new event Action<DateTime> DisconnectEvent;

        public UVSensorAPI()
        {
            base.module_base = new UVSensorBase();
        }

        public new async Task<UVDataSet> GetData()
        {
            if (module_base.isBusy)
            {
                return new UVDataSet(0)
                {
                    ErrorCode = Convert.ToInt32(clsErrorCode.Error.ModuleIsBusy)
                };
            }
            WaitAsyncForGetDataTask.Reset();
            await Task.Run(() => GetDataTask());
            WaitAsyncForGetDataTask.WaitOne();
            return PreDataSet;
        }

        internal override void GetDataTask()
        {
            byte[] dataByteAry;
            bool IsTimeout;
            try
            {
                dataByteAry = module_base.GetAccData_HighSpeedWay(out uv_dataset.TimeSpend, out IsTimeout);
                uv_dataset.ErrorCode = IsTimeout ? Convert.ToInt32(clsErrorCode.Error.DATA_GET_TIMEOUT) : 0;
                module_base.isBusy = false;

                if (dataByteAry.Length < 4)
                {
                    uv_dataset.ErrorCode = Convert.ToInt32(clsErrorCode.Error.DATA_GET_TIMEOUT);
                    uv_dataset = PreDataSet;
                    WaitAsyncForGetDataTask.Set();
                    return;
                }
                if (uv_dataset.ErrorCode != 0)
                {
                    WaitAsyncForGetDataTask.Set();
                    return;
                }
                PreDataSet = ConverterTools.UVPacketToDatatSet(dataByteAry);
                PreDataSet.RecieveTime = DateTime.Now;
                AddNewDataToTempData(PreDataSet);
                WaitAsyncForGetDataTask.Set();
            }
            catch (Exception ex)
            {
                if (DisconnectEvent != null)
                    DisconnectEvent.Invoke(DateTime.Now);
            }

        }

        private void AddNewDataToTempData(UVDataSet NewDataSet)
        {
            if (NewDataSet.ErrorCode != 0)
                return;

            if (NewDataSet.ParticalValueDict.Count != 5)
                return;

        }
    }
}
