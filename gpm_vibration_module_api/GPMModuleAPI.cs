#define YCM
#define BR460800

using gpm_vibration_module_api.GpmMath;
using gpm_vibration_module_api.Module;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using FftSharp;
using static gpm_vibration_module_api.ClsModuleBase;
using Accord.Audio.Filters;
using static gpm_vibration_module_api.GpmMath.Window;
using gpm_vibration_module_api.sys;
using gpm_module_api.License;
using gpm_module_api;
using WINDOW = gpm_vibration_module_api.GpmMath.Window.WINDOW;

namespace gpm_vibration_module_api
{
    /// <summary>
    /// For User using.
    /// </summary>
    public class GPMModuleAPI
    {
        public GPMModuleAPI(GPMModulesServer.ConnectInState _ConnectObj = null)
        {
            if (_ConnectObj != null)
            {
                this.SensorIP = _ConnectObj.IP;
                this.ModuleSocket = _ConnectObj.ClientSocket;
            }
            DataSet.AutoDelete();
            Sys_Config_Load();

            // stake.API.KeyProInsertEvent += API_KeyProInsertEvent;
            //  KeyproMdule.API.KeyProRemoveEvent += API_KeyProRemoveEvent;
#if KeyproEnable
            KeyProExisStatus = Enviroment.IsNoNeedKey ? staKeypro.KeyProCheckStatus.Exist : staKeypro.KeyproCheck().ExistSttate;
#endif
            WaitAsyncForGetDataTask = new ManualResetEvent(false);
            WaitAsyncForParametersSet = new ManualResetEvent(true);
            GetDataTaskPause = new ManualResetEvent(true);

            module_base.module_settings.SensorType = clsEnum.Module_Setting_Enum.SENSOR_TYPE.Genernal;
            WifiSensorUsing = true;

            module_base.DataRecieve += Module_base_DataReady;
            Thread.Sleep(100);

            if (_ConnectObj != null)
                Connect(SensorIP, SensorPort, ModuleSocket, true);

        }

        const string LicenseFilePath = "license.lic";
        internal bool LicenseCheck = false;
        /// <summary>
        /// Sampling Rate計算結果儲存
        /// </summary>
        public struct Sampling_Rate_Cal_Result
        {
            public List<DataSet> Datasets_For_Cal;
            public enum ERROR_CODE
            {
                DATA_LACK = 403, SETTING_ERROR = 404, OK = 0
            }
            public DateTime Test_Time;
            public double[] Sampling_Rate;
            public ERROR_CODE RESULT;
        }
        #region Public Property
        public string Location { get; set; }
        public event Action<DateTime> DisconnectEvent;
        public event Action<DataSet> DataRecieve;
        public Action<string> ReConnectEvent { get; set; }
        public string SensorIP { get; private set; } = "";
        public bool IsDataHandShakeNormal { private set; get; }
        #endregion

        #region Private Property
        private clsEnum.FWSetting_Enum.ACC_CONVERT_ALGRIUM ACC_CONVERT_ALGRIUM = clsEnum.FWSetting_Enum.ACC_CONVERT_ALGRIUM.Old;
        /// <summary>
        /// 控制器底層控制
        /// </summary>
        internal ClsModuleBase module_base = new ClsModuleBase();
        /// <summary>
        /// 設定感測器安裝位置名稱
        /// </summary>
        private MeasureOption option = new MeasureOption();
        private DataSet DataSetRet = new DataSet(1000);
        private bool IsGetFFT = false;
        private bool IsGetOtherFeatures = false;
        internal ManualResetEvent WaitAsyncForGetDataTask;
        internal ManualResetEvent WaitAsyncForParametersSet;
        /// <summary>
        /// 斷線事件
        /// </summary>

        private ManualResetEvent GetDataTaskPause;
        private bool IsGetDataTaskPaused = true;
        private List<double> Freq_Vec = new List<double>();
        private int DataSetCnt = 0;
        private int SensorPort;
        private int window_size = 512;
        #endregion

        #region 存取子
        /// <summary>
        /// 取得數據封包擷取模式
        /// </summary>
        public DAQMode DAQMode
        {
            get
            {
                return module_base.module_settings.dAQMode;
            }

        }

        public bool LowPassFilterActive
        {
            get
            {
                if (module_base.module_settings.lowPassFilter == null)
                    return false;
                return module_base.module_settings.lowPassFilter.Active;
            }
            set
            {

                module_base.module_settings.lowPassFilter.Active = value;
                Sensor_Config_Save();
            }
        }
        public double LowPassFilterCutOffFreq
        {
            get
            {
                if (module_base.module_settings.lowPassFilter == null)
                    return 1700;
                return module_base.module_settings.lowPassFilter.CutOffFreq;
            }
            set
            {
                module_base.module_settings.lowPassFilter.CutOffFreq = value;
                Sensor_Config_Save();
            }
        }

        public Socket ModuleSocket
        {
            get
            {
                return module_base.module_socket;
            }
            set
            {
                module_base.module_socket = value;
                SensorIP = GetIPFromSocketObj(value);
            }
        }


        public int PacketNumComp
        {
            get
            {
                return module_base.module_settings.comp_len;
            }
            set
            {
                module_base.module_settings.comp_len = value;
                Sensor_Config_Save();
            }
        }



