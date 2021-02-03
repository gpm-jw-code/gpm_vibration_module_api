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
                module_port.DataReceived += Module_port_DataReceived;
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
            StateForAPI.CheckLen = CheckLen;
            receiveDone.Reset();
            Console.WriteLine("Serial Port Send:" + cmd.ToCommaString());
            module_port.Write(cmd, 0, cmd.Length);
            Thread.Sleep(1);
            Task.Run(() => TimeoutChecker(timeout));
            //await SyncRecieve(CheckLen, timeout);
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
            Thread.Sleep(1);
            Task.Run(() => TimeoutChecker(timeout));
            //await SyncRecieve(CheckLen, timeout);
            receiveDone.WaitOne();
            return StateForAPI;
        }

        private void Module_port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                Console.WriteLine("DataReceived event trigger");
                byte[] buffer = new byte[module_port.BytesToRead];
                Console.WriteLine($"Byte to read: {module_port.BytesToRead}");
                module_port.Read(buffer, 0, buffer.Length);
                StateForAPI.DataByteList.AddRange(buffer);
                if (StateForAPI.DataByteList.Count >= StateForAPI.CheckLen)
                {
                    module_port.DiscardInBuffer();
                    StateForAPI.ErrorCode = clsErrorCode.Error.None;
                    receiveDone.Set();
                    return;
                }
            }
            catch (Exception ex)
            {
                StateForAPI.IsDataReach = true;
                StateForAPI.ErrorCode = clsErrorCode.Error.DATA_GET_TIMEOUT;
                receiveDone.Set();
            }
           
        }
        private void TimeoutChecker(int timeout)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (!StateForAPI.IsDataReach)
            {
                if (sw.ElapsedMilliseconds > timeout)
                {
                    module_port.DiscardInBuffer();
                    module_port.DiscardOutBuffer();
                    StateForAPI.ErrorCode = clsErrorCode.Error.DATA_GET_TIMEOUT;
                    receiveDone.Set();
                }
                Thread.Sleep(1);
            }
        }

        private Stopwatch SyncRecieveStopwatch = new Stopwatch();

    }
}
