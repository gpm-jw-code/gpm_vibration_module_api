using gpm_vibration_module_api.GPMBase;
using gpm_vibration_module_api.GpmMath;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static gpm_vibration_module_api.clsEnum.Module_Setting_Enum;

namespace gpm_vibration_module_api
{
    /// <summary>
    /// This is for 2021 New Module Type
    /// </summary>
    public class GPMModuleAPI_HSR
    {
        public enum Filter
        {
            MA, LowPass
        }
        /// <summary>
        /// Serial Port通訊介面
        /// </summary>
        private ModuleSerialPortBase SerialPortBase;
        private readonly NET.DeviceTestData DeviceData = new NET.DeviceTestData();
        private Stopwatch HSStopWatch = new Stopwatch();
        private int GetDataInteruptFlag;
        private bool GetDataFirstCall = true;


        internal bool IsKX134Sensor = true;
        internal bool _Is485Module = false;
        internal int DataLenMiniLen = 256;
        internal int ParamSetCheckLen = 8;
        /// <summary>
        /// TCP/IP 通訊界面
        /// </summary>
        public AsynchronousClient AsynchronousClient;
        public int GetDataCalledNum { get; private set; }
        public int GetDataSuccessNum { get; private set; }
        public DataSet DataSetForUser { get; private set; }
        public event Action<int> DataPacketOnchange;
        public GPMModuleAPI_HSR()
        {
            IsKX134Sensor = true;
            _Is485Module = false;
            Settings = new ModuleSetting();
            Tools.Logger.Event_Log.Log($"GPMModuleAPI_HSR 物件建立");
        }

        public string IP { get; private set; }
        public int Port { get; private set; }

        public string PortName { get; private set; }

        public int BaudRate { get; private set; }

        public ModuleSetting Settings { get; internal set; } = new ModuleSetting();

        #region API FOR USER

        virtual public Socket ModuleSocket
        {
            get
            {
                return AsynchronousClient.client;
            }
            set
            {
                AsynchronousClient.client = value;
            }
        }

        virtual public bool FilterActive { get; set; } = false;

        virtual public bool LowPassFilterActive { get; set; } = false;
        /// <summary>
        /// 低通濾波器截止頻率
        /// </summary>
        virtual public double LowPassFilterCutOffFreq { get; set; } = 3000;

        public int MovingAverageSize { get; set; } = 2;

        public Filter filter = Filter.LowPass;

        /// <summary>
        /// 取得連線狀態
        /// </summary>
        public bool Connected
        {
            get
            {
                try
                {
                    if (!_Is485Module)
                        return AsynchronousClient.client.Connected;
                    else
                        return SerialPortBase.module_port.IsOpen;
                }
                catch (Exception)
                {
                    return false;
                }

            }
        }
        /// <summary>
        /// 設定/取得封包資料長度
        /// </summary>
        public int DataLength
        {

            get
            {
                return Settings.DataLength;
            }
        }


        /// <summary>
        /// 設定/取得量測範圍
        /// </summary>
        public MEASURE_RANGE MeasureRange
        {
            get
            {
                return Settings.mEASURE_RANGE;
            }
        }

        /// <summary>
        /// [韌體參數]最小封包單位長度
        /// </summary>
        public int MiniPacketDataLen = 128;

        public double SamplingRate
        {
            get
            {
                return Settings.SamplingRate;
            }
            set
            {
                Settings.SamplingRate = value;
            }
        }

        public int ParamSetRetryNumber = 5;
        public int GetDataRetryNumber = 30;

        public virtual int AccDataRevTimeOut
        {
            set
            {
                try
                {
                    if (Int32.TryParse(value + "", out int _result))
                        Settings.AccDataRevTimeout = _result;
                }
                catch (Exception ex)
                {
                }
            }
            get
            {
                return Settings.AccDataRevTimeout;
            }
        }
        /// <summary>
        /// 跟模組連線
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="Port"></param>
        /// <returns></returns>
        public async Task<int> Connect(string IP, int Port)
        {
            //var licCheckRet = 0;
            //if ((licCheckRet = LicenseCheckProcess()) != 0)
            //    return licCheckRet;
            try
            {
                SerialPortBase = null;
                Tools.Logger.Event_Log.Log($"使用者嘗試連線...{IP}:{Port}");
                if (Connected) //如果本來已經有連線，就先斷線。
                    Disconnect();
                this.IP = IP;
                this.Port = Port;
                AsynchronousClient = new AsynchronousClient();
                AsynchronousClient.DataPacketLenOnchange += AsynchronousClient_DataPacketLenOnchange1;
                int isconnect = await AsynchronousClient.AsyncConnect(IP, Port);
                if (isconnect != 0)
                {
                    Disconnect();
                    Tools.Logger.Event_Log.Log("TCP/IP Connecet fail.");
                    return isconnect;
                }
                await GetDataInterupt();
                Tools.Logger.Event_Log.Log("Device Connected.");
                _Is485Module = false;
                return 0;
            }
            catch (Exception ex)
            {
                Tools.Logger.Event_Log.Log($"Connect exception occurred! {ex.Message}");
                return (int)clsErrorCode.Error.SYSTEM_ERROR;
            }

        }

