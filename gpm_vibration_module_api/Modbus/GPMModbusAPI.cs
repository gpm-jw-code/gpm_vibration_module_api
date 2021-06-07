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
        public enum CONNECTION_TYPE
        {
            TCP, RTU
        }
        public bool IsTest = false;
        public bool IsReadBaudRateWhenConnected = false;
        public CONNECTION_TYPE Connection_Type { get; private set; } = CONNECTION_TYPE.RTU;
        public int BaudRate { get; private set; } = 9600;
        #region STRUCT
        /// <summary>
        /// 暫存器位址設定
        /// </summary>
        internal struct Register
        {
            public const int VEValuesRegStartIndex = 0;
            public const int VEValuesRegLen = 6;

            public const int TotalVEValueRegStartIndex = 6;
            public const int TotalVEValueRegLen = 2;

            public const int RMSValuesRegStartIndex = 8;
            public const int RMSValuesRegLen = 6;

            public const int P2PValuesRegStartIndex = 14;
            public const int P2PValuesRegLen = 6;

            public const int AllValuesRegStartIndex = 0;
            public const int AllValuesRegLen = 20;
            //ID
            public const int IDRegIndex = 144;
            public const int RangeRegStart = 81;
            public const int BaudRateSetRegIndex = 146;
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
            Connection_Type = CONNECTION_TYPE.TCP;
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
            bool IsConnected = modbusClient.Connect();
            if (IsConnected && IsReadBaudRateWhenConnected)
            {
                int CurrentBaudRate = ReadBaudRateSetting();
                this.BaudRate = CurrentBaudRate != -1 ? CurrentBaudRate : BaudRate;
            }
            Connection_Type = CONNECTION_TYPE.RTU;
            return IsConnected;
        }

        private int ReadBaudRateSetting()
        {
            RecieveData = false;
            int ret = modbusClient.ReadHoldingRegisters(Register.BaudRateSetRegIndex, 1).FirstOrDefault();
            if (ret != default)
                return ret == 0 ? 9600 : 115200;
            else
                return -1;
        }

        /// <summary>
        /// 讀取3軸振動能量值
        /// </summary>
        /// <returns></returns>
        public async Task<double[]> ReadVEValues()
        {
            RecieveData = false;
            return await GetF03FloatValue(Register.VEValuesRegStartIndex, Register.VEValuesRegLen);
        }
        /// <summary>
        /// 讀取總能量值
        /// </summary>
        /// <returns></returns>
        public async Task<double> ReadTotalVEValues()
        {
            RecieveData = false;
            return (await GetF03FloatValue(Register.TotalVEValueRegStartIndex, Register.TotalVEValueRegLen))[0];
        }
        /// <summary>
        /// 讀取3軸RMS值
        /// </summary>
        /// <returns></returns>
        public async Task<double[]> ReadRMSValues()
        {
            RecieveData = false;
            return await GetF03FloatValue(Register.RMSValuesRegStartIndex, Register.RMSValuesRegLen);
        }
        /// <summary>
        /// 讀取3軸P2P值
        /// </summary>
        /// <returns></returns>
        public async Task<double[]> ReadP2PValues()
        {
            RecieveData = false;
            return await GetF03FloatValue(Register.P2PValuesRegStartIndex, Register.P2PValuesRegLen);
        }
        /// <summary>
        /// 讀取所有特徵值(3軸能量值+總能量值+3軸RMS值)
        /// </summary>
        /// <returns></returns>
        public async Task<double[]> ReadAllValues()
        {
            RecieveData = false;
            return await GetF03FloatValue(Register.AllValuesRegStartIndex, Register.AllValuesRegLen);
        }
        /// <summary>
        /// 查詢Device ID
        /// </summary>
        /// <returns></returns>
        public string GetSlaveID()
        {
            RecieveData = false;
            var ID_int = modbusClient.ReadHoldingRegisters(Register.IDRegIndex, 1).First();
            return ID_int.ToString("X2");
        }

        /// <summary>
        /// 進行鮑率設定
        /// </summary>
        /// <param name="baud"></param>
        /// <returns>false>設定失敗 ; true>設定成功</returns>
        public async Task<bool> BaudRateSetting(int baud)
        {
            if (Connection_Type != CONNECTION_TYPE.TCP)
                throw new Exception("鮑率設定必須在Modbus TCP模式下操作");
            if (baud != 115200 | baud != 9600)
                throw new Exception($"{baud}是不允許的鮑率設定值");
            RecieveData = false;
            await Task.Run(() => modbusClient.WriteSingleRegister(Register.BaudRateSetRegIndex, baud == 115200 ? 1 : 0));
            BaudRate = RecieveData ? baud : BaudRate;
            return RecieveData;
        }
        /// <summary>
        /// 版本號查詢 
        /// </summary>
        public string GetVersion()
        {
            var version = "err_err_err";
            RecieveData = false;
            byte[] byteVals = new byte[4] { 0x31, 0x2E, 0x30, 0x36 };
            if (!IsTest)
            {
                int[] intVals = modbusClient.ReadHoldingRegisters(240, 2);
                version = Encoding.ASCII.GetString(intVals.IntAryToByteAry());
                return version;
            }
            else
                return Encoding.ASCII.GetString(byteVals);
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
            modbusClient.WriteSingleRegister(Register.IDRegIndex, ID);
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
            modbusClient.WriteSingleRegister(Register.RangeRegStart, valwrite);
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
        internal static byte[] IntAryToByteAry(this int[] intAry)
        {
            List<byte> byteList = new List<byte>();
            for (int i = 0; i < intAry.Length; i++)
            {
                byteList.Add((byte)intAry[i]);
            }
            return byteList.ToArray();
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
