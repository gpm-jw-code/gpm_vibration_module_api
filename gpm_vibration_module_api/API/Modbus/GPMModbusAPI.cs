﻿//#define v110
using gpm_vibration_module_api.API.Modbus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static gpm_vibration_module_api.Modbus.ModbusClient;

namespace gpm_vibration_module_api.Modbus
{
    public partial class GPMModbusAPI
    {
        public enum MODBUS_ERRORCODE
        {
            API_INNER_EXCEPTION = -400,
            PACKET_LOSS = -404
        }


        public delegate void Event_GetDataTimeOut(string IP, string SerialPort, string SlaveID);

        public Event_GetDataTimeOut EventGetDataTimeOut;

        internal string SlaveID;
        private string ComName;
        private string IP;
        private string Port;
        internal bool IsWaitingForTCPReconnectResult = false;
        internal bool IsTest = false;
        public bool IsReadBaudRateWhenConnected = false;
        private CONNECTION_TYPE _ConnectType = CONNECTION_TYPE.TCP;

        private DateTime LastCommandTime = default;
        public CONNECTION_TYPE Connection_Type
        {
            get
            {
                return _ConnectType;
            }
        }
        public int BaudRate { get; private set; } = 9600;
        public bool Connected
        {
            get
            {
                switch (_ConnectType)
                {
                    case CONNECTION_TYPE.TCP:
                        if (modbusClient_TCP == null)
                        {
                            return false;
                        }
                        return modbusClient_TCP.Connected;
                        break;
                    case CONNECTION_TYPE.RTU:
                        if (ModbusClient_RTU == null)
                        {
                            return false;
                        }
                        return ModbusClient_RTU.Connected;
                        break;
                    default:
                        break;
                }
                return modbusClient_TCP.Connected;
            }
        }

        public void Disconnect()
        {
            switch (_ConnectType)
            {
                case CONNECTION_TYPE.TCP:
                    TCPSocketManager.TCPSocketCancelRegist(IP, Convert.ToInt32(Port), SlaveID);
                    break;
                case CONNECTION_TYPE.RTU:
                    SerialPortManager.SerialPortCancelRegist(ComName, SlaveID);
                    break;
                default:
                    break;
            }
            return;
        }

