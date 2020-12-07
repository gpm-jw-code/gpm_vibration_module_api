using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gpm_module_api.Tools
{
    public class FirmwareBurning
    {

        /// <summary>
        /// 接收LOG事件
        /// </summary>
        public static event Action<ProcessState> BurningMsgRecieve;
        public ProcessState BurningProcssState { get; private set; } = new ProcessState();

        public class ProcessState
        {
            public DateTime time;
            public string Message = "";
            public STEP Process_Step;
            public enum STEP
            {
                START,
                FirmwareLoad,
                Finish,
                FirmwareLoadFinish,
                TryConnectToController,
                ConnectToControllerSucess,
                SerialPortOpenSucess,
                TryINTOBMODE,
                INTOBMODE_SUCESS,
                StartWriteInFirmwareData,
                TryOpenSerialPort,
                RecieveSensorReply
            }
            public RESULT result;

            public List<string> ListReply = new List<string>();
        }
        public class PARAM
        {
            public enum POTOCOL
            {
                TTL, SOCKET
            }
            public POTOCOL potocol = POTOCOL.SOCKET;
            public string ContollerIP = "192.168.0.3";
            public int ContollerPort = 5000;
            public string FirmwareFilePath;
            public int IntoBurningModeTimeout = 5000;
            public int FrameBurningTimeout = 3000;
            public Module.TCPIPModule.SocketTCPIP Socket;
            public SerialPort serialPort;
            public string PortName = "COM3";
            public int PortBaudRate = 115200;
            public bool IsNeedToWriteINTOBMODE = true;
        }

        public struct RESULT
        {
            public enum FAIL_REASON
            {
                NORMAL,
                FirmwareFileFormatIncorrect,
                SocketConnectFail,
                InToBurnModeFail,
                FirmwareDataWriteInTimeout,
                PotocolNoSelect,
                SerialPortOpenFail,
                RelyContainsErr
            }
            public bool IsOK;
            public FAIL_REASON FailReason;
        }

        private static ManualResetEvent WaitSignal;
        private static PARAM param;


        private static void Log(ProcessState state)
        {
            state.time = DateTime.Now;
            Console.WriteLine(state.Process_Step);
            Console.WriteLine("Result => " + state.result.IsOK);

            BurningMsgRecieve?.Invoke(state);
        }

        public static async Task<RESULT> ExecuteBurningProcess(PARAM parameters)
        {
            RESULT _result = new RESULT { IsOK = true };
            ProcessState processState = new ProcessState() { result = _result, Process_Step = ProcessState.STEP.START };
            Thread.Sleep(1000);
            Log(processState);
            param = parameters;
            WaitSignal = new ManualResetEvent(false);

            //嘗試獲取韌體文本
            processState.Message = "";
            processState.Process_Step = ProcessState.STEP.FirmwareLoad;
            Log(processState);
            var firmwareData = FirmwareFileConvertToBytesData(param.FirmwareFilePath);



            if (firmwareData.Count == 0)
            {
                _result = new RESULT { IsOK = false, FailReason = RESULT.FAIL_REASON.FirmwareFileFormatIncorrect };
                Log(new ProcessState { result = _result, Process_Step = ProcessState.STEP.Finish, Message = "NG" });
                return _result;
            }
            processState.Process_Step = ProcessState.STEP.FirmwareLoadFinish;
            processState.Message = "OK";

            Log(processState); Thread.Sleep(1000);
            //嘗試與控制器進行Connect 
            if (param.potocol == PARAM.POTOCOL.SOCKET)
            {
                processState.Process_Step = ProcessState.STEP.TryConnectToController;
                Log(processState); Thread.Sleep(1000);
                Module.TCPIPModule.SocketTCPIP socketTCPIP = new Module.TCPIPModule.SocketTCPIP(param.ContollerIP, param.ContollerPort);
                socketTCPIP.recieveEvent += SocketTCPIP_recieveEvent;
                var ret = socketTCPIP.Connect();
                if (ret.Item1 == false)
                {
                    _result = new RESULT { IsOK = false, FailReason = RESULT.FAIL_REASON.SocketConnectFail };
                    Log(new ProcessState { result = _result, Process_Step = ProcessState.STEP.Finish });
                    return _result;
                }
                param.Socket = socketTCPIP;
                processState.Process_Step = ProcessState.STEP.ConnectToControllerSucess;
                Log(processState); Thread.Sleep(1000);
            }
            else
            {
                processState.Message = "";
                processState.Process_Step = ProcessState.STEP.TryOpenSerialPort;
                Log(processState); Thread.Sleep(1000);
                param.serialPort = OpenPort(param.PortName, param.PortBaudRate);
                if (param.serialPort == null)
                {
                    _result = new RESULT { IsOK = false, FailReason = RESULT.FAIL_REASON.SerialPortOpenFail };
                    Log(new ProcessState { result = _result, Process_Step = ProcessState.STEP.Finish, Message = "Fail!" });
                    return _result;
                }
                processState.Message = "OK";
                processState.Process_Step = ProcessState.STEP.SerialPortOpenSucess;
                Log(processState); Thread.Sleep(1000);
                param.serialPort.DataReceived += SerialPort_DataReceived;
            }

            if (param.IsNeedToWriteINTOBMODE == true)
            {
                //嘗試進入燒機模式
                processState.Message = "";
                processState.Process_Step = ProcessState.STEP.TryINTOBMODE;
                Log(processState); Thread.Sleep(1000);
                var IsIntoBurningMode = await IntoBurningMode();
                if (IsIntoBurningMode == false)
                {
                    _result = new RESULT { IsOK = false, FailReason = RESULT.FAIL_REASON.InToBurnModeFail };
                    Log(new ProcessState { result = _result, Process_Step = ProcessState.STEP.Finish });
                    return _result;
                }
                processState.Process_Step = ProcessState.STEP.INTOBMODE_SUCESS;
                Log(processState); Thread.Sleep(1000);
            }
            processState.Process_Step = ProcessState.STEP.StartWriteInFirmwareData;
            FirmwareWriteInState = new ProcessState() { Process_Step = ProcessState.STEP.RecieveSensorReply };
            Log(processState); Thread.Sleep(1000);
            var result = await FirmwareDataWriteIn(firmwareData);
            CloseConnection();
            _result = new RESULT { IsOK = result.IsOK, FailReason = result.FailReason };
            processState.result = _result;
            processState.Process_Step = ProcessState.STEP.Finish;
            Log(processState); Thread.Sleep(1000);
            return _result;
        }

        private static void CloseConnection()
        {
            if (param.potocol == PARAM.POTOCOL.SOCKET)
            {
                param.Socket.Close();
            }
            else
                param.serialPort.Close();
        }
        private static ProcessState FirmwareWriteInState = new ProcessState() { Process_Step = ProcessState.STEP.RecieveSensorReply };
        private static void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var port = sender as SerialPort;
            var byteAry = Encoding.ASCII.GetBytes(port.ReadExisting());
            var Msg = Encoding.ASCII.GetString(byteAry);
            Console.WriteLine(FirmwareWriteInState.Message);
            FirmwareWriteInState.Message += Msg + ",";
            Log(FirmwareWriteInState);
            WaitSignal.Set();
            if (Msg.Contains("OK") | Msg.Contains("@") | Msg.Contains("?"))
            {
                FirmwareWriteInState.ListReply.Add(Msg);
                TimeoutBreakFlag = 1;
            }

        }

        private static void SocketTCPIP_recieveEvent(byte[] obj)
        {
            var Msg = Encoding.ASCII.GetString(obj).ToUpper();
            FirmwareWriteInState.Message += Msg + ",";
            Console.WriteLine(FirmwareWriteInState.Message);
            Log(FirmwareWriteInState);
            WaitSignal.Set();
            if (Msg.Contains("OK") | Msg.Contains("@") | Msg.Contains("?"))
            {
                FirmwareWriteInState.ListReply.Add(Msg);
                TimeoutBreakFlag = 1;
            }
        }
        private static int TimeoutBreakFlag = 0;
        private static async Task<bool> TimeoutCheck(int Timeout)
        {
            TimeoutBreakFlag = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (sw.ElapsedMilliseconds < Timeout)
            {
                if (TimeoutBreakFlag == 1)
                {
                    sw.Stop();
                    return true;
                }
                Thread.Sleep(1);
            }
            sw.Stop();
            return false;
        }
        public static async Task<bool> IntoBurningMode()
        {
            if (param.potocol == PARAM.POTOCOL.SOCKET)
            {
                param.Socket.Send(Encoding.ASCII.GetBytes("INTOBMODE\r\n"));
                var ret = await TimeoutCheck(param.IntoBurningModeTimeout);
                return ret;
            }
            else
            {
                param.serialPort.Write("INTOBMODE\r\n");
                var ret = await TimeoutCheck(param.IntoBurningModeTimeout);
                param.serialPort.Close();
                return ret;
            }
        }



        public static async Task<RESULT> FirmwareDataWriteIn(List<byte[]> FirmwareData)
        {
            if (param.potocol == PARAM.POTOCOL.TTL)
            {
                param.serialPort = OpenPort(param.PortName, 115200); //寫韌體時固定115200
                if (param.serialPort == null) return new RESULT { IsOK = false, FailReason = RESULT.FAIL_REASON.SerialPortOpenFail };
                //port.write(b'\xff\xff')
                param.serialPort.Write(new byte[] { 0xFF, 0xFF }, 0, 2);
                //var ret = await TimeoutCheck(param.FrameBurningTimeout);
                //if (ret == false)
                //    return new RESULT { IsOK = false, FailReason = RESULT.FAIL_REASON.FirmwareDataWriteInTimeout };
            }
            else
            {

            }


            foreach (var frame in FirmwareData)
            {
                if (param.potocol == PARAM.POTOCOL.SOCKET)
                    param.Socket.Send(frame);
                else
                    param.serialPort.Write(frame, 0, frame.Count());
                var ret = await TimeoutCheck(param.FrameBurningTimeout);
                if (ret == false)
                    return new RESULT { IsOK = false, FailReason = RESULT.FAIL_REASON.FirmwareDataWriteInTimeout };
            }
            var NumOfAT = FirmwareWriteInState.ListReply.FindAll(ch => ch == "@").Count;
            var IsAllReplyAT = (NumOfAT == FirmwareData.Count);
            return new RESULT { IsOK = IsAllReplyAT, FailReason = IsAllReplyAT ? RESULT.FAIL_REASON.NORMAL : RESULT.FAIL_REASON.RelyContainsErr };
        }


        public static List<byte[]> FirmwareFileConvertToBytesData(string FirmwareFilePath)
        {
            if (File.Exists(FirmwareFilePath) == false)
                return new List<byte[]>();
            List<byte[]> ToBurningBytesList = new List<byte[]>();
            byte[] FileBytes = File.ReadAllBytes(FirmwareFilePath);
            StringBuilder sb = new StringBuilder();
            if (FileBytes[0] != 36)
            {
                return new List<byte[]>();
            }

            for (int i = 0; i < FileBytes.Length; i++)
            {
                if (FileBytes[i] == 36) //head[0]
                {
                    var datalen = FileBytes[i + 1]; //head[1] : len
                    var cmd = FileBytes[i + 2];  //head[2] : 指令
                    if (cmd != 48 && cmd != 49 && cmd != 50 && cmd != 51 && cmd != 52 && cmd != 53 && cmd != 54)
                    {
                        continue;
                    }
                    byte[] frame = new byte[datalen + 2];
                    Array.Copy(FileBytes, i, frame, 0, frame.Length);

                    if (frame.Length - 2 == datalen && datalen <= 131)
                    {
                        i = i + frame.Length - 1;
                        ToBurningBytesList.Add(frame);
                    }
                }
            }
            return ToBurningBytesList;
        }


        public static SerialPort OpenPort(string portname, int baudrate)
        {
            SerialPort port;
            port = new SerialPort()
            {
                BaudRate = baudrate,
                PortName = portname,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                ReadTimeout = 10000,
                DiscardNull = true

            };
            try
            {
                port.Open();
                return port;
            }
            catch (Exception ex)
            {
                return null;
            }


        }
    }
}
