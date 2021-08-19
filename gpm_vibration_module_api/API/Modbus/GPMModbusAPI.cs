﻿//#define v110
using gpm_vibration_module_api.API.Modbus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gpm_vibration_module_api.Modbus
{
    public class GPMModbusAPI
    {
        public enum MODBUS_ERRORCODE
        {
            API_INNER_EXCEPTION = -400,
            PACKET_LOSS = -404
        }
        public enum CONNECTION_TYPE
        {
            TCP, RTU
        }
        private string SlaveID;
        private string PortName;
        internal bool IsTest = false;
        public bool IsReadBaudRateWhenConnected = false;
        public CONNECTION_TYPE Connection_Type { get; private set; } = CONNECTION_TYPE.RTU;
        public int BaudRate { get; private set; } = 9600;
        public bool Connected
        {
            get
            {
                return modbusClient_TCP.Connected;
            }
        }
        public void DisConnect()
        {
            modbusClient_TCP.Disconnect();

        }
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
            public const int RangeRegStart = 129;
            public const int BaudRateSetRegIndex = 146;
        }
        #endregion
        private bool RecieveData = false;
        private ModbusClient modbusClient_TCP = new ModbusClient();

        public GPMModbusAPI()
        {
            modbusClient_TCP.ReceiveDataChanged += new ModbusClient.ReceiveDataChangedHandler(ModbusClient_ReceiveDataChanged);
            modbusClient_TCP.SendDataChanged += new ModbusClient.SendDataChangedHandler(ModbusClient_SendDataChanged);
            modbusClient_TCP.ConnectedChanged += new ModbusClient.ConnectedChangedHandler(ModbusClient_ConnectedChanged);
        }
        /// <summary>
        /// Modbus TCP連線
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="Port"></param>
        /// <returns></returns>
        public bool Connect(string IP, int Port, string SlaveID)
        {
            this.SlaveID = SlaveID;
            modbusClient_TCP.IPAddress = IP;
            modbusClient_TCP.Port = Port;
            modbusClient_TCP.SerialPort = null;
            modbusClient_TCP.UnitIdentifier = byte.Parse(SlaveID);
            Connection_Type = CONNECTION_TYPE.TCP;
            try
            {
                bool IsConnected = modbusClient_TCP.Connect();
                if (IsConnected && IsReadBaudRateWhenConnected)
                {
                    int CurrentBaudRate = ReadBaudRateSetting();
                    this.BaudRate = CurrentBaudRate != -1 ? CurrentBaudRate : BaudRate;
                }
#if v110
            //因應韌體Bug>連線上後會自動發一個封包過來...
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (modbusClient.tcpClient.Client.Available == 0)
            {
                if (sw.ElapsedMilliseconds > 5000)
                    break;
                Thread.Sleep(1);
            }
            modbusClient.BuferClear();
#endif
                return IsConnected;
            }
            catch (Exception ex)
            {
                throw ex;
            }

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
        public bool Connect(string ComPort, string SlaveID, int BaudRate, System.IO.Ports.Parity parity = System.IO.Ports.Parity.None, System.IO.Ports.StopBits StopBits = System.IO.Ports.StopBits.One)
        {
            this.SlaveID = SlaveID;
            this.PortName = ComPort;
            ModbusClient modbus_cli = SerialPortManager.OpenRTU(ComPort, BaudRate, SlaveID);
            //modbusClient.SerialPort = ComPort;
            //modbusClient.Baudrate = BaudRate;
            //modbusClient.Parity = parity;
            //modbusClient.StopBits = StopBits;
            if (modbus_cli.Connected && IsReadBaudRateWhenConnected)
            {
                int CurrentBaudRate = ReadBaudRateSetting();
                this.BaudRate = CurrentBaudRate != -1 ? CurrentBaudRate : BaudRate;
            }
            Connection_Type = CONNECTION_TYPE.RTU;
            return modbus_cli.Connected;
        }

        /// <summary>
        /// 讀取鮑率設定值
        /// </summary>
        /// <returns>if return -1 > 表示模組回傳的封包數據有異常 </returns>
        public int ReadBaudRateSetting()
        {
            RecieveData = false;
            int[] intAry = null;
            if (Connection_Type == CONNECTION_TYPE.TCP)
                intAry = modbusClient_TCP.ReadHoldingRegisters(Register.BaudRateSetRegIndex, 1);
            else //RTU 要排隊
                intAry = SerialPortManager.ReadHoldingRegisters(SlaveID, PortName, Register.BaudRateSetRegIndex, 1);

            if (intAry != null && intAry.ToList().All(val => val >= 0))
            {
                if (intAry[0] != 0)
                    return (int)MODBUS_ERRORCODE.PACKET_LOSS;
                return intAry[1] == 0 ? 9600 : 115200;
            }
            else
                return intAry == null ? (int)MODBUS_ERRORCODE.API_INNER_EXCEPTION : intAry[0];

        }

        /// <summary>
        /// 讀取3軸振動能量值
        /// </summary>
        /// <returns>If return double array = [-1,-1,-1],表示接收到的封包有異常(比如長度不足)，解封包時發生錯誤</returns>
        public async Task<double[]> ReadVEValues()
        {
            RecieveData = false;
            double[] dVals = (await GetF03FloatValue(Register.VEValuesRegStartIndex, Register.VEValuesRegLen));
            if (dVals != null && dVals.Length > 0 && dVals.All(val => Math.Abs(val) < (double)decimal.MaxValue))
                return dVals;
            else
                return new double[] { -1, -1, -1 };
        }
        /// <summary>
        /// 讀取總能量值
        /// </summary>
        /// <returns>If return -1 > 表示接收到的封包有異常(比如長度不足)，解封包時發生錯誤</returns>
        public async Task<double> ReadTotalVEValues()
        {
            RecieveData = false;
            double[] dVals = (await GetF03FloatValue(Register.TotalVEValueRegStartIndex, Register.TotalVEValueRegLen));
            if (dVals != null && dVals.Length > 0 && dVals.All(val => Math.Abs(val) < (double)decimal.MaxValue))
                return dVals.First();
            else
                return -1;
        }
        /// <summary>
        /// 讀取3軸RMS值
        /// </summary>
        /// <returns>If return double array = [-1,-1,-1],表示接收到的封包有異常(比如長度不足)，解封包時發生錯誤</returns>
        public async Task<double[]> ReadRMSValues()
        {
            RecieveData = false;
            double[] dVals = (await GetF03FloatValue(Register.RMSValuesRegStartIndex, Register.RMSValuesRegLen));
            if (dVals != null && dVals.Length > 0 && dVals.All(val => Math.Abs(val) < (double)decimal.MaxValue))
                return dVals;
            else
                return new double[] { -1, -1, -1 };
        }
        /// <summary>
        /// 讀取3軸P2P值
        /// </summary>
        /// <returns>If return double array = [-1,-1,-1],表示接收到的封包有異常(比如長度不足)，解封包時發生錯誤</returns>
        public async Task<double[]> ReadP2PValues()
        {
            RecieveData = false;
            double[] dVals = (await GetF03FloatValue(Register.P2PValuesRegStartIndex, Register.P2PValuesRegLen));
            if (dVals != null && dVals.Length > 0 && dVals.All(val => Math.Abs(val) < (double)decimal.MaxValue))
                return dVals;
            else
                return new double[] { -1, -1, -1 };
        }
        /// <summary>
        /// 讀取所有特徵值(3軸能量值+總能量值+3軸RMS值)
        /// </summary>
        /// <returns>If return double array = [-1, -1, -1, -1, -1, -1, -1],表示接收到的封包有異常(比如長度不足)，解封包時發生錯誤</returns>
        public async Task<double[]> ReadAllValues()
        {
            RecieveData = false;
            double[] dVals = (await GetF03FloatValue(Register.AllValuesRegStartIndex, Register.AllValuesRegLen));
            if (dVals != null && dVals.Length > 0 && dVals.All(val => Math.Abs(val) < (double)decimal.MaxValue))
                return dVals;
            else
                return new double[] { -1, -1, -1, -1, -1, -1, -1 };
        }
        /// <summary>
        /// 查詢Device ID
        /// </summary>
        /// <returns></returns>
        public string GetSlaveID()
        {
            RecieveData = false;
            int ID_int = -1;
            if (Connection_Type == CONNECTION_TYPE.TCP)
            {
                modbusClient_TCP.UnitIdentifier = 0xf0;
                ID_int = modbusClient_TCP.ReadHoldingRegisters(Register.IDRegIndex, 1)[1];
                modbusClient_TCP.UnitIdentifier = (byte)ID_int;
            }
            else
                ID_int = SerialPortManager.ReadHoldingRegisters("240", PortName, Register.IDRegIndex, 1)[1];
            return ID_int+"";
        }
        public int GetCurrentMeasureRange()
        {
            RecieveData = false;
            var ints = Connection_Type == CONNECTION_TYPE.TCP ?
                modbusClient_TCP.ReadHoldingRegisters(Register.RangeRegStart - 1, 4) :
                SerialPortManager.ReadHoldingRegisters(SlaveID, PortName, Register.RangeRegStart - 1, 4);
            if (ints[3] == 0x00)
                return 2;
            if (ints[3] == 0x10)
                return 4;
            if (ints[3] == 0x20)
                return 8;
            if (ints[3] == 0x30)
                return 16;
            else
                throw new Exception("Measure Range Read Failure");
        }
        /// <summary>
        /// 進行鮑率設定
        /// </summary>
        /// <param name="baud"></param>
        /// <returns>false>設定失敗 ; true>設定成功</returns>
        public bool BaudRateSetting(int baud)
        {
            if (Connection_Type != CONNECTION_TYPE.TCP)
                throw new Exception("鮑率設定必須在Modbus TCP模式下操作");
            if (baud != 115200 && baud != 9600)
                throw new Exception($"{baud}是不允許的鮑率設定值");
            RecieveData = false;
            modbusClient_TCP.WriteSingleRegister(Register.BaudRateSetRegIndex, baud == 115200 ? 1 : 0);
            BaudRate = RecieveData ? baud : BaudRate;
            modbusClient_TCP.BuferClear();
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
                int[] intVals = Connection_Type == CONNECTION_TYPE.TCP ?
                    modbusClient_TCP.ReadHoldingRegisters(240, 2) :
                    SerialPortManager.ReadHoldingRegisters(SlaveID, PortName, 240, 2);
                if (intVals == null)
                    return "READ VERSION ERROR!";
                version = Encoding.ASCII.GetString(intVals.ToByteAry());
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
            if (Connection_Type == CONNECTION_TYPE.TCP)
            {
                var oriID = modbusClient_TCP.UnitIdentifier;
                RecieveData = false;
                //modbusClient.UnitIdentifier = 0xF0;
                modbusClient_TCP.WriteSingleRegister(Register.IDRegIndex, ID);

                if (RecieveData)
                {
                    modbusClient_TCP.UnitIdentifier = ID;
                }
                else
                {
                    modbusClient_TCP.UnitIdentifier = oriID;
                }
            }
            else
            {
                var oriID = SlaveID;
                SerialPortManager.WriteSingleRegister(SlaveID, PortName, Register.IDRegIndex, ID);
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
            if (Connection_Type == CONNECTION_TYPE.TCP)
                modbusClient_TCP.WriteSingleRegister(Register.RangeRegStart, valwrite);
            else
                SerialPortManager.WriteSingleRegister(SlaveID, PortName, Register.RangeRegStart, valwrite);
        }


        /// <summary>
        /// 下達F03指令並轉成浮點數回傳
        /// </summary>
        /// <param name="start"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        private async Task<double[]> GetF03FloatValue(int start, int len)
        {
            int[] values = null;
            values = Connection_Type == CONNECTION_TYPE.TCP ? modbusClient_TCP.ReadHoldingRegisters(start, len) :
                SerialPortManager.ReadHoldingRegisters(SlaveID, PortName, start, len);
            return values.ToIEEE754FloatAry();
        }

        public async Task<double[]> TestGetF03FloatValue()
        {
            int[] values = null;
            return values.ToIEEE754FloatAry();
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
        internal static double[] ToIEEE754FloatAry(this int[] intAry)
        {
            if (intAry == null)
                return null;
            List<double> valuesList = new List<double>();
            for (int i = 0; i < intAry.Length; i += 4)
            {
                var hexstring = intAry[i].ToString("X2") + intAry[i + 1].ToString("X2") + intAry[i + 2].ToString("X2") + intAry[i + 3].ToString("X2");
                double dVal = hexstring.ToFloat();
                valuesList.Add(dVal);
            }
            return valuesList.ToArray();
        }
        internal static byte[] ToByteAry(this int[] intAry)
        {
            List<byte> byteList = new List<byte>();
            for (int i = 0; i < intAry.Length; i++)
            {
                byteList.Add((byte)intAry[i]);
            }
            return byteList.ToArray();
        }

        static float ToFloat(this string Hex32Input)
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