        public int WindowSize
        {
            get
            { return window_size; }
            set
            { window_size = value; }
        }
        /// <summary>
        /// 設定/取得量測範圍
        /// </summary>
        public clsEnum.Module_Setting_Enum.MEASURE_RANGE MeasureRange
        {
            internal set
            {
                module_base.setTaskObj = new ClsParamSetTaskObj(module_base.module_settings.dAQMode)
                {
                    SettingItem = 2,
                    SettingValue = value
                };
                StartParamSetTaskAsync();
            }
            get
            {
                return module_base.module_settings.MeasureRange;
            }
        }
        public int MeasureRange_IntType
        {

            set
            {
                //BULKBreak();
                module_base.setTaskObj.SettingItem = 2;
                switch (value)
                {
                    case 2:
                        module_base.setTaskObj.SettingValue = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_2G;
                        break;
                    case 4:
                        module_base.setTaskObj.SettingValue = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_4G;
                        break;
                    case 8:
                        module_base.setTaskObj.SettingValue = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_8G;
                        break;
                    case 16:
                        module_base.setTaskObj.SettingValue = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_16G;
                        break;
                    default:
                        module_base.setTaskObj.SettingValue = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_2G;
                        break;
                }

                StartParamSetTaskAsync();
            }
            get
            {
                return 16384 / Convert.ToInt32(module_base.module_settings.MeasureRange) * 2;
            }
        }

        /// <summary>
        /// 設定/取得封包資料長度
        /// </summary>
        public int DataLength
        {
            internal set
            {

                module_base.module_settings.DataLength = value;
            }
            get
            {
                return module_base.module_settings.DataLength;
            }
        }
        /// <summary>
        /// 設定/取得封包資料長度
        /// </summary>
        public int DataLength_IntType
        {
            set
            {
                if (SensorType != clsEnum.Module_Setting_Enum.SENSOR_TYPE.High)
                    return;
                module_base.setTaskObj.SettingItem = 1;
                switch (value)
                {
                    case 512:
                        module_base.setTaskObj.SettingValue = clsEnum.Module_Setting_Enum.DATA_LENGTH.x1;
                        break;
                    case 1024:
                        module_base.setTaskObj.SettingValue = clsEnum.Module_Setting_Enum.DATA_LENGTH.x2;
                        break;
                    case 2048:
                        module_base.setTaskObj.SettingValue = clsEnum.Module_Setting_Enum.DATA_LENGTH.x4;
                        break;
                    case 4096:
                        module_base.setTaskObj.SettingValue = clsEnum.Module_Setting_Enum.DATA_LENGTH.x8;
                        break;
                    default:
                        module_base.setTaskObj.SettingValue = clsEnum.Module_Setting_Enum.DATA_LENGTH.x1;
                        break;
                }
                StartParamSetTaskAsync();
            }
            get
            {
                return Convert.ToInt32(module_base.module_settings.DataLength);
            }
        }

        /// <summary>
        /// 設定感測器類型
        /// </summary>
        public clsEnum.Module_Setting_Enum.SENSOR_TYPE SensorType
        {
            internal set
            {
                module_base.setTaskObj.SettingItem = 0;
                module_base.setTaskObj.SettingValue = value;
                StartParamSetTaskAsync();
            }
            get
            {
                return module_base.module_settings.SensorType;
            }
        }
        /// <summary>
        /// 設定加速規濾波設定
        /// </summary>
        public clsEnum.Module_Setting_Enum.ODR ODR
        {
            set
            {
                module_base.setTaskObj.SettingItem = 3;
                module_base.setTaskObj.SettingValue = value;
                StartParamSetTaskAsync();
            }
            get
            {
                return module_base.module_settings.ODR;
            }
        }

        public int AccDataRevTimeOut
        {
            set
            {
                try
                {
                    int _result;
                    Int32.TryParse(value + "", out _result);
                    module_base.acc_data_rev_timeout = _result;
                }
                catch (Exception ex)
                {
                }
            }
            get
            {
                return module_base.acc_data_rev_timeout;
            }
        }

