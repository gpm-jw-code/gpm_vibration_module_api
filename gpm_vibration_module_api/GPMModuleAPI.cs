using gpm_module_api;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static gpm_vibration_module_api.clsEnum.Module_Setting_Enum;

namespace gpm_vibration_module_api
{
    public class GPMModuleAPI : GPMModuleAPI_HSR
    {
        public ModuleSetting_GEN Settings = new ModuleSetting_GEN();
        public ModuleSetting_485 Settings_485 = new ModuleSetting_485();
        private GPMModulesServer.ConnectInState obj;

        public bool Is485Module
        {
            get { return _Is485Module; }
            set
            {
                base._Is485Module = value;
                if (_Is485Module)
                {
                    base.Settings = Settings_485;
                    ParamSetCheckLen = 6;
                    ParamSetRetryNumber = 2;
                }
                else
                {
                    base.Settings = Settings;
                    ParamSetCheckLen = 8;
                }
            }
        }



        public GPMModuleAPI()
        {
            IsKX134Sensor = false;
            DataLenMiniLen = 512;
            LowPassFilterCutOffFreq = 3000;
        }

        public GPMModuleAPI(GPMModulesServer.ConnectInState obj)
        {
            this.obj = obj;
            AsynchronousClient.client = obj.ClientSocket;
        }

        #region API FOR USER

        public override Socket ModuleSocket
        {
            get { return base.ModuleSocket; }
            set { base.ModuleSocket = value; }
        }
        public override DAQMode DAQMode { get { return base.DAQMode; } }
        public override bool LowPassFilterActive
        {
            get { return base.LowPassFilterActive; }
            set { base.LowPassFilterActive = value; }
        }
        public override double LowPassFilterCutOffFreq
        {
            get
            {
                return base.LowPassFilterCutOffFreq;
            }
            set { base.LowPassFilterCutOffFreq = value; }
        }


        /// <summary>
        /// 取得感測數據集物件
        /// </summary>
        /// <param name="IsGetFFT"></param>
        /// <param name="IsGetOtherFeatures"></param>
        /// <returns></returns>
        public override async Task<DataSet> GetData(bool IsGetFFT, bool IsGetOtherFeatures)
        {
            if (Settings.Mode == gpm_vibration_module_api.DAQMode.Low_Sampling)
                return await base.GetData(IsGetFFT, IsGetOtherFeatures);
            else
            {
                int ErrorCnt = 0;
                DataSet dataSet_ret = new DataSet(base.Settings.SamplingRate);
                //拿好幾次 組合
                while (dataSet_ret.AccData.X.Count != base.Settings.DataLength)
                {
                    DataSet dataSet_slice = await base.GetData(IsGetFFT, IsGetOtherFeatures);
                    if (dataSet_slice == null)
                    {
                        Thread.Sleep(1);
                        continue;
                    }
                    if (dataSet_slice.ErrorCode == 0)
                        dataSet_ret.AddData(dataSet_slice);
                    else
                    {
                        ErrorCnt += 1;
                        if (ErrorCnt > (Is485Module ? 20 : 5))
                        {
                            if (Is485Module)
                            {
                                Disconnect();
                                int reopen_ret = await Open(PortName, BaudRate);
                                Console.WriteLine($"重新開啟通訊埠 {(reopen_ret == 0 ? "成功" : "失敗")}");
                                if (reopen_ret != 0 | ErrorCnt > 40)
                                    return new DataSet(0) { ErrorCode = (int)clsErrorCode.Error.DATA_GET_TIMEOUT };
                            }
                            else
                                return new DataSet(0) { ErrorCode = (int)clsErrorCode.Error.DATA_GET_TIMEOUT };
                        }
                    }
                    Thread.Sleep(1);
                }
                FFTAndFeatureCal(ref dataSet_ret, IsGetFFT, IsGetOtherFeatures, base.Settings.SamplingRate);
                return dataSet_ret;
            }
        }
        public async Task<DataSet> GetData_ByID(byte ID, bool IsGetFFT, bool IsGetOtherFeatures)
        {
            base.Settings.SlaveID = ID;
            //MeasureRangeCheck(ID);
            var data = GetData(IsGetFFT, IsGetOtherFeatures).Result;
            data.ID = ID;
            return data;
        }

        public async Task<int> IDSetting(byte ID_Ori, byte ID_ToChange)
        {
            base.Settings.SlaveID = ID_Ori;
            var state = await SendMessageMiddleware(base.Settings.ModifyIDCmbByteForModbus(ID_ToChange), 8, 3000);
            return state.ErrorCode == clsErrorCode.Error.None ? 0 : -999;
        }

        private async void MeasureRangeCheck(int ID)
        {
            byte[] SettingBytes = await GetDeviceParamsBytes_ByID((byte)ID);
            if (SettingBytes == null)
                return;
            var mr_Setting = SettingBytes[6].ToMeasureGENRange();
            if (base.Settings.mEASURE_RANGE != mr_Setting)
            {
                var mr_set_ret = await Measure_Range_Setting_ByID((byte)ID, mr_Setting);
                Console.WriteLine("變更量測範圍>>" + mr_Setting);
            }

        }
        /// <summary>
        /// 設定取樣模式
        /// </summary>
        /// <param name="Mode"></param>
        /// <returns></returns>
        public async override Task<int> DAQModeSetting(DAQMode Mode)
        {
            return await base.DAQModeSetting(Mode);
        } /// <summary>
          /// 設定取樣模式
          /// </summary>
          /// <param name="Mode"></param>
          /// <returns></returns>
        public async Task<int> DAQModeSetting_ByID(byte ID, DAQMode Mode)
        {
            base.Settings.SlaveID = ID;
            return DAQModeSetting(Mode).Result;
        }