        private bool ReceiveData = false;
        private ModbusClient modbusClient_TCP;
        private ModbusClient ModbusClient_RTU;
        public GPMModbusAPI()
        {
            //modbusClient_TCP.ReceiveDataChanged += new ModbusClient.ReceiveDataChangedHandler(ModbusClient_ReceiveDataChanged);
            //modbusClient_TCP.SendDataChanged += new ModbusClient.SendDataChangedHandler(ModbusClient_SendDataChanged);
            //modbusClient_TCP.ConnectedChanged += new ModbusClient.ConnectedChangedHandler(ModbusClient_ConnectedChanged);
        }
        /// <summary>
        /// Modbus TCP連線
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="Port"></param>
        /// <returns></returns>
        public bool Connect(string IP, int Port, string SlaveID)
        {
            ModbusClient_RTU = null;
            this.SlaveID = SlaveID;
            this.Port = Port.ToString();
            this.IP = IP;

            //if (DeviceInfoGetFromHtmlPage.GET_Protocol_Type(IP) != CONNECTION_TYPE.TCP)
            //    throw new Exception("控制器目前的通訊不是 MODBUS TCP!");

            modbusClient_TCP = TCPSocketManager.TCPSocketRegist(IP, Port, SlaveID, this);
            this._ConnectType = CONNECTION_TYPE.TCP;

            return modbusClient_TCP.Connected;

            //modbusClient_TCP = new ModbusClient();
            //modbusClient_TCP.IPAddress = IP;
            //modbusClient_TCP.Port = Port;
            //modbusClient_TCP.SerialPort = null;
            //modbusClient_TCP.UnitIdentifier = byte.Parse(SlaveID);
            //modbusClient_TCP.connect_type = CONNECTION_TYPE.TCP;
            try
            {
                bool IsConnected = modbusClient_TCP.Connect();
                if (IsConnected && IsReadBaudRateWhenConnected)
                {
                    int CurrentBaudRate = ReadBaudRateSetting().Result;
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

        public bool TCPConnectRetry(string IP, string Port, string SlaveID)
        {
            IsWaitingForTCPReconnectResult = true;
            TCPSocketManager.ConnectionRetry(IP, Port, SlaveID);
            while (IsWaitingForTCPReconnectResult)
            {
                Thread.Sleep(1);
            }
            return Connected;
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
        public bool Connect(string ComPort, string SlaveID, int BaudRate, Parity parity = Parity.None, StopBits StopBits = StopBits.One)
        {
            modbusClient_TCP = null;
            this.SlaveID = SlaveID;
            this.ComName = ComPort;
            ModbusClient_RTU = SerialPortManager.SerialPortRegist(ComPort, BaudRate, SlaveID, parity, StopBits, this);
            if (ModbusClient_RTU.Connected && IsReadBaudRateWhenConnected)
            {
                int CurrentBaudRate = ReadBaudRateSetting().Result;
                this.BaudRate = CurrentBaudRate != -1 ? CurrentBaudRate : BaudRate;
            }
            ModbusClient_RTU.connect_type = CONNECTION_TYPE.RTU;
            this._ConnectType = CONNECTION_TYPE.RTU;
            return ModbusClient_RTU.Connected;
        }


        /// <summary>
        /// 讀取鮑率設定值
        /// </summary>
        /// <returns>if return -1 > 表示模組回傳的封包數據有異常 </returns>
        public async Task<int> ReadBaudRateSetting()
        {
            ReceiveData = false;
            int[] intAry = null;
            if (Connection_Type == CONNECTION_TYPE.TCP)
                intAry = await TCPReadHoldingRegister(Register.BaudRateSetRegIndex, 1, SlaveID);
            else //RTU 要排隊
                intAry = await RTUReadHoldingRegister(Register.BaudRateSetRegIndex, 1, SlaveID);

            if (intAry != null && intAry.ToList().All(val => val >= 0))
            {
                if (intAry[0] != 0)
                    return (int)MODBUS_ERRORCODE.PACKET_LOSS;
                return intAry[1] == 0 ? 9600 : 115200;
            }
            else
                return intAry == null ? (int)MODBUS_ERRORCODE.API_INNER_EXCEPTION : intAry[0];

        }

        public async Task<double[]> ReadDisplacement_RMSValues()
        {
            ReceiveData = false;
            double[] dVals = await GetF03FloatValue(Register.Displacement_RMSRegStartIndex, Register.Displacement_RMSRegLen);
            if (dVals != null && dVals.Length > 0 && dVals.All(val => Math.Abs(val) < (double)decimal.MaxValue))
                return dVals;
            else
                return new double[] { -1, -1, -1 };
        }

        public async Task<double[]> ReadDisplacement_P2PValues()
        {
            ReceiveData = false;
            double[] dVals = await GetF03FloatValue(Register.Displacement_P2PRegStartIndex, Register.Displacement_P2PRegLen);
            if (dVals != null && dVals.Length > 0 && dVals.All(val => Math.Abs(val) < (double)decimal.MaxValue))
                return dVals;
            else
                return new double[] { -1, -1, -1 };
        }


        public async Task<double[]> ReadVelocity_RMSValues()
        {
            ReceiveData = false;
            double[] dVals = await GetF03FloatValue(Register.Velocity_RMSRegStartIndex, Register.Velocity_RMSRegLen);
            if (dVals != null && dVals.Length > 0 && dVals.All(val => Math.Abs(val) < (double)decimal.MaxValue))
                return dVals;
            else
                return new double[] { -1, -1, -1 };
        }

        public async Task<double[]> ReadVelocity_P2PValues()
        {
            ReceiveData = false;
            double[] dVals = await GetF03FloatValue(Register.Velocity_P2PRegStartIndex, Register.Velocity_P2PRegLen);
            if (dVals != null && dVals.Length > 0 && dVals.All(val => Math.Abs(val) < (double)decimal.MaxValue))
                return dVals;
            else
                return new double[] { -1, -1, -1 };
        }



        public async Task<double[]> ReadCustomValue(int StartIndex, int Length)
        {
            ReceiveData = false;
            double[] dVals = (await GetF03FloatValue(StartIndex, Length));
            if (dVals != null && dVals.Length > 0 && dVals.All(val => Math.Abs(val) < (double)decimal.MaxValue))
                return dVals;
            else
                return dVals;
        }

        /// <summary>
        /// 讀取3軸振動能量值
        /// </summary>
        /// <returns>If return double array = [-1,-1,-1],表示接收到的封包有異常(比如長度不足)，解封包時發生錯誤</returns>
        public async Task<double[]> ReadVEValues()
        {
            ReceiveData = false;
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
            ReceiveData = false;
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
            ReceiveData = false;
            double[] dVals = GetF03FloatValue(Register.RMSValuesRegStartIndex, Register.RMSValuesRegLen).Result;
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
            ReceiveData = false;
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
            ReceiveData = false;
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
            ReceiveData = false;
            int ID_int = -1;
            if (Connection_Type == CONNECTION_TYPE.TCP)
            {
                modbusClient_TCP.UnitIdentifier = 0xf0;
                ID_int = TCPReadHoldingRegister(Register.IDRegIndex, 1, "240").Result[1];
                modbusClient_TCP.UnitIdentifier = (byte)ID_int;
            }
            else
                ID_int = RTUReadHoldingRegister(Register.IDRegIndex, 1, "240").Result[1];
            return ID_int + "";
        }
        public int GetCurrentMeasureRange()
        {
            ReceiveData = false;
            int[] ints = null;
            if (Connection_Type == CONNECTION_TYPE.TCP)
                ints = TCPReadHoldingRegister(Register.RangeRegStart - 1, 4, SlaveID).Result;
            else
                ints = RTUReadHoldingRegister(Register.RangeRegStart - 1, 4, SlaveID).Result;
            if (ints == null)
            {
                throw new Exception("Measure Range Read Failure");
            }
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
        internal ManualResetEvent mre_data_reach_done_ctl = new ManualResetEvent(false);
        private async Task WaitDataReachDone()
        {
            mre_data_reach_done_ctl.WaitOne();
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
            ReceiveData = false;
            modbusClient_TCP.WriteSingleRegister(Register.BaudRateSetRegIndex, baud == 115200 ? 1 : 0);
            BaudRate = ReceiveData ? baud : BaudRate;
            modbusClient_TCP.BuferClear();
            return ReceiveData;
        }
        /// <summary>
        /// 版本號查詢 
        /// </summary>
        public string GetVersion()
        {
            var version = "err_err_err";
            ReceiveData = false;
            byte[] byteVals = new byte[4] { 0x31, 0x2E, 0x30, 0x36 };
            if (!IsTest)
            {
                int[] intVals = null;
                if (Connection_Type == CONNECTION_TYPE.TCP)
                    intVals = TCPReadHoldingRegister(240, 2, SlaveID).Result;
                else
                {
                    intVals = RTUReadHoldingRegister(240, 2, SlaveID).Result;
                }
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
                ReceiveData = false;
                //modbusClient.UnitIdentifier = 0xF0;
                modbusClient_TCP.WriteSingleRegister(Register.IDRegIndex, ID);

                if (ReceiveData)
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
                if (LastCommandTime.AddSeconds(3) > DateTime.Now)
                {
                    Thread.Sleep(100);
                }
                LastCommandTime = DateTime.Now;
                SerialPortManager.SendWriteSingleRegisterRequest(SlaveID, ComName, Register.IDRegIndex, ID);
            }
        }
        /// <summary>
        /// 設定量測範圍
        /// </summary>
        /// <param name="Range"></param>
        public void MeasureRangeSet(int Range)
        {
            IsResultLoadOK = false;
            ReceiveData = false;
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
                TCPSocketManager.SendWriteSingleRegisterRequest(SlaveID, this.IP, this.Port, Register.RangeRegStart, valwrite);
            else
            {
                if (LastCommandTime.AddSeconds(3) > DateTime.Now)
                {
                    Thread.Sleep(100);
                }
                LastCommandTime = DateTime.Now;
                SerialPortManager.SendWriteSingleRegisterRequest(SlaveID, ComName, Register.RangeRegStart, valwrite);
            }

            while (!IsResultLoadOK)
            {
                Thread.Sleep(1);
            }
            IsResultLoadOK = false;
        }

        /// <summary>
        /// 設定取樣率
        /// </summary>
        /// <param name="samplingRate"></param>
        public void SetSamplingRate(int samplingRate)
        {
            ReceiveData = false;
            if (Connection_Type == CONNECTION_TYPE.TCP)
                TCPSocketManager.SendWriteSingleRegisterRequest(SlaveID, this.IP, this.Port, Register.SamplingRateReg, samplingRate);
            else
                if (LastCommandTime.AddSeconds(3) > DateTime.Now)
                {
                    Thread.Sleep(100);
                }
                LastCommandTime = DateTime.Now;
                SerialPortManager.SendWriteSingleRegisterRequest(SlaveID, ComName, Register.SamplingRateReg, samplingRate);
        }

        /// <summary>
        /// 取得取樣率
        /// </summary>
        /// <returns></returns>
        public int GetSamplingRate(bool SandBoxTest = false)
        {
            if (SandBoxTest)
            {
                byte[] testData = BitConverter.GetBytes(5000);
                testData = testData.Reverse().ToArray();
                return (testData[2] * 16 * 16) + testData[3];
            }

            ReceiveData = false;
            int[] intVals = null;
            if (Connection_Type == CONNECTION_TYPE.TCP)
                intVals = TCPReadHoldingRegister(Register.SamplingRateReg, 1, SlaveID).Result;
            else
            {
                intVals = RTUReadHoldingRegister(Register.SamplingRateReg, 1, SlaveID).Result;
            }
            if (intVals == null)
                return -1;
            if (intVals.Length != 2)
                return -1;
            int SamplingRate = (intVals[0] * 16 * 16) + intVals[1];
            return SamplingRate;
        }


        int[] Response = null;
        public int DelayTime = 0;
        bool IsResultLoadOK = false;

        public void GetRequestResult(int[] Response, int delayTime)
        {
            this.Response = Response;
            this.DelayTime = delayTime;
            IsResultLoadOK = true;
        }

        private async Task<int[]> TCPReadHoldingRegister(int StartAddress, int DataLength, string SlaveID)
        {
            IsResultLoadOK = false;
            var Req = TCPSocketManager.SendReadHoldingRegistersRequest(SlaveID, this.IP, this.Port, StartAddress, DataLength);

            while (!IsResultLoadOK)
            {
                Thread.Sleep(1);
            }
            IsResultLoadOK = false;
            return this.Response;
        }

        internal async Task<int[]> RTUReadHoldingRegister(int start, int len, string SlaveID)
        {
            if (LastCommandTime.AddSeconds(3)>DateTime.Now)
            {
                Thread.Sleep(100);
            }
            LastCommandTime = DateTime.Now;
            IsResultLoadOK = false;
            API.Modbus.Request req = SerialPortManager.SendReadHoldingRegistersRequest(SlaveID, ComName, start, len);
            Stopwatch sw = new Stopwatch();
            sw.Start();

            while (!IsResultLoadOK)
            {
                Thread.Sleep(1);
            }
            IsResultLoadOK = false;
            return this.Response;

            //await Task.Run(() =>
            //{
            //    while ((req_final = modbus_cli.ReadHoldingResults.ToArray().FirstOrDefault(i => i != null && i.key == req.key)) == null)
            //    {
            //        Thread.Sleep(1);
            //        //Console.WriteLine($"[{SlaveID}] Wait response");
            //    }
            //});


            //int[] values = req_final.ReadHoldingRegisterData;
            //try
            //{
            //    lock (modbus_cli.ReadHoldingResults)
            //    {
            //        int index = modbus_cli.ReadHoldingResults.FindIndex(r => r == req_final);
            //        if (index != -1)
            //            modbus_cli.ReadHoldingResults.RemoveAt(index);
            //    }
            //    return values;
            //}
            //catch (Exception ex)
            //{
            //    throw ex;
            //}
        }

        /// <summary>
        /// 下達F03指令並轉成浮點數回傳
        /// </summary>
        /// <param name="start"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        internal async Task<double[]> GetF03FloatValue(int start, int len)
        {
            int[] values = null;
            if (Connection_Type == CONNECTION_TYPE.TCP)
            {
                if (!Connected)
                {
                    return null;
                }
                Stopwatch sw = new Stopwatch();
                sw.Start();
                values = await TCPReadHoldingRegister(start, len, SlaveID);
                sw.Stop();
                Console.WriteLine($"[TCP] ReadHoldingRegisters Time spend:{sw.ElapsedMilliseconds} ms");
            }
            else
            {
                values = await RTUReadHoldingRegister(start, len, SlaveID);
            }
            if (values == null)
            {
                return null;
            }
            Thread.Sleep(1);
            return values.ToIEEE754FloatAry();
        }

    }

    public static class Extension
    {
        internal static double[] ToIEEE754FloatAry(this int[] intAry)
        {
            if (intAry == null)
                return null;
            List<double> valuesList = new List<double>();
            if (intAry.Length == 1)
            {
                if (intAry[0] == -1)
                {
                    return new double[1] { -1 };
                }
                else if (intAry[0] == -2)
                {
                    return new double[1] { -2 };
                }
            }
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
