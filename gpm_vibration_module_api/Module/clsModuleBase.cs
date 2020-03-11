using gpm_vibration_module_api.Tools;
using System;
using System.Collections.Generic;
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
        public Module.clsModuleSettings moduleSettings = new Module.clsModuleSettings();
        private ManualResetEvent pausesignal;
        private bool IsPauseReady = true;
        internal bool IsOldFWUsing = false;
        public clsModuleBase()
        {
            pausesignal = new ManualResetEvent(true);
        }

        public Socket ModuleSocket { get; internal set; }
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
            if (moduleSettings.WifiControllUseHighSppedSensor)
                returnBytes = 8;
            moduleSettings.SensorType = sensorType != null ? (clsEnum.Module_Setting_Enum.SensorType)sensorType : moduleSettings.SensorType;
            moduleSettings.DataLength = dataLength != null ? (clsEnum.Module_Setting_Enum.DataLength)dataLength : moduleSettings.DataLength;
            moduleSettings.MeasureRange = measureRange != null ? (clsEnum.Module_Setting_Enum.MeasureRange)measureRange : moduleSettings.MeasureRange;
            moduleSettings.ODR = oDR != null ? (clsEnum.Module_Setting_Enum.ODR)oDR : moduleSettings.ODR;
            var ParamReturn = WriteParameterToController(moduleSettings.ByteAryOfParameters, returnBytes);
            DefineSettingByParameters(ParamReturn);
            return moduleSettings.ByteAryOfParameters;
        }

        private void DefineSettingByParameters(byte[] Parameters)
        {
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

        }

        internal void TinySensorFWUpdate(List<byte[]> efm8DataFrames)
        {
            try
            {
                SocketBufferClear();
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


        internal event Action<DataSet> DataReady;

        private Thread THBulkProcess = null;
        private int RecieveCnt = 0;
        /// <summary>
        /// 開始巨量資料接收
        /// </summary>
        public void StartGetBulkData(MeasureOption option)
        {
            RecieveCnt = 0;
            SocketBufferClear();

            if (THBulkProcess == null)
            {
                THBulkProcess = new Thread(BulkBufferProcess) { IsBackground = true };
                THBulkProcess.Start();
                BulkState = new SocketState() { buffer = new byte[512], workSocket = ModuleSocket, BufferSize = 512, WindowSize = option.WindowSize };
                ModuleSocket.BeginReceive(BulkState.buffer, 0, BulkState.BufferSize, 0, new AsyncCallback(receiveCallBack_Bulk), BulkState);
            }
            WaitForBufferRecieveDone = new ManualResetEvent(true);
            var cmdbytes = Encoding.ASCII.GetBytes(clsEnum.ControllerCommand.BULKVALUE + "\r\n");
            ModuleSocket.Send(cmdbytes, 0, cmdbytes.Length, SocketFlags.None);
        }

        internal void BulkBreak()
        {
            var cmd = Encoding.ASCII.GetBytes("BULKBREAK\r\n");
            ModuleSocket.Send(cmd, 0, cmd.Length, 0);
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
            try
            {
                BulkState.AR = ar;
                SocketState state = (SocketState)ar.AsyncState;
                var client = state.workSocket;
                int bytesRead = client.EndReceive(ar);
                if (bytesRead > 0)
                {
                    var rev = new byte[bytesRead];
                    Array.Copy(state.buffer, 0, rev, 0, bytesRead);
                    Bulk_Buffer.AddRange(rev);
                }
                state.buffer = new byte[512];
                client.BeginReceive(state.buffer, 0, state.BufferSize, 0, (receiveCallBack_Bulk), state);
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp.Message);
            }
            //WaitForBufferRecieveDone.Set();
        }

        private void BulkBufferProcess()
        {
            while (true)
            {
                var condition = BulkState.WindowSize * 6;
                if (Bulk_Buffer.Count >= condition)
                {
                    RecieveCnt++;
                    Console.WriteLine(DateTime.Now + " Buffer ~~~~~~~~~ 有了");
                    Console.WriteLine(DateTime.Now + " " + RecieveCnt);
                    try
                    {
                        var startIndex = 0;
                        //for (startIndex = 0; true; startIndex++)
                        //{
                        //    if (Bulk_Buffer[startIndex] == 13 && Bulk_Buffer[startIndex + 1] == 10)
                        //    {
                        //        break;
                        //    }
                        //}
                        //startIndex = startIndex == 6 ? 0 : startIndex + 2;

                        byte[] rev = new byte[condition];
                        Array.Copy(Bulk_Buffer.ToArray(), startIndex, rev, 0, rev.Length);
                        Bulk_Buffer.RemoveRange(0, condition + startIndex);
                        var doubleOutput = BytesToDoubleList(rev, false);
                        var dataset = new DataSet();
                        dataset.AccData.X = (doubleOutput[0]);
                        dataset.AccData.Y = (doubleOutput[1]);
                        dataset.AccData.Z = (doubleOutput[2]);
                        DataReady?.BeginInvoke(dataset, null, null);
                    }
                    catch (Exception exp)
                    {
                        continue;
                    }

                }
                else
                {
                    //Console.WriteLine(DateTime.Now + " Buffer 空了");
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
        internal byte[] GetAccData_HighSpeedWay(out long timespend)
        {
            try
            {
                revBNUm = 0;
                WaitForBufferRecieveDone = new ManualResetEvent(false);
                AccDataBuffer.Clear();
                SocketBufferClear();
                var cmdbytes = Encoding.ASCII.GetBytes(clsEnum.ControllerCommand.READVALUE + "\r\n");

                ModuleSocket.Send(cmdbytes, 0, cmdbytes.Length, SocketFlags.None);
                var s1 = DateTime.Now;
                while (ModuleSocket.Available < 3072)
                {
                    Thread.Sleep(1);

                }
                Console.WriteLine(ModuleSocket.Available);
                Console.WriteLine((DateTime.Now - s1).TotalMilliseconds);
                var Datalength = Convert.ToInt32(moduleSettings.DataLength) * 6;
                byte[] Datas = new byte[Datalength];
                st_time = DateTime.Now;
                SocketState state = new SocketState() { buffer = new byte[Datalength], workSocket = ModuleSocket, BufferSize = Datalength };
                Thread thmonitorBuffer = new Thread(monitorBuffer);
                thmonitorBuffer.Start(ModuleSocket);
                ModuleSocket.BeginReceive(state.buffer, 0, state.BufferSize, 0, new AsyncCallback(receiveCallBack), state);
                WaitForBufferRecieveDone.WaitOne();
                var ed_time = DateTime.Now;
                timespend = (ed_time - st_time).Ticks / 10000; //1 tick = 100 nanosecond  = 0.0001 毫秒
                Console.WriteLine("Waitone : " + timespend);
                return AccDataBuffer.ToArray();
            }
            catch (Exception exp)
            {
                timespend = -1;
                AccDataBuffer.Clear();
                WaitForBufferRecieveDone.Set();
                return new byte[0];
            }
        }

        private void monitorBuffer(object skobj)
        {
            Socket s = ModuleSocket;
            Console.WriteLine(s.Available + " aaa1aa23");
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
                    Console.WriteLine("Num sum = " + revBNUm);
                    var rev = new byte[bytesRead];
                    Array.Copy(state.buffer, 0, rev, 0, bytesRead);
                    AccDataBuffer.AddRange(rev);
                    if (AccDataBuffer.Count == state.BufferSize)
                    {
                        var ed_time = DateTime.Now;
                        var timespend = (ed_time - st_time).Ticks / 10000; //1 tick = 100 nanosecond  = 0.0001 毫秒
                        Console.WriteLine("No Waitone : " + timespend);
                        //WaitForBufferRecieveDone.Set();
                    }
                    else
                        client.BeginReceive(state.buffer, 0, state.BufferSize, 0,
                            new AsyncCallback(receiveCallBack), state);
                }
                else
                {

                }
                WaitForBufferRecieveDone.Set();
            }
            catch
            {
                AccDataBuffer.Clear();
                WaitForBufferRecieveDone.Set();
            }
            //            AccDataBuffer.AddRange(state.buffer);
        }

        /// <summary>
        /// 發送任意bytes給控制器
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="ExpectRetrunSize"></param>
        /// <returns></returns>
        internal byte[] SendCommand(byte[] Data, int ExpectRetrunSize)
        {
            byte[] returnData = new byte[ExpectRetrunSize];
            try
            {
                SocketBufferClear();
                ModuleSocket.Send(Data, 0, Data.Length, SocketFlags.None);
                int RecieveByteNum = 0;
                int timespend = 0;
                while (RecieveByteNum < ExpectRetrunSize)
                {
                    timespend++;
                    if (timespend > 5000)
                        return new byte[0];
                    int avaliable = ModuleSocket.Available;
                    //returnData = new byte[avaliable];
                    // ModuleSocket.Receive(returnData, RecieveByteNum, ExpectRetrunSize, 0);
                    ModuleSocket.Receive(returnData, RecieveByteNum, avaliable, 0);
                    RecieveByteNum += avaliable;
                }
                return returnData;
            }
            catch (SocketException exp)
            {
                return returnData;
            }
        }


        /// <summary>
        /// 清空socket buffer
        /// </summary>
        private void SocketBufferClear()
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


    }
}