        public async Task<int> Open(string PortName, int BaudRate)
        {
            var licCheckRet = 0;
            if ((licCheckRet = LicenseCheckProcess()) != 0)
                return licCheckRet;
            this.PortName = PortName;
            this.BaudRate = BaudRate;
            Tools.Logger.Event_Log.Log($"(Serial Port)使用者嘗試連線..PORTNAME:{PortName}");
            if (SerialPortBase != null)
                SerialPortBase.PortClose();
            SerialPortBase = new ModuleSerialPortBase();
            _Is485Module = true;
            return await SerialPortBase.Open(PortName, BaudRate);
        }

        private int LicenseCheckProcess()
        {
            gpm_module_api.License.LicenseCheck licenseChecker = new gpm_module_api.License.LicenseCheck();
            var licState = licenseChecker.Check("license.lic");
            switch (licState.CHECK_RESULT)
            {
                case gpm_module_api.License.CHECK_RESULT.PASS:
                    return 0;
                case gpm_module_api.License.CHECK_RESULT.EXPIRED:
                    return (int)clsErrorCode.Error.LicenseExpired;
                case gpm_module_api.License.CHECK_RESULT.FAIL:
                    return (int)clsErrorCode.Error.LicenseCheckFail;
                case gpm_module_api.License.CHECK_RESULT.LOSS:
                    return (int)clsErrorCode.Error.LicenseFileNoExist;
                default:
                    return (int)clsErrorCode.Error.LicenseCheckFail;
            }
        }

        /// <summary>
        /// 斷開與Device的TCP/IP連線
        /// </summary>
        /// <returns></returns>
        public int Disconnect()
        {
            Tools.Logger.Event_Log.Log("Try Disconnect.");
            if (!_Is485Module)
            {
                try
                {
                    AsynchronousClient?.Disconnect();
                    AsynchronousClient = null;
                    GC.Collect();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + "|" + ex.StackTrace);
                }
            }
            else
                SerialPortBase.PortClose();
            return 0;
        }

