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
    internal class ClsModuleBase
    {

        internal int acc_data_rev_timeout = 8000; //unit: ms
        internal int fw_parm_rw_timeout = 5000; //unit: ms
        public Module.clsModuleSettings module_settings = new Module.clsModuleSettings();
        private ManualResetEvent pause_signal;
        private bool is_pause_ready = true;
        internal bool is_old_firmware_using = false;
        public double sampling_rate { get; internal set; } = 1000.0;
        public ClsModuleBase()
        {
            pause_signal = new ManualResetEvent(true);
            bulk_request_pause_signal = new ManualResetEvent(true);
            wait_for_data_get_signal = new ManualResetEvent(true);
        }

        public Socket module_socket { get; internal set; }
        public bool is_bulk_break { get; private set; } = true;

        private string ip;
        private int port;


        public int Connect()
        {
            return Connect(ip, port);
        }
        /// <summary>
        /// 連線
        /// </summary>
        public int Connect(string ModuleIP, int ModulePort)
        {
            try
            {
                ip = ModuleIP;
                port = ModulePort;
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(ModuleIP), ModulePort);
                module_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                module_socket.ReceiveBufferSize = 8192;
                module_socket.ReceiveTimeout = 30000;
                module_socket.Connect(remoteEP);
                if (module_socket.Connected)
                    return 0;
                else
                    return Convert.ToInt32(clsErrorCode.Error.ConnectFail);
            }
            catch (SocketException exp)
            {
                return Convert.ToInt32(clsErrorCode.Error.ConnectFail);
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
                Array.Copy(Parameters, 0, ToWrite, 1, Parameters.Length);
                return SendCommand(ToWrite, ExceptLength).Result;
            }
            catch (OperationCanceledException ex)
            {
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
        public byte[] SettingToController(clsEnum.Module_Setting_Enum.SENSOR_TYPE? sensorType,
            clsEnum.Module_Setting_Enum.DATA_LENGTH? dataLength,
            clsEnum.Module_Setting_Enum.MEASURE_RANGE? measureRange,
            clsEnum.Module_Setting_Enum.ODR? oDR)
        {

            Module.clsModuleSettings _UserSetting = new clsModuleSettings();
            _UserSetting = module_settings.DeepClone();

            var returnBytes = _UserSetting.SensorType == clsEnum.Module_Setting_Enum.SENSOR_TYPE.High | sensorType == clsEnum.Module_Setting_Enum.SENSOR_TYPE.High ? 8 : 8;
            //if (moduleSettings.WifiControllUseHighSppedSensor)
            //    returnBytes = 8;
            _UserSetting.SensorType = sensorType != null ? (clsEnum.Module_Setting_Enum.SENSOR_TYPE)sensorType : module_settings.SensorType;
            _UserSetting.DataLength = dataLength != null ? (clsEnum.Module_Setting_Enum.DATA_LENGTH)dataLength : module_settings.DataLength;
            _UserSetting.MeasureRange = measureRange != null ? (clsEnum.Module_Setting_Enum.MEASURE_RANGE)measureRange : module_settings.MeasureRange;
            _UserSetting.ODR = oDR != null ? (clsEnum.Module_Setting_Enum.ODR)oDR : module_settings.ODR;
            var ParamReturn = WriteParameterToController(_UserSetting.ByteAryOfParameters, returnBytes);
            Console.WriteLine($"Controller Return:{ObjectAryToString(" ", ParamReturn)}");
            if (IsReturnRight(_UserSetting.ByteAryOfParameters, ParamReturn))
            {
                Console.WriteLine("PARAM SETTING OK");
                DefineSettingByParameters(ParamReturn);
                return module_settings.ByteAryOfParameters;
            }
            else
            {
                Console.WriteLine("PARAM SETTING FAILURE");
                DefineSettingByParameters(module_settings.ByteAryOfParameters);
                return new byte[0];
            }
        }

        private bool IsReturnRight(byte[] send, byte[] rev)
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

            return check1_result == true | check2_result == true;
        }

        private string ObjectAryToString(string split, byte[] obj)
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

        internal void DefineSettingByParameters(byte[] Parameters)
        {
            if (Parameters == null | Parameters.Length == 0)
            {
                Console.WriteLine($" Parameters write fail...");
                return;
            }

            Console.WriteLine(ObjectAryToString(",", Parameters));

            var ParametersToDefine = (Parameters.Length != 8 | Parameters[0] != 0x02) ? new byte[] { 0x01, 0x00, 0x9f, 0x00, 0x00, 0x00, 0x00, 0x00 } : Parameters;
            if (Parameters.Length == 1 && Parameters[0] == 0x02)
                return;
            ParametersToDefine[0] = Parameters[0];
            Array.Copy(Parameters, 1, ParametersToDefine, 1, 7);

            var TypeByte = ParametersToDefine[0];
            var DataLengthByte = ParametersToDefine[1];
            var ODRByte = ParametersToDefine[2];
            var MeasureRangeByte = ParametersToDefine[3];

            //switch (TypeByte)
            //{
            //    case 0x01:
            //        module_settings.SensorType = clsEnum.Module_Setting_Enum.SENSOR_TYPE.Genernal;
            //        break;
            //    case 0x02:
            //        module_settings.SensorType = clsEnum.Module_Setting_Enum.SENSOR_TYPE.High;
            //        break;
            //    default:
            //        module_settings.SensorType = clsEnum.Module_Setting_Enum.SENSOR_TYPE.Genernal;
            //        break;
            //}

            switch (DataLengthByte)
            {
                case 0x01:
                    module_settings.DataLength = clsEnum.Module_Setting_Enum.DATA_LENGTH.x1;
                    break;
                case 0x02:
                    module_settings.DataLength = clsEnum.Module_Setting_Enum.DATA_LENGTH.x2;
                    break;
                case 0x04:
                    module_settings.DataLength = clsEnum.Module_Setting_Enum.DATA_LENGTH.x4;
                    break;
                case 0x08:
                    module_settings.DataLength = clsEnum.Module_Setting_Enum.DATA_LENGTH.x8;
                    break;
                default:
                    module_settings.DataLength = clsEnum.Module_Setting_Enum.DATA_LENGTH.x1;
                    break;
            }

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

        private List<byte> AccDataBuffer = new List<byte>();

        private class SocketState
        {
            public Socket work_socket_ = null;
            public int buffer_size_ = 256;
            public byte[] buffer_;
            public int window_size_ = 512;
            public StringBuilder string_builder_ = new StringBuilder();
            public IAsyncResult async_result_;
            public bool is_data_recieve_done_flag_ = false;
            public bool is_data_recieve_timeout_ = false;
            public TIMEOUT_CHEK_ITEM task_of_now = TIMEOUT_CHEK_ITEM.Read_Acc_Data;
            public byte[] data_rev_;
            public CancellationTokenSource cancel_token_source_;
        }

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
                THBulkProcess = new Thread(BulkBufferProcess) { IsBackground = true };
                THBulkProcess.Start();
                BulkState = new SocketState() { buffer_ = new byte[512], work_socket_ = module_socket, buffer_size_ = 512, window_size_ = option.WindowSize, task_of_now = TIMEOUT_CHEK_ITEM.Read_Acc_Data };
                module_socket.BeginReceive(BulkState.buffer_, 0, BulkState.buffer_size_, 0, new AsyncCallback(receiveCallBack_Bulk), BulkState);
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

        private void SendBulkBreakCmd()
        {
            bulk_request_pause_signal.Reset();
            var cmdbytes = Encoding.ASCII.GetBytes(clsEnum.ControllerCommand.BULKBREAK + "\r\n");
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
                    Array.Copy(BulkState.buffer_, 0, rev, 0, bytesRead);
                    Bulk_Buffer.AddRange(rev);
                }
                else
                {

                }

                BulkState.buffer_ = new byte[512];

            }
            catch (Exception exp)
            {
                Tools.Logger.Event_Log.Log($"[receiveCallBack_Bulk] ERROR OCCURED");
                Tools.Logger.Code_Error_Log.Log($"[receiveCallBack_Bulk] {exp.Message},{exp.StackTrace}");
                Console.WriteLine("[receiveCallBack_Bulk] " + exp.Message);
            }
            try
            {
                client.BeginReceive(BulkState.buffer_, 0, BulkState.buffer_size_, 0, new AsyncCallback(receiveCallBack_Bulk), BulkState);
            }
            catch (Exception exp)
            {
                Console.WriteLine("[receiveCallBack_Bulk] " + exp.Message);
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
                    //RecieveCnt++;
                    //Console.WriteLine(DateTime.Now + " Buffer ~~~~~~~~~ 有了");
                    // Console.WriteLine(DateTime.Now + " " + RecieveCnt);
                    try
                    {
                        var startIndex = 0;
                        byte[] rev = new byte[condition];
                        Array.Copy(Bulk_Buffer.ToArray(), startIndex, rev, 0, rev.Length);
                        Bulk_Buffer.RemoveRange(0, condition + startIndex);
                        var doubleOutput = BytesToDoubleList(rev, false);
                        var dataset = new DataSet(sampling_rate);
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

        private List<double>[] BytesToDoubleList(byte[] buffer, bool HeadExist)
        {
            var LSB = Convert.ToInt32(module_settings.MeasureRange);
            clsEnum.FWSetting_Enum.ACC_CONVERT_ALGRIUM alg = clsEnum.FWSetting_Enum.ACC_CONVERT_ALGRIUM.Bulk;
            int splitIndex = -1;
            if (HeadExist == true)
            {
                try
                {
                    for (int i = 0; true; i++)
                    {
                        if (buffer[i] == 13)
                        {
                            if (buffer[i + 1] == 10)
                            {
                                splitIndex = i;
                                break;
                            }
                        }
                    }
                }
                catch
                {
                    return new List<double>[3];
                }
            }
            var x = new List<double>();
            var y = new List<double>();
            var z = new List<double>();
            var startIndex = splitIndex == 6 ? 0 : splitIndex + 2;
            startIndex = HeadExist ? startIndex : 0;
            var multiple = HeadExist ? 8 : 6;
            try
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (HeadExist)
                        if ((8 * i) + startIndex + 0 + 5 >= buffer.Length)
                            break;
                    x.Add(ConverterTools.bytesToDouble(buffer[(multiple * i) + startIndex + 0], buffer[(multiple * i) + startIndex + 1], alg) / LSB);
                    y.Add(ConverterTools.bytesToDouble(buffer[(multiple * i) + startIndex + 2], buffer[(multiple * i) + startIndex + 3], alg) / LSB);
                    z.Add(ConverterTools.bytesToDouble(buffer[(multiple * i) + startIndex + 4], buffer[(multiple * i) + startIndex + 5], alg) / LSB);
                }
            }
            catch (Exception exp)
            {

            }
            return new List<double>[3] { x, y, z };
        }

        DateTime st_time;
        int revBNUm = 0;
        internal byte[] GetAccData_HighSpeedWay(out long timespend, out bool IsTimeout)
        {
            bulk_use = false;
            try
            {
                revBNUm = 0;
                WaitForBufferRecieveDone = new ManualResetEvent(false);
                AccDataBuffer.Clear();
                SocketBufferClear();
                var cmdbytes = Encoding.ASCII.GetBytes(clsEnum.ControllerCommand.READVALUE + "\r\n");
                module_socket.Send(cmdbytes, 0, cmdbytes.Length, SocketFlags.None);
                var Datalength = Convert.ToInt32(module_settings.DataLength) * 6;
                byte[] Datas = new byte[Datalength];
                st_time = DateTime.Now;
                SocketBufferClear();
                SocketState state = new SocketState() { buffer_ = new byte[Datalength], work_socket_ = module_socket, buffer_size_ = Datalength, task_of_now = TIMEOUT_CHEK_ITEM.Read_Acc_Data };
                StartTimeoutCheckout(state);
                module_socket.BeginReceive(state.buffer_, 0, state.buffer_size_, 0, new AsyncCallback(receiveCallBack), state);
                WaitForBufferRecieveDone.WaitOne();
                var ed_time = DateTime.Now;
                timespend = (ed_time - st_time).Ticks / 10000; //1 tick = 100 nanosecond  = 0.0001 毫秒
                IsTimeout = state.is_data_recieve_timeout_;
                if (AccDataBuffer.Count == 0)
                {

                }
                return AccDataBuffer.ToArray();
            }
            catch (Exception exp)
            {
                timespend = -1;
                AccDataBuffer.Clear();
                WaitForBufferRecieveDone.Set();
                IsTimeout = false;
                return new byte[0];
            }
        }

        private void StartTimeoutCheckout(SocketState state)
        {
            Thread timeoutCheckThread = new Thread(TimeoutCheck);
            timeoutCheckThread.Start(state);
        }

        private enum TIMEOUT_CHEK_ITEM
        {
            Read_Acc_Data, FW_Param_RW
        }

        private void TimeoutCheck(object state)
        {
            SocketState _state = (SocketState)state;
            Stopwatch timer = new Stopwatch();
            timer.Start();
            var timeoutPeriod = _state.task_of_now == TIMEOUT_CHEK_ITEM.Read_Acc_Data ? acc_data_rev_timeout : fw_parm_rw_timeout;
            while (_state.is_data_recieve_done_flag_ == false)
            {
                if (timer.ElapsedMilliseconds >= timeoutPeriod) //TODO 外部可調
                {

                    _state.is_data_recieve_timeout_ = true;
                    AccDataBuffer.Clear();
                    if (_state.task_of_now == TIMEOUT_CHEK_ITEM.Read_Acc_Data)
                        WaitForBufferRecieveDone.Set();
                    if (_state.task_of_now == TIMEOUT_CHEK_ITEM.FW_Param_RW)
                    {
                        _state.cancel_token_source_.Cancel();
                    }
                    return;
                }
                //Thread.Sleep(1);
            }
        }

        private ManualResetEvent WaitForBufferRecieveDone;
        private void receiveCallBack(IAsyncResult ar)
        {
            try
            {
                SocketState state = (SocketState)ar.AsyncState;
                var client = state.work_socket_;
                int bytesRead = client.EndReceive(ar);
                if (bytesRead > 0)
                {
                    revBNUm += bytesRead;
                    //Console.WriteLine("Num sum = " + revBNUm);
                    var rev = new byte[bytesRead];
                    Array.Copy(state.buffer_, 0, rev, 0, bytesRead);
                    AccDataBuffer.AddRange(rev);
                    if (AccDataBuffer.Count == state.buffer_size_)
                    {
                        var ed_time = DateTime.Now;
                        var timespend = (ed_time - st_time).Ticks / 10000; //1 tick = 100 nanosecond  = 0.0001 毫秒
                        //Console.WriteLine("No Waitone : " + timespend);
                        //WaitForBufferRecieveDone.Set();
                        state.is_data_recieve_done_flag_ = true;
                        WaitForBufferRecieveDone.Set();
                    }
                    else
                    {
                        state.is_data_recieve_done_flag_ = false;
                        client.BeginReceive(state.buffer_, 0, state.buffer_size_, 0,
                             new AsyncCallback(receiveCallBack), state);
                    }
                }
                else
                {

                }

            }
            catch
            {
                AccDataBuffer.Clear();
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

        private CancellationTokenSource param_setting_task_cancel_token_Source;
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
        private bool bulk_use = false;

        internal class CommandTask
        {
            public byte[] Data;
            public int ExpectReturSize;
            public bool IsAutoStartBulk = false;
        }
        private async Task SendCommandToController(object _state)
        {
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
                Console.WriteLine("Write to Control : " + ObjectAryToString(",", Data));
                module_socket.Send(Data, 0, Data.Length, SocketFlags.None);
                int RecieveByteNum = 0;
                int timespend = 0;
                List<byte> RecievByteList = new List<byte>();
                while (true)
                {
                    if (param_setting_task_cancel_token_Source.IsCancellationRequested == true | RecieveByteNum == ExpectRetrunSize)
                    {
                        break;
                    }
                    try
                    {
                        int avaliable = module_socket.Available;
                        returnData = new byte[avaliable];
                        if (avaliable != 0)
                        {
                            module_socket.Receive(returnData, RecieveByteNum, avaliable, 0);
                            RecieveByteNum += avaliable;
                            RecievByteList.AddRange(returnData);
                        }
                    }
                    catch
                    {
                        RecieveByteNum = 0;
                        Disconnect();
                        Connect();
                        module_socket.Send(Data, 0, Data.Length, SocketFlags.None);
                    }
                    Thread.Sleep(1);
                }
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
