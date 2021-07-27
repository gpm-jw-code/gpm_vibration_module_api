using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gpm_vibration_module_api.ThreeInOne
{
    public class SerialProtocolBase
    {
        internal string ErrorState = "";
        private SerialPort _serialPort;
        public int TotalDataByteLen = 3092;
        internal bool _isDataRecieveDone = false;
        internal List<byte> TempDataByteList = new List<byte>();
        internal bool IsSimulator = false;
        internal bool Open(string ComPort, int BaudRate = 115200)
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
                return false;
            }
        }

        internal void Close()
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
            _isDataRecieveDone = TempDataByteList.Count >= TotalDataByteLen;
            if (_isDataRecieveDone)
            {
                _serialPort.DataReceived -= _serialPort_DataReceived;
            }
        }

        internal bool SendCommand(string asciiCmd)
        {
            return SendCommand(Encoding.ASCII.GetBytes(asciiCmd));
        }
        internal bool SendCommand(byte[] bytesCmd)
        {
            try
            {
                _serialPort.DiscardOutBuffer();
                _serialPort.DiscardInBuffer();
                TempDataByteList.Clear();
                _isDataRecieveDone = false;
                _serialPort.DataReceived += _serialPort_DataReceived;
                if (_serialPort == null | !_serialPort.IsOpen)
                    throw new Exception("Serial Port 尚未開啟,無法進行'Write'作業");

                _serialPort.Write(bytesCmd, 0, bytesCmd.Length);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