        /// <summary>
        /// 單軸資料長度設定
        /// </summary>
        /// <param name="N"></param>
        /// <returns></returns>
        virtual public async Task<int> Data_Length_Setting(int N)
        {
            try
            {
                Tools.Logger.Event_Log.Log($"使用者嘗試修改資料長度({N})");
                if (N % DataLenMiniLen != 0)
                {
                    Tools.Logger.Event_Log.Log($"資料長度({N})不合乎規範 須為{DataLenMiniLen}的倍數");
                    return (int)clsErrorCode.Error.DATA_LENGTH_SETTING_VALUE_ILLEGAL;
                }
                //await GetDataInterupt();
                var ori_Set = Settings.DataLength;
                Settings.DataLength = N;
                var state = await SendMessageMiddleware(Settings.SettingBytesWithHead, ParamSetCheckLen, Timeout: 3000);
                int retry_cnt = 0;
                while (state.ErrorCode != clsErrorCode.Error.None)
                {
                    retry_cnt++;
                    state = (await SendMessageMiddleware(Settings.SettingBytesWithHead, ParamSetCheckLen, Timeout: 3000));
                    retry_cnt++;
                    if (retry_cnt >= ParamSetRetryNumber)
                        return (int)state.ErrorCode;
                    Thread.Sleep(1);
                    //return (int)state.ErrorCode;
                }
                byte[] Param_Ret = state.DataByteList.ToArray();
                if (!ParamRetCheck(Settings._SettingBytes, Param_Ret))
                {
                    Tools.Logger.Event_Log.Log($"Data Length Setting Range Fail::{state.ErrorCode}");
                    Settings.DataLength = ori_Set;
                    return -1;
                }
                else
                {
                    Settings.DataLength = N;
                    Tools.Logger.Event_Log.Log($"Data Length Setting OK::{N}");
                    Console.WriteLine("量測時間預估:" + Settings.Measure_Time + " ms");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        /// <summary>
        /// 量測範圍設定
        /// </summary>
        /// <param name="mr_select"></param>
        /// <returns></returns>
        virtual public async Task<int> Measure_Range_Setting(MEASURE_RANGE mr_select)
        {
            Tools.Logger.Event_Log.Log($"使用者嘗試修改量測範圍({mr_select})");
            var ori_Set = Settings.mEASURE_RANGE;
            //await GetDataInterupt();
            Settings.mEASURE_RANGE = mr_select;
            var state = (await SendMessageMiddleware(Settings.SettingBytesWithHead, ParamSetCheckLen, Timeout: 3000));
            int retry_cnt = 0;
            while (state.ErrorCode != clsErrorCode.Error.None)
            {
                retry_cnt++;
                state = (await SendMessageMiddleware(Settings.SettingBytesWithHead, ParamSetCheckLen, Timeout: 3000));
                if (retry_cnt >= ParamSetRetryNumber)
                    return (int)state.ErrorCode;
                Thread.Sleep(1);
            }
            byte[] Param_Ret = state.DataByteList.ToArray();
            if (!ParamRetCheck(Settings.SettingBytes, Param_Ret))
            {
                Tools.Logger.Event_Log.Log($"Measurement Range Setting Fail{state.ErrorCode}");
                Settings.mEASURE_RANGE = ori_Set;
                return -1;
            }
            else
            {
                Tools.Logger.Event_Log.Log($"Measurement Range Setting OK::{mr_select}");
                return 0;
            }
        }
        virtual public DAQMode DAQMode
        {
            get
            {
                return Settings.Mode;
            }
        }
        /// <summary>
        /// DAQ MODE SETTING::HSR Version isn't support, always retrun 0.
        /// </summary>
        /// <param name="Mode"></param>
        /// <returns></returns>
        virtual public async Task<int> DAQModeSetting(DAQMode Mode)
        {
            if (IsKX134Sensor)
                return 0;
            var oriMode = Settings.Mode;
            Settings.Mode = Mode;
            var state = await SendMessageMiddleware(Settings.SettingBytesWithHead, ParamSetCheckLen, Timeout: 3000);
            if (state.ErrorCode != clsErrorCode.Error.None)
            {
                Settings.Mode = oriMode;
                return (int)state.ErrorCode;
            }
            else
                return 0;
        }
        /// <summary>
        /// GetData方法呼叫統計
        /// </summary>
        private void GetDataCalledStasify()
        {
            if (GetDataCalledNum >= 65535)
                GetDataCalledNum = GetDataSuccessNum = 0;
            GetDataCalledNum += 1;
        }

        virtual public async Task<DataSet> GetData(bool IsGetFFT, bool IsGetOtherFeatures)
        {
            GetDataCalledStasify();
            GetDataInteruptFlag = 0;
            Tools.Logger.Event_Log.Log("GetData Fun called");
            try
            {
                // var timeout = GetDataFirstCall ? 500 : Settings.Measure_Time + 8000;
                HSStopWatch.Restart();
                var state_obj = SendGetRawDataCmd(_Is485Module ? 1000 : Settings.AccDataRevTimeout);
                HSStopWatch.Stop();
                #region Retry
                ////Timeout後會自動重連，所以可以重試
                //int retry_cnt = 0;
                //while (state_obj.ErrorCode != clsErrorCode.Error.None)
                //{
                //    GetDataFirstCall = false;
                //    await Reconnect();
                //    state_obj = await AsynchronousClient.SendMessage("READVALUE\r\n", Settings.PackageTotalLen, Timeout: timeout);
                //    retry_cnt++;
                //    if (retry_cnt >= GetDataRetryNumber)
                //    {
                //        GetDataFirstCall = false;
                //        return new DataSet(0) { ErrorCode = (int)state_obj.ErrorCode };
                //    }
                //    Thread.Sleep(1);
                //}
                #endregion
                GetDataFirstCall = false;
                //DataUploadToWeb();
                if (state_obj.ErrorCode == clsErrorCode.Error.None)
                {
                    GetDataSuccessNum++;
                    Tools.Logger.Event_Log.Log("封包接收完成:" + state_obj.DataByteList.Count + $"(單軸長度:{state_obj.DataByteList.Count / 6}|設定值:{Settings.DataLength})");
                    DataSetForUser = PostProcessing(state_obj.DataByteList, IsGetFFT, IsGetOtherFeatures);
                    DataSetForUser.RecieveTime = DateTime.Now;
                    DataSetForUser.TimeSpend = HSStopWatch.ElapsedMilliseconds;
                    return DataSetForUser;
                }
                else
                    return new DataSet(0) { ErrorCode = state_obj.ErrorCode == clsErrorCode.Error.DATA_GET_INTERUPT ? (int)clsErrorCode.Error.DATA_GET_INTERUPT : (int)clsErrorCode.Error.DATA_GET_TIMEOUT };
            }
            catch (Exception ex)
            {
                Tools.Logger.Code_Error_Log.Log(ex);
                return new DataSet(0) { ErrorCode = (int)clsErrorCode.Error.DATA_GET_TIMEOUT };
            }

        }

        /// <summary>
        /// 中斷GetData任務
        /// </summary>
        public async Task GetDataInterupt()
        {
            await SendMessageMiddleware("00000000000", 0, -1); //當裝置處於持續發送封包的狀態下，若收到長度11的位元組後會停止
        }
        /// <summary>
        /// 取得量測範圍列舉陣列
        /// </summary>
        /// <returns></returns>
        static public object[] GetMEASURE_RANGES()
        {
            return new object[] {
             MEASURE_RANGE.MR_8G,
             MEASURE_RANGE.MR_16G,
             MEASURE_RANGE.MR_32G,
             MEASURE_RANGE.MR_64G,
            };
        }
        /// <summary>
        /// 取得裝置參數設定組
        /// </summary>
        /// <returns></returns>
        virtual public async Task<string> GetDeviceParams()
        {
            var state = await SendMessageMiddleware("READSTVAL\r\n", 8, 1000);
            return state.ErrorCode == clsErrorCode.Error.None ? state.DataByteList.ToArray().ToCommaString() : state.ErrorCode.ToString();
        }

        #endregion
        #region Internal Methods

        /// <summary>
        /// 發送獲取RAW DATA 的指令(overrideable)
        /// </summary>
        /// <returns></returns>
        virtual internal StateObject SendGetRawDataCmd(int Timeout = 3000)
        {
            StateObject state_obj = null;
            if (!_Is485Module)
                state_obj = SendMessageMiddleware("READVALUE\r\n", Settings.PackageTotalLen, Timeout).Result;
            else
                state_obj = SendMessageMiddleware(Settings.READRAWCmdByteForModbus, Settings.PackageTotalLen, Timeout).Result;
            return state_obj;
        }
        #endregion
        #region Private Methods
        private void AsynchronousClient_DataPacketLenOnchange1(object sender, int e)
        {
            DataPacketOnchange?.BeginInvoke(e, null, null);
        }
        private async Task DataUploadToWeb()
        {
            try
            {
                DeviceData.IP = _Is485Module ? $"Serial Port({PortName}:{BaudRate})" : IP;
                DeviceData.PCName = System.Net.Dns.GetHostName();
                DeviceData.ModuleConnected = Connected;
                DeviceData.DataLenSet = DataLength;
                DeviceData.MEASRangeSet = MeasureRange.ToString();
                DeviceData.ModuleConnected = Connected;
                DeviceData.ErrorCode = DataSetForUser == null ? 444 : DataSetForUser.ErrorCode;
                DeviceData.SendRequestNumber = GetDataCalledNum;
                DeviceData.DeviceReplyOKNumber = GetDataSuccessNum;
                DeviceData.MeasureTime = DataSetForUser == null ? -999 : (int)DataSetForUser.TimeSpend;
                NET.WebAPI.DeviceDataPost(DeviceData);
            }
            catch (Exception ex)
            {
            }

        }
        internal async Task<StateObject> SendMessageMiddleware(string msg, int CheckLen, int Timeout)
        {
            if (!_Is485Module)
                return await AsynchronousClient.SendMessage(msg, CheckLen, Timeout);
            else
                return await SerialPortBase.SendMessage(msg, CheckLen, Timeout);
        }
        internal async Task<StateObject> SendMessageMiddleware(byte[] msg, int CheckLen, int Timeout)
        {
            if (!_Is485Module)
                return await AsynchronousClient.SendMessage(msg, CheckLen, Timeout);
            else
                return await SerialPortBase.SendMessage(msg, CheckLen, Timeout);
        }
        private void AsynchronousClient_DataPacketLenOnchange(int obj)
        {
            DataPacketOnchange?.BeginInvoke(obj, null, null);
        }
        internal async Task<int> Connect()
        {
            return await Connect(this.IP, this.Port);
        }
        private new async Task<int> Reconnect()
        {
            #region Timeout的時候將連線關閉在重連(才能把非同步接收進程關掉)
            Settings.DefaulDataLength = Settings.DataLength;
            Disconnect();
            var connect_ret = await Connect(IP, Port);
            Console.WriteLine($"Timeout>Disconnect.Reconnect process::{connect_ret}");
            return connect_ret;
            #endregion
        }
        private void DeviceSocketHand()
        {
            Disconnect();
            Connect();
        }
        private async Task<int> SensorConnectionCheck()
        {
            var state_object = await AsynchronousClient.SendMessage("READSTVAL\r\n", 8, 2000);
            if (state_object.ErrorCode != clsErrorCode.Error.None)
            {
                return (int)clsErrorCode.Error.SensorNoConnection;
            }
            else
            {
                return ParamRetCheck(state_object.DataByteList.ToArray()) ? 0 : (int)clsErrorCode.Error.SensorBroken;
            }
        }

        /// <summary>
        /// 參數設定結束後，檢查模組回傳值跟寫入值是否一樣
        /// </summary>
        /// <param name="Send"></param>
        /// <param name="Ret"></param>
        /// <returns></returns>
        private bool ParamRetCheck(byte[] Send, byte[] Ret)
        {
            byte[] _send = new byte[8];
            //如果開頭是0x53 >去頭去尾 
            if (Send[0] == 83)
                Array.Copy(Send, 1, _send, 0, 8);
            else
                _send = Send;
            for (int i = 0; i < _send.Length; i++)
            {
                if (_send[i] != Ret[i])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 檢查預設值是否正確
        /// </summary>
        /// <param name="Ret"></param>
        /// <returns></returns>
        private bool ParamRetCheck(byte[] Ret)
        {
            List<int[]> Asserts = new List<int[]>() {
              new int[2]{1,Settings.DefaulDataLength/256}, new int[2]{3,192}, new int[2]{4, 47}
            };
            foreach (var assert_item in Asserts)
            {
                if (Ret[assert_item[0]] != assert_item[1])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 寫入預設值
        /// </summary>
        /// <returns></returns>
        private async Task<int> SetDefaul()
        {
            var state_1 = await Measure_Range_Setting(MEASURE_RANGE.MR_8G);
            var state_2 = await Data_Length_Setting(Settings.DefaulDataLength);
            if (state_1 == 0 && state_2 == 0)
                return 0;
            else
                return (int)clsErrorCode.Error.SelfTestFail;
        }

        /// <summary>
        /// 數據後處理(濾波、FFT、特徵演算)
        /// </summary>
        /// <param name="raw_bytes"></param>
        /// <param name="GetFFT"></param>
        /// <param name="GetFeatures"></param>
        /// <returns></returns>
        private DataSet PostProcessing(List<byte> raw_bytes, bool GetFFT, bool GetFeatures)
        {
            try
            {
                byte[] _raw_bytes;
                if (_Is485Module)
                {
                    _raw_bytes = new byte[raw_bytes.Count - 3];
                    Array.Copy(raw_bytes.ToArray(), 1, _raw_bytes, 0, _raw_bytes.Length);
                }
                else
                {
                    try
                    {
                        _raw_bytes = new byte[Settings.DataLength * 6];
                        Array.Copy(raw_bytes.ToArray(), 0, _raw_bytes, 0, _raw_bytes.Length);
                    }
                    catch (Exception ex)
                    {
                        Tools.Logger.Event_Log.Log("[ARRAY COPY FAIL]" + ex.Message);
                        return new DataSet(0) { ErrorCode = (int)clsErrorCode.Error.SYSTEM_ERROR };
                    }
                }
                List<List<double>> ori_xyz_data_list = null;
                if (IsKX134Sensor)
                    ori_xyz_data_list = Tools.ConverterTools.AccPacketToListDouble_KX134(_raw_bytes, Settings.LSB, MiniPacketDataLen);
                else
                    ori_xyz_data_list = Tools.ConverterTools.AccPacketToListDouble(_raw_bytes, Settings.mEASURE_RANGE, Settings.Mode);
                // List<List<double>> XYZ_Acc_Data_List = Filters.LPF(ori_xyz_data_list, LowpassFilterCutOffFreq, Settings.SamplingRate); //濾波
                DataSet dataSet_ret = new DataSet(Settings.SamplingRate) { RecieveTime = DateTime.Now };

                dataSet_ret.AccData.X = ori_xyz_data_list[0];
                dataSet_ret.AccData.Y = ori_xyz_data_list[1];
                dataSet_ret.AccData.Z = ori_xyz_data_list[2];

                if (FilterActive)
                {
                    List<List<double>> XYZ_Acc_Data_List = null;
                    if (filter == Filter.MA)
                        XYZ_Acc_Data_List = Filters.MovingAverage(ori_xyz_data_list, Convert.ToInt32(Settings.SamplingRate), MovingAverageSize); //濾波
                    else
                        XYZ_Acc_Data_List = Filters.LPF(ori_xyz_data_list, LowPassFilterCutOffFreq, Settings.SamplingRate); //濾波
                    dataSet_ret.AccData_Filtered.X = XYZ_Acc_Data_List[0];
                    dataSet_ret.AccData_Filtered.Y = XYZ_Acc_Data_List[1];
                    dataSet_ret.AccData_Filtered.Z = XYZ_Acc_Data_List[2];
                }

                FFTAndFeatureCal(ref dataSet_ret, GetFFT, GetFeatures, Settings.SamplingRate);
                return dataSet_ret;
            }
            catch (Exception ex)
            {
                Tools.Logger.Event_Log.Log("[PostProcessingError]" + ex.Message);
                return new DataSet(0) { ErrorCode = (int)clsErrorCode.Error.PostProcessingError };
            }
        }
        internal void FFTAndFeatureCal(ref DataSet dataSet_ret, bool fft, bool other_feature, double samplingRate)
        {
            if (fft)
            {
                DataForFFTCompensate(ref dataSet_ret);
                var n = dataSet_ret.AccData.X.Count / 2;
                dataSet_ret.PSDData.X = FftSharp.Transform.FFTmagnitude(dataSet_ret.AccData.X.ToArray()).ToList().GetRange(1, n);
                dataSet_ret.PSDData.Y = FftSharp.Transform.FFTmagnitude(dataSet_ret.AccData.Y.ToArray()).ToList().GetRange(1, n);
                dataSet_ret.PSDData.Z = FftSharp.Transform.FFTmagnitude(dataSet_ret.AccData.Z.ToArray()).ToList().GetRange(1, n);
                dataSet_ret.FFTData.X = FFT.GetFFT(dataSet_ret.AccData.acc_x_For_FFT);
                dataSet_ret.FFTData.Y = FFT.GetFFT(dataSet_ret.AccData.acc_y_For_FFT);
                dataSet_ret.FFTData.Z = FFT.GetFFT(dataSet_ret.AccData.acc_z_For_FFT);
                dataSet_ret.Features.VibrationEnergy.X = Stastify.GetOA(dataSet_ret.FFTData.X);
                dataSet_ret.Features.VibrationEnergy.Y = Stastify.GetOA(dataSet_ret.FFTData.Y);
                dataSet_ret.Features.VibrationEnergy.Z = Stastify.GetOA(dataSet_ret.FFTData.Z);

                if (FilterActive)
                {
                    dataSet_ret.FFTData_Filtered.X = FFT.GetFFT(dataSet_ret.AccData_Filtered.X);
                    dataSet_ret.FFTData_Filtered.Y = FFT.GetFFT(dataSet_ret.AccData_Filtered.Y);
                    dataSet_ret.FFTData_Filtered.Z = FFT.GetFFT(dataSet_ret.AccData_Filtered.Z);
                    dataSet_ret.Features_Filtered.VibrationEnergy.X = Stastify.GetOA(dataSet_ret.FFTData_Filtered.X);
                    dataSet_ret.Features_Filtered.VibrationEnergy.Y = Stastify.GetOA(dataSet_ret.FFTData_Filtered.Y);
                    dataSet_ret.Features_Filtered.VibrationEnergy.Z = Stastify.GetOA(dataSet_ret.FFTData_Filtered.Z);
                }

                dataSet_ret.FFTData.FreqVec = FreqVecCal(dataSet_ret.FFTData.X.Count, samplingRate); ;

            }

            if (other_feature)
            {
                dataSet_ret.Features.AccP2P.X = Stastify.GetPP(dataSet_ret.AccData.X);
                dataSet_ret.Features.AccP2P.Y = Stastify.GetPP(dataSet_ret.AccData.Y);
                dataSet_ret.Features.AccP2P.Z = Stastify.GetPP(dataSet_ret.AccData.Z);

                dataSet_ret.Features.AccRMS.X = Stastify.RMS(dataSet_ret.AccData.X);
                dataSet_ret.Features.AccRMS.Y = Stastify.RMS(dataSet_ret.AccData.Y);
                dataSet_ret.Features.AccRMS.Z = Stastify.RMS(dataSet_ret.AccData.Z);

                if (FilterActive)
                {
                    dataSet_ret.Features_Filtered.AccP2P.X = Stastify.GetPP(dataSet_ret.AccData_Filtered.X);
                    dataSet_ret.Features_Filtered.AccP2P.Y = Stastify.GetPP(dataSet_ret.AccData_Filtered.Y);
                    dataSet_ret.Features_Filtered.AccP2P.Z = Stastify.GetPP(dataSet_ret.AccData_Filtered.Z);

                    dataSet_ret.Features_Filtered.AccRMS.X = Stastify.RMS(dataSet_ret.AccData_Filtered.X);
                    dataSet_ret.Features_Filtered.AccRMS.Y = Stastify.RMS(dataSet_ret.AccData_Filtered.Y);
                    dataSet_ret.Features_Filtered.AccRMS.Z = Stastify.RMS(dataSet_ret.AccData_Filtered.Z);
                }
            }
        }
        internal void DataForFFTCompensate(ref DataSet dataSet_ret)
        {
            var rem = 0.0;
            var sample_num = dataSet_ret.AccData.X.Count;
            rem = sample_num / 512.0;
            #region 補數據作FFT
            int result;
            bool parsedSuccessfully = int.TryParse(rem + "", out result);
            bool IsPowOF2 = parsedSuccessfully ? FftSharp.Transform.IsPowerOfTwo(result) : false;
            if (parsedSuccessfully == false | IsPowOF2 == false)//非整數或不是2的密次
            {
                var _d = FindMinPowOf2(sample_num);
                var comp_num = _d - sample_num;
                double[] comp_datas_x = new double[comp_num];
                double[] comp_datas_y = new double[comp_num];
                double[] comp_datas_z = new double[comp_num];

                Array.Copy(dataSet_ret.AccData.X.ToArray(), sample_num - comp_num, comp_datas_x, 0, comp_num);
                Array.Copy(dataSet_ret.AccData.Y.ToArray(), sample_num - comp_num, comp_datas_y, 0, comp_num);
                Array.Copy(dataSet_ret.AccData.Z.ToArray(), sample_num - comp_num, comp_datas_z, 0, comp_num);

                dataSet_ret.AccData.acc_x_For_FFT.AddRange(dataSet_ret.AccData.X);
                dataSet_ret.AccData.acc_x_For_FFT.AddRange(comp_datas_x.ToList());
                dataSet_ret.AccData.acc_y_For_FFT.AddRange(dataSet_ret.AccData.Y);
                dataSet_ret.AccData.acc_y_For_FFT.AddRange(comp_datas_y.ToList());
                dataSet_ret.AccData.acc_z_For_FFT.AddRange(dataSet_ret.AccData.Z);
                dataSet_ret.AccData.acc_z_For_FFT.AddRange(comp_datas_z.ToList());
            }
            #endregion
            else
            {
                dataSet_ret.AccData.acc_x_For_FFT.AddRange(dataSet_ret.AccData.X);
                dataSet_ret.AccData.acc_y_For_FFT.AddRange(dataSet_ret.AccData.Y);
                dataSet_ret.AccData.acc_z_For_FFT.AddRange(dataSet_ret.AccData.Z);
            }
        }
        internal int FindMinPowOf2(int Pt_num)
        {
            var Min_WS = -1;
            for (int i = 0; true; i++)
            {
                Min_WS = (int)Math.Pow(2, i);

                if (Min_WS > Pt_num)
                    return Min_WS;
            }

        }

        internal List<double> FreqVecCal(int FFTWindowSize, double samplingRate)
        {
            var freqVec = new List<double>();
            var NysFreq = samplingRate / 2;
            for (int i = 0; i < FFTWindowSize; i++)
            {
                freqVec.Add((NysFreq / (double)FFTWindowSize) * (double)i);
            }
            return freqVec;
        }
        #endregion

    }

    /// <summary>
    /// 高階版本感測器設定物件
    /// </summary>
    public class ModuleSetting
    {
        public int AccDataRevTimeout = 3000;
        public byte SlaveID = 0x00;
        public byte[] READParamCmdByteForModbus
        {
            get
            {
                byte[] cmd = { SlaveID, 0x03, 0x00, 0x10, 0x00, 0x04 };
                var crc = BitConverter.GetBytes(calculateCRC(cmd, 6, 0));
                return new byte[8] { SlaveID, 0x03, 0x00, 0x10, 0x00, 0x04, crc[0], crc[1] };
            }
        }

        public byte[] ModifyIDCmbByteForModbus(byte ID_ToChange)
        {
            byte[] cmd = { SlaveID, 0x06, 0x00, 0x20, 0x00, ID_ToChange };
            var crc = BitConverter.GetBytes(calculateCRC(cmd, 6, 0));
            return new byte[8] { SlaveID, 0x06, 0x00, 0x20, 0x00, ID_ToChange, crc[0], crc[1] };
        }

        public byte[] READRAWCmdByteForModbus
        {
            get
            {
                byte[] cmd = { SlaveID, 0x03, 0x00, 0xFF, 0x00, 0xFF };
                var crc = BitConverter.GetBytes(calculateCRC(cmd, 6, 0));
                return new byte[8] { SlaveID, 0x03, 0x00, 0xFF, 0x00, 0xFF, crc[0], crc[1] };
            }
        }
        internal enum SettingItem
        {
            SetDaqMode, SetDataLen, SetMeasureRange, NotSpecify
        }
        internal SettingItem settingItem = SettingItem.NotSpecify;
        public double SamplingRate { get; set; } = 10064;

        internal byte[] _SettingBytes = new byte[8] { 0x00, 0x02, 0x00, 0xC0, 0x2F, 0x00, 0x00, 0x00 };

        virtual internal byte[] SettingBytesWithHead
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
        internal byte[] SettingBytes
        {
            get
            {
                UpdateSettingBytes();
                return _SettingBytes;
            }
        }


        /// <summary>
        /// 單軸資料長度
        /// </summary>
        internal int _DataLength = 512;

        internal int DefaulDataLength = 256;
        /// <summary>
        /// 單軸資料長度
        /// </summary>
        virtual public int DataLength
        {
            get { return _DataLength; }
            set
            {
                _DataLength = value;
                UpdateSettingBytes();
            }
        }
        /// <summary>
        /// 取得封包總長度
        /// </summary>
        virtual public int PackageTotalLen
        {
            get
            {
                return _DataLength * 6;
            }
        }

        internal MEASURE_RANGE _mEASURE_RANGE = MEASURE_RANGE.MR_8G;
        virtual public MEASURE_RANGE mEASURE_RANGE
        {
            get { return _mEASURE_RANGE; }
            set
            {
                _mEASURE_RANGE = value;
                UpdateSettingBytes();
            }
        }
        internal DAQMode _Mode = DAQMode.High_Sampling;
        virtual public DAQMode Mode
        {
            get { return _Mode; }
            set
            {
                SamplingRate = value == DAQMode.High_Sampling ? 8000 : 3500;
                _Mode = value;
                UpdateSettingBytes();
            }
        }

        /// <summary>
        /// 取得當前的LSB值(與量測範圍直接相關)
        /// </summary>
        internal int LSB
        {
            get
            {
                return (int)_mEASURE_RANGE;
            }
        }

        /// <summary>
        /// 量測時間估計(毫秒)
        /// </summary>
        public int Measure_Time
        {
            get
            {
                int meas_time = (int)Math.Floor((double)_DataLength / SamplingRate * 1000);
                return meas_time;
            }
        }

        virtual internal void UpdateSettingBytes()
        {
            ///長度;先計算倍率
            var ratio = _DataLength * 6 / 1536;
            var DLHLBytes = ratio.ToHLBytes();
            _SettingBytes[0] = DLHLBytes[0];
            _SettingBytes[1] = DLHLBytes[1];
            //量測範圍
            _SettingBytes[3] = _mEASURE_RANGE.ToKXByte();
        }

        /// <summary>
        /// Calculates the CRC16 for Modbus-RTU
        /// </summary>
        /// <param name="data">Byte buffer to send</param>
        /// <param name="numberOfBytes">Number of bytes to calculate CRC</param>
        /// <param name="startByte">First byte in buffer to start calculating CRC</param>
        internal UInt16 calculateCRC(byte[] data, UInt16 numberOfBytes, int startByte)
        {
            byte[] auchCRCHi = {
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
            0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
            0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01,
            0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81,
            0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
            0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01,
            0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
            0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
            0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01,
            0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
            0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
            0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01,
            0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
            0x40
            };

            byte[] auchCRCLo = {
            0x00, 0xC0, 0xC1, 0x01, 0xC3, 0x03, 0x02, 0xC2, 0xC6, 0x06, 0x07, 0xC7, 0x05, 0xC5, 0xC4,
            0x04, 0xCC, 0x0C, 0x0D, 0xCD, 0x0F, 0xCF, 0xCE, 0x0E, 0x0A, 0xCA, 0xCB, 0x0B, 0xC9, 0x09,
            0x08, 0xC8, 0xD8, 0x18, 0x19, 0xD9, 0x1B, 0xDB, 0xDA, 0x1A, 0x1E, 0xDE, 0xDF, 0x1F, 0xDD,
            0x1D, 0x1C, 0xDC, 0x14, 0xD4, 0xD5, 0x15, 0xD7, 0x17, 0x16, 0xD6, 0xD2, 0x12, 0x13, 0xD3,
            0x11, 0xD1, 0xD0, 0x10, 0xF0, 0x30, 0x31, 0xF1, 0x33, 0xF3, 0xF2, 0x32, 0x36, 0xF6, 0xF7,
            0x37, 0xF5, 0x35, 0x34, 0xF4, 0x3C, 0xFC, 0xFD, 0x3D, 0xFF, 0x3F, 0x3E, 0xFE, 0xFA, 0x3A,
            0x3B, 0xFB, 0x39, 0xF9, 0xF8, 0x38, 0x28, 0xE8, 0xE9, 0x29, 0xEB, 0x2B, 0x2A, 0xEA, 0xEE,
            0x2E, 0x2F, 0xEF, 0x2D, 0xED, 0xEC, 0x2C, 0xE4, 0x24, 0x25, 0xE5, 0x27, 0xE7, 0xE6, 0x26,
            0x22, 0xE2, 0xE3, 0x23, 0xE1, 0x21, 0x20, 0xE0, 0xA0, 0x60, 0x61, 0xA1, 0x63, 0xA3, 0xA2,
            0x62, 0x66, 0xA6, 0xA7, 0x67, 0xA5, 0x65, 0x64, 0xA4, 0x6C, 0xAC, 0xAD, 0x6D, 0xAF, 0x6F,
            0x6E, 0xAE, 0xAA, 0x6A, 0x6B, 0xAB, 0x69, 0xA9, 0xA8, 0x68, 0x78, 0xB8, 0xB9, 0x79, 0xBB,
            0x7B, 0x7A, 0xBA, 0xBE, 0x7E, 0x7F, 0xBF, 0x7D, 0xBD, 0xBC, 0x7C, 0xB4, 0x74, 0x75, 0xB5,
            0x77, 0xB7, 0xB6, 0x76, 0x72, 0xB2, 0xB3, 0x73, 0xB1, 0x71, 0x70, 0xB0, 0x50, 0x90, 0x91,
            0x51, 0x93, 0x53, 0x52, 0x92, 0x96, 0x56, 0x57, 0x97, 0x55, 0x95, 0x94, 0x54, 0x9C, 0x5C,
            0x5D, 0x9D, 0x5F, 0x9F, 0x9E, 0x5E, 0x5A, 0x9A, 0x9B, 0x5B, 0x99, 0x59, 0x58, 0x98, 0x88,
            0x48, 0x49, 0x89, 0x4B, 0x8B, 0x8A, 0x4A, 0x4E, 0x8E, 0x8F, 0x4F, 0x8D, 0x4D, 0x4C, 0x8C,
            0x44, 0x84, 0x85, 0x45, 0x87, 0x47, 0x46, 0x86, 0x82, 0x42, 0x43, 0x83, 0x41, 0x81, 0x80,
            0x40
            };
            UInt16 usDataLen = numberOfBytes;
            byte uchCRCHi = 0xFF;
            byte uchCRCLo = 0xFF;
            int i = 0;
            int uIndex;
            while (usDataLen > 0)
            {
                usDataLen--;
                if ((i + startByte) < data.Length)
                {
                    uIndex = uchCRCLo ^ data[i + startByte];
                    uchCRCLo = (byte)(uchCRCHi ^ auchCRCHi[uIndex]);
                    uchCRCHi = auchCRCLo[uIndex];
                }
                i++;
            }
            return (UInt16)((UInt16)uchCRCHi << 8 | uchCRCLo);
        }

    }



}

