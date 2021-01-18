using gpm_module_api;
using System;
using System.Threading.Tasks;
using static gpm_vibration_module_api.clsEnum.Module_Setting_Enum;

namespace gpm_vibration_module_api
{
    public class GPMModuleAPI : GPMModuleAPI_HSR
    {
        public ModuleSetting_GEN Settings = new ModuleSetting_GEN();
        private GPMModulesServer.ConnectInState obj;

        public GPMModuleAPI()
        {
            base.Settings = this.Settings;
            IsDaulMCUMode = false;
            DataLenMiniLen = 512;
            LowpassFilterCutOffFreq = 3000;
        }

        public GPMModuleAPI(GPMModulesServer.ConnectInState obj)
        {
            this.obj = obj;
            AsynchronousClient.client = obj.ClientSocket;
        }

        #region API FOR USER

        /// <summary>
        /// 取得感測數據集物件
        /// </summary>
        /// <param name="IsGetFFT"></param>
        /// <param name="IsGetOtherFeatures"></param>
        /// <returns></returns>
        public override async Task<DataSet> GetData(bool IsGetFFT, bool IsGetOtherFeatures)
        {
            if (Settings.Mode == DAQMode.Low_Sampling)
                return await base.GetData(IsGetFFT, IsGetOtherFeatures);
            else
            {
                DataSet dataSet_ret = new DataSet(Settings.SamplingRate);
                //拿好幾次 組合
                while (dataSet_ret.AccData.X.Count != Settings.DataLength)
                {
                    DataSet dataSet_slice = await base.GetData(IsGetFFT, IsGetOtherFeatures);
                    dataSet_ret.AddData(dataSet_slice);
                }
                FFTAndFeatureCal(ref dataSet_ret, IsGetFFT, IsGetOtherFeatures, Settings.SamplingRate);
                return dataSet_ret;
            }
        }

        /// <summary>
        /// 設定取樣模式
        /// </summary>
        /// <param name="Mode"></param>
        /// <returns></returns>
        public override async Task<int> DAQModeSetting(DAQMode Mode)
        {
            var oriMode = Settings.Mode;
            Settings.Mode = Mode;
            var state = await base.SendMessageMiddleware(Settings.SettingBytesWithHead, 8, 3000);
            if (state.ErrorCode != clsErrorCode.Error.None)
            {
                Settings.Mode = oriMode;
                return (int)state.ErrorCode;
            }
            else
                return 0;
        }
        /// <summary>
        /// 設定量測資料長度
        /// </summary>
        /// <param name="N"></param>
        /// <returns></returns>
        public override Task<int> Data_Length_Setting(int N)
        {
            return base.Data_Length_Setting(N);
        }
        /// <summary>
        /// 取得量測範圍列舉陣列
        /// </summary>
        /// <returns></returns>
        static public object[] GetMEASURE_RANGES()
        {

            return new object[] {
             MEASURE_RANGE.MR_2G,
             MEASURE_RANGE.MR_4G,
             MEASURE_RANGE.MR_8G,
             MEASURE_RANGE.MR_16G,
            };
        }
        #endregion
    }

    public class ModuleSetting_GEN : ModuleSetting
    {
        public ModuleSetting_GEN()
        {
            base._SettingBytes = new byte[8] { 0x00, 0x00, 0x9F, 0x00, 0x0F, 0x00, 0x00, 0x00 };
        }
        public override int PackageTotalLen
        {
            get
            {
                if (_Mode == DAQMode.High_Sampling)
                    return 3072;
                else
                    return 3072 * (_DataLength / 512);
            }
        }
        internal override byte[] SettingBytesWithHead
        {
            get
            {
                byte[] bytes = new byte[11];
                bytes[0] = 0x53;
                bytes[9] = 0x0d;
                bytes[10] = 0x0a;
                Array.Copy(_SettingBytes, 0, bytes, 1, 8);
                return bytes;
            }
        }
        public override int DataLength { get => base.DataLength; set => base.DataLength = value; }
        internal override void UpdateSettingBytes()
        {
            if (_Mode == DAQMode.Low_Sampling)
            {
                int _ratio = _DataLength / 512;  //倍數
                _SettingBytes[0] = (byte)_ratio;
            }
            else
            {
                _SettingBytes[0] = 0x00;
            }
            //量測範圍
            _SettingBytes[3] = _mEASURE_RANGE.ToGENByte();
        }
    }
}
