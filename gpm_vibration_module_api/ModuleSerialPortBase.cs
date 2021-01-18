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
                throw ex;
            }
        }
        internal async Task<int> Open(string ComPort, int BaudRate=1152000, Parity parity= Parity.None, StopBits StopBits= StopBits.One)
        {
            PortClose();
            try
            {
                module_port = new SerialPort();
                module_port.PortName = ComPort;
                module_port.BaudRate = BaudRate;
                module_port.Parity = parity;
                module_port.StopBits = StopBits;
                module_port.Open();
                return module_port.IsOpen ? 0 : -1; //TODO Error Code add
            }
            catch (Exception ex)
            {
                return -1;
            }
        }

        internal async Task<StateObject> SendMessage(byte[] cmd, int CheckLen, int timeout)
        {
            StateForAPI = new StateObject();
            receiveDone.Reset();
            module_port.Write(cmd,0,cmd.Length);
            await SyncRecieve(CheckLen, timeout);
            receiveDone.WaitOne();
            module_port.DiscardOutBuffer();
            return StateForAPI;
        }

        internal async Task<StateObject> SendMessage(string msg, int CheckLen, int timeout)
        {
            StateForAPI = new StateObject();
            receiveDone.Reset();
            module_port.Write(msg);
            await SyncRecieve(CheckLen, timeout);
            receiveDone.WaitOne();
            module_port.DiscardOutBuffer();
            return StateForAPI;
        }
        private Stopwatch SyncRecieveStopwatch = new Stopwatch();
        private async Task SyncRecieve(int CheckLen, int timeout)
        {
            SyncRecieveStopwatch.Restart();
            while (StateForAPI.IsDataReach == false)
            {
                byte[] buffer = new byte[module_port.BytesToRead];
                module_port.Read(buffer, 0, buffer.Length);
                StateForAPI.DataByteList.AddRange(buffer);
                if (StateForAPI.DataByteList.Count >= CheckLen)
                {
                    module_port.DiscardInBuffer();
                    StateForAPI.ErrorCode = clsErrorCode.Error.None;
                    receiveDone.Set();
                    return;
                }
                if (SyncRecieveStopwatch.ElapsedMilliseconds > timeout)
                {
                    StateForAPI.ErrorCode = CheckLen == 8 ? clsErrorCode.Error.PARAM_HS_TIMEOUT : clsErrorCode.Error.DATA_GET_TIMEOUT;
                    receiveDone.Set();
                    return;
                }
            }
        }
    }
}
