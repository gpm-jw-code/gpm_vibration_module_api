#define YCM
//#define BR115200
#define BR460800
//#define BR921600

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

namespace gpm_vibration_module_api
{
    /// <summary>
    /// For User using.
    /// </summary>
    public class GPMModuleAPI
    {
        /// <summary>
        /// 控制器底層控制
        /// </summary>
        private ClsModuleBase module_base = new ClsModuleBase();


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

#if KeyproEnable
        private clsEnum.KeyPro.KeyProExisStatus KeyProExisStatus = clsEnum.KeyPro.KeyProExisStatus.NoInsert;
#else
        private clsEnum.KeyPro.KEYPRO_EXIST_STATE KeyProExisStatus = clsEnum.KeyPro.KEYPRO_EXIST_STATE.Exist;

#endif
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

        private void UpdateParam()
        {
            var param = module_base.module_settings.ByteAryOfParameters;
            var cmd = new byte[11] { 0x53, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0d, 0x0a };
            Array.Copy(param, 0, cmd, 1, param.Length);
            module_base.SendCommand(cmd, 8);
        }

        private class ClsParamSetTaskObj
        {
            public object SettingItem;
            public object SettingValue;
        }

        private MeasureOption option = new MeasureOption();
        private DataSet DataSetRet = new DataSet(1000);
        private ClsParamSetTaskObj setTaskObj = new ClsParamSetTaskObj();
        private bool IsGetFFT = false;
        private bool IsGetOtherFeatures = false;
        private ManualResetEvent WaitAsyncForGetDataTask;
        private ManualResetEvent WaitAsyncForParametersSet;
        private event Action<string> FunctionCalled;
        /// <summary>
        /// 斷線事件
        /// </summary>
        public event Action<DateTime> DisconnectEvent;


        private int window_size = 512;
        public int WindowSize
        {
            get
            { return window_size; }
            set
            { window_size = value; }
        }

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

        /// <summary>
        /// 計算Sensor 取樣頻率
        /// </summary>
        /// <param name="ref_Frq">參考激振源頻率</param>
        /// <param name="Period">測試秒數</param>
        /// <returns></returns>
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




        public string SensorIP { get; private set; } = "";
        private int SensorPort;
        public enum Enum_AccGetMethod
        {
            Auto, Manual
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
        public event Action<string> ConnectEvent;
        public Enum_AccGetMethod NowAccGetMethod = Enum_AccGetMethod.Manual;

