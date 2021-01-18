using gpm_vibration_module_api.GPMBase;
using gpm_vibration_module_api.GpmMath;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        internal int DataLenMiniLen = 256;
        private Stopwatch HSStopWatch = new Stopwatch();
        private int GetDataInteruptFlag;
        internal bool IsDaulMCUMode = true;

        private readonly NET.DeviceTestData DeviceData = new NET.DeviceTestData();
        public int GetDataCalledNum { get; private set; }
        public int GetDataSuccessNum { get; private set; }
        public DataSet DataSetForUser { get; private set; }
        public event Action<int> DataPacketOnchange;
        /// <summary>
        /// TCP/IP 通訊界面
        /// </summary>
        public AsynchronousClient AsynchronousClient;
        /// <summary>
        /// Serial Port通訊介面
        /// </summary>
        private ModuleSerialPortBase SerialPortBase;
        public GPMModuleAPI_HSR()
        {
            AsynchronousClient = new AsynchronousClient();
            AsynchronousClient.DataPacketLenOnchange += AsynchronousClient_DataPacketLenOnchange1;
            Tools.Logger.Event_Log.Log($"GPMModuleAPI_HSR 物件建立");
        }

        private void AsynchronousClient_DataPacketLenOnchange1(object sender, int e)
        {
            DataPacketOnchange?.BeginInvoke(e, null, null);
        }
        public string IP { get; private set; }
        public int Port { get; private set; }
        private bool GetDataFirstCall = true;

        public ModuleSetting Settings { get; internal set; } = new ModuleSetting();

        #region API FOR USER

        /// <summary>
        /// 低通濾波器截止頻率
        /// </summary>
        public double LowpassFilterCutOffFreq { get; set; } = 4000;

        /// <summary>
        /// 取得連線狀態
        /// </summary>
        public bool Connected
        {
            get
            {
                try
                {
                    if (SerialPortBase == null)
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


        /// <summary>
        /// 跟模組連線
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="Port"></param>
        /// <returns></returns>
        public async Task<int> Connect(string IP, int Port)
        {
            try
            {
                SerialPortBase = null;
                Tools.Logger.Event_Log.Log($"使用者嘗試連線...{IP}:{Port}");
                if (Connected) //如果本來已經有連線，就先斷線。
                    Disconnect();
                this.IP = IP;
                this.Port = Port;
                int isconnect = await AsynchronousClient.AsyncConnect(IP, Port);
                if (isconnect != 0)
                {
                    Disconnect();
                    Tools.Logger.Event_Log.Log("TCP/IP Connecet fail.");
                    return isconnect;
                }
                await GetDataInterupt();
                Tools.Logger.Event_Log.Log("Device Connected.");
                return 0;
            }
            catch (Exception ex)
            {
                Tools.Logger.Event_Log.Log($"Connect exception occurred! {ex.Message}");
                return (int)clsErrorCode.Error.SYSTEM_ERROR;
            }

        }

        public async Task<int> Open(string PortName, int BaudRate = 115200)
        {
            Tools.Logger.Event_Log.Log($"(Serial Port)使用者嘗試連線..PORTNAME:{PortName}");
            SerialPortBase = new ModuleSerialPortBase();
            return await SerialPortBase.Open(PortName, BaudRate);
        }

        /// <summary>
        /// 斷開與Device的TCP/IP連線
        /// </summary>
        /// <returns></returns>
        public int Disconnect()
        {
            Tools.Logger.Event_Log.Log("Try Disconnect.");
            if (SerialPortBase == null)
            {
                AsynchronousClient.Disconnect();
                AsynchronousClient = null;
                GC.Collect();
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
                var state = await SendMessageMiddleware(Settings.SettingBytesWithHead, 8, Timeout: 3000);
                int retry_cnt = 0;
                while (state.ErrorCode != clsErrorCode.Error.None)
                {
                    retry_cnt++;
                    state = (await SendMessageMiddleware(Settings.SettingBytesWithHead, 8, Timeout: 3000));
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
        public async Task<int> Measure_Range_Setting(MEASURE_RANGE mr_select)
        {
            Tools.Logger.Event_Log.Log($"使用者嘗試修改量測範圍({mr_select})");
            var ori_Set = Settings.mEASURE_RANGE;
            //await GetDataInterupt();
            Settings.mEASURE_RANGE = mr_select;
            var state = (await SendMessageMiddleware(Settings.SettingBytesWithHead, 8, Timeout: 3000));
            int retry_cnt = 0;
            while (state.ErrorCode != clsErrorCode.Error.None)
            {
                retry_cnt++;
                state = (await SendMessageMiddleware(Settings.SettingBytesWithHead, 8, Timeout: 3000));
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

        /// <summary>
        /// DAQ MODE SETTING::HSR Version isn't support, always retrun 0.
        /// </summary>
        /// <param name="Mode"></param>
        /// <param name="IsNeedReboot"></param>
        /// <returns></returns>
        virtual public async Task<int> DAQModeSetting(DAQMode Mode)
        {
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
                var state_obj = await SendMessageMiddleware("READVALUE\r\n", Settings.PackageTotalLen, Timeout: 3000);
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
                _ = DataUploadToWeb();
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
        public async Task<string> GetDeviceParams()
        {
            var state = await SendMessageMiddleware("READSTVAL\r\n", 8, 1000);
            return state.ErrorCode == clsErrorCode.Error.None ? state.DataByteList.ToArray().ToCommaString() : state.ErrorCode.ToString();
        }

        #endregion

        #region Private Methods
        private async Task DataUploadToWeb()
        {
            try
            {
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
            if (SerialPortBase == null)
                return await AsynchronousClient.SendMessage(msg, CheckLen, Timeout);
            else
                return await SerialPortBase.SendMessage(msg, CheckLen, Timeout);
        }
        internal async Task<StateObject> SendMessageMiddleware(byte[] msg, int CheckLen, int Timeout)
        {
            if (SerialPortBase == null)
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
                List<List<double>> ori_xyz_data_list = null;
                if (IsDaulMCUMode)
                    ori_xyz_data_list = Tools.ConverterTools.AccPacketToListDouble_KX134(raw_bytes.ToArray(), Settings.LSB, MiniPacketDataLen);
                else
                    ori_xyz_data_list = Tools.ConverterTools.AccPacketToListDouble(raw_bytes.ToArray(), Settings.mEASURE_RANGE, Settings.Mode);
                List<List<double>> XYZ_Acc_Data_List = Filters.LPF(ori_xyz_data_list, LowpassFilterCutOffFreq, Settings.SamplingRate); //濾波
                DataSet dataSet_ret = new DataSet(Settings.SamplingRate) { RecieveTime = DateTime.Now };
                dataSet_ret.AccData.X = XYZ_Acc_Data_List[0];
                dataSet_ret.AccData.Y = XYZ_Acc_Data_List[1];
                dataSet_ret.AccData.Z = XYZ_Acc_Data_List[2];
                FFTAndFeatureCal(ref dataSet_ret, GetFFT, GetFeatures, Settings.SamplingRate);
                return dataSet_ret;
            }
            catch (Exception ex)
            {
                return new DataSet(0) { ErrorCode = (int)clsErrorCode.Error.PostProcessingError };
            }


        }
        internal void FFTAndFeatureCal(ref DataSet dataSet_ret, bool fft, bool other_feature, double samplingRate)
        {
            if (fft)
            {
                DataForFFTCompensate(ref dataSet_ret);
                dataSet_ret.FFTData.X = FFT.GetFFT(dataSet_ret.AccData.acc_x_For_FFT);
                dataSet_ret.FFTData.Y = FFT.GetFFT(dataSet_ret.AccData.acc_y_For_FFT);
                dataSet_ret.FFTData.Z = FFT.GetFFT(dataSet_ret.AccData.acc_z_For_FFT);
                dataSet_ret.Features.VibrationEnergy.X = Stastify.GetOA(dataSet_ret.FFTData.X);
                dataSet_ret.Features.VibrationEnergy.Y = Stastify.GetOA(dataSet_ret.FFTData.Y);
                dataSet_ret.Features.VibrationEnergy.Z = Stastify.GetOA(dataSet_ret.FFTData.Z);
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

    public class ModuleSetting
    {
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
        internal DAQMode _Mode = DAQMode.High_Sampling;
        public DAQMode Mode
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
        public MEASURE_RANGE mEASURE_RANGE
        {
            get { return _mEASURE_RANGE; }
            set
            {
                _mEASURE_RANGE = value;
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

    }



}

