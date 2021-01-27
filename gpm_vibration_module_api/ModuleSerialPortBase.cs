using gpm_vibration_module_api.GPMBase;
using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace gpm_vibration_module_api
{
    internal class ModuleSerialPortBase
    {

        internal SerialPort module_port;
        private ManualResetEvent receiveDone = new ManualResetEvent(false);
        internal StateObject StateForAPI = new StateObject();
        internal ModuleSerialPortBase()
        {

        }
        internal void PortClose()
        {
            if (module_port == null) return;
            try
            {
                module_port.DiscardInBuffer();
                module_port.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        internal async Task<int> Open(string ComPort, int BaudRate = 1152000, Parity parity = Parity.None, StopBits StopBits = StopBits.One)
        {
            PortClose();
            try
            {
                module_port = new SerialPort();
                module_port.PortName = ComPort;
                module_port.BaudRate = BaudRate;
                module_port.Parity = parity;
                module_port.StopBits = StopBits;
                module_port.ReadTimeout = 1000;
                module_port.ReadBufferSize = 8192;
                module_port.DtrEnable = true;
                module_port.RtsEnable = true;
                module_port.Open();
                module_port.DiscardInBuffer();
                module_port.DiscardOutBuffer();
                return module_port.IsOpen ? 0 : -1; //TODO Error Code add
            }
            catch (Exception ex)
            {
                return (int)clsErrorCode.Error.SerialPortOpenFail;
            }
        }

        internal async Task<StateObject> SendMessage(byte[] cmd, int CheckLen, int timeout)
        {
            StateForAPI = new StateObject();
            receiveDone.Reset();
            Console.WriteLine("Serial Port Send:" + cmd.ToCommaString());
            module_port.Write(cmd, 0, cmd.Length);
            await SyncRecieve(CheckLen, timeout);
            receiveDone.WaitOne();
            module_port.DiscardOutBuffer();
            return StateForAPI;
        }

        internal async Task<StateObject> SendMessage(string msg, int CheckLen, int timeout)
        {
            module_port.DiscardNull = true;
            Console.WriteLine("Serial Port Send:" + msg);
            StateForAPI = new StateObject();
            receiveDone.Reset();
            module_port.Write(msg);
            await SyncRecieve(CheckLen, timeout);
            receiveDone.WaitOne();
            return StateForAPI;
        }
        private Stopwatch SyncRecieveStopwatch = new Stopwatch();
        private async Task SyncRecieve(int CheckLen, int timeout)
        {
            SyncRecieveStopwatch.Restart();
            while (StateForAPI.IsDataReach == false)
            {
                module_port.ReadTimeout = timeout;
                int byteToRead = module_port.BytesToRead;
                if (byteToRead == 0)
                {
                    if (SyncRecieveStopwatch.ElapsedMilliseconds > 3000)
                    {
                        StateForAPI.ErrorCode = CheckLen == 8 ? clsErrorCode.Error.PARAM_HS_TIMEOUT : clsErrorCode.Error.DATA_GET_TIMEOUT;
                        receiveDone.Set();
                        return;
                    }
                    Thread.Sleep(16);
                    continue;
                }
                byte[] buffer = new byte[module_port.BytesToRead];

                module_port.Read(buffer, 0, buffer.Length);
                StateForAPI.DataByteList.AddRange(buffer);
                //StateForAPI.DataByteList.Add((byte)module_port.ReadByte());
                if (StateForAPI.DataByteList.Count >= CheckLen)
                {
                    module_port.DiscardInBuffer();
                    StateForAPI.ErrorCode = clsErrorCode.Error.None;
                    receiveDone.Set();
                    return;
                }

                Thread.Sleep(10);
            }
        }
    }
}
