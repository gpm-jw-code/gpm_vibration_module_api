using gpm_vibration_module_api.GPMBase;
using gpm_vibration_module_api.GpmMath;
using gpm_vibration_module_api.Model.VibSensorParamSetting;
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
            Settings = new ModuleSetting_HighClassVersion() { _mEASURE_RANGE = MEASURE_RANGE.MR_8G };
            Console.WriteLine($"GPMModuleAPI_HSR 物件建立");
            //Tools.Logger.Event_Log.Log($"GPMModuleAPI_HSR 物件建立");
        }

        /// <summary>
        /// 是否為5K雙核版本
        /// </summary>
        public bool Is5KDaulCPUVersion
        {
            get => Settings.Is5KDaulCPUVersion;
            set
            {
                IsKX134Sensor = !value;
                Settings.Is5KDaulCPUVersion = value;
                SamplingRate = value ? 5032 : 10064;
                Settings._mEASURE_RANGE = value ? MEASURE_RANGE.MR_2G : MEASURE_RANGE.MR_8G;
            }
        }

        public string IP { get; set; }
        public int Port { get; set; }

        public string PortName { get; private set; }

        public int BaudRate { get; private set; }

        public ModuleSetting_HighClassVersion Settings { get; internal set; } = new ModuleSetting_HighClassVersion();

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
                if (AsynchronousClient == null) return false;
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
                return Settings.DataOuputLength;
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

        virtual public double SamplingRate
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
                int errorCode = await AsynchronousClient.AsyncConnect(IP, Port);
                if (errorCode != 0)
                {
                    Disconnect();
                    Tools.Logger.Event_Log.Log($"TCP/IP Connecet fail,ErrorCode:{errorCode}");
                    return errorCode;
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
                }
                catch (Exception ex)
                {
                    Tools.Logger.Event_Log.Log($"Try Disconnect fail>{ex.StackTrace} {ex.Message}.");
                    Console.WriteLine(ex.Message + "|" + ex.StackTrace);
                }
            }
            else
                SerialPortBase.PortClose();
            Tools.Logger.Event_Log.Log("Try Disconnect FNISH");
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
                var ori_Set = Settings.DataOuputLength;
                Settings.DataOuputLength = N;
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
                    Settings.DataOuputLength = ori_Set;
                    return -1;
                }
                else
                {
                    Settings.DataOuputLength = N;
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
        /// 修改ODR設定
        /// </summary>
        /// <param name="ODR"></param>
        public async Task<int> ODRSetting(double ODR)
        {
            Tools.Logger.Event_Log.Log($"使用者嘗試修改量測ODR({ODR})");
            if (ODR == Settings.ODR)
                return 0;
            var ori_ODR = Settings.ODR;
            Settings.ODR = ODR;
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
                Tools.Logger.Event_Log.Log($"ODR Setting Fail{state.ErrorCode}");
                Settings.ODR = ori_ODR;
                return -1;
            }
            else
            {
                Tools.Logger.Event_Log.Log($"ODR Setting OK::{ODR}");
                return 0;
            }
        }
        /// <summary>
        /// 量測範圍設定
        /// </summary>
        /// <param name="mr_select"></param>
        /// <returns></returns>
        virtual public async Task<int> Measure_Range_Setting(MEASURE_RANGE mr_select)
        {
            if (!IsKX134Sensor && ((int)mr_select) < 2048)
                return (int)clsErrorCode.Error.MRSettingOutOfRange;
            if (IsKX134Sensor && ((int)mr_select) > 4096)
                return (int)clsErrorCode.Error.MRSettingOutOfRange;
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
            StateObject state;
            int cnt = 0;
            while ((state = await SendMessageMiddleware(Settings.SettingBytesWithHead, ParamSetCheckLen, Timeout: 1000)).ErrorCode != clsErrorCode.Error.None)
            {
                cnt += 1;
                if (cnt == 2)
                    break;
                Thread.Sleep(1);
            }

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

        private StateObject state_obj = new StateObject() { ErrorCode = clsErrorCode.Error.DATA_GET_TIMEOUT };
        virtual public async Task<DataSet> GetData(bool IsGetFFT, bool IsGetOtherFeatures)
        {
            //Tools.Logger.Event_Log.Log($"Memory Used:{sys.Utility.GetUsedPhysMB()} MB");

            GetDataCalledStasify();
            Tools.Logger.Event_Log.Log("GetData Fun called");
            try
            {
                // var timeout = GetDataFirstCall ? 500 : Settings.Measure_Time + 8000;
                HSStopWatch.Restart();
                clsErrorCode.Error retcode = clsErrorCode.Error.DATA_GET_TIMEOUT;
                int retry = 0;
                state_obj.ClearBuffer();
                while (state_obj.ErrorCode != clsErrorCode.Error.None)
                {
                    state_obj.ClearBuffer();
                    if (retry >= 5)
                    {
                        Tools.Logger.Event_Log.Log($"GetData Retry Number >5, return {state_obj.ErrorCode}");
                        break;
                    }
                    Tools.Logger.Event_Log.Log($"GetData > SendGetRawDataCmd({retry})");
                    state_obj = SendGetRawDataCmd(_Is485Module ? 1000 : Settings.AccDataRevTimeout);
                    retcode = state_obj.ErrorCode;
                    retry += 1;
                    //Tools.Logger.Event_Log.Log($"Memory Used:{sys.Utility.GetUsedPhysMB()} MB");
                    Thread.Sleep(1);
                }

                HSStopWatch.Stop();
                //DataUploadToWeb();
                //Tools.Logger.Event_Log.Log($"Memory Used:{sys.Utility.GetUsedPhysMB()} MB");
                if (state_obj.ErrorCode == clsErrorCode.Error.None)
                {
                    GetDataSuccessNum++;
                    Tools.Logger.Event_Log.Log("封包接收完成:" + state_obj.DataByteList.Count + $"(單軸長度:{state_obj.DataByteList.Count / 6}|設定值:{Settings.DataOuputLength})");
                    DataSetForUser = PostProcessing(state_obj.DataByteList, IsGetFFT, IsGetOtherFeatures);
                    DataSetForUser.RecieveTime = DateTime.Now;
                    DataSetForUser.TimeSpend = HSStopWatch.ElapsedMilliseconds;
                    return DataSetForUser;
                }
                else
                {
                    return new DataSet(0) { ErrorCode = state_obj.ErrorCode == clsErrorCode.Error.DATA_GET_INTERUPT ? (int)clsErrorCode.Error.DATA_GET_INTERUPT : (int)clsErrorCode.Error.DATA_GET_TIMEOUT };
                }
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
            return state.ErrorCode == clsErrorCode.Error.None ? state.DataByteList.ToArray().ToCommaHexString() : state.ErrorCode.ToString();
        }
        virtual public async Task<bool> SendKXRegisterSetting(int CNTL1, int ODCNTL)
        {
            Settings._SettingBytes[3] = (byte)CNTL1;
            Settings._SettingBytes[4] = (byte)ODCNTL;
            var state = await SendMessageMiddleware(Settings.SettingBytesWithHead, ParamSetCheckLen, Timeout: 3000);
            if (state.ErrorCode == clsErrorCode.Error.None)
            {
                //定義量測範圍
                var bitStringCNTL1 = Convert.ToString(state.DataByteList[3], 2);
                MeasureRangeDefine(bitStringCNTL1);
                var bitStringODCNTL = Convert.ToString(state.DataByteList[4], 2);
                ODRDefine(bitStringODCNTL);
                //定義ODR
            }
            return state.ErrorCode == clsErrorCode.Error.None;
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
                state_obj = SendMessageMiddleware("READVALUE\r\n", Settings.PacketLengthOfDeviceShoultReturn, Timeout).Result;
            else
                state_obj = SendMessageMiddleware(Settings.READRAWCmdByteForModbus, Settings.PacketLengthOfDeviceShoultReturn, Timeout).Result;
            return state_obj;
        }
        #endregion
        #region Private Methods

        /// <summary>
        /// 從參數回傳值定義量測範圍
        /// </summary>
        /// <param name="bitString"></param>
        private void MeasureRangeDefine(string bitString)
        { //補0
            var zero_com_num = 8 - bitString.Length;
            string zerosstr = "";
            for (int i = 0; i < zero_com_num; i++)
                zerosstr += "0";
            bitString = zerosstr + bitString;
            if (bitString[3] == '1' && bitString[4] == '1')
                Settings.mEASURE_RANGE = MEASURE_RANGE.MR_64G;
            if (bitString[3] == '1' && bitString[4] == '0')
                Settings.mEASURE_RANGE = MEASURE_RANGE.MR_32G;
            if (bitString[3] == '0' && bitString[4] == '1')
                Settings.mEASURE_RANGE = MEASURE_RANGE.MR_16G;
            if (bitString[3] == '0' && bitString[4] == '0')
                Settings.mEASURE_RANGE = MEASURE_RANGE.MR_8G;
        }
        /// <summary>
        /// 從參數回傳值定義ODR
        /// </summary>
        /// <param name="bitString"></param>
        private void ODRDefine(string bitString)
        {
            //補0
            var zero_com_num = 8 - bitString.Length;
            string zerosstr = "";
            for (int i = 0; i < zero_com_num; i++)
                zerosstr += "0";
            bitString = zerosstr + bitString;
            var osa3 = bitString[4];
            var osa2 = bitString[5];
            var osa1 = bitString[6];
            var osa0 = bitString[7];
            if (osa3 == '1' && osa2 == '1' && osa1 == '1' && osa0 == '1')
                Settings.ODR = 25600;
            if (osa3 == '1' && osa2 == '1' && osa1 == '1' && osa0 == '0')
                Settings.ODR = 12800;
            if (osa3 == '1' && osa2 == '1' && osa1 == '0' && osa0 == '1')
                Settings.ODR = 6400;
            if (osa3 == '1' && osa2 == '1' && osa1 == '0' && osa0 == '0')
                Settings.ODR = 3200;
            if (osa3 == '1' && osa2 == '0' && osa1 == '1' && osa0 == '1')
                Settings.ODR = 1600;
            if (osa3 == '1' && osa2 == '0' && osa1 == '1' && osa0 == '0')
                Settings.ODR = 800;
            if (osa3 == '1' && osa2 == '0' && osa1 == '0' && osa0 == '1')
                Settings.ODR = 400;
            if (osa3 == '1' && osa2 == '0' && osa1 == '0' && osa0 == '0')
                Settings.ODR = 200;
            if (osa3 == '0' && osa2 == '1' && osa1 == '1' && osa0 == '1')
                Settings.ODR = 100;
            if (osa3 == '0' && osa2 == '1' && osa1 == '1' && osa0 == '0')
                Settings.ODR = 50;
            if (osa3 == '0' && osa2 == '1' && osa1 == '0' && osa0 == '1')
                Settings.ODR = 25;
            if (osa3 == '0' && osa2 == '1' && osa1 == '0' && osa0 == '0')
                Settings.ODR = 12.5;
            if (osa3 == '0' && osa2 == '0' && osa1 == '1' && osa0 == '1')
                Settings.ODR = 6.25;
            if (osa3 == '0' && osa2 == '0' && osa1 == '1' && osa0 == '0')
                Settings.ODR = 3.125;
            if (osa3 == '0' && osa2 == '0' && osa1 == '0' && osa0 == '1')
                Settings.ODR = 1.563;
            if (osa3 == '0' && osa2 == '0' && osa1 == '0' && osa0 == '0')
                Settings.ODR = 0.781;
        }
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
        public async Task<int> Connect()
        {
            return await Connect(this.IP, this.Port);
        }
        private new async Task<int> Reconnect()
        {
            #region Timeout的時候將連線關閉在重連(才能把非同步接收進程關掉)
            Settings.DefaulDataOutPutLength = Settings.DataOuputLength;
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
              new int[2]{1,Settings.DefaulDataOutPutLength/256}, new int[2]{3,192}, new int[2]{4, 47}
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
            var state_2 = await Data_Length_Setting(Settings.DefaulDataOutPutLength);
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
                        int len = (IsKX134Sensor | Is5KDaulCPUVersion) ? Settings.DataOuputLength * Settings.CompValueOfDownSample * 6 : raw_bytes.Count;
                        _raw_bytes = new byte[len];
                        Array.Copy(raw_bytes.ToArray(), 0, _raw_bytes, 0, _raw_bytes.Length);
                    }
                    catch (Exception ex)
                    {
                        Tools.Logger.Event_Log.Log("[ARRAY COPY FAIL]" + ex.Message);
                        return new DataSet(0) { ErrorCode = (int)clsErrorCode.Error.SYSTEM_ERROR };
                    }
                }
                List<List<double>> ori_xyz_data_list = null;

                if (IsKX134Sensor | Is5KDaulCPUVersion)
                    ori_xyz_data_list = Tools.ConverterTools.AccPacketToListDouble_KX134(_raw_bytes, Settings.LSB, MiniPacketDataLen);
                else
                    ori_xyz_data_list = Tools.ConverterTools.AccPacketToListDouble(_raw_bytes, Settings.mEASURE_RANGE, Settings.Mode);
                // List<List<double>> XYZ_Acc_Data_List = Filters.LPF(ori_xyz_data_list, LowpassFilterCutOffFreq, Settings.SamplingRate); //濾波

                List<List<double>> down_sample_ls = DownSampleProcessingAsync(ori_xyz_data_list);

                DataSet dataSet_ret = new DataSet(Settings.SamplingRate) { RecieveTime = DateTime.Now };
                //dataSet_ret.AccData.X = ori_xyz_data_list[0];
                //dataSet_ret.AccData.Y = ori_xyz_data_list[1];
                //dataSet_ret.AccData.Z = ori_xyz_data_list[2];

                dataSet_ret.AccData.X = down_sample_ls[0];
                dataSet_ret.AccData.Y = down_sample_ls[1];
                dataSet_ret.AccData.Z = down_sample_ls[2];

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
                PhysicalQuantityCal(ref dataSet_ret, Settings.SamplingRate);
                FFTAndFeatureCal(ref dataSet_ret, GetFFT, GetFeatures, Settings.SamplingRate);
                return dataSet_ret;
            }
            catch (Exception ex)
            {
                Tools.Logger.Event_Log.Log("[PostProcessingError]" + ex.Message);
                return new DataSet(0) { ErrorCode = (int)clsErrorCode.Error.PostProcessingError };
            }
        }

        private void PhysicalQuantityCal(ref DataSet dataSet_ret, double samplingRate)
        {
            dataSet_ret.PhysicalQuantity = new DataSet.clsPhysicalQuantity();
            double[] g_array_x = dataSet_ret.AccData.X.ToArray();
            double[] g_array_y = dataSet_ret.AccData.Y.ToArray();
            double[] g_array_z = dataSet_ret.AccData.Z.ToArray();
            dataSet_ret.PhysicalQuantity.X.Displacement = JMAlgorithm.PhysicalQuantity.GetDisplacement(g_array_x, samplingRate);
            dataSet_ret.PhysicalQuantity.Y.Displacement = JMAlgorithm.PhysicalQuantity.GetDisplacement(g_array_y, samplingRate);
            dataSet_ret.PhysicalQuantity.Z.Displacement = JMAlgorithm.PhysicalQuantity.GetDisplacement(g_array_z, samplingRate);

            dataSet_ret.PhysicalQuantity.X.Velocity = JMAlgorithm.PhysicalQuantity.GetVelocity(g_array_x, samplingRate);
            dataSet_ret.PhysicalQuantity.Y.Velocity = JMAlgorithm.PhysicalQuantity.GetVelocity(g_array_y, samplingRate);
            dataSet_ret.PhysicalQuantity.Z.Velocity = JMAlgorithm.PhysicalQuantity.GetVelocity(g_array_z, samplingRate);

        }

        /// <summary>
        /// 降取樣
        /// </summary>
        /// <param name="ori_xyz_data_list"></param>
        private List<List<double>> DownSampleProcessingAsync(List<List<double>> ori_xyz_data_list)
        {
            double d;

            List<double>[] output = new List<double>[3];
            int ra = Convert.ToInt32(Math.Ceiling(1.0 / Settings._downSamplingRatio) + "");
            int output_len = Convert.ToInt32(Math.Floor(ori_xyz_data_list[0].Count * Settings._downSamplingRatio) + "");
            for (int i = 0; i < 3; i++)
            {

                List<double> ortSamples = ori_xyz_data_list[i];
                List<double> downSamples = new List<double>();
                //ratio = 0.5  1/0.5 = 2 '
                for (int j = 0; j < ori_xyz_data_list[0].Count; j += ra)
                {
                    downSamples.Add(ortSamples[j]);
                }
                output[i] = downSamples;

            }
            return output.ToList();
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



}