        public override async Task<int> Measure_Range_Setting(MEASURE_RANGE mr_select)
        {
            return await base.Measure_Range_Setting(mr_select);
        }
        public async Task<int> Measure_Range_Setting_ByID(byte ID, MEASURE_RANGE mr_select)
        {
            base.Settings.SlaveID = ID;
            return await base.Measure_Range_Setting(mr_select);
        }

        public async Task<string> GetDeviceParamsStr_ByID(byte ID)
        {
            Settings.SlaveID = ID;
            var state = await SendMessageMiddleware(Settings.READParamCmdByteForModbus, 13, 1000);
            return state.ErrorCode == clsErrorCode.Error.None ? state.DataByteList.ToArray().ToCommaString() : state.ErrorCode.ToString();
        }
        public async Task<byte[]> GetDeviceParamsBytes_ByID(byte ID)
        {
            Settings.SlaveID = ID;
            var state = await SendMessageMiddleware(Settings.READParamCmdByteForModbus, 13, 1000);
            return state.ErrorCode == clsErrorCode.Error.None ? state.DataByteList.ToArray() : null;
        }
        public override async Task<string> GetDeviceParams()
        {

            var state = await SendMessageMiddleware("READSTVAL\r\n", ParamSetCheckLen, 1000);
            return state.ErrorCode == clsErrorCode.Error.None ? state.DataByteList.ToArray().ToCommaString() : state.ErrorCode.ToString();

        }


        /// <summary>
        /// 設定量測資料長度
        /// </summary>
        /// <param name="N"></param>
        /// <returns></returns>
        public override async Task<int> Data_Length_Setting(int N)
        {
            return await base.Data_Length_Setting(N);
        }
        public async Task<int> Data_Length_Setting_ByID(byte ID, int N)
        {
            base.Settings.SlaveID = ID;
            return await Data_Length_Setting(N);
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

    public class ModuleSetting_485 : ModuleSetting
    {

        public ModuleSetting_485()
        {
            base._SettingBytes = new byte[6] { 0x00, 0x06, 0x00, 0x00, 0x00, 0x00 };
            base.Mode = DAQMode.High_Sampling;
        }
        public override int PackageTotalLen
        {
            get
            {
                if (_Mode == DAQMode.High_Sampling)
                    return 3072 + 3; //包含ID(Head) 跟 oxod oxoa(Tail)
                else
                    return 3072 * (_DataLength / 512); //待確認?
            }
        }
        internal override byte[] SettingBytesWithHead
        {
            get
            {
                byte[] cmd = _SettingBytes;
                var crc = BitConverter.GetBytes(calculateCRC(cmd, 6, 0));
                List<byte> cmdlist = new List<byte>();
                cmdlist.AddRange(cmd);
                cmdlist.AddRange(crc);
                return cmdlist.ToArray();
            }
        }
        public override int DataLength
        {
            get
            {
                return base.DataLength;
            }
            set
            {
                settingItem = SettingItem.SetDataLen;
                base.DataLength = value;
            }
        }

        public override MEASURE_RANGE mEASURE_RANGE
        {
            get
            {
                return base.mEASURE_RANGE;
            }
            set
            {
                settingItem = SettingItem.SetMeasureRange;
                base.mEASURE_RANGE = value;
            }
        }
        public override DAQMode Mode
        {
            get
            {
                return base.Mode;
            }
            set
            {
                settingItem = SettingItem.SetDaqMode;
                base.Mode = value;
            }
        }
        internal override void UpdateSettingBytes()
        {
            _SettingBytes[0] = SlaveID;
            _SettingBytes[1] = 0x06; //function code
            _SettingBytes[2] = 0x00;
            switch (settingItem)
            {
                case SettingItem.SetDaqMode:
                    _SettingBytes[3] = 0x10; _SettingBytes[4] = 0;
                    if (_Mode == DAQMode.Low_Sampling)
                    {
                        int _ratio = _DataLength / 512;  //倍數
                        _SettingBytes[5] = (byte)_ratio;
                    }
                    else
                        _SettingBytes[5] = 0x00;
                    break;
                case SettingItem.SetDataLen:
                    _SettingBytes[3] = 0x10; _SettingBytes[4] = 0;
                    if (_Mode == DAQMode.Low_Sampling)
                    {
                        int _ratio = _DataLength / 512;  //倍數
                        _SettingBytes[5] = (byte)_ratio;
                    }
                    else
                        _SettingBytes[5] = 0x00;
                    break;
                case SettingItem.SetMeasureRange:
                    _SettingBytes[3] = 0x11;
                    _SettingBytes[4] = 0x9F;
                    _SettingBytes[5] = _mEASURE_RANGE.ToGENByte();//量測範圍
                    break;
                case SettingItem.NotSpecify:
                    break;
                default:
                    break;
            }
        }
    }

    public class ModuleSetting_GEN : ModuleSetting
    {
        public ModuleSetting_GEN()
        {
            base._SettingBytes = new byte[8] { 0x00, 0x00, 0x9F, 0x00, 0x00, 0x00, 0x00, 0x10 };
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
        public override int DataLength
        {
            get { return base.DataLength; }
            set { base.DataLength = value; }
        }
        internal override void UpdateSettingBytes()
        {
            if (_Mode == DAQMode.Low_Sampling)
            {
                int _ratio = _DataLength / 512;  //倍數
                _SettingBytes[1] = (byte)_ratio;
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
