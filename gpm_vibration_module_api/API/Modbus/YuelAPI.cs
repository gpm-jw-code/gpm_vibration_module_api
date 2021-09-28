using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using gpm_vibration_module_api.Modbus;

namespace gpm_vibration_module_api.API.Modbus
{
    /// <summary>
    /// 用來讀取允宥的溫溼度模組 > RS485 MODBUS RTU
    /// </summary>
    public class YuelAPI : GPMModbusAPI
    {
        internal new struct Register
        {
            public const int TemperatureStart = 1024;
            public const int HumidityStart = 1028;
            public const int ModelNameStart = 32; //size:8
            public const int SerialNumberStart = 48;
            public const int FirmwareVersion = 64;
        }

        public double GetTemperature()
        {
            var temperature = GetF03FloatValue(Register.TemperatureStart, 2).Result[0];
            return temperature;
        }
        public double GetHumidity()
        {
            var humidity = GetF03FloatValue(Register.HumidityStart, 4).Result[0];
            return humidity;
        }
        public string GetSerialNumber()
        {
            string serialNumber = "";
            int[] values = RTUReadHoldingRegister(Register.SerialNumberStart, 6, SlaveID).Result;
            serialNumber=Encoding.ASCII.GetString(values.ToByteAry()).Replace("\0","");
            return serialNumber;
        }

        public string GetModelName()
        {
            int[] values = RTUReadHoldingRegister(Register.ModelNameStart, 8, SlaveID).Result;
            string ModelName = Encoding.ASCII.GetString(values.ToByteAry()).Replace("\0", "");
            return ModelName;
        }

        public string GetFWVersion()
        {
            int[] values = RTUReadHoldingRegister(Register.FirmwareVersion, 3, SlaveID).Result;
            string fwVersion = Encoding.ASCII.GetString(values.ToByteAry()).Replace("\0", "");
            return fwVersion;
        }
    }
}
