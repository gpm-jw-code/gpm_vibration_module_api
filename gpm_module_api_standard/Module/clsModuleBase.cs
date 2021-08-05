using gpm_module_api.VibrationSensor;
using gpm_vibration_module_api.Module;
using gpm_vibration_module_api.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gpm_vibration_module_api
{
    /// <summary>
    /// 振動感測模組底層控制類別
    /// </summary>
    public class ClsModuleBase
    {
        public Socket module_socket { get; internal set; }
        public bool is_bulk_break { get; private set; } = true;

        public Module.clsModuleSettings module_settings = new Module.clsModuleSettings();
        private ManualResetEvent pause_signal;
        private bool is_pause_ready = true;
        private string ip;
        private int port;


        internal static int delay_ = 30;
        internal int acc_data_rev_timeout = 8000; //unit: ms
        internal int fw_parm_rw_timeout = 5000; //unit: ms
        internal bool is_old_firmware_using = false;

        internal ClsParamSetTaskObj setTaskObj = new ClsParamSetTaskObj(DAQMode.High_Sampling);
        public ClsModuleBase()
        {
            pause_signal = new ManualResetEvent(true);
            bulk_request_pause_signal = new ManualResetEvent(true);
            wait_for_data_get_signal = new ManualResetEvent(true);
        }

        public async Task<int> Connect()
        {
            var ret = await Connect(ip, port);
            return ret;
        }
        /// <summary>
        /// 連線
        /// </summary>
        public async Task<int> Connect(string ModuleIP, int ModulePort)
        {
            try
            {
                ip = ModuleIP;
                port = ModulePort;
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(ModuleIP), ModulePort);
                module_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                module_socket.ReceiveBufferSize = 100000;
                module_socket.DontFragment = false;
                module_socket.ReceiveTimeout = 30000;
                module_socket.Ttl = 125;
                module_socket.NoDelay = true;
                module_socket.Blocking = true;
                module_socket.Connect(remoteEP);
                if (module_socket.Connected)
                    return 0;
                else
                    return Convert.ToInt32(clsErrorCode.Error.CONNECT_FAIL);
            }
            catch (SocketException exp)
            {
                return Convert.ToInt32(clsErrorCode.Error.CONNECT_FAIL);
            }
        }
        /// <summary>
        /// 斷線,釋放資源
        /// </summary>
        /// <returns></returns>
        public int Disconnect()
        {
            try
            {
                if (bulk_use)
                    SendBulkBreakCmd();
                module_socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception)
            {
            }
            try
            {
                module_socket.Close();
            }
            catch (Exception)
            {

            }
            return 0;
        }

        /// <summary>
        /// 寫入參數至控制器
        /// </summary>
        /// <param name="Parameters"></param>
        /// <returns></returns>
        public byte[] WriteParameterToController(byte[] Parameters, int ExceptLength)
        {
            try
            {
                SocketBufferClear();
                byte[] ToWrite = new byte[11];
                ToWrite[0] = 0x53;
                ToWrite[9] = 0x0d;
                ToWrite[10] = 0x0a;
                if (!module_settings.Is_Daul_MCU_Mode)
                {
                    ToWrite[1] = 0x01;
                    ToWrite[7] = 0x00;
                    ToWrite[8] = Convert.ToByte(delay_);///強制寫DELAY TIME

                }
                Array.Copy(Parameters, 0, ToWrite, 1, Parameters.Length);

                return SendCommand(ToWrite, ExceptLength).Result;
            }
            catch (OperationCanceledException ex)
            {
                Tools.Logger.Event_Log.Log("[WriteParameterToController]使用者中斷");
                return new byte[0];
            }
            catch
            {
                return new byte[0];
            }
        }
        /// <summary>
        /// 寫入參數至控制器(function)
        /// </summary>
        /// <param name="sensorType"></param>
        /// <param name="dataLength"></param>
        /// <param name="measureRange"></param>
        /// <param name="oDR"></param>
        /// <returns></returns>
        public Tuple<byte[], int> SettingToController(clsEnum.Module_Setting_Enum.SENSOR_TYPE? sensorType,

            clsEnum.Module_Setting_Enum.MEASURE_RANGE? measureRange,
            clsEnum.Module_Setting_Enum.ODR? oDR, int dataLength = -1)
        {

            Module.clsModuleSettings _UserSetting = new clsModuleSettings();
            _UserSetting = module_settings.DeepClone();
            _UserSetting.dAQMode = setTaskObj.DAQMode;
            var returnBytes = 8;
            //if (moduleSettings.WifiControllUseHighSppedSensor)
            //    returnBytes = 8;
            _UserSetting.SensorType = clsEnum.Module_Setting_Enum.SENSOR_TYPE.Genernal;
            var comp = 0;
            if (module_settings.Is_Daul_MCU_Mode == false && dataLength >= 16)
                comp = module_settings.comp_len;
            else
                comp = 0;
            _UserSetting.DataLength = dataLength != -1 ? dataLength + comp : module_settings.Is_Daul_MCU_Mode ? module_settings.DataBytesSize : module_settings.DataLength + comp;
            _UserSetting.MeasureRange = measureRange != null ? (clsEnum.Module_Setting_Enum.MEASURE_RANGE)measureRange : module_settings.MeasureRange;
            _UserSetting.ODR = oDR != null ? (clsEnum.Module_Setting_Enum.ODR)oDR : module_settings.ODR;
            var ParamReturn = WriteParameterToController(_UserSetting.ByteAryOfParameters, returnBytes);
            Console.WriteLine($"Controller Return:{ObjectAryToString(" ", ParamReturn)}");
            if (Is_PARAM_Return_Correct(_UserSetting.ByteAryOfParameters, ParamReturn))
            {
                Tools.Logger.Event_Log.Log("PARAM SETTING OK");
                Console.WriteLine("PARAM SETTING OK");
                DefineSettingByParameters(ParamReturn);
                Tools.Logger.Event_Log.Log($"PARAM NOW:{ObjectAryToString(",", module_settings.ByteAryOfParameters)}");
                return new Tuple<byte[], int>(module_settings.ByteAryOfParameters, 0);
            }
            else
            {
                Tools.Logger.Event_Log.Log("PARAM SETTING FAILURE");
                Console.WriteLine("PARAM SETTING FAILURE");
                DefineSettingByParameters(module_settings.ByteAryOfParameters);
                return new Tuple<byte[], int>(ParamReturn, ParamReturn.Length == 0 ? Convert.ToInt32(clsErrorCode.Error.PARAM_HS_TIMEOUT) : Convert.ToInt32(clsErrorCode.Error.ERROR_PARAM_RETURN_FROM_CONTROLLER));
            }
        }

        internal bool Is_PARAM_Return_Correct(byte[] send, byte[] rev)
        {
            bool check1_result = false;
            bool check2_result = true;
            if (rev.Length == 0 | rev.Length != send.Length) return false;

            string _send_string = send[0] + "," + send[1] + "," + send[2];
            string _rev_string = rev[0] + "," + rev[1] + "," + rev[2] + "," + rev[3] + "," + rev[4] + "," + rev[5] + "," + rev[6] + "," + rev[7];
            check1_result = _rev_string.Contains(_send_string);

            for (int i = 0; i < send.Length; i++)
            {
                if (send[i] != rev[i])
                {
                    check2_result = false; break;
                }
            }
            Tools.Logger.Event_Log.Log($"check1:{check1_result}[{_send_string}]/[{_rev_string}],check2:{check2_result}");

            return check1_result == true | check2_result == true;
        }

        public static string ObjectAryToString(string split, byte[] obj)
        {
            var _s = "";

            foreach (var item in obj)
            {
                _s += item + split;
            }
            if (_s.Length >= 1)
                _s.Remove(_s.Length - 1);
            return _s;
        }


        private void SettingDefine_FD(byte[] Parameters)
        {
            var DataLengthByte = Parameters[1];
            var ODRByte = Parameters[2];
            var MeasureRangeByte = Parameters[3];

            module_settings.DataLength = DataLengthByte == 0 ? 1 : (int)DataLengthByte - (DataLengthByte >= 16 ? module_settings.comp_len : 0);

            switch (ODRByte)
            {
                case 0x9F:
                    module_settings.ODR = clsEnum.Module_Setting_Enum.ODR._9F;
                    break;
                case 0x87:
                    module_settings.ODR = clsEnum.Module_Setting_Enum.ODR._87;
                    break;
                default:
                    module_settings.ODR = clsEnum.Module_Setting_Enum.ODR._9F;
                    break;
            }

            switch (MeasureRangeByte)
            {
                case 0x00:
                    module_settings.MeasureRange = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_2G;
                    break;
                case 0x10:
                    module_settings.MeasureRange = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_4G;
                    break;
                case 0x20:
                    module_settings.MeasureRange = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_8G;
                    break;
                case 0x30:
                    module_settings.MeasureRange = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_16G;
                    break;
                default:
                    module_settings.MeasureRange = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_2G;
                    break;
            }

        }

        public void SettingDefine_KX134(byte[] Parameters)
        {
            var MeasureRangeByte = Parameters[3];
            var ODCNTLByte = Parameters[4];

            module_settings.DataBytesSize = (256 * Parameters[0] + Parameters[1]) * 1536;
            module_settings.DataLength = module_settings.DataBytesSize;
            switch (MeasureRangeByte)
            {
                case 0xC0:
                    module_settings.MeasureRange = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_8G; //kx134 oNLY
                    break;
                case 0xC8:
                    module_settings.MeasureRange = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_16G;  //kx134 oNLY
                    break;
                case 0xD0:
                    module_settings.MeasureRange = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_32G; //kx134 oNLY
                    break;
                case 0xD8:
                    module_settings.MeasureRange = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_64G; //kx134 oNLY
                    break;
                default:
                    module_settings.MeasureRange = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_8G;
                    break;
            }
        }

        internal void DefineSettingByParameters(byte[] Parameters)
        {
            if (Parameters == null | Parameters.Length != 8)
            {
                Console.WriteLine($" Parameters write fail...");
                return;
            }

            Console.WriteLine(ObjectAryToString(",", Parameters));

            if (!module_settings.Is_Daul_MCU_Mode)
                SettingDefine_FD(Parameters);
            else
                SettingDefine_KX134(Parameters);

            module_settings.ByteAryOfParameters = Parameters;
        }
        private void WaitPause()
        {
            pause_signal.Reset();
            while (is_pause_ready == false)
            {
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// 發送指令至控制器
        /// </summary>
        /// <param name="Timespend">傳回耗時</param>
        /// <returns></returns>
        internal class SocketState
        {
            public enum ABNORMAL
            {
                Normal, Timeout, Disconnect, Cancel

            }

            public ABNORMAL Abnormal_Type = ABNORMAL.Normal;
            public Socket work_socket_ = null;
            public static int Packet_Receive_Size = 256;
            public byte[] buffer_;
            public int window_size_ = 512;
            public StringBuilder string_builder_ = new StringBuilder();
            public IAsyncResult async_result_;
            public bool is_data_recieve_done_flag_ = false;
            public bool is_data_recieve_timeout_ = false;
            public TIMEOUT_CHEK_ITEM task_of_now = TIMEOUT_CHEK_ITEM.Read_Acc_Data;
            public byte[] data_rev_ = new byte[0];
            public CancellationTokenSource cancel_token_source_;

            public long time_spend_;
            public List<byte> temp_rev_data = new List<byte>();
        }
        internal CancellationTokenSource timeout_task_cancel_source = new CancellationTokenSource();
        internal struct SendCmdType
        {
        }

        internal void TinySensorFWUpdate(List<byte[]> efm8DataFrames)
        {
            try
            {
                SocketBufferClear();
                module_socket.Send(Encoding.ASCII.GetBytes("INTOBMODE\r\n"));

                foreach (var item in efm8DataFrames)
                {
                    module_socket.Send(item, 0, item.Length, SocketFlags.None);
                    Thread.Sleep(100);
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp.Message + "__" + exp.StackTrace);
            }
        }


        internal event Action<DataSet> DataRecieve;
        private MeasureOption userOption;
        private Thread THBulkProcess = null;
        /// <summary>
        /// 開始巨量資料接收
        /// </summary>
        internal void StartGetData_Bulk(MeasureOption option)
        {
            bulk_use = true;
            Tools.Logger.Event_Log.Log($"[StartDataRecieve] Start Data Recieve(BULK Method). Options:{option._Description}");
            userOption = option;
            SendBulkBreakCmd();
            SocketBufferClear();
            if (THBulkProcess == null)
            {
                Task.Run(() => BulkBufferProcess());
                BulkState = new SocketState() { buffer_ = new byte[800000], work_socket_ = module_socket, window_size_ = option.WindowSize, task_of_now = TIMEOUT_CHEK_ITEM.Read_Acc_Data };
                module_socket.BeginReceive(BulkState.buffer_, 0, SocketState.Packet_Receive_Size, 0, new AsyncCallback(receiveCallBack_Bulk), BulkState);
            }
            Bulk_Buffer.Clear();
            Tools.Logger.Event_Log.Log($"[StartGetData_Bulk] Send:{clsEnum.ControllerCommand.BULKVALUE + "\r\n"}");
            SendBulkDataStartCmd();
        }
        internal void SendBulkDataStartCmd()
        {
            try
            {
                //BulkBreak();
                var cmdbytes = Encoding.ASCII.GetBytes(clsEnum.ControllerCommand.BULKVALUE + "\r\n");
                module_socket.Send(cmdbytes, 0, cmdbytes.Length, SocketFlags.None);
            }
            catch (Exception exp)
            {
                Tools.Logger.Event_Log.Log($"[SendBulkDataStartCmd] {exp.Message + exp.StackTrace}");
            }
            bulk_request_pause_signal.Set();

        }

        internal void SendBulkBreakCmd()
        {
            bulk_request_pause_signal.Reset();
            var cmdbytes = Encoding.ASCII.GetBytes(clsEnum.ControllerCommand.BULKBREAK.ToString() + "\r\n");
            try
            {
                module_socket.Send(cmdbytes, 0, cmdbytes.Length, SocketFlags.None);
            }
            catch (Exception exp)
            {
                Console.WriteLine("[SendBulkDataStartCmd()] " + exp.Message);
            }
        }

        internal void BulkBreak()
        {
            bulk_request_pause_signal.Reset();
            Thread.Sleep(500);

            //IsBulkBreak = true;
            while (!is_bulk_break)
            {
                Thread.Sleep(1);
            }

            while (module_socket.Available != 0)
            {
                SocketBufferClear();
                SendBulkBreakCmd();
                Thread.Sleep(100);
            }
            Bulk_Buffer.Clear();
            BulkState.buffer_ = new byte[512];

            if (BulkState.async_result_ == null)
            {
                is_bulk_break = true;
                return;
            }
            try
            {
                module_socket.EndReceive(BulkState.async_result_);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
            }
            is_bulk_break = true;

        }

        private Dictionary<string, List<double>> WindowData = new Dictionary<string, List<double>>()
        {
            { "X", new List<double>() },
            { "Y", new List<double>() },
            { "Z", new List<double>() }
        };

        private List<double>[] RedundentData = new List<double>[3];
        private List<byte> Bulk_Buffer = new List<byte>();
        private List<byte> Bulk_temp_buffer = new List<byte>();
        private SocketState BulkState = new SocketState();
        /// <summary>
        /// 巨量資料非同步接收
        /// </summary>
        /// <param name="ar"></param>
        private void receiveCallBack_Bulk(IAsyncResult ar)
        {
            //如果是巨量暫停狀態，收到的東西都是設定的東西
            BulkState = (SocketState)ar.AsyncState;
            BulkState.async_result_ = ar;
            bulk_request_pause_signal.WaitOne();
            var client = BulkState.work_socket_;
            try
            {

                int bytesRead = client.EndReceive(ar);
                if (bytesRead > 0)
                {
                    var rev = new byte[bytesRead];
                    Array.Copy(BulkState.buffer_, 0, rev, 0, 6);
                    Bulk_temp_buffer.AddRange(rev);
                    if (Bulk_temp_buffer.Count >= 3072)
                    {
                        Bulk_Buffer.AddRange(Bulk_temp_buffer);
                        Bulk_temp_buffer.Clear();
                    }
                }
                else
                {

                }

                BulkState.buffer_ = new byte[512];

            }
            catch (Exception exp)
            {
                Tools.Logger.Code_Error_Log.Log($"[receiveCallBack_Bulk] ERROR OCCURED {exp.Message + exp.StackTrace}");
            }
            try
            {
                client.BeginReceive(BulkState.buffer_, 0, SocketState.Packet_Receive_Size, 0, new AsyncCallback(receiveCallBack_Bulk), BulkState);
            }
            catch (Exception exp)
            {
                Tools.Logger.Code_Error_Log.Log("[receiveCallBack_Bulk] " + exp.Message);
            }
            //WaitForBufferRecieveDone.Set();
        }

        /// <summary>
        /// 非同步處理封包數據,並拋出給註冊者
        /// </summary>
        private void BulkBufferProcess()
        {
            while (true)
            {
                is_bulk_break = true;
                bulk_request_pause_signal.WaitOne();
                is_bulk_break = false;
                var condition = BulkState.window_size_ * 6;
                if (Bulk_Buffer.Count >= condition)
                {
                    Tools.Logger.Event_Log.Log($"Bulk Buffer Length:{Bulk_Buffer.Count}");
                    var _ = Bulk_Buffer.Contains(159);
                    try
                    {
                        var startIndex = 0;
                        byte[] rev = new byte[condition];
                        Array.Copy(Bulk_Buffer.ToArray(), startIndex, rev, 0, rev.Length);
                        Bulk_Buffer.RemoveRange(0, condition + startIndex);
                        DataSet.ADCError _adcError;
                        //var doubleOutput = BytesToDoubleList(rev, false);
                        var doubleOutput = ConverterTools.AccPacketToListDouble(rev, module_settings.MeasureRange, module_settings.dAQMode);
                        var dataset = new DataSet(module_settings.sampling_rate_);
                        dataset.AccData.X = (doubleOutput[0]);
                        dataset.AccData.Y = (doubleOutput[1]);
                        dataset.AccData.Z = (doubleOutput[2]);
                        //DataReady?.BeginInvoke(dataset, null, null);
                        DataRecieve?.Invoke(dataset);
                    }
                    catch (Exception exp)
                    {
                        continue;
                    }

                }
                else
                {
                    try
                    {
                        SendBulkDataStartCmd();
                    }
                    catch
                    {
                        Thread.Sleep(1);
                    }
                    //if (RecieveCnt == 100)
                    //{
                    //    SendBulkDataStartCmd();
                    //    RecieveCnt = 0;
                    //}
                }
                Thread.Sleep(1);
            }
        }
        internal SocketState state;
        internal bool isBusy = false;
        AsyncCallback RevCallBack;

        internal virtual SocketState GetAccData_HighSpeedWay(out long timespend, out bool IsTimeout)
        {
            if (RevCallBack == null)
                RevCallBack = new AsyncCallback(receiveCallBack);
            state = new SocketState();
            isBusy = true;
            acc_data_read_task_token_source = new CancellationTokenSource();
            bulk_use = false;
            try
            {
                var Is_Daul_MCU_Mode = module_settings.Is_Daul_MCU_Mode;
                WaitForBufferRecieveDone = new ManualResetEvent(false);
                SocketBufferClear();
                var cmdbytes = Encoding.ASCII.GetBytes(clsEnum.ControllerCommand.READVALUE + "\r\n");
                module_socket.Send(cmdbytes, 0, cmdbytes.Length, SocketFlags.None);
                int Datalength = 0;
                if (!module_settings.Is_Daul_MCU_Mode)
                    Datalength = module_settings.dAQMode == DAQMode.High_Sampling ? (Is_Daul_MCU_Mode ? module_settings.DataLength * 3072 : 3072) : (Is_Daul_MCU_Mode ? module_settings.DataBytesSize : module_settings.DataLength * 3072);
                else
                    Datalength = module_settings.DataBytesSize;
                byte[] Datas = new byte[Datalength];
                SocketBufferClear();
                module_socket.ReceiveBufferSize = Datalength;
                state = new SocketState()
                {
                    window_size_ = Datalength,
                    buffer_ = new byte[SocketState.Packet_Receive_Size],
                    work_socket_ = module_socket,
                    //buffer_size_ = Datalength / 512,
                    task_of_now = TIMEOUT_CHEK_ITEM.Read_Acc_Data,
                    data_rev_ = new byte[Datalength],
                    is_data_recieve_done_flag_ = false,
                    time_spend_ = -1,
                    temp_rev_data = new List<byte>()
                };
                Tools.Logger.Event_Log.Log($"receiveCallBack_begining." + $"window_size_ {Datalength}" +
                    $"buffer_size_ {Datalength}");
                module_socket.BeginReceive(state.buffer_, 0, SocketState.Packet_Receive_Size, 0, RevCallBack, state);
                var task = Task.Run(() => TimeoutCheck(state));
                WaitForBufferRecieveDone.WaitOne();
                timespend = task.Result; //1 tick = 100 nanosecond  = 0.0001 毫秒
                IsTimeout = state.is_data_recieve_timeout_;
                Tools.Logger.Event_Log.Log($"Timeout Detector ..Task { state.task_of_now}..[In Time] , Spend:{timespend} ms");
                return state;
            }
            catch (Exception exp)
            {
                Tools.Logger.Code_Error_Log.Log(exp);
                isBusy = false;
                state.is_data_recieve_done_flag_ = true;
                timespend = -1;
                WaitForBufferRecieveDone.Set();
                IsTimeout = false;
                return state;
            }
        }




        internal enum TIMEOUT_CHEK_ITEM
        {
            Read_Acc_Data, FW_Param_RW
        }

        internal async Task<long> TimeoutCheck(SocketState _state)
        {
            timeout_task_cancel_source = new CancellationTokenSource();

            Stopwatch timer = new Stopwatch();
            timer.Start();
            var timeoutPeriod = _state.task_of_now == TIMEOUT_CHEK_ITEM.Read_Acc_Data ? acc_data_rev_timeout : fw_parm_rw_timeout;
            while (_state.is_data_recieve_done_flag_ == false)
            {
                Thread.Sleep(1);
                try
                {

                    if (timeout_task_cancel_source.Token.IsCancellationRequested)
                    {
                        timeout_task_cancel_source.Token.ThrowIfCancellationRequested();
                    }

                    if (_state.task_of_now == TIMEOUT_CHEK_ITEM.Read_Acc_Data && acc_data_read_task_token_source.IsCancellationRequested)
                    {
                        acc_data_read_task_token_source.Token.ThrowIfCancellationRequested();
                    }
                    if (_state.task_of_now == TIMEOUT_CHEK_ITEM.FW_Param_RW && param_setting_task_cancel_token_Source.IsCancellationRequested)
                    {
                        param_setting_task_cancel_token_Source.Token.ThrowIfCancellationRequested();
                    }
                }
                catch (OperationCanceledException ex)
                {
                    _state.Abnormal_Type = SocketState.ABNORMAL.Cancel;
                    _state.is_data_recieve_done_flag_ = true;
                    _state.is_data_recieve_timeout_ = false;
                    Tools.Logger.Event_Log.Log("[TimeoutCheck]使用者中斷");
                    break;
                }
                catch (Exception ex)
                {
                    Tools.Logger.Code_Error_Log.Log($"[TimeoutCheck]系統例外.,.{ex.Message + "\r\n" + ex.StackTrace}");
                    break;
                }


                if (timer.ElapsedMilliseconds >= timeoutPeriod) //TODO 外部可調
                {
                    timer.Stop();
                    _state.time_spend_ = timer.ElapsedMilliseconds;
                    _state.Abnormal_Type = SocketState.ABNORMAL.Timeout;
                    Tools.Logger.Event_Log.Log($"Timeout Detector ..Task{ _state.task_of_now}..[Timeout] , Spend:{timer.ElapsedMilliseconds} ms");
                    _state.is_data_recieve_timeout_ = true;
                    if (_state.task_of_now == TIMEOUT_CHEK_ITEM.Read_Acc_Data)
                    {
                        isBusy = false;
                        WaitForBufferRecieveDone.Set();
                        acc_data_read_task_token_source.Cancel();
                    }
                    if (_state.task_of_now == TIMEOUT_CHEK_ITEM.FW_Param_RW)
                    {
                        param_setting_task_cancel_token_Source.Cancel();
                    }
                    return timer.ElapsedMilliseconds;
                }
            }
            timer.Stop();

            _state.Abnormal_Type = SocketState.ABNORMAL.Normal;
            return timer.ElapsedMilliseconds;
        }

        internal CancellationTokenSource acc_data_read_task_token_source = new CancellationTokenSource();

        internal ManualResetEvent WaitForBufferRecieveDone;
        internal virtual void receiveCallBack(IAsyncResult ar)
        {
            var Is_Daul_MCU_Mode = module_settings.Is_Daul_MCU_Mode;
            //Tools.Logger.Event_Log.Log($"receiveCallBack process");
            isBusy = true;
            SocketState state = (SocketState)ar.AsyncState;
            try
            {
                if (acc_data_read_task_token_source.IsCancellationRequested)
                    acc_data_read_task_token_source.Token.ThrowIfCancellationRequested();
                var client = state.work_socket_;
                int bytesRead = client.EndReceive(ar);
                //Tools.Logger.Event_Log.Log($"封包接收,大小:{bytesRead}");


                if (bytesRead > 0)
                {
                    var rev = new byte[bytesRead];
                    Array.Copy(state.buffer_, 0, rev, 0, bytesRead);
                    state.temp_rev_data.AddRange(rev);
                    //Console.WriteLine(state.temp_rev_data.Count);
                    state.is_data_recieve_done_flag_ = false;
                    //client.BeginReceive(state.buffer_, 0, SocketState.Packet_Receive_Size, 0,  RevCallBack, state);
                    Console.WriteLine(bytesRead + "/" + state.temp_rev_data.Count + "");
                    if (state.temp_rev_data.Count >= state.window_size_)
                    {

                        Array.Copy(state.temp_rev_data.ToArray(), 0, state.data_rev_, 0, state.window_size_);
                        state.temp_rev_data.Clear();
                        if (!Is_Daul_MCU_Mode)
                            SendBulkBreakCmd();
                        WaitForBufferRecieveDone.Set();
                        state.is_data_recieve_done_flag_ = true;
                        return;
                    }
                    else
                    {
                        client.BeginReceive(state.buffer_, 0, SocketState.Packet_Receive_Size, 0, RevCallBack, state);
                    }
                }
                else
                {
                    //Tools.Logger.Event_Log.Log($"封包接收,大小:{0}");

                }

            }
            catch (OperationCanceledException ex)
            {
                Tools.Logger.Event_Log.Log("[receiveCallBack] OperationCanceledException 使用者中斷");
                isBusy = false;
                state.is_data_recieve_done_flag_ = true;
                state.Abnormal_Type = SocketState.ABNORMAL.Disconnect;
                WaitForBufferRecieveDone.Set();
            }
            catch (SocketException ex)
            {
                state.Abnormal_Type = SocketState.ABNORMAL.Disconnect;
                state.is_data_recieve_done_flag_ = true;
                Tools.Logger.Event_Log.Log($"[receiveCallBack_SocketException] Exception {ex.Message }");
                Tools.Logger.Code_Error_Log.Log($"[receiveCallBack] SocketException {ex.Message + "\r\n" + ex.StackTrace}");
                isBusy = false;
                WaitForBufferRecieveDone.Set();
            }
            catch (Exception ex)
            {
                state.Abnormal_Type = SocketState.ABNORMAL.Disconnect;
                state.is_data_recieve_done_flag_ = true;
                Tools.Logger.Event_Log.Log($"[receiveCallBack_Exception] Exception {ex.Message }");
                Tools.Logger.Code_Error_Log.Log($"[receiveCallBack] Exception {ex.Message + "\r\n" + ex.StackTrace}");
                isBusy = false;
                WaitForBufferRecieveDone.Set();

            }
            //            AccDataBuffer.AddRange(state.buffer);
        }

        /// <summary>
        /// 確認參數是否合理
        /// </summary>
        internal void CheckParamIllegeAndFixIt(ref byte[] Param)
        {
            if (Param[1] != 0x00 && Param[1] != 0x01 && Param[1] != 0x02 && Param[1] != 0x03)
            {
                Param[1] = 0x00;
            }

            if (Param[2] != 0x87 && Param[2] != 0x9F)
            {
                Param[2] = 0x9F;
            }

            if (Param[3] != 0x00 && Param[3] != 0x10 && Param[3] != 0x20 && Param[3] != 0x30)
            {
                Param[3] = 0x00;
            }

        }

        internal CancellationTokenSource param_setting_task_cancel_token_Source;
        private ManualResetEvent bulk_request_pause_signal;
        private ManualResetEvent wait_for_data_get_signal;
        internal CommandTask send_cmd_task_obj = new CommandTask();
        /// <summary>
        /// 發送任意bytes給控制器
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="ExpectRetrunSize"></param>
        /// <returns></returns>
        /// 
        internal async Task<byte[]> SendCommand(byte[] Data, int ExpectRetrunSize)
        {
            param_setting_task_cancel_token_Source = new CancellationTokenSource();
            send_cmd_task_obj.Data = Data;
            send_cmd_task_obj.ExpectReturSize = ExpectRetrunSize;
            wait_for_data_get_signal.Reset();
            SocketState state = new SocketState()
            {
                cancel_token_source_ = param_setting_task_cancel_token_Source,
                is_data_recieve_done_flag_ = false,
                task_of_now = TIMEOUT_CHEK_ITEM.FW_Param_RW
            };

            var _task = Task.Run(() => TimeoutCheck(state));
            await SendCommandToController(state);
            //任務完了
            // Console.WriteLine($"SendCommandToController Task End. IsCanceled:{task.IsCanceled}, IsCompleted:{task.IsCompleted}");
            var istimeout = state.is_data_recieve_timeout_;
            if (send_cmd_task_obj.IsAutoStartBulk)
            {
                userOption = new MeasureOption() { WindowSize = 512 };
                StartGetData_Bulk(option: userOption);
            }

            return state.data_rev_;
        }
        private byte[] DataFromSensor = new byte[0];
        internal bool bulk_use = false;

        internal class CommandTask
        {
            public byte[] Data;
            public int ExpectReturSize;
            public bool IsAutoStartBulk = false;
        }
        private async Task SendCommandToController(object _state)
        {
            Stopwatch COUNTER = new Stopwatch();
            COUNTER.Start();
            SocketState state = (SocketState)_state;
            state.is_data_recieve_done_flag_ = false;
            state.is_data_recieve_timeout_ = false;

            byte[] Data = send_cmd_task_obj.Data;
            int ExpectRetrunSize = send_cmd_task_obj.ExpectReturSize;
            if (bulk_use)
                BulkBreak();
            byte[] returnData = new byte[0];
            try
            {
                SocketBufferClear();
                Tools.Logger.Event_Log.Log("Write to Control : " + ObjectAryToString(",", Data));
                module_socket.Send(Data, 0, Data.Length, SocketFlags.None);
                int RecieveByteNum = 0;
                int timespend = 0;
                List<byte> RecievByteList = new List<byte>();
                while (true)
                {
                    if (param_setting_task_cancel_token_Source.IsCancellationRequested == true | RecievByteList.Count == ExpectRetrunSize)
                    {
                        Console.WriteLine(RecievByteList.Count + "/" + ExpectRetrunSize);
                        break;
                    }
                    try
                    {
                        int avaliable = module_socket.Available;
                        returnData = new byte[avaliable];
                        if (avaliable != 0)
                        {
                            Console.WriteLine("參數封包接收:" + avaliable);
                            module_socket.Receive(returnData, RecieveByteNum, avaliable, 0);
                            RecieveByteNum += avaliable;
                            RecievByteList.AddRange(returnData);
                        }
                    }
                    catch
                    {
                        RecieveByteNum = 0;
                        Disconnect();
                        await Connect();
                        SocketBufferClear();
                        module_socket.Send(Data, 0, Data.Length, SocketFlags.None);
                    }

                    //Thread.Sleep(1);
                }
                COUNTER.Stop();
                Tools.Logger.Event_Log.Log($"SendCommandToController ,SPEND:{COUNTER.ElapsedMilliseconds}");
                state.is_data_recieve_done_flag_ = true;
                state.data_rev_ = RecievByteList.ToArray();
            }
            catch (SocketException exp)
            {
                DataFromSensor = returnData;
            }
            wait_for_data_get_signal.Set();
        }




        /// <summary>
        /// 清空socket buffer
        /// </summary>
        internal void SocketBufferClear()
        {
            try
            {
                if (module_socket == null)
                    return;
                if (module_socket.Available != 0)
                {
                    var size = module_socket.Available;
                    byte[] buffer = new byte[size];
                    module_socket.Receive(buffer, 0, size, SocketFlags.None);
                }
            }
            catch
            {

            }
        }

        internal double GetUV()
        {
            var bytesRetrumn = SendCommand(Encoding.ASCII.GetBytes("READVALUE\r\n"), 4);
            if (bytesRetrumn.Result.Length == 0)
                return -1; //Timeout
            return ConvertToUVSensingValue(bytesRetrumn.Result);
        }

        private double ConvertToUVSensingValue(byte[] _dataBytes)
        {
            byte hb = _dataBytes[0];
            byte Lb = _dataBytes[1];
            return hb * 256 + Lb;
            //Covert to double (count num)
        }

    }
}
