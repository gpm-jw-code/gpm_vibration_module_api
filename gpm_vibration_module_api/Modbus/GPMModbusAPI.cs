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
        internal struct Register
        {
            public const int VEValuesRegStart = 0;
            public const int VEValuesRegLen = 6;
            public const int TotalVEValueRegStart = 6;
            public const int TotalVEValueRegLen = 2;
            public const int RMSValuesRegStart = 8;
            public const int RMSValuesRegLen = 6;
            public const int AllValuesRegStart = 0;
            public const int AllValuesRegLen = 14;
            //ID
            public const int IDReg = 20;
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

        public bool Connect(string IP, int Port)
        {
            modbusClient.IPAddress = IP;
            modbusClient.Port = Port;
            modbusClient.SerialPort = null;
            return modbusClient.Connect();
        }
        public bool Connect(string ComPort, string SlaveID, int BaudRate, System.IO.Ports.Parity parity, System.IO.Ports.StopBits StopBits)
        {
            modbusClient.SerialPort = ComPort;
            modbusClient.UnitIdentifier = byte.Parse(SlaveID);
            modbusClient.Baudrate = BaudRate;
            modbusClient.Parity = parity;
            modbusClient.StopBits = StopBits;
            return modbusClient.Connect();
        }

        public double[] ReadVEValues()
        {
            RecieveData = false;
            return F03FloatValue(Register.VEValuesRegStart, Register.VEValuesRegLen);
        }
        public double ReadTotalVEValues()
        {
            RecieveData = false;
            return F03FloatValue(Register.TotalVEValueRegStart, Register.TotalVEValueRegLen)[0];
        }
        public double[] ReadRMSValues()
        {
            RecieveData = false;
            return F03FloatValue(Register.RMSValuesRegStart, Register.RMSValuesRegLen);
        }
        public double[] ReadAllValues()
        {
            RecieveData = false;
            return F03FloatValue(Register.AllValuesRegStart, Register.AllValuesRegLen);
        }

        public string GetSlaveID()
        {
            RecieveData = false;
            var ID_int =  modbusClient.ReadHoldingRegisters(Register.IDReg, 1).First();
            return ID_int.ToString("X2");
        }

        public void SlaveIDSetting(byte ID)
        {
            var oriID = modbusClient.UnitIdentifier;
            RecieveData = false;
            modbusClient.UnitIdentifier = 0xF0;
            modbusClient.WriteSingleRegister(Register.IDReg, ID);
            if(RecieveData)
            {
                modbusClient.UnitIdentifier = ID;
            }
            else
            {
                modbusClient.UnitIdentifier = oriID;
            }
        }

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
            modbusClient.WriteSingleRegister(11, valwrite);
        }
       

        private double[] F03FloatValue(int start, int len)
        {
            int[] values = modbusClient.ReadHoldingRegisters(start, len);
            return values.IEEE753FloatAry(); ;
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
        internal static double[] IEEE753FloatAry(this int[] intAry)
        {
            List<double> valuesList = new List<double>();
            for (int i = 0; i < intAry.Length; i++)
            {
                if (i % 4 == 0)
                    valuesList.Add(ToFloat(intAry[i], intAry[i + 1], intAry[i+2], intAry[i + 3]));
            }
            return valuesList.ToArray();
        }
        public static double ToFloat(int HIGHIntH, int HIGHIntL, int LOWIntH, int LOWIntL)
        {
           var hexstring =  HIGHIntH.ToString("X2") + HIGHIntL.ToString("X2") + LOWIntH.ToString("X2") + LOWIntL.ToString("X2");
            return Hex32toFloat(hexstring);
        }

        static float Hex32toFloat(string Hex32Input)
        {
            double doubleout=0.0;
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