        public int AccDataRevTimeOut
        {
            set
            {
                try
                {
                    Int32.TryParse(value + "", out int _result);
                    module_base.acc_data_rev_timeout = _result;
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }


        public int ParamRWTimeOut
        {
            set
            {
                try
                {
                    Int32.TryParse(value + "", out int _result);
                    module_base.fw_parm_rw_timeout = _result;
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
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
        /// <returns></returns>
        public async Task<int> Connect(string IP, int Port, bool IsSelfTest = true)
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

        public bool IsDataHandShakeNormal { private set; get; }


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

            //send_bytes[2] = GetByteValofDLDefine(module_base.module_settings.DataLength);
            send_bytes[1] = 0x01;
            send_bytes[2] = 0x00; //此版本強制寫0 僅用單次傳一包(大小個軸為512筆)
            send_bytes[6] = 0x00;
            send_bytes[4] = GetByteValofMRDefine(module_base.module_settings.MeasureRange);

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
                Freq_Vec = FreqVecCal(Convert.ToInt32(module_base.module_settings.DataLength) / 2);
                return true;
            }
        }


        /// <summary>
        /// 斷開與控制器的連線
        /// </summary>
        public int Disconnect()
        {
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

        private async Task<int> StartParamSetTaskAsync(bool IsNeedReboot = true)
        {
            try
            {
                module_base.acc_data_read_task_token_source.Cancel();
                if (IsNeedReboot)
                    Tools.Logger.Event_Log.Log($"Reconnect Before Any Action TEST.{await Reconnect()}");
                WaitAsyncForParametersSet.Reset();
                var _ret = await Task.Run(() => ParamSetTask());
                Sensor_Config_Save();
                WaitAsyncForParametersSet.Set();
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

                switch (Convert.ToInt32(setTaskObj.SettingItem))
                {
                    case 0:
                    ret = module_base.SettingToController((clsEnum.Module_Setting_Enum.SENSOR_TYPE) setTaskObj.SettingValue, null, null, null);
                    break;
                    case 1:
                    ret = module_base.SettingToController(null, (clsEnum.Module_Setting_Enum.DATA_LENGTH) setTaskObj.SettingValue, null, null);
                    break;
                    case 2:
                    ret = module_base.SettingToController(null, null, (clsEnum.Module_Setting_Enum.MEASURE_RANGE) setTaskObj.SettingValue, null);
                    break;
                    case 3:
                    ret = module_base.SettingToController(null, null, null, (clsEnum.Module_Setting_Enum.ODR) setTaskObj.SettingValue);
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
        public int Sensor_Config_Save()
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
                return 0;
            }
            catch (IOException ex)
            {
                Tools.Logger.Code_Error_Log.Log($"[Sensor_Config_Save] {ex.Message}{ex.StackTrace}");
                return -1;
            }
            catch (Exception ex)
            {
                Tools.Logger.Code_Error_Log.Log($"[Sensor_Config_Save] {ex.Message}{ex.StackTrace}");
                return -1;
            }
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
                        clsModuleSettings setting = (clsModuleSettings) xs.Deserialize(fs);
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
                        sampling_rate_ = 8000
                    };
                    Sensor_Config_Save();
                }
                module_base.module_settings.DataLength = clsEnum.Module_Setting_Enum.DATA_LENGTH.x1;
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
                    sampling_rate_ = 5000
                };
                Sensor_Config_Save();
            }

        }


        private void Sys_Config_Load()
        {
            var _settings = sys.Settings_Ctrl.Log_Config();
            sys.Utility.my_setting = _settings;
            Tools.Logger.Event_Log.is_log_enable = Tools.Logger.Code_Error_Log.is_log_enable = _settings.is_write_log_to_HardDisk;
            //SamplingRate = Convert.ToDouble(_settings.sampling_rate_of_vibration_sensor);
        }

        public async Task<int> Measure_Range_Setting(clsEnum.Module_Setting_Enum.MEASURE_RANGE MeasureRange, bool IsNeedReboot = true)
        {
            setTaskObj = new ClsParamSetTaskObj
            {
                SettingItem = 2,
                SettingValue = MeasureRange
            };
            var ret = await StartParamSetTaskAsync(IsNeedReboot);
            module_base.module_settings.MeasureRange = ret == 0 ? MeasureRange : module_base.module_settings.MeasureRange;
            return ret;
        }

        public async Task<int> Data_Length_Setting(clsEnum.Module_Setting_Enum.DATA_LENGTH DataLength)
        {
            //setTaskObj = new ClsParamSetTaskObj
            //{
            //    SettingItem = 1,
            //    SettingValue = DataLength
            //};
            //var ret = await StartParamSetTaskAsync();
            //module_base.module_settings.DataLength = ret == 0 ? DataLength : module_base.module_settings.DataLength;
            this.DataLength = DataLength;
            Freq_Vec = FreqVecCal();
            return 0;
        }

        /// <summary>
        /// 設定/取得量測範圍
        /// </summary>
        public clsEnum.Module_Setting_Enum.MEASURE_RANGE MeasureRange
        {
            internal set
            {
                setTaskObj = new ClsParamSetTaskObj
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

        public async Task<byte[]> ReadStval()
        {
            module_base.SocketBufferClear();
            byte[] cmd = Encoding.ASCII.GetBytes("READSTVAL\r\n");
            var _ret = module_base.SendCommand(cmd, 8);
            await _ret;
            return _ret.Result;
        }
        public int MeasureRange_IntType
        {

            set
            {
                //BULKBreak();
                setTaskObj.SettingItem = 2;
                switch (value)
                {
                    case 2:
                    setTaskObj.SettingValue = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_2G;
                    break;
                    case 4:
                    setTaskObj.SettingValue = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_4G;
                    break;
                    case 8:
                    setTaskObj.SettingValue = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_8G;
                    break;
                    case 16:
                    setTaskObj.SettingValue = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_16G;
                    break;
                    default:
                    setTaskObj.SettingValue = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_2G;
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
        public clsEnum.Module_Setting_Enum.DATA_LENGTH DataLength
        {
            internal set
            {
                //if (SensorType != clsEnum.Module_Setting_Enum.SENSOR_TYPE.High)
                //    return;
                //setTaskObj.SettingItem = 1;
                //setTaskObj.SettingValue = value;
                //StartParamSetTaskAsync();
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
                setTaskObj.SettingItem = 1;
                switch (value)
                {
                    case 512:
                    setTaskObj.SettingValue = clsEnum.Module_Setting_Enum.DATA_LENGTH.x1;
                    break;
                    case 1024:
                    setTaskObj.SettingValue = clsEnum.Module_Setting_Enum.DATA_LENGTH.x2;
                    break;
                    case 2048:
                    setTaskObj.SettingValue = clsEnum.Module_Setting_Enum.DATA_LENGTH.x4;
                    break;
                    case 4096:
                    setTaskObj.SettingValue = clsEnum.Module_Setting_Enum.DATA_LENGTH.x8;
                    break;
                    default:
                    setTaskObj.SettingValue = clsEnum.Module_Setting_Enum.DATA_LENGTH.x1;
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
                setTaskObj.SettingItem = 0;
                setTaskObj.SettingValue = value;
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
                setTaskObj.SettingItem = 3;
                setTaskObj.SettingValue = value;
                StartParamSetTaskAsync();
            }
            get
            {
                return module_base.module_settings.ODR;
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
        /// 設定感測器安裝位置名稱
        /// </summary>
        public string Location { get; set; }


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

        public event Action<DataSet> DataRecieve;
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
            await Task.Run(() =>
            {
                DataSetCnt = 0;//歸零
                GetDataTask();
            });
            WaitAsyncForGetDataTask.WaitOne();
            return DataSetRet;

        }



        private ManualResetEvent GetDataTaskPause;


        private bool IsGetDataTaskPaused = true;

        public void Stop_All_Action()
        {
            module_base.timeout_task_cancel_source.Cancel();
            module_base.acc_data_read_task_token_source.Cancel();
            module_base.param_setting_task_cancel_token_Source?.Cancel();
            module_base.isBusy = false;
            Thread.Sleep(500);
        }

        private DataSet ConvertToDataSet(byte[] AccPacket)
        {
            // var datas = Tools.ConverterTools.AccPacketToListDouble(AccPacket, MeasureRange, DeterminALG());
            var datas = Tools.ConverterTools.AccPacketToListDouble(AccPacket, MeasureRange, clsEnum.FWSetting_Enum.ACC_CONVERT_ALGRIUM.Old);
            var DataSetRet = new DataSet(SamplingRate);
            DataSetRet.AccData.X = datas[0];
            DataSetRet.AccData.Y = datas[1];
            DataSetRet.AccData.Z = datas[2];


            return DataSetRet;
        }

        private int DataSetCnt = 0;

        private void GetDataTask()
        {
            //IsGetDataTaskPaused = true;
            //GetDataTaskPause.WaitOne();
            //WaitAsyncForParametersSet.WaitOne();
            //IsGetDataTaskPaused = false;
            DataSetRet = new DataSet(module_base.module_settings.sampling_rate_);
            while (DataSetCnt < ((int) DataLength / 512))
            {
                try
                {
                    byte[] AccPacket;
                    bool IsTimeout;
                    AccPacket = module_base.GetAccData_HighSpeedWay(out DataSetRet.TimeSpend, out IsTimeout);
                    DataSetRet.ErrorCode = IsTimeout ? Convert.ToInt32(clsErrorCode.Error.DATA_GET_TIMEOUT) : 0;
                    if (AccPacket.Length < 3072)
                    {
                        DataSetRet.ErrorCode = Convert.ToInt32(clsErrorCode.Error.DATA_GET_TIMEOUT);
                        WaitAsyncForGetDataTask.Set();
                        return;
                    }
                    ///
                    DataSetRet.AddData(ConvertToDataSet(AccPacket));

                }
                catch (SocketException exp)
                {
                    if (DisconnectEvent != null)
                        DisconnectEvent.Invoke(DateTime.Now);
                }
                catch (Exception exp)
                {
                    DataSetRet.AccData.X.Clear();
                    DataSetRet.AccData.Y.Clear();
                    DataSetRet.AccData.Z.Clear();
                    DataSetRet.AccData.X.Add(-99999);
                    DataSetRet.AccData.Y.Add(-99999);
                    DataSetRet.AccData.Z.Add(-99999);
                }
                DataSetCnt++;
                Thread.Sleep(1);
            }
            if (IsGetFFT)
            {
                DataSetRet.FFTData.X = GpmMath.FFT.GetFFT(DataSetRet.AccData.X);
                DataSetRet.FFTData.Y = GpmMath.FFT.GetFFT(DataSetRet.AccData.Y);
                DataSetRet.FFTData.Z = GpmMath.FFT.GetFFT(DataSetRet.AccData.Z);
                DataSetRet.FFTData.FreqVec = Freq_Vec;
                int melBinCount = 30;
                DataSetRet.MelBankData.X = Transform.MelScale(Transform.FFTmagnitude(DataSetRet.AccData.X.ToArray()), Convert.ToInt32(SamplingRate), melBinCount).ToList();
                DataSetRet.MelBankData.Y = Transform.MelScale(Transform.FFTmagnitude(DataSetRet.AccData.Y.ToArray()), Convert.ToInt32(SamplingRate), melBinCount).ToList();
                DataSetRet.MelBankData.Z = Transform.MelScale(Transform.FFTmagnitude(DataSetRet.AccData.Z.ToArray()), Convert.ToInt32(SamplingRate), melBinCount).ToList();

            }

            if (IsGetOtherFeatures)
            {
                if (IsGetFFT)
                {
                    DataSetRet.Features.VibrationEnergy.X = Stastify.GetOA(DataSetRet.FFTData.X);
                    DataSetRet.Features.VibrationEnergy.Y = Stastify.GetOA(DataSetRet.FFTData.Y);
                    DataSetRet.Features.VibrationEnergy.Z = Stastify.GetOA(DataSetRet.FFTData.Z);
                }
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

        private List<double> Freq_Vec = new List<double>();


        internal List<double> FreqVecCal()
        {
            return FftSharp.Transform.FFTfreq(Convert.ToInt32(SamplingRate), Convert.ToInt32(DataLength)/2).ToList();
        }

        private List<double> FreqVecCal(int FFTWindowSize)
        {
            var freqVec = new List<double>();
            var NysFreq = module_base.module_settings.sampling_rate_ / 2;
            for (int i = 0; i < FFTWindowSize; i++)
            {
                freqVec.Add((NysFreq / (double) FFTWindowSize) * (double) i);
            }
            return freqVec;
        }
        public string GetIPFromSocketObj(Socket sensorSocket)
        {
            IPEndPoint IPP = (IPEndPoint) sensorSocket.RemoteEndPoint;
            string Ip = IPP.Address.ToString();
            return Ip;
        }

        private void TinySensorFWUpdate(List<byte[]> data)
        {
            module_base.TinySensorFWUpdate(data);
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
                Freq_Vec = FreqVecCal();
                module_base.module_settings.sampling_rate_ = value;
                Sensor_Config_Save();
            }
        }

        public Action<string> ReConnectEvent { get; set; }
    }
}
