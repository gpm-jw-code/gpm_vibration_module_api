using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gpm_vibration_module_api.GPMBase
{
    /// <summary>
    /// 底層通訊類別
    /// </summary>
    public class AsynchronousClient
    {

        // The port number for the remote device.
        private string IP;
        private string cmd = "";
        private int Port = 5000;
        private int TimeoutDetectEndFlag = 0;
        private bool SyncRevRunning = false;
        private AsyncCallback DataRecieveCallBack = null;
        public Socket client;
        public StateObject StateForAPI { get; private set; } = new StateObject() { };
        private StateObject NoConnectionStateForAPI = new StateObject { ErrorCode = clsErrorCode.Error.NoConnection };
        // ManualResetEvent instances signal completion.
        private ManualResetEvent connectDone = new ManualResetEvent(false);
        private ManualResetEvent sendDone = new ManualResetEvent(false);
        private ManualResetEvent receiveDone = new ManualResetEvent(false);

        internal Action DeviceSocketHanding;
        internal Action Reconnect;
        internal event EventHandler<int> DataPacketLenOnchange;
        private IAsyncResult ar_current;

        /// <summary>
        /// 非同步TCP/IP Socket 連線
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        internal async Task<int> AsyncConnect(string ip, int port)
        {
            IP = ip;
            Port = port;
            connectDone.Reset();
            // Connect to a remote device.
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(ip), port);
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    ReceiveBufferSize = 900000,
                    ReceiveTimeout = 3000,
                    NoDelay = true,
                    Blocking = true
                };
                client.Connect(remoteEP);
                //client.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), client);
                //connectDone.WaitOne();
                return client.Connected ? 0 : (int)clsErrorCode.Error.CONNECT_FAIL;
            }
            catch (Exception ex)
            {
                Tools.Logger.Event_Log.Log($"AsyncConnect exception occurred!{ex.Message}");
                return (int)clsErrorCode.Error.CONNECT_FAIL;
            }
        }
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;
                // Complete the connection.
                client.EndConnect(ar);
                // Signal that the connection has been made.
                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                connectDone.Set();
            }
        }
        /// <summary>
        /// 斷開TCP/IP連線
        /// </summary>
        public void Disconnect()
        {
            var fun_Name = "Disconnect";
            try
            {
                client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(fun_Name + ex.Message);
            }
            try
            {
                client.Shutdown(SocketShutdown.Both);
            }
            catch (Exception ex)
            {
                Console.WriteLine(fun_Name + ex.Message);
            }
        }

        /// <summary>
        /// Send data to device 
        /// </summary>
        /// <param name="Msg">數據(指令)封包</param>
        /// <param name="CheckLen"></param>
        /// <returns></returns>
        internal async Task<StateObject> SendMessage(byte[] Msg, int CheckLen, int Timeout = 5000)
        {
            try
            {
                interuptFlag = 0;
                StateForAPI.ClearBuffer();
                //if (!client.Connected) return NoConnectionStateForAPI;
                StateForAPI.CheckLen = CheckLen;
                SocketBufferClear();
                cmd = Encoding.ASCII.GetString(Msg);
                Tools.Logger.Event_Log.Log($"Send {cmd} to Device.");
                receiveDone.Reset();
                // Send test data to the remote device.
                bool sendOK = Send(client, Msg);
                if (!sendOK)
                {
                    Tools.Logger.Event_Log.Log("SEND FAIL! SOCKET連線未建立 ");
                    return StateForAPI;
                }
                if (Timeout == -1)
                    return StateForAPI;
                sendDone.WaitOne();
                await SyncReceive(client, Timeout);
                //if (Timeout == -1)
                //  TimeoutDetection(Timeout);
                //AsyncReceive(client, CheckLen);
                Tools.Logger.Event_Log.Log("SendMessage FINISH ");
                receiveDone.WaitOne();
                return StateForAPI;
            }
            catch (Exception ex)
            {
                StateForAPI.ErrorCode = clsErrorCode.Error.CONNECT_FAIL;
                Tools.Logger.Code_Error_Log.Log(ex.Message);
                return StateForAPI;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        internal async Task SendMsgAndInterupt(string msg)
        {
            Send(client, msg);
            sendDone.WaitOne();
            if (SyncRevRunning)
                interuptFlag = 1;
        }

        internal async Task<StateObject> SendMessage(string Msg, int CheckLen, int Timeout)
        {
            try
            {
                return await SendMessage(Encoding.ASCII.GetBytes(Msg), CheckLen, Timeout);
            }
            catch (Exception ex)
            {
                Tools.Logger.Code_Error_Log.Log(ex);
                throw ex;
            }

        }
        private Stopwatch SyncTevTimer = new Stopwatch();
        /// <summary>
        /// 同步阻塞方法接收資料
        /// </summary>
        /// <param name="client"></param>
        /// <param name="timeout"></param>
        private async Task SyncReceive(Socket client, int timeout)
        {
            SyncTevTimer.Restart();
            client.ReceiveTimeout = timeout;
            Tools.Logger.Event_Log.Log($"SyncReceive Task start. Data Length recieved should be :{StateForAPI.CheckLen}");
            while (!StateForAPI.IsDataReach)
            {
                SyncRevRunning = true;
                if (interuptFlag == 1)
                {
                    SyncRevRunning = false;
                    interuptFlag = 0;
                    Tools.Logger.Event_Log.Log($"interuptFlag trigger. 同步接收數據中斷");
                    StateForAPI.ErrorCode = clsErrorCode.Error.DATA_GET_INTERUPT;
                    receiveDone.Set();
                    return;
                }
                byte[] buf = new byte[client.Available];
                SocketError errorCode;
                client.Receive(buf, 0, buf.Length, SocketFlags.None, out errorCode);
                if (errorCode != SocketError.Success)
                {
                    StateForAPI.ErrorCode = clsErrorCode.Error.DATA_GET_TIMEOUT;
                    Tools.Logger.Event_Log.Log($"Data Sync Receieve Error Occur(Socket Error Code:{errorCode} || Data Size = {StateForAPI.DataByteList.Count})");
                    receiveDone.Set(); break;
                }
                StateForAPI.DataByteList.AddRange(buf);
                Thread.Sleep(1);
            }
            //client.Send(new byte[11] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, }, 0, 11, SocketFlags.None);
            SyncRevRunning = false;
            Tools.Logger.Event_Log.Log($"Data Sync Receieve Done( Data Size = {StateForAPI.DataByteList.Count})");
            StateForAPI.ErrorCode = StateForAPI.IsDataReach ? clsErrorCode.Error.None : clsErrorCode.Error.DATA_GET_TIMEOUT;
            receiveDone.Set();
        }

        /// <summary>
        /// 非同步方法接收資料
        /// </summary>
        /// <param name="client"></param>
        /// <param name="checkLen"></param>
        private void AsyncReceive(Socket client, int checkLen)
        {
            try
            {
                // Create the state object.
                StateForAPI = new StateObject() { CheckLen = checkLen };
                StateForAPI.workSocket = client;
                Console.WriteLine("Check Length : " + StateForAPI.CheckLen);
                DataRecieveCallBack = DataRecieveCallBack == null ? new AsyncCallback(ReceiveCallback) : DataRecieveCallBack;
                client.BeginReceive(StateForAPI.buffer, 0, StateObject.BufferSize, 0,
                    DataRecieveCallBack, StateForAPI);
            }
            catch (Exception e)
            {
                Tools.Logger.Code_Error_Log.Log(e);
                throw e;
            }
        }



        /// <summary>
        /// 非同步接收回調函數
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveCallback(IAsyncResult ar)
        {
            ar_current = ar;
            try
            {
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;
                // Read data from the remote device.
                int bytesRead = client.EndReceive(ar);
                if (bytesRead > 0)
                {
                    //state.byteRevCnt += bytesRead;
                    Tools.Logger.Event_Log.Log($"[ReceiveCallBack] Recieve:{state.byteRevCnt}/{state.CheckLen}");
                    //從buffer取出
                    byte[] data_slice = new byte[bytesRead];
                    Array.Copy(state.buffer, 0, data_slice, 0, data_slice.Length);
                    state.DataByteList.AddRange(data_slice);
                    // There might be more data, so store the data received so far.
                    if (state.IsDataReach)
                    {
                        Tools.Logger.Event_Log.Log($"Data receieve Done({state.CheckLen})");
                        TimeoutDetectEndFlag = 1;
                        receiveDone.Set();
                        return;
                    }
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                       DataRecieveCallBack, state);
                }
                else
                {
                    // All the data has arrived; put it in response.
                }
            }
            catch (ObjectDisposedException ex)
            {
                //正常的 因為與Client的連線被強制關閉
                Console.WriteLine("Connection Closed forced!");
            }
            catch (Exception e)
            {
                Tools.Logger.Code_Error_Log.Log(e);
            }

        }
        internal const int ParamWriteTimeout = 3000;
        internal int GetDataTimeout = 8000;
        private int interuptFlag;

        /// <summary>
        /// 非同步接收方法 timeout 偵測::送出Cmd後N秒Timeout
        /// </summary>
        /// <param name="timeout">Timeout 時間設定</param>
        /// <returns></returns>
        private async Task TimeoutDetection(int timeout = 10000)//毫秒
        {
            Task.Run(async () =>
             {
                 TimeoutDetectEndFlag = 0;
                 Stopwatch watcher = new Stopwatch();
                 watcher.Start();
                 while (true)
                 {
                     if (watcher.ElapsedMilliseconds > timeout)
                     {
                         StateForAPI = new StateObject()
                         {
                             ErrorCode = cmd == "READVALUE\r\n" ? clsErrorCode.Error.DATA_GET_TIMEOUT :
                                                         clsErrorCode.Error.PARAM_HS_TIMEOUT
                         };
                         Tools.Logger.Event_Log.Log($"(CMD:{cmd}) Data receieve Timeout(timeout設定:{timeout} ms)");
                         receiveDone.Set();
                         return;
                     }
                     if (TimeoutDetectEndFlag == 1)
                     {
                         return;
                     }
                     Thread.Sleep(1);
                 }

             });
        }
        private void Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);
            Send(client, byteData);
        }
        internal bool Send(Socket client, byte[] byteData)
        {
            if (client == null)
            {
                receiveDone.Set();
                sendDone.Set();
                StateForAPI.ErrorCode = clsErrorCode.Error.NoConnection; ;
                return false;
            }
            // Begin sending the data to the remote device.
            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);
            return true;
        }
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;
                int bytesSent = client.EndSend(ar);
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void SocketBufferClear()
        {
            try
            {
                if (client.Available != 0)
                {
                    byte[] buf = new byte[client.Available];
                    client.Receive(buf, buf.Length, SocketFlags.None);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Nothing" + ex.Message);
            }
            finally
            {

                Console.WriteLine("Socket buffer clear");
            }
        }
    }

    // State object for receiving data from remote device.
    public class StateObject
    {
        internal clsErrorCode.Error ErrorCode = clsErrorCode.Error.None;
        // Client socket.
        internal Socket workSocket = null;
        // Size of receive buffer.
        internal const int BufferSize = 256;
        // Receive buffer.
        internal byte[] buffer = new byte[BufferSize];
        // Received data string.
        internal StringBuilder sb = new StringBuilder();
        internal int CheckLen = 8;
        internal int byteRevCnt
        {
            get
            {
                return DataByteList.Count;
            }
        }
        /// <summary>
        /// 儲存模組回傳值
        /// </summary>
        public List<byte> DataByteList = new List<byte>();
        private bool _IsDataReach = false;
        internal bool IsDataReach
        {
            get
            {
                return CheckLen <= byteRevCnt;
            }
            set
            {
                _IsDataReach = value;
            }
        }

        internal void ClearBuffer()
        {
            DataByteList.Clear();
            ErrorCode = clsErrorCode.Error.DATA_GET_TIMEOUT;
        }
    }
}