        public int ParamRWTimeOut
        {
            set
            {
                try
                {
                    int _result;
                    Int32.TryParse(value + "", out  _result);
                    module_base.fw_parm_rw_timeout = _result;
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            get
            {
                return module_base.fw_parm_rw_timeout;
            }
        }
        /// <summary>
        /// 取得連線狀態
        /// </summary>
        public bool Connected
        {
            get
            {
                if (module_base.module_socket == null)
                    return false;
                return module_base.module_socket.Connected;
            }
        }

        /// <summary>
        /// 設定/取得感測器取樣頻率
        /// </summary>
        public double SamplingRate
        {
            get
            {
                return module_base.module_settings.sampling_rate_;
            }
            set
            {
                module_base.module_settings.sampling_rate_ = value;

                Freq_Vec = FreqVecCal();
                Sensor_Config_Save();
            }
        }
        public bool WifiSensorUsing
        {
            set
            {
                module_base.module_settings.WifiControllUseHighSppedSensor = value;
            }
            get
            {
                return module_base.module_settings.WifiControllUseHighSppedSensor;
            }
        }
        #endregion


        public int Buffer_size
        {
            get
            {
                return SocketState.Packet_Receive_Size;
            }
            set
            {
                SocketState.Packet_Receive_Size = value;
                Sensor_Config_Save();
            }
        }

        public int Delay_Fine_tune
        {
            set
            {
                ClsModuleBase.delay_ = value;
                Sensor_Config_Save();
            }
            get
            {
                return ClsModuleBase.delay_;
            }
        }

        public enum Enum_AccGetMethod
        {
            Auto, Manual
        }
#if KeyproEnable
        private clsEnum.KeyPro.KEYPRO_EXIST_STATE KeyProExisStatus = clsEnum.KeyPro.KEYPRO_EXIST_STATE.NoInsert;
#else
        private clsEnum.KeyPro.KEYPRO_EXIST_STATE KeyProExisStatus = clsEnum.KeyPro.KEYPRO_EXIST_STATE.Exist;

#endif
        #region Constructors
        public GPMModuleAPI(string IP = null)
        {
            Sys_Config_Load();

            if (IP != null)
                SensorIP = IP;

            KeyproMdule.API.KeyProInsertEvent += API_KeyProInsertEvent;
            KeyproMdule.API.KeyProRemoveEvent += API_KeyProRemoveEvent;
#if KeyproEnable
            var ret = KeyproMdule.API.IsKeyInsert();
#endif
            WaitAsyncForGetDataTask = new ManualResetEvent(false);
            WaitAsyncForParametersSet = new ManualResetEvent(true);
            GetDataTaskPause = new ManualResetEvent(true);

#if YCM
            module_base.module_settings.SensorType = clsEnum.Module_Setting_Enum.SENSOR_TYPE.High;
            WifiSensorUsing = false;

#else
            module_base.moduleSettings.SensorType = clsEnum.Module_Setting_Enum.SensorType.Genernal;
            WifiSensorUsing = true;

            module_base.DataRecieve += Module_base_DataReady;
#endif
            module_base.DataRecieve += Module_base_DataReady;
            Thread.Sleep(100);

        }

        public GPMModuleAPI(clsEnum.Module_Setting_Enum.SENSOR_TYPE sensorType)
        {
            KeyproMdule.API.KeyProInsertEvent += API_KeyProInsertEvent;
            KeyproMdule.API.KeyProRemoveEvent += API_KeyProRemoveEvent;
#if KeyproEnable
            var ret = KeyproMdule.API.IsKeyInsert();
#endif
            WaitAsyncForGetDataTask = new ManualResetEvent(false);
            WaitAsyncForParametersSet = new ManualResetEvent(true);
            GetDataTaskPause = new ManualResetEvent(true);
            module_base.module_settings.SensorType = sensorType;
            module_base.DataRecieve += Module_base_DataReady;
        }
        #endregion

        /// <summary>
        /// 存放所有連線socket
        /// </summary>
        public static Dictionary<string, Socket> socket_conected_list { private set; get; } = new Dictionary<string, Socket>();
        /// <summary>
        /// 斷開所有的模組連線並釋放資源
        /// </summary>
        public static void Dispose()
        {
            foreach (var sock in socket_conected_list)
            {
                try
                {
                    sock.Value.Shutdown(SocketShutdown.Both);
                }
                catch
                {

                }
                try
                {
                    sock.Value.Close();
                }
                catch
                {

                }
            }
        }

        /// <summary>
        /// 設定DAQ擷取模式 
        /// </summary>
        /// <param name="Mode"></param>
        /// <returns><para> 0: 切換成功   </para> <para> Others: Error Code </para></returns>
        /// 
        public virtual async Task<int> DAQModeSetting(DAQMode Mode,bool IsNeedReboot=false)
        {
            if (Mode == module_base.module_settings.dAQMode)
                return 0;
            var ret = await Task<int>.Run(async () =>
           {
               var ret2 = await Data_Length_Setting(Mode, IsNeedReboot);
               if (ret2 == 0)
               {
                   SamplingRate = Convert.ToDouble(Mode);
                   ACC_CONVERT_ALGRIUM = Mode == DAQMode.High_Sampling ? clsEnum.FWSetting_Enum.ACC_CONVERT_ALGRIUM.Old : clsEnum.FWSetting_Enum.ACC_CONVERT_ALGRIUM.New;
                   module_base.module_settings.dAQMode = Mode;
                   Sensor_Config_Save();
               }
               return ret2;
           });
            return ret;
        }




        private void UpdateParam()
        {
            var param = module_base.module_settings.ByteAryOfParameters;
            var cmd = new byte[11] { 0x53, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0d, 0x0a };
            Array.Copy(param, 0, cmd, 1, param.Length);
            module_base.SendCommand(cmd, 8);
        }

        /// <summary>
        /// 計算Sensor 取樣頻率
        /// </summary>
        /// <param name="ref_Frq">參考激振源頻率</param>
        /// <param name="Period">測試秒數</param>
        /// <returns><para> 0: 切換成功   </para> <para> Others: Error Code </para></returns>
        public async Task<Sampling_Rate_Cal_Result> Start_Sampling_Rate_Calculate(double ref_Frq, int Period)
        {

            Stop_All_Action();

            var srcr_state = new Sampling_Rate_Cal_Result() { Datasets_For_Cal = new List<DataSet>(), Sampling_Rate = new double[3] };

            if (ref_Frq <= 0 | Period <= 0)
            {
                srcr_state.RESULT = Sampling_Rate_Cal_Result.ERROR_CODE.SETTING_ERROR;
                return srcr_state;
            }

            Stopwatch timer = new Stopwatch(); timer.Start();
            while (timer.ElapsedMilliseconds < Period)
            {
                var dataset = await GetData(false, false);
                srcr_state.Datasets_For_Cal.Add(dataset);
            }

            srcr_state.Datasets_For_Cal = (from dataset in srcr_state.Datasets_For_Cal
                                           where dataset.ErrorCode == 0
                                           select dataset).ToList();

            srcr_state.RESULT = srcr_state.Datasets_For_Cal.Count < Tools.Sampling_Rate_Calculator.Datasets_minimum ? Sampling_Rate_Cal_Result.ERROR_CODE.DATA_LACK : Sampling_Rate_Cal_Result.ERROR_CODE.OK;
            if (srcr_state.RESULT != Sampling_Rate_Cal_Result.ERROR_CODE.OK)
            {
                srcr_state.Sampling_Rate[0] = srcr_state.Sampling_Rate[1] = srcr_state.Sampling_Rate[2] = -1;
                return srcr_state;
            }
            var ret = Tools.Sampling_Rate_Calculator.Calibration(srcr_state.Datasets_For_Cal, ref_Frq);
            srcr_state.Sampling_Rate[0] = ret.X_SamplingRate;
            srcr_state.Sampling_Rate[1] = ret.Y_SamplingRate;
            srcr_state.Sampling_Rate[2] = ret.Z_SamplingRate;

            return srcr_state;
        }

        private void API_KeyProRemoveEvent(DateTime obj)
        {
            KeyProExisStatus = clsEnum.KeyPro.KEYPRO_EXIST_STATE.NoInsert;
        }

        private void API_KeyProInsertEvent(DateTime obj)
        {
            KeyProExisStatus = clsEnum.KeyPro.KEYPRO_EXIST_STATE.Exist;
        }




        public event Action<string> ConnectEvent;
        public Enum_AccGetMethod NowAccGetMethod = Enum_AccGetMethod.Manual;


        /// <summary>
        /// 與控制器進行連線
        /// </summary>
        /// <param name="IP">控制器IP</param>
        /// <param name="Port">控制器Port</param>
        /// <returns></returns>
        public async Task<int> Connect(string IP, int Port, bool IsSelfTest = true)
        {
            return await Connect(IP, Port, null, IsSelfTest);
        }


        public async Task<int> Connect(bool IsSelfTest = false)
        {
            var task = Connect(SensorIP, SensorPort, IsSelfTest);
            await task;
            return task.Result;
        }
        /// <summary>
        /// 與控制器進行連線
        /// </summary>
        /// <param name="IP">控制器IP</param>
        /// <param name="Port">控制器Port</param>
        /// <returns><para> 0: 連線成功   </para> <para> Others: Error Code </para></returns>
        public async Task<int> Connect(string IP, int Port = -1, Socket module_Socket = null, bool IsSelfTest = true)
        {
            IP = IP.Replace(" ", "");
            var IPPortCheckResult = IPPortCheck(IP, Port);
            if (IPPortCheckResult != 0)
                return IPPortCheckResult;

            if (LicenseCheck)
            {
                LicenseCheck licenseChecker = new LicenseCheck();
                LicenseCheckState check_state = licenseChecker.Check(LicenseFilePath);
                switch (check_state.CHECK_RESULT)
                {
                    case CHECK_RESULT.PASS:
                        break;
                    case CHECK_RESULT.EXPIRED:
                        return (int)clsErrorCode.Error.LicenseExpired;
                    case CHECK_RESULT.FAIL:
                        return (int)clsErrorCode.Error.LicenseCheckFail;
                    case CHECK_RESULT.LOSS:
                        return (int)clsErrorCode.Error.LicenseFileNoExist;
                    default:
                        break;
                }
            }

            try
            {
                SocketInitialize(IP, Port);
                GetDataWaitFlagOn = true;
                Stop_All_Action();
                module_base.acc_data_read_task_token_source.Cancel();
                var ret = await module_base.Connect(IP, Port);
                if (ret == 0)
                {
                    ConnectEvent?.Invoke(IP);
                    ReConnectEvent?.Invoke(IP);
                    Sensor_Config_Load();
                    if (IsSelfTest)
                    {
                        IsDataHandShakeNormal = await SelfTest();
                        ret = IsDataHandShakeNormal ? ret : Convert.ToInt32(clsErrorCode.Error.SelfTestFail);
                    }
                }
                else
                {

                }
                socket_conected_list[IP] = module_base.module_socket;
                var err_descript = ret == 1506 ? "socket connect_but handshake fail" : ret == 603 ? "Connection can't established" : "???";
                Tools.Logger.Event_Log.Log($"[Fun: Connecnt() ] {(ret == 0 ? "Successfully Established Connection." : $"{err_descript}ErrorCode:{ret}.")} IP:{IP}, Port:{Port}");
                return ret;
            }
            catch (OperationCanceledException exp)
            {
                return Convert.ToInt32(clsErrorCode.Error.SelfTestFail);
            }
            catch (Exception exp)
            {
                Tools.Logger.Event_Log.Log($"[Fun: Connecnt() ] Connect Fail, EXCEPTION OCCURIED");
                Tools.Logger.Code_Error_Log.Log($"[Fun: Connecnt() ] {exp.Message},{exp.StackTrace}");
                return -69;
            }
        }

        /// <summary>
        /// 檢查IP跟Port傳入值是否合法
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="Port"></param>
        /// <returns></returns>
        private int IPPortCheck(string IP, int Port)
        {
            Tools.Logger.Event_Log.Log($"[Fun: Connecnt() ] IP:{IP}, Port:{Port}");
            if (KeyProExisStatus == clsEnum.KeyPro.KEYPRO_EXIST_STATE.NoInsert)
            {
                Tools.Logger.Event_Log.Log($"[Fun: Connecnt() ] Connect Fail, Keypro Not Found");
                return Convert.ToInt32(clsErrorCode.Error.KEYPRO_NOT_FOUND);
            }
            if (IP.Split('.').Length != 4 | IP == "")
            {
                Tools.Logger.Event_Log.Log($"[Fun: Connecnt() ] Connect Fail, IP Illegal");
                return Convert.ToInt32(clsErrorCode.Error.IPIllegal);
            }
            if (Port <= 0)
            {
                Tools.Logger.Event_Log.Log($"[Fun: Connecnt() ] Connect Fail, Port Illegal");
                return Convert.ToInt32(clsErrorCode.Error.PortIllegal);
            }
            return 0;
        }

        /// <summary>
        /// 初始化Socket物件
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="Port"></param>
        private void SocketInitialize(string IP, int Port)
        {
            try
            {
                if (socket_conected_list.ContainsKey(IP))
                {
                    Tools.Logger.Event_Log.Log($"[Fun: Connecnt() ] Detect Connection already , do Disconnect()");
                    Disconnect();
                }
                SensorIP = IP;
                SensorPort = Port;
                Socket _socket = null;
                if (socket_conected_list.TryGetValue(IP, out _socket))
                {
                    try
                    {
                        _socket.Shutdown(SocketShutdown.Both);
                        _socket.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                }
                else
                    socket_conected_list.Add(IP, null);
            }
            catch (Exception ex)
            {
            }

        }

        private byte GetByteValofMRDefine(clsEnum.Module_Setting_Enum.MEASURE_RANGE mr)
        {
            switch (mr)
            {
                case clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_2G:
                    return 0x00;
                case clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_4G:
                    return 0x10;
                case clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_8G:
                    return 0x20;
                case clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_16G:
                    return 0x30;
                default:
                    return 0x00;
            }
        }

        private byte GetByteValofDLDefine(clsEnum.Module_Setting_Enum.DATA_LENGTH dl)
        {
            switch (dl)
            {
                case clsEnum.Module_Setting_Enum.DATA_LENGTH.x1:
                    return 0x01;
                case clsEnum.Module_Setting_Enum.DATA_LENGTH.x2:
                    return 0x02;
                case clsEnum.Module_Setting_Enum.DATA_LENGTH.x4:
                    return 0x04;
                case clsEnum.Module_Setting_Enum.DATA_LENGTH.x8:
                    return 0x08;
                case clsEnum.Module_Setting_Enum.DATA_LENGTH.x16:
                    return 0x10;
                default:
                    return 0x01;
            }
        }

        private async Task<bool> SelfTest()
        {
            Tools.Logger.Event_Log.Log("[SelfTEst] Write..");
            byte[] send_bytes = new byte[11] { 0x53, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0d, 0x0a };
            Array.Copy(module_base.module_settings.ByteAryOfParameters, 0, send_bytes, 1, module_base.module_settings.ByteAryOfParameters.Length);
            send_bytes[1] = 0x01;
            send_bytes[2] = (byte)(module_base.module_settings.dAQMode == DAQMode.High_Sampling ? 0 : module_base.module_settings.DataLength + module_base.module_settings.comp_len); //此版本強制寫0 僅用單次傳一包(大小個軸為512筆)
            send_bytes[4] = GetByteValofMRDefine(module_base.module_settings.MeasureRange);
            send_bytes[6] = 0x00;
            ///強制寫DELAY TIME
            send_bytes[7] = 0x00;
            send_bytes[8] = Convert.ToByte(Delay_Fine_tune);



            Tools.Logger.Event_Log.Log($"[SelfTEst] Write..{ClsModuleBase.ObjectAryToString(",", send_bytes)}");
            var _return1 = await module_base.SendCommand(send_bytes, 8); //Cover控制器在Socket連線後第一次寫參數會回傳錯的值
            var _return2 = await module_base.SendCommand(send_bytes, 8);
            var send_bytes_use_to_check = new byte[8];
            Array.Copy(send_bytes, 1, send_bytes_use_to_check, 0, 8);
            if (module_base.Is_PARAM_Return_Correct(send_bytes_use_to_check, _return2) == false)
            {
                Tools.Logger.Event_Log.Log("[SelfTEst] ...Defaul PARAM SETTING FAIL..");
                return false;
            }
            else
            {
                Tools.Logger.Event_Log.Log("[SelfTEst] ...Defaul PARAM SETTING PASS..");
                var PARAM = _return2;
                //module_base.CheckParamIllegeAndFixIt(ref PARAM);
                module_base.DefineSettingByParameters(PARAM);
                Freq_Vec = FreqVecCal(module_base.module_settings.DataLength * 512 / 2);
                return true;
            }
        }


        /// <summary>
        /// 斷開與控制器的連線
        /// </summary>
        public int Disconnect()
        {
            Stop_All_Action();
            BULKBreak();
            return module_base.Disconnect();
        }


        public void BULKBreak()
        {
            if (Connected)
            {
                module_base.BulkBreak();
            }
        }

        public async Task<bool> Reconnect()
        {
            module_base.Disconnect();
            var _ret = await Connect(IsSelfTest: true);
            if (_ret == 0) return true;
            else return false;
        }

        private async Task<int> StartParamSetTaskAsync(bool IsNeedReboot = false)
        {

            await Task.Run(() =>
            {
                ParameterWriteInFlagOn = true;
                while (GetDataWaitFlagOn == false)
                {
                    Thread.Sleep(1);
                    Console.WriteLine("等待GET DATA TASK 完成...");
                }
                GetDataWaitFlagOn = true;
            });
            try
            {
                module_base.acc_data_read_task_token_source.Cancel();
                if (IsNeedReboot)
                    Tools.Logger.Event_Log.Log($"Reconnect Before Any Action TEST.{await Reconnect()}");
                WaitAsyncForParametersSet.Reset();
                var _ret = await Task.Run(() => ParamSetTask());
                Sensor_Config_Save();
                WaitAsyncForParametersSet.Set();
                ParameterWriteInFlagOn = false;
                return _ret;
            }
            catch (Exception ex)
            {
                Tools.Logger.Code_Error_Log.Log("[StartParamSetTaskAsync] " + ex.Message + "\r\n" + ex.StackTrace);
                return Convert.ToInt32(clsErrorCode.Error.SYSTEM_ERROR);
            }
            // SendBulkDataStartCmd();
        }

        private async Task<int> ParamSetTask()
        {
            var ret = new Tuple<byte[], int>(null, -1);
            int try_time = 0;
            while (ret.Item2 != 0 && try_time < sys.Utility.my_setting.parma_write_fail_retry_time + 1)
            {
                Tools.Logger.Event_Log.Log($"[ParamSetTask] 寫入設定至控制器({try_time})");
                try_time++; //first =1, second =2
                Tools.Logger.Event_Log.Log($"[ParamSetTask] 寫入設定至控制器({try_time})");

                switch (Convert.ToInt32(module_base.setTaskObj.SettingItem))
                {
                    case 0:
                        ret = module_base.SettingToController((clsEnum.Module_Setting_Enum.SENSOR_TYPE)module_base.setTaskObj.SettingValue, null, null, -1);
                        break;
                    case 1:
                        ret = module_base.SettingToController(null, null, null, (int)module_base.setTaskObj.SettingValue);
                        break;
                    case 2:
                        ret = module_base.SettingToController(null, (clsEnum.Module_Setting_Enum.MEASURE_RANGE)module_base.setTaskObj.SettingValue, null, -1);
                        break;
                    case 3:
                        ret = module_base.SettingToController(null, null, (clsEnum.Module_Setting_Enum.ODR)module_base.setTaskObj.SettingValue, -1);
                        break;
                }

            }
            module_base.SocketBufferClear();
            if (ret.Item2 != 0)
            {
                return ret.Item2;
            }
            else
            {
                WaitAsyncForParametersSet.Set();
                return 0;
            }
        }

        /// <summary>
        /// 儲存控制器參數到硬碟 路徑: Environment.CurrentDirectory + $@"\SensorConfig\{moduleIP}\"
        /// </summary>
        public void Sensor_Config_Save()
        {
            Thread th = new Thread(() =>
            {

                try
                {
                    var ModelSavePath = "SensorConfig\\" + SensorIP;
                    if (!Directory.Exists(ModelSavePath))
                        Directory.CreateDirectory(ModelSavePath);
                    var filepath = Path.Combine(ModelSavePath, "Controller_Parameters.xml");
                    if (!File.Exists(filepath))
                        File.Create(filepath).Close();
                    using (FileStream fs = new FileStream(filepath, FileMode.Create))
                    {
                        XmlSerializer xs = new XmlSerializer(typeof(clsModuleSettings));
                        xs.Serialize(fs, module_base.module_settings);
                    }
                    return;
                }
                catch (IOException ex)
                {
                    Tools.Logger.Code_Error_Log.Log($"[Sensor_Config_Save] {ex.Message}{ex.StackTrace}");
                    return;
                }
                catch (Exception ex)
                {
                    Tools.Logger.Code_Error_Log.Log($"[Sensor_Config_Save] {ex.Message}{ex.StackTrace}");
                    return;
                }
            })
            {
                IsBackground = true
            };
            th.Start();
        }

        private void Sensor_Config_Load()
        {
            try
            {
                var configpath = "SensorConfig\\" + SensorIP + "\\Controller_Parameters.xml";
                if (File.Exists(configpath) && Tools.VersionManager.JudgeSensorConfigVersion(configpath) == Tools.VersionManager.VERSION.NEW)
                {
                    using (FileStream fs = new FileStream(configpath, FileMode.Open))
                    {
                        XmlSerializer xs = new XmlSerializer(typeof(clsModuleSettings));
                        clsModuleSettings setting = (clsModuleSettings)xs.Deserialize(fs);
                        module_base.module_settings = setting;
                    };
                }
                else
                {
                    module_base.module_settings = new clsModuleSettings
                    {
                        SensorType = SensorType,
                        DataLength = DataLength,
                        MeasureRange = MeasureRange,
                        ODR = ODR,
                        sampling_rate_ = 8000,
                    };
                    Sensor_Config_Save();
                }
                if (module_base.module_settings.DataLength != 1 && (module_base.module_settings.DataLength & 2) != 0)
                    module_base.module_settings.DataLength -= module_base.module_settings.comp_len;
                if (module_base.module_settings.dAQMode == DAQMode.High_Sampling)
                    module_base.module_settings.DataLength = 1;
            }
            catch (Exception ex)
            {
                Tools.Logger.Code_Error_Log.Log($"[Sensor_Config_Load] {ex.Message + ex.StackTrace}");
                module_base.module_settings = new clsModuleSettings
                {
                    SensorType = SensorType,
                    DataLength = DataLength,
                    MeasureRange = MeasureRange,
                    ODR = ODR,
                    sampling_rate_ = 5000,
                };
                Sensor_Config_Save();

            }

        }
        public Settings_Items Sys_Setting
        {
            get
            {
                return sys.Utility.my_setting;
            }
        }

        private void Sys_Config_Load()
        {
            var _settings = sys.Settings_Ctrl.Log_Config();
            sys.Utility.my_setting = _settings;
            Tools.Logger.Event_Log.is_log_enable = Tools.Logger.Code_Error_Log.is_log_enable = _settings.is_write_log_to_HardDisk;
            //SamplingRate = Convert.ToDouble(_settings.sampling_rate_of_vibration_sensor);
        }

        /// <summary>
        /// 設定量測範圍
        /// </summary>
        /// <param name="MeasureRange"> 量測範圍列舉</param>
        /// <param name="IsNeedReboot">是否要重新啟動模組</param>
        /// <returns><para> 0: 設定成功   </para> <para> Others: Error Code </para></returns>
        public async Task<int> Measure_Range_Setting(clsEnum.Module_Setting_Enum.MEASURE_RANGE MeasureRange, bool IsNeedReboot = true)
        {
            module_base.setTaskObj = new ClsParamSetTaskObj(module_base.module_settings.dAQMode)
            {
                SettingItem = 2,
                SettingValue = MeasureRange
            };
            var ret = await StartParamSetTaskAsync(IsNeedReboot);
            module_base.module_settings.MeasureRange = ret == 0 ? MeasureRange : module_base.module_settings.MeasureRange;


            return ret;
        }

        /// <summary>
        /// 設定資料擷取倍率
        /// </summary>
        /// <param name="N">資料倍率,必須為2的指數</param>
        /// <returns><para> 0: 設定成功   </para> <para> Others: Error Code </para></returns>
        public async Task<int> Data_Length_Setting(int N, bool IsNeedReboot = false)
        {

            if (!GpmMath.Numeric.Tools.IsPowerOf2(N) | N > 64)
                return Convert.ToInt32(clsErrorCode.Error.DATA_LENGTH_SETTING_VALUE_ILLEGAL);
            if (module_base.module_settings.dAQMode == DAQMode.Low_Sampling)
            {
                module_base.setTaskObj = new ClsParamSetTaskObj(module_base.module_settings.dAQMode)
                {
                    SettingItem = 1,
                    SettingValue = N
                };
                var ret = await StartParamSetTaskAsync(IsNeedReboot);
                module_base.module_settings.DataLength = ret == 0 ? N : module_base.module_settings.DataLength;
            }
            else
                this.DataLength = N;
            Freq_Vec = FreqVecCal();
            return 0;
        }


        private async Task<int> Data_Length_Setting(DAQMode dAQMode, bool IsNeedReboot = false)
        {
            module_base.setTaskObj = new ClsParamSetTaskObj(dAQMode)
            {
                SettingItem = 1,
                SettingValue = DataLength,
            };
            var ret = await StartParamSetTaskAsync(IsNeedReboot);
            //module_base.module_settings.DataLength = ret == 0 ? DataLength : module_base.module_settings.DataLength;
            return ret;
        }


        public async Task<byte[]> ReadStval()
        {
            module_base.SocketBufferClear();
            byte[] cmd = Encoding.ASCII.GetBytes("READSTVAL\r\n");
            var _ret = module_base.SendCommand(cmd, 8);
            await _ret;
            return _ret.Result;
        }





        public void StartDataRecieve(MeasureOption option)
        {
            this.option = option;

            module_base.StartGetData_Bulk(option);
        }
        public bool IsAutoResumeBulkAfterWriteSetting
        {
            set
            {
                module_base.send_cmd_task_obj.IsAutoStartBulk = value;
            }
        }


        private void Module_base_DataReady(DataSet dataSet)
        {
            //Console.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.ffff"));
            dataSet.FFTData.X = FFT.GetFFT(dataSet.AccData.X, IsZeroAdd: false);
            dataSet.FFTData.Y = FFT.GetFFT(dataSet.AccData.Y, IsZeroAdd: false);
            dataSet.FFTData.Z = FFT.GetFFT(dataSet.AccData.Z, IsZeroAdd: false);
            dataSet.FFTData.FreqVec = Freq_Vec;

            dataSet.Features.VibrationEnergy.X = Stastify.GetOA(dataSet.FFTData.X);
            dataSet.Features.VibrationEnergy.Y = Stastify.GetOA(dataSet.FFTData.Y);
            dataSet.Features.VibrationEnergy.Z = Stastify.GetOA(dataSet.FFTData.Z);
            dataSet.Features.AccP2P.X = Stastify.GetPP(dataSet.AccData.X);
            dataSet.Features.AccP2P.Y = Stastify.GetPP(dataSet.AccData.Y);
            dataSet.Features.AccP2P.Z = Stastify.GetPP(dataSet.AccData.Z);

            dataSet.Features.AccRMS.X = Stastify.RMS(dataSet.AccData.X);
            dataSet.Features.AccRMS.Y = Stastify.RMS(dataSet.AccData.Y);
            dataSet.Features.AccRMS.Z = Stastify.RMS(dataSet.AccData.Z);
            DataRecieve?.Invoke(dataSet);
        }
        private void SendBulkDataStartCmd()
        {
            module_base.SendBulkDataStartCmd();
        }

        private bool ParameterWriteInFlagOn = false;
        private bool GetDataWaitFlagOn = true;
        /// <summary>
        /// 取得三軸加速度量測值
        /// </summary>
        public async Task<DataSet> GetData(bool IsGetFFT, bool IsGetOtherFeatures, WINDOW window = WINDOW.none)
        {
            if (module_base.isBusy)
            {
                return new DataSet(module_base.module_settings.sampling_rate_)
                {
                    AccData = DataSetRet.AccData,
                    FFTData = DataSetRet.FFTData,
                    Features = DataSetRet.Features,
                    ErrorCode = Convert.ToInt32(clsErrorCode.Error.ModuleIsBusy)
                };
            }

            if (KeyProExisStatus == clsEnum.KeyPro.KEYPRO_EXIST_STATE.NoInsert)
                return new DataSet(module_base.module_settings.sampling_rate_) { ErrorCode = Convert.ToInt32(clsErrorCode.Error.KEYPRO_NOT_FOUND) };
            // if (Connected == false)
            //     return new DataSet(module_base.SamplingRate) { ErrorCode = Convert.ToInt32(clsErrorCode.Error.NoConnection) };
            WaitAsyncForParametersSet.Set();
            WaitAsyncForGetDataTask.Reset();
            this.IsGetFFT = IsGetFFT;
            this.IsGetOtherFeatures = IsGetOtherFeatures;

            Thread th = new Thread(() =>
            {
                var t = 0;
                while (ParameterWriteInFlagOn)
                {
                    if (t > 5000)
                    {
                        Tools.Logger.Event_Log.Log("ParameterWriteInFlagOn 等待逾時");
                        GetDataWaitFlagOn = false;
                        ParameterWriteInFlagOn = false;
                        break;
                    }
                    GetDataWaitFlagOn = true;
                    Thread.Sleep(1);
                    Console.WriteLine("GET DATA任務暫停中>>等待參數寫入任務完成");
                }

            });

            await Task.Run(() =>
            {
                GetDataWaitFlagOn = false;
                GetDataTask();
                GetDataWaitFlagOn = true;
            });
            WaitAsyncForGetDataTask.WaitOne();
            return DataSetRet;

        }

        public void Stop_All_Action()
        {
            module_base.timeout_task_cancel_source.Cancel();
            module_base.acc_data_read_task_token_source.Cancel();
            module_base.param_setting_task_cancel_token_Source?.Cancel();
            module_base.isBusy = false;
            Thread.Sleep(500);
        }
        private DataSet ConvertToDataSet(byte[] AccPacket, bool lowPassFilter = false)
        {
            // var datas = Tools.ConverterTools.AccPacketToListDouble(AccPacket, MeasureRange, DeterminALG());
            var datas = Tools.ConverterTools.AccPacketToListDouble(AccPacket, MeasureRange, DAQMode);

            if (lowPassFilter)
            {
                LowPassFilter passFilter = new LowPassFilter(module_base.module_settings.lowPassFilter.CutOffFreq, module_base.module_settings.sampling_rate_);
                var alpha = passFilter.Alpha;
                for (int i = 0; i < 3; i++)
                {
                    List<double> real_data = datas[i];
                    List<double> y = new List<double>() { real_data[0] };
                    for (int t = 1; t < real_data.Count; t++)
                    {
                        var _y = y[t - 1] + alpha * (real_data[t] - y[t - 1]);
                        y.Add(_y);
                    }
                    datas[i] = y;
                }

            }

            var DataSetRet = new DataSet(SamplingRate);
            DataSetRet.AccData.X = datas[0];
            DataSetRet.AccData.Y = datas[1];
            DataSetRet.AccData.Z = datas[2];


            return DataSetRet;
        }

        public void GenOneAccDataObject()
        {
            try
            {
                byte[] AccPacket;
                bool IsTimeout;
                AccPacket = module_base.GetAccData_HighSpeedWay(out DataSetRet.TimeSpend, out IsTimeout);

                DataSetRet.ErrorCode = IsTimeout ? Convert.ToInt32(clsErrorCode.Error.DATA_GET_TIMEOUT) : 0;
                if (AccPacket.Length < (module_base.module_settings.dAQMode == DAQMode.High_Sampling ? 3072 : (DataLength) * 3072))
                {

                    //Tools.Logger.Event_Log.Log($"Raw Data bytes Insufficent :: {AccPacket.Length}<{(DataLength) * 3072}");
                    DataSetRet.ErrorCode = Convert.ToInt32(clsErrorCode.Error.DATA_GET_TIMEOUT);
                    WaitAsyncForGetDataTask.Set();
                    return;
                }
                module_base.state = null;
                module_base.SocketBufferClear();
                ///
                DataSetRet.AddData(ConvertToDataSet(AccPacket, lowPassFilter: module_base.module_settings.lowPassFilter.Active));

            }
            catch (SocketException exp)
            {
                Tools.Logger.Code_Error_Log.Log(exp);
                GetDataWaitFlagOn = true;
                if (DisconnectEvent != null)
                    DisconnectEvent.Invoke(DateTime.Now);
            }
            catch (Exception exp)
            {
                Tools.Logger.Code_Error_Log.Log(exp);
                GetDataWaitFlagOn = true;
                DataSetRet.AccData.X.Clear();
                DataSetRet.AccData.Y.Clear();
                DataSetRet.AccData.Z.Clear();
                DataSetRet.AccData.X.Add(-99999);
                DataSetRet.AccData.Y.Add(-99999);
                DataSetRet.AccData.Z.Add(-99999);
            }
        }
        internal virtual void GetDataTask()
        {
            //IsGetDataTaskPaused = true;
            //GetDataTaskPause.WaitOne();
            //WaitAsyncForParametersSet.WaitOne();
            //IsGetDataTaskPaused = false;
            DataSetRet = new DataSet(module_base.module_settings.sampling_rate_);
            if (module_base.module_settings.dAQMode == DAQMode.High_Sampling)
            {
                DataSetCnt = 0;//歸零
                Tools.Logger.Event_Log.Log($"Aim:{DataLength}");
                while (DataSetCnt < DataLength)
                {
                    Tools.Logger.Event_Log.Log($"GenOneAccDataObject:{DataSetCnt}");
                    GenOneAccDataObject();
                    DataSetCnt++;
                    Tools.Logger.Event_Log.Log($"ACQ Process:{DataSetCnt}/{DataLength}");
                    Thread.Sleep(1);
                }
            }
            else
                GenOneAccDataObject();
            if (IsGetFFT && Numeric.Tools.IsPowerOf2(DataSetRet.AccData.X.Count))
            {
                DataSetRet.FFTData.X = FFT.GetFFT(DataSetRet.AccData.X);
                DataSetRet.FFTData.Y = FFT.GetFFT(DataSetRet.AccData.Y);
                DataSetRet.FFTData.Z = FFT.GetFFT(DataSetRet.AccData.Z);

                DataSetRet.Features.VibrationEnergy.X = Stastify.GetOA(DataSetRet.FFTData.X);
                DataSetRet.Features.VibrationEnergy.Y = Stastify.GetOA(DataSetRet.FFTData.Y);
                DataSetRet.Features.VibrationEnergy.Z = Stastify.GetOA(DataSetRet.FFTData.Z);

                DataSetRet.FFTData.FreqVec = Freq_Vec;

            }

            if (IsGetOtherFeatures)
            {
                DataSetRet.Features.AccP2P.X = Stastify.GetPP(DataSetRet.AccData.X);
                DataSetRet.Features.AccP2P.Y = Stastify.GetPP(DataSetRet.AccData.Y);
                DataSetRet.Features.AccP2P.Z = Stastify.GetPP(DataSetRet.AccData.Z);

                DataSetRet.Features.AccRMS.X = Stastify.RMS(DataSetRet.AccData.X);
                DataSetRet.Features.AccRMS.Y = Stastify.RMS(DataSetRet.AccData.Y);
                DataSetRet.Features.AccRMS.Z = Stastify.RMS(DataSetRet.AccData.Z);
            }
            module_base.isBusy = false;
            WaitAsyncForGetDataTask.Set();
        }
        private clsEnum.FWSetting_Enum.ACC_CONVERT_ALGRIUM DeterminALG()
        {
            if (module_base.module_settings.ByteAryOfParameters[1] == 0x00)
                return clsEnum.FWSetting_Enum.ACC_CONVERT_ALGRIUM.Old;
            else
                return clsEnum.FWSetting_Enum.ACC_CONVERT_ALGRIUM.New;
        }

        internal List<double> FreqVecCal()
        {
            return FftSharp.Transform.FFTfreq(Convert.ToInt32(SamplingRate), DataLength * 512 / 2).ToList();
        }

        private List<double> FreqVecCal(int FFTWindowSize)
        {
            var freqVec = new List<double>();
            var NysFreq = module_base.module_settings.sampling_rate_ / 2;
            for (int i = 0; i < FFTWindowSize; i++)
            {
                freqVec.Add((NysFreq / (double)FFTWindowSize) * (double)i);
            }
            return freqVec;
        }
        public string GetIPFromSocketObj(Socket sensorSocket)
        {
            IPEndPoint IPP = (IPEndPoint)sensorSocket.RemoteEndPoint;
            string Ip = IPP.Address.ToString();
            return Ip;
        }

        private void TinySensorFWUpdate(List<byte[]> data)
        {
            module_base.TinySensorFWUpdate(data);
        }




    }
}
