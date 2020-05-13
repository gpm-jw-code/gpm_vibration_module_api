#define YCM
//#define BR115200
#define BR460800
//#define BR921600

using gpm_vibration_module_api.Module;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace gpm_vibration_module_api
{
    /// <summary>
    /// For User using.
    /// </summary>
    public class GPMModuleAPI
    {
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
        /// 控制器底層控制
        /// </summary>
        private ClsModuleBase module_base = new ClsModuleBase();
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

        private void API_KeyProRemoveEvent(DateTime obj)
        {
            KeyProExisStatus = clsEnum.KeyPro.KEYPRO_EXIST_STATE.NoInsert;
        }

        private void API_KeyProInsertEvent(DateTime obj)
        {
            KeyProExisStatus = clsEnum.KeyPro.KEYPRO_EXIST_STATE.Exist;
        }

        public GPMModuleAPI(string IP = null)
        {
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
        internal event Action<string> ConnectEvent;
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
                return Convert.ToInt32(clsErrorCode.Error.KeyproNotFound);
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
                module_base.acc_data_read_task_token_source.Cancel();
                var ret = module_base.Connect(IP, Port);
                if (ret == 0)
                {
                    ConnectEvent?.Invoke(IP);
                    ReConnectEvent?.Invoke(IP);
                    if (IsSelfTest)
                        IsDataHandShakeNormal = await SelfTest();
                }
                else
                {

                }
                socket_conected_list[IP] = module_base.module_socket;

                Tools.Logger.Event_Log.Log($"[Fun: Connecnt() ] {(ret == 0 ? "Successfully Established Connection." : $"Couldn't Not Established Connection, ERROR_CODE={ret}.")} IP:{IP}, Port:{Port}");
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



        private async Task<bool> SelfTest()
        {
            Tools.Logger.Event_Log.Log("[SelfTEst] Write..");
            byte[] send_bytes = new byte[11] { 0x53, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0d, 0x0a };
            Array.Copy(module_base.module_settings.ByteAryOfParameters, 0, send_bytes, 1, module_base.module_settings.ByteAryOfParameters.Length);
            Tools.Logger.Event_Log.Log($"[SelfTEst] Write..{ClsModuleBase.ObjectAryToString(",",send_bytes)}");
            var _return1 = await module_base.SendCommand(send_bytes, 8);
            var _return2 = await module_base.SendCommand(send_bytes, 8);
            if (module_base.Is_PARAM_Return_Correct(module_base.module_settings.ByteAryOfParameters, _return2) == false)
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

        private async Task<int> StartParamSetTaskAsync()
        {
            try
            {
                module_base.acc_data_read_task_token_source.Cancel();
                Console.WriteLine($"Reconnect Before Any Action TEST.{await Reconnect()}");
                WaitAsyncForParametersSet.Reset();
                var _ret = await Task.Run(() => ParamSetTask());
                Save();
                WaitAsyncForParametersSet.Set();
                return _ret;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
                return Convert.ToInt32(clsErrorCode.Error.ParametersSettingTimeout);
            }
            // SendBulkDataStartCmd();
        }

        private async Task<int> ParamSetTask()
        {
            var ret = new byte[0];
            switch (Convert.ToInt32(setTaskObj.SettingItem))
            {
                case 0:
                    ret = module_base.SettingToController((clsEnum.Module_Setting_Enum.SENSOR_TYPE)setTaskObj.SettingValue, null, null, null);
                    break;
                case 1:
                    ret = module_base.SettingToController(null, (clsEnum.Module_Setting_Enum.DATA_LENGTH)setTaskObj.SettingValue, null, null);
                    break;
                case 2:
                    ret = module_base.SettingToController(null, null, (clsEnum.Module_Setting_Enum.MEASURE_RANGE)setTaskObj.SettingValue, null);
                    break;
                case 3:
                    ret = module_base.SettingToController(null, null, null, (clsEnum.Module_Setting_Enum.ODR)setTaskObj.SettingValue);
                    break;
            }
            module_base.SocketBufferClear();
            if (ret.Length == 0) return Convert.ToInt32(clsErrorCode.Error.ParametersSettingTimeout);
            else
            {
                WaitAsyncForParametersSet.Set();
                GetDataTask();
                return 0;
            }
        }

        /// <summary>
        /// 儲存控制器參數到硬碟 路徑: Environment.CurrentDirectory + $@"\SensorConfig\{moduleIP}\"
        /// </summary>
        public int Save()
        {
            try
            {
                var ModelSavePath = "SensorConfig\\" + SensorIP;
                if (!Directory.Exists(ModelSavePath))
                    Directory.CreateDirectory(ModelSavePath);
                var filepath = Path.Combine(ModelSavePath, "Controller_Parameters.xml");
                if (!File.Exists(filepath))
                    File.Create(filepath).Close();
                FileStream fs = new FileStream(filepath, FileMode.Create);
                XmlSerializer xs = new XmlSerializer(typeof(clsModuleSettings));
                xs.Serialize(fs, module_base.module_settings);
                fs.Close();

                return 0;
            }
            catch (IOException exp)
            {
                return -1;
            }
        }

        private void Load()
        {
            var configpath = "SensorConfig\\" + SensorIP + "\\Controller_Parameters.xml";
            if (File.Exists(configpath))
            {
                FileStream fs = new FileStream(configpath, FileMode.Open);
                XmlSerializer xs = new XmlSerializer(typeof(clsModuleSettings));
                clsModuleSettings setting = (clsModuleSettings)xs.Deserialize(fs);
                fs.Flush();
                fs.Close();
                SensorType = setting.SensorType;
                MeasureRange = setting.MeasureRange;
                DataLength = setting.DataLength;
                ODR = setting.ODR;
                module_base.module_settings = setting;
            }
            else
            {
                SensorType = clsEnum.Module_Setting_Enum.SENSOR_TYPE.Genernal;
                MeasureRange = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_2G;
                DataLength = clsEnum.Module_Setting_Enum.DATA_LENGTH.x1;
                ODR = clsEnum.Module_Setting_Enum.ODR._9F;
                module_base.module_settings = new clsModuleSettings
                {
                    SensorType = SensorType,
                    DataLength = DataLength,
                    MeasureRange = MeasureRange,
                    ODR = ODR
                };
                Save();
            }

        }

        public async Task<int> Measure_Range_Setting(clsEnum.Module_Setting_Enum.MEASURE_RANGE MeasureRange)
        {

            setTaskObj = new ClsParamSetTaskObj
            {
                SettingItem = 2,
                SettingValue = MeasureRange
            };
            var ret = await StartParamSetTaskAsync();
            module_base.module_settings.MeasureRange = ret == 0 ? MeasureRange : module_base.module_settings.MeasureRange;
            return ret;
        }

        public async Task<int> Data_Length_Setting(clsEnum.Module_Setting_Enum.DATA_LENGTH DataLength)
        {
            setTaskObj = new ClsParamSetTaskObj
            {
                SettingItem = 1,
                SettingValue = DataLength
            };
            var ret = await StartParamSetTaskAsync();
            module_base.module_settings.DataLength = ret == 0 ? DataLength : module_base.module_settings.DataLength;
            return ret;
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
                if (SensorType != clsEnum.Module_Setting_Enum.SENSOR_TYPE.High)
                    return;
                setTaskObj.SettingItem = 1;
                setTaskObj.SettingValue = value;
                StartParamSetTaskAsync();
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
            dataSet.FFTData.X = GpmMath.FFT.GetFFT(dataSet.AccData.X, IsZeroAdd: false);
            dataSet.FFTData.Y = GpmMath.FFT.GetFFT(dataSet.AccData.Y, IsZeroAdd: false);
            dataSet.FFTData.Z = GpmMath.FFT.GetFFT(dataSet.AccData.Z, IsZeroAdd: false);
            dataSet.FFTData.FreqsVec = FreqVecCal(dataSet.FFTData.X.Count);

            dataSet.Features.VibrationEnergy.X = GpmMath.Stastify.GetOA(dataSet.FFTData.X);
            dataSet.Features.VibrationEnergy.Y = GpmMath.Stastify.GetOA(dataSet.FFTData.Y);
            dataSet.Features.VibrationEnergy.Z = GpmMath.Stastify.GetOA(dataSet.FFTData.Z);
            dataSet.Features.AccP2P.X = GpmMath.Stastify.GetPP(dataSet.AccData.X);
            dataSet.Features.AccP2P.Y = GpmMath.Stastify.GetPP(dataSet.AccData.Y);
            dataSet.Features.AccP2P.Z = GpmMath.Stastify.GetPP(dataSet.AccData.Z);

            dataSet.Features.AccRMS.X = GpmMath.Stastify.RMS(dataSet.AccData.X);
            dataSet.Features.AccRMS.Y = GpmMath.Stastify.RMS(dataSet.AccData.Y);
            dataSet.Features.AccRMS.Z = GpmMath.Stastify.RMS(dataSet.AccData.Z);
            DataRecieve?.Invoke(dataSet);
        }
        private void SendBulkDataStartCmd()
        {
            module_base.SendBulkDataStartCmd();
        }
        /// <summary>
        /// 取得三軸加速度量測值
        /// </summary>
        public async Task<DataSet> GetData(bool IsGetFFT, bool IsGetOtherFeatures)
        {
            if (module_base.isBusy)
            {
                return new DataSet(module_base.sampling_rate)
                {
                    AccData = DataSetRet.AccData,
                    FFTData = DataSetRet.FFTData,
                    Features = DataSetRet.Features,
                    ErrorCode = Convert.ToInt32(clsErrorCode.Error.ModuleIsBusy)
                };
            }

            if (KeyProExisStatus == clsEnum.KeyPro.KEYPRO_EXIST_STATE.NoInsert)
                return new DataSet(module_base.sampling_rate) { ErrorCode = Convert.ToInt32(clsErrorCode.Error.KeyproNotFound) };
            // if (Connected == false)
            //     return new DataSet(module_base.SamplingRate) { ErrorCode = Convert.ToInt32(clsErrorCode.Error.NoConnection) };
            WaitAsyncForParametersSet.Set();
            WaitAsyncForGetDataTask.Reset();
            this.IsGetFFT = IsGetFFT;
            this.IsGetOtherFeatures = IsGetOtherFeatures;
            await Task.Run(() => GetDataTask());
            WaitAsyncForGetDataTask.WaitOne();
            return DataSetRet;

        }

        public UVDataSet GetUVSensingValue()
        {
            var _uvVal = module_base.GetUV();
            UVDataSet _uvDataSet = new UVDataSet
            {
                UVValue = _uvVal,
                ErrorCode = _uvVal == -1 ? Convert.ToInt32(clsErrorCode.Error.DataGetTimeout) : 0,
                RecieveTime = DateTime.Now
            };
            return _uvDataSet;
        }

        private ManualResetEvent GetDataTaskPause;
       
      
        private bool IsGetDataTaskPaused = true;

        public void Stop_All_Action()
        {
            module_base.acc_data_read_task_token_source.Cancel();
        }
        private void GetDataTask()
        {
            //IsGetDataTaskPaused = true;
            //GetDataTaskPause.WaitOne();
            //WaitAsyncForParametersSet.WaitOne();
            //IsGetDataTaskPaused = false;
            DataSetRet = new DataSet(module_base.sampling_rate);
            try
            {
                byte[] AccPacket;
                bool IsTimeout;
                AccPacket = module_base.GetAccData_HighSpeedWay(out DataSetRet.TimeSpend, out IsTimeout);
                module_base.isBusy = false;
                if (AccPacket.Length < Convert.ToInt32(DataLength) * 6)
                {
                    DataSetRet.ErrorCode = Convert.ToInt32(clsErrorCode.Error.DataGetTimeout);
                    WaitAsyncForGetDataTask.Set();
                    return;
                }
                var datas = Tools.ConverterTools.AccPacketToListDouble(AccPacket, MeasureRange, DeterminALG());
                DataSetRet.AccData.X = datas[0];
                DataSetRet.AccData.Y = datas[1];
                DataSetRet.AccData.Z = datas[2];

                if (IsGetFFT)
                {
                    DataSetRet.FFTData.X = GpmMath.FFT.GetFFT(DataSetRet.AccData.X);
                    DataSetRet.FFTData.Y = GpmMath.FFT.GetFFT(DataSetRet.AccData.Y);
                    DataSetRet.FFTData.Z = GpmMath.FFT.GetFFT(DataSetRet.AccData.Z);
                    DataSetRet.FFTData.FreqsVec = FreqVecCal(DataSetRet.FFTData.X.Count);
                }

                if (IsGetOtherFeatures)
                {
                    if (IsGetFFT)
                    {
                        DataSetRet.Features.VibrationEnergy.X = GpmMath.Stastify.GetOA(DataSetRet.FFTData.X);
                        DataSetRet.Features.VibrationEnergy.Y = GpmMath.Stastify.GetOA(DataSetRet.FFTData.Y);
                        DataSetRet.Features.VibrationEnergy.Z = GpmMath.Stastify.GetOA(DataSetRet.FFTData.Z);
                    }
                    DataSetRet.Features.AccP2P.X = GpmMath.Stastify.GetPP(DataSetRet.AccData.X);
                    DataSetRet.Features.AccP2P.Y = GpmMath.Stastify.GetPP(DataSetRet.AccData.Y);
                    DataSetRet.Features.AccP2P.Z = GpmMath.Stastify.GetPP(DataSetRet.AccData.Z);

                    DataSetRet.Features.AccRMS.X = GpmMath.Stastify.RMS(DataSetRet.AccData.X);
                    DataSetRet.Features.AccRMS.Y = GpmMath.Stastify.RMS(DataSetRet.AccData.Y);
                    DataSetRet.Features.AccRMS.Z = GpmMath.Stastify.RMS(DataSetRet.AccData.Z);
                }
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

            WaitAsyncForGetDataTask.Set();
        }

        private clsEnum.FWSetting_Enum.ACC_CONVERT_ALGRIUM DeterminALG()
        {
            if (module_base.module_settings.ByteAryOfParameters[1] == 0x00)
                return clsEnum.FWSetting_Enum.ACC_CONVERT_ALGRIUM.Old;
            else
                return clsEnum.FWSetting_Enum.ACC_CONVERT_ALGRIUM.New;
        }
        private List<double> FreqVecCal(int FFTWindowSize)
        {
            var freqVec = new List<double>();
            var NysFreq = module_base.sampling_rate / 2;
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


        /// <summary>
        /// 設定/取得感測器取樣頻率
        /// </summary>
        public double SamplingRate
        {
            get
            {
                return module_base.sampling_rate;
            }
            set
            {
                module_base.sampling_rate = value;
            }
        }

        public Action<string> ReConnectEvent { get; set; }
    }
}
