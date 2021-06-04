using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gpm_vibration_module_api.Modbus
{
    public class GPMModbusAPI
    {
        #region STRUCT
        /// <summary>
        /// 暫存器位址設定
        /// </summary>
        internal struct Register
        {
            public const int VEValuesRegStart = 0;
            public const int VEValuesRegLen = 6;

            public const int TotalVEValueRegStart = 6;
            public const int TotalVEValueRegLen = 2;

            public const int RMSValuesRegStart = 8;
            public const int RMSValuesRegLen = 6;

            public const int P2PValuesRegStart = 14;
            public const int P2PValuesRegLen = 6;

            public const int AllValuesRegStart = 0;
            public const int AllValuesRegLen = 14;
            //ID
            public const int IDReg = 90;
        }
        #endregion
        private bool RecieveData = false;
        private ModbusClient modbusClient = new ModbusClient();

        public GPMModbusAPI()
        {
            modbusClient.ReceiveDataChanged += new ModbusClient.ReceiveDataChangedHandler(ModbusClient_ReceiveDataChanged);
            modbusClient.SendDataChanged += new ModbusClient.SendDataChangedHandler(ModbusClient_SendDataChanged);
            modbusClient.ConnectedChanged += new ModbusClient.ConnectedChangedHandler(ModbusClient_ConnectedChanged);
        }
        /// <summary>
        /// Modbus TCP連線
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="Port"></param>
        /// <returns></returns>
        public bool Connect(string IP, int Port, string SlaveID)
        {
            modbusClient.IPAddress = IP;
            modbusClient.Port = Port;
            modbusClient.SerialPort = null;
            modbusClient.UnitIdentifier = byte.Parse(SlaveID);
            return modbusClient.Connect();
        }
        /// <summary>
        /// Modbus RTU連線
        /// </summary>
        /// <param name="ComPort"></param>
        /// <param name="SlaveID"></param>
        /// <param name="BaudRate"></param>
        /// <param name="parity"></param>
        /// <param name="StopBits"></param>
        /// <returns></returns>
        public bool Connect(string ComPort, string SlaveID, int BaudRate, System.IO.Ports.Parity parity, System.IO.Ports.StopBits StopBits)
        {
            modbusClient.SerialPort = ComPort;
            modbusClient.UnitIdentifier = byte.Parse(SlaveID);
            modbusClient.Baudrate = BaudRate;
            modbusClient.Parity = parity;
            modbusClient.StopBits = StopBits;
            return modbusClient.Connect();
        }
        /// <summary>
        /// 讀取3軸振動能量值
        /// </summary>
        /// <returns></returns>
        public async Task<double[]> ReadVEValues()
        {
            RecieveData = false;
            return await GetF03FloatValue(Register.VEValuesRegStart, Register.VEValuesRegLen);
        }
        /// <summary>
        /// 讀取總能量值
        /// </summary>
        /// <returns></returns>
        public async Task<double> ReadTotalVEValues()
        {
            RecieveData = false;
            return (await GetF03FloatValue(Register.TotalVEValueRegStart, Register.TotalVEValueRegLen))[0];
        }
        /// <summary>
        /// 讀取3軸RMS值
        /// </summary>
        /// <returns></returns>
        public async Task<double[]> ReadRMSValues()
        {
            RecieveData = false;
            return await GetF03FloatValue(Register.RMSValuesRegStart, Register.RMSValuesRegLen);
        }
        /// <summary>
        /// 讀取3軸P2P值
        /// </summary>
        /// <returns></returns>
        public async Task<double[]> ReadP2PValues()
        {
            RecieveData = false;
            return await GetF03FloatValue(Register.P2PValuesRegStart, Register.P2PValuesRegLen);
        }
        /// <summary>
        /// 讀取所有特徵值(3軸能量值+總能量值+3軸RMS值)
        /// </summary>
        /// <returns></returns>
        public async Task<double[]> ReadAllValues()
        {
            RecieveData = false;
            return await GetF03FloatValue(Register.AllValuesRegStart, Register.AllValuesRegLen);
        }
        /// <summary>
        /// 查詢Device ID
        /// </summary>
        /// <returns></returns>
        public string GetSlaveID()
        {
            RecieveData = false;
            var ID_int = modbusClient.ReadHoldingRegisters(Register.IDReg, 1).First();
            return ID_int.ToString("X2");
        }
        /// <summary>
        /// 設定Device ID
        /// </summary>
        /// <param name="ID"></param>
        public void SlaveIDSetting(byte ID)
        {
            var oriID = modbusClient.UnitIdentifier;
            RecieveData = false;
            modbusClient.UnitIdentifier = 0xF0;
            modbusClient.WriteSingleRegister(Register.IDReg, ID);
            if (RecieveData)
            {
                modbusClient.UnitIdentifier = ID;
            }
            else
            {
                modbusClient.UnitIdentifier = oriID;
            }
        }
        /// <summary>
        /// 設定量測範圍
        /// </summary>
        /// <param name="Range"></param>
        public void MeasureRangeSet(int Range)
        {
            RecieveData = false;
            int valwrite = 0;
            switch (Range)
            {
                case 2:
                    valwrite = 40704;
                    break;
                case 4:
                    valwrite = 40720;
                    break;
                case 8:
                    valwrite = 40736;
                    break;
                case 16:
                    valwrite = 40752;
                    break;
                default:
                    break;
            }
            modbusClient.WriteSingleRegister(81, valwrite);
        }

        /// <summary>
        /// 下達F03指令並轉成浮點數回傳
        /// </summary>
        /// <param name="start"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        private async Task<double[]> GetF03FloatValue(int start, int len)
        {
            int[] values = modbusClient.ReadHoldingRegisters(start, len);
            return values.IEEE754FloatAry(); ;
        }

        private void ModbusClient_ConnectedChanged(object sender)
        {
            Console.WriteLine(sender);
        }

        private void ModbusClient_SendDataChanged(object sender)
        {

        }

        private void ModbusClient_ReceiveDataChanged(object sender)
        {
            RecieveData = true;
        }
    }

    public static class Extension
    {
        internal static double[] IEEE754FloatAry(this int[] intAry)
        {
            List<double> valuesList = new List<double>();
            for (int i = 0; i < intAry.Length; i += 4)
            {
                var hexstring = intAry[i].ToString("X2") + intAry[i + 1].ToString("X2") + intAry[i + 2].ToString("X2") + intAry[i + 3].ToString("X2");
                valuesList.Add(Hex32toFloat(hexstring));
            }
            return valuesList.ToArray();
        }

        static float Hex32toFloat(string Hex32Input)
        {
            double doubleout = 0.0;
            UInt64 bigendian;
            bool success = UInt64.TryParse(Hex32Input,
                System.Globalization.NumberStyles.HexNumber, null, out bigendian);
            if (success)
            {
                double fractionDivide = Math.Pow(2, 23);

                int sign = (bigendian & 0x80000000) == 0 ? 1 : -1;
                Int64 exponent = ((Int64)(bigendian & 0x7F800000) >> 23) - (Int64)127;
                UInt64 fraction = (bigendian & 0x007FFFFF);
                if (fraction == 0)
                    doubleout = sign * Math.Pow(2, exponent);
                else
                    doubleout = sign * (1 + (fraction / fractionDivide)) * Math.Pow(2, exponent);
            }
            return (float)doubleout;
        }
    }
}
