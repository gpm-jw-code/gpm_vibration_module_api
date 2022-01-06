using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gpm_vibration_module_api.ThreeInOne
{
    public class SerialProtocolBase
    {
        internal string ErrorState = "";
        protected SerialPort _serialPort;
        public int TotalDataByteLen = 3092;
        public bool _isDataRecieveDone = false;
        private bool CRCL_CHECK = false;
        public List<byte> TempDataByteList = new List<byte>();
        internal bool IsSimulator = false;

        public long HandShakeTime { get; private set; }

        virtual public bool Open(string ComPort, int BaudRate = 115200)
        {
            try
            {
                _serialPort = new SerialPort()
                {
                    PortName = ComPort.ToUpper(),
                    BaudRate = BaudRate,
                };
                _serialPort.DataReceived += _serialPort_DataReceived;

                if (_serialPort.IsOpen)
                    _serialPort.Close();
                _serialPort.Open();
                return true;
            }
            catch (Exception ex)
            {
                ErrorState = ex.Message + ex.StackTrace;
                Console.WriteLine(ErrorState);
                return false;
            }
        }

        public void Close()
        {
            try
            {
                _serialPort.Close();
            }
            catch (Exception ex)
            {
                //沒關係
                Console.WriteLine(ex.Message);
            }
        }

        internal virtual void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            int numOfReadByte = sp.BytesToRead;
            byte[] buff = new byte[numOfReadByte];
            sp.Read(buff, 0, numOfReadByte);
            TempDataByteList.AddRange(buff);
            if (CRCL_CHECK)
                _isDataRecieveDone = Encoding.ASCII.GetString(TempDataByteList.ToArray()).Contains("\r\n");
            else
            {
                _isDataRecieveDone = TempDataByteList.Count >= TotalDataByteLen;
            }
            if (_isDataRecieveDone)
            {
                _serialPort.DataReceived -= _serialPort_DataReceived;
            }
        }

        public async Task<bool> SendCommand(string asciiCmd)
        {
            return await SendCommand(Encoding.ASCII.GetBytes(asciiCmd), true, false);
        }
        public async Task<bool> SendCommand(byte[] bytesCmd, bool isReviceData = true)
        {
            try
            {
                return await SendCommand(bytesCmd, isReviceData,false);
            }
            catch(Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> SendCommand(byte[] bytesCmd, bool isReviceData = true , bool CRCL = false)
        {
            try
            {
                CRCL_CHECK = CRCL;
                _serialPort.DiscardOutBuffer();
                _serialPort.DiscardInBuffer();
                TempDataByteList.Clear();
                _isDataRecieveDone = false;
                if (isReviceData | CRCL)
                    _serialPort.DataReceived += _serialPort_DataReceived;
                if (_serialPort == null | !_serialPort.IsOpen)
                    throw new Exception("Serial Port 尚未開啟,無法進行'Write'作業");

                _serialPort.Write(bytesCmd, 0, bytesCmd.Length);
                if (isReviceData | CRCL)
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    while (!_isDataRecieveDone)
                    {
                        if (sw.ElapsedMilliseconds > 20000)
                            return false;
                        Thread.Sleep(1);
                    }
                    sw.Stop();
                    HandShakeTime = sw.ElapsedMilliseconds;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SendCommand(string asciiCmd, bool isReviceData = true, bool CRCL = false)
        {
            try
            {
                return await SendCommand(Encoding.ASCII.GetBytes(asciiCmd), isReviceData, CRCL);
            }
            catch
            {
                return false;
            }
        }
    }
}
