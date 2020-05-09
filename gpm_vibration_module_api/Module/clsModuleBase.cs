using gpm_vibration_module_api.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace gpm_vibration_module_api
{

    /// <summary>
    /// 振動感測模組底層控制類別
    /// </summary>
    internal class clsModuleBase
    {

        internal int timeout = 8000; //unit: ms
        public Module.clsModuleSettings moduleSettings = new Module.clsModuleSettings();
        private ManualResetEvent pausesignal;
        private bool IsPauseReady = true;
        internal bool IsOldFWUsing = false;
        public double SamplingRate { get; internal set; } = 1000.0;
        public clsModuleBase()
        {
            pausesignal = new ManualResetEvent(true);
            BulkRequestPause = new ManualResetEvent(true);
            WaitForDataGet = new ManualResetEvent(true);
        }

        public Socket ModuleSocket { get; internal set; }
        public bool IsBulkBreak { get; private set; } = true;

        /// <summary>
        /// 連線
        /// </summary>
        public int Connect(string ModuleIP, int ModulePort)
        {
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(ModuleIP), ModulePort);
                ModuleSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                ModuleSocket.ReceiveBufferSize = 8192;
                ModuleSocket.ReceiveTimeout = 10000;
                ModuleSocket.Connect(remoteEP);
                if (ModuleSocket.Connected)
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
                SendBulkBreakCmd();
                ModuleSocket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception)
            {
            }
            try
            {
                ModuleSocket.Close();
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
        public byte[] WriteParameterToController(byte[] Parameters, int returnBytes)
        {
            SocketBufferClear();
            byte[] ToWrite = new byte[11];
            ToWrite[0] = 0x53;
            ToWrite[9] = 0x0d;
            ToWrite[10] = 0x0a;
            Array.Copy(Parameters, 0, ToWrite, 1, Parameters.Length);
            return SendCommand(ToWrite, returnBytes);
        }
        /// <summary>
        /// 寫入參數至控制器(function)
        /// </summary>
        /// <param name="sensorType"></param>
        /// <param name="dataLength"></param>
        /// <param name="measureRange"></param>
        /// <param name="oDR"></param>
        /// <returns></returns>
        public byte[] WriteParameterToController(clsEnum.Module_Setting_Enum.SensorType? sensorType,
            clsEnum.Module_Setting_Enum.DataLength? dataLength,
            clsEnum.Module_Setting_Enum.MeasureRange? measureRange,
            clsEnum.Module_Setting_Enum.ODR? oDR)
        {
            var returnBytes = moduleSettings.SensorType == clsEnum.Module_Setting_Enum.SensorType.High | sensorType == clsEnum.Module_Setting_Enum.SensorType.High ? 8 : 8;
            //if (moduleSettings.WifiControllUseHighSppedSensor)
            //    returnBytes = 8;
            moduleSettings.SensorType = sensorType != null ? (clsEnum.Module_Setting_Enum.SensorType)sensorType : moduleSettings.SensorType;
            moduleSettings.DataLength = dataLength != null ? (clsEnum.Module_Setting_Enum.DataLength)dataLength : moduleSettings.DataLength;
            moduleSettings.MeasureRange = measureRange != null ? (clsEnum.Module_Setting_Enum.MeasureRange)measureRange : moduleSettings.MeasureRange;
            moduleSettings.ODR = oDR != null ? (clsEnum.Module_Setting_Enum.ODR)oDR : moduleSettings.ODR;
            var ParamReturn = WriteParameterToController(moduleSettings.ByteAryOfParameters, returnBytes);
            DefineSettingByParameters(ParamReturn);
            return moduleSettings.ByteAryOfParameters;
        }

        private string ObjectAryToString(string split, byte[] obj)
        {
            var _s = "";

            foreach (var item in obj)
            {
                _s += item + split;
            }
            _s.Remove(_s.Length - 1);
            return _s;
        }

        private void DefineSettingByParameters(byte[] Parameters)
        {
            if (Parameters.Length == 0)
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

            switch (TypeByte)
            {
                case 0x01:
                    moduleSettings.SensorType = clsEnum.Module_Setting_Enum.SensorType.Genernal;
                    break;
                case 0x02:
                    moduleSettings.SensorType = clsEnum.Module_Setting_Enum.SensorType.High;
                    break;
                default:
                    moduleSettings.SensorType = clsEnum.Module_Setting_Enum.SensorType.Genernal;
                    break;
            }

            switch (DataLengthByte)
            {
                case 0x00:
                    moduleSettings.DataLength = clsEnum.Module_Setting_Enum.DataLength.x1;
                    break;
                case 0x01:
                    moduleSettings.DataLength = clsEnum.Module_Setting_Enum.DataLength.x2;
                    break;
                case 0x02:
                    moduleSettings.DataLength = clsEnum.Module_Setting_Enum.DataLength.x4;
                    break;
                case 0x03:
                    moduleSettings.DataLength = clsEnum.Module_Setting_Enum.DataLength.x8;
                    break;
                default:
                    moduleSettings.DataLength = clsEnum.Module_Setting_Enum.DataLength.x1;
                    break;
            }

            switch (ODRByte)
            {
                case 0x9F:
                    moduleSettings.ODR = clsEnum.Module_Setting_Enum.ODR._9F;
                    break;
                case 0x87:
                    moduleSettings.ODR = clsEnum.Module_Setting_Enum.ODR._87;
                    break;
                default:
                    moduleSettings.ODR = clsEnum.Module_Setting_Enum.ODR._9F;
                    break;
            }

            switch (MeasureRangeByte)
            {
                case 0x00:
                    moduleSettings.MeasureRange = clsEnum.Module_Setting_Enum.MeasureRange.MR_2G;
                    break;
                case 0x10:
                    moduleSettings.MeasureRange = clsEnum.Module_Setting_Enum.MeasureRange.MR_4G;
                    break;
                case 0x20:
                    moduleSettings.MeasureRange = clsEnum.Module_Setting_Enum.MeasureRange.MR_8G;
                    break;
                case 0x30:
                    moduleSettings.MeasureRange = clsEnum.Module_Setting_Enum.MeasureRange.MR_16G;
                    break;
                default:
                    moduleSettings.MeasureRange = clsEnum.Module_Setting_Enum.MeasureRange.MR_2G;
                    break;
            }
        }
        private void WaitPause()
        {
            pausesignal.Reset();
            while (IsPauseReady == false)
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
            public Socket workSocket = null;
            public int BufferSize = 256;
            public byte[] buffer;
            public int WindowSize = 512;
            public StringBuilder sb = new StringBuilder();
            public IAsyncResult AR;
            public bool IsDoneFlag = false;
            public bool IsTimeout = false;
        }

        internal void TinySensorFWUpdate(List<byte[]> efm8DataFrames)
        {
            try
            {
                SocketBufferClear();
                ModuleSocket.Send(Encoding.ASCII.GetBytes("INTOBMODE\r\n"));

                foreach (var item in efm8DataFrames)
                {
                    ModuleSocket.Send(item, 0, item.Length, SocketFlags.None);
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
        public void StartGetBulkData(MeasureOption option)
        {
            userOption = option;
            SendBulkBreakCmd();
            SocketBufferClear();
            if (THBulkProcess == null)
            {
                THBulkProcess = new Thread(BulkBufferProcess) { IsBackground = true };
                THBulkProcess.Start();
                BulkState = new SocketState() { buffer = new byte[512], workSocket = ModuleSocket, BufferSize = 512, WindowSize = option.WindowSize };
                ModuleSocket.BeginReceive(BulkState.buffer, 0, BulkState.BufferSize, 0, new AsyncCallback(receiveCallBack_Bulk), BulkState);
            }
            Bulk_Buffer.Clear();
            SendBulkDataStartCmd();
        }
        internal void SendBulkDataStartCmd()
        {
            try
            {
                //BulkBreak();
                var cmdbytes = Encoding.ASCII.GetBytes(clsEnum.ControllerCommand.BULKVALUE + "\r\n");
                ModuleSocket.Send(cmdbytes, 0, cmdbytes.Length, SocketFlags.None);
            }
            catch (Exception exp)
            {
                // Console.WriteLine("[SendBulkDataStartCmd()] " + exp.Message);
            }
            BulkRequestPause.Set();

        }

        private void SendBulkBreakCmd()
        {
            BulkRequestPause.Reset();
            var cmdbytes = Encoding.ASCII.GetBytes(clsEnum.ControllerCommand.BULKBREAK + "\r\n");
            try
            {
                ModuleSocket.Send(cmdbytes, 0, cmdbytes.Length, SocketFlags.None);
            }
            catch (Exception exp)
            {
                Console.WriteLine("[SendBulkDataStartCmd()] " + exp.Message);
            }
        }

        internal void BulkBreak()
        {
            BulkRequestPause.Reset();
            Thread.Sleep(500);

            //IsBulkBreak = true;
            while (!IsBulkBreak)
            {
                Thread.Sleep(1);
            }

            while (ModuleSocket.Available != 0)
            {
                SocketBufferClear();
                SendBulkBreakCmd();
                Thread.Sleep(100);
            }
            Bulk_Buffer.Clear();
            BulkState.buffer = new byte[512];
            try
            {
                ModuleSocket.EndReceive(BulkState.AR);
            }
            catch
            {

            }
            IsBulkBreak = true;

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
            BulkState.AR = ar;
            BulkRequestPause.WaitOne();
            var client = BulkState.workSocket;
            try
            {

                int bytesRead = client.EndReceive(ar);
                if (bytesRead > 0)
                {
                    var rev = new byte[bytesRead];
                    Array.Copy(BulkState.buffer, 0, rev, 0, bytesRead);
                    Bulk_Buffer.AddRange(rev);
                }
                else
                {

                }

                BulkState.buffer = new byte[512];

            }
            catch (Exception exp)
            {
                Console.WriteLine("[receiveCallBack_Bulk] " + exp.Message);
            }
            try
            {
                client.BeginReceive(BulkState.buffer, 0, BulkState.BufferSize, 0, new AsyncCallback(receiveCallBack_Bulk), BulkState);
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
                IsBulkBreak = true;
                BulkRequestPause.WaitOne();
                IsBulkBreak = false;
                var condition = BulkState.WindowSize * 6;
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
                        var dataset = new DataSet(SamplingRate);
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
            var LSB = Convert.ToInt32(moduleSettings.MeasureRange);
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
                    x.Add(ConverterTools.bytesToDouble(buffer[(multiple * i) + startIndex + 0], buffer[(multiple * i) + startIndex + 1]) / LSB);
                    y.Add(ConverterTools.bytesToDouble(buffer[(multiple * i) + startIndex + 2], buffer[(multiple * i) + startIndex + 3]) / LSB);
                    z.Add(ConverterTools.bytesToDouble(buffer[(multiple * i) + startIndex + 4], buffer[(multiple * i) + startIndex + 5]) / LSB);
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
            try
            {
                revBNUm = 0;
                WaitForBufferRecieveDone = new ManualResetEvent(false);
                AccDataBuffer.Clear();
                SocketBufferClear();
                var cmdbytes = Encoding.ASCII.GetBytes(clsEnum.ControllerCommand.READVALUE + "\r\n");
                ModuleSocket.Send(cmdbytes, 0, cmdbytes.Length, SocketFlags.None);
                var Datalength = Convert.ToInt32(moduleSettings.DataLength) * 6;
                byte[] Datas = new byte[Datalength];
                st_time = DateTime.Now;
                SocketState state = new SocketState() { buffer = new byte[Datalength], workSocket = ModuleSocket, BufferSize = Datalength };
                StartTimeoutCheckout(state);
                ModuleSocket.BeginReceive(state.buffer, 0, state.BufferSize, 0, new AsyncCallback(receiveCallBack), state);
                WaitForBufferRecieveDone.WaitOne();
                var ed_time = DateTime.Now;
                timespend = (ed_time - st_time).Ticks / 10000; //1 tick = 100 nanosecond  = 0.0001 毫秒
                IsTimeout = state.IsTimeout;
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

        private void TimeoutCheck(object state)
        {
            SocketState _state = (SocketState)state;
            Stopwatch timer = new Stopwatch();
            timer.Start();
            while (_state.IsDoneFlag == false)
            {
                if (timer.ElapsedMilliseconds >= timeout) //TODO 外部可調
                {
                    _state.IsTimeout = true;
                    AccDataBuffer.Clear();
                    WaitForBufferRecieveDone.Set();
                    return;
                }
                Thread.Sleep(1);
            }
        }

        private ManualResetEvent WaitForBufferRecieveDone;
        private void receiveCallBack(IAsyncResult ar)
        {
            try
            {
                SocketState state = (SocketState)ar.AsyncState;
                var client = state.workSocket;
                int bytesRead = client.EndReceive(ar);
                if (bytesRead > 0)
                {
                    revBNUm += bytesRead;
                    //Console.WriteLine("Num sum = " + revBNUm);
                    var rev = new byte[bytesRead];
                    Array.Copy(state.buffer, 0, rev, 0, bytesRead);
                    AccDataBuffer.AddRange(rev);
                    if (AccDataBuffer.Count == state.BufferSize)
                    {
                        var ed_time = DateTime.Now;
                        var timespend = (ed_time - st_time).Ticks / 10000; //1 tick = 100 nanosecond  = 0.0001 毫秒
                        //Console.WriteLine("No Waitone : " + timespend);
                        //WaitForBufferRecieveDone.Set();
                        state.IsDoneFlag = true;
                        WaitForBufferRecieveDone.Set();
                    }
                    else
                    {
                        state.IsDoneFlag = false;
                        client.BeginReceive(state.buffer, 0, state.BufferSize, 0,
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

        private ManualResetEvent BulkRequestPause;
        private ManualResetEvent WaitForDataGet;
        internal CommandTask SendCmdTaskObj = new CommandTask();
        /// <summary>
        /// 發送任意bytes給控制器
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="ExpectRetrunSize"></param>
        /// <returns></returns>
        internal byte[] SendCommand(byte[] Data, int ExpectRetrunSize)
        {
            SendCmdTaskObj.Data = Data;
            SendCmdTaskObj.ExpectReturSize = ExpectRetrunSize;
            WaitForDataGet.Reset();
            Thread th = new Thread(SendCommandThread);
            th.Start();
            WaitForDataGet.WaitOne();
            if (SendCmdTaskObj.IsAutoStartBulk)
            {
                userOption = new MeasureOption() { WindowSize = 512 };
                StartGetBulkData(option: userOption);
            }
            return DataFromSensor;

            //BulkBreak();
            //byte[] returnData = null;
            //try
            //{
            //    SocketBufferClear();
            //    while (ModuleSocket.Available != 0)
            //    {
            //        SocketBufferClear();
            //    }
            //    ModuleSocket.Send(Data, 0, Data.Length, SocketFlags.None);
            //    int RecieveByteNum = 0;
            //    int timespend = 0;
            //    while (RecieveByteNum < ExpectRetrunSize)
            //    {
            //        try
            //        {
            //            timespend++;
            //            if (timespend > 5000)
            //                return new byte[0];
            //            int avaliable = ModuleSocket.Available;
            //            returnData = new byte[avaliable];
            //            //returnData = new byte[avaliable];
            //            // ModuleSocket.Receive(returnData, RecieveByteNum, ExpectRetrunSize, 0);
            //            if (avaliable != 0)
            //            {
            //                ModuleSocket.Receive(returnData, RecieveByteNum, avaliable, 0);
            //                RecieveByteNum += avaliable;
            //            }
            //        }
            //        catch
            //        {

            //        }
            //        Thread.Sleep(1);
            //    }
            //    return returnData;
            //}
            //catch (SocketException exp)
            //{
            //    return returnData;
            //}
        }
        private byte[] DataFromSensor;
        internal class CommandTask
        {
            public byte[] Data;
            public int ExpectReturSize;
            public bool IsAutoStartBulk = false;
        }
        private void SendCommandThread()
        {
            byte[] Data = SendCmdTaskObj.Data;
            int ExpectRetrunSize = SendCmdTaskObj.ExpectReturSize;
            BulkBreak();
            byte[] returnData = null;
            try
            {
                SocketBufferClear();
                while (ModuleSocket.Available != 0)
                {
                    SocketBufferClear();
                }
                Console.WriteLine("Write to Control : " + ObjectAryToString(",", Data));
                ModuleSocket.Send(Data, 0, Data.Length, SocketFlags.None);
                int RecieveByteNum = 0;
                int timespend = 0;
                while (RecieveByteNum < ExpectRetrunSize)
                {
                    try
                    {
                        timespend++;
                        if (timespend > 5000)
                        {
                            DataFromSensor = new byte[0];
                            WaitForDataGet.Set();
                            return;
                        }
                        int avaliable = ModuleSocket.Available;
                        returnData = new byte[avaliable];
                        //returnData = new byte[avaliable];
                        // ModuleSocket.Receive(returnData, RecieveByteNum, ExpectRetrunSize, 0);
                        if (avaliable != 0)
                        {
                            ModuleSocket.Receive(returnData, RecieveByteNum, avaliable, 0);
                            RecieveByteNum += avaliable;
                        }
                    }
                    catch
                    {

                    }
                    Thread.Sleep(1);
                }
                DataFromSensor = returnData;
            }
            catch (SocketException exp)
            {
                DataFromSensor = returnData;
            }
            WaitForDataGet.Set();
        }


        /// <summary>
        /// 清空socket buffer
        /// </summary>
        internal void SocketBufferClear()
        {
            try
            {
                if (ModuleSocket == null)
                    return;
                if (ModuleSocket.Available != 0)
                {
                    var size = ModuleSocket.Available;
                    byte[] buffer = new byte[size];
                    ModuleSocket.Receive(buffer, 0, size, SocketFlags.None);
                }
            }
            catch
            {

            }
        }

        internal double GetUV()
        {
            var bytesRetrumn = SendCommand(Encoding.ASCII.GetBytes("READVALUE\r\n"), 4);
            if (bytesRetrumn.Length == 0)
                return -1; //Timeout
            return ConvertToUVSensingValue(bytesRetrumn);
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
