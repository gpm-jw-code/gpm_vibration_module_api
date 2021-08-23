using gpm_vibration_module_api.Modbus;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gpm_vibration_module_api.API.Modbus
{
    public static class SerialPortManager
    {
        public static Dictionary<string, ModbusClient> DictModbusRTU = new Dictionary<string, ModbusClient>();
        public static ModbusClient OpenRTU(string ComName, int BaudRate, string SlaveID)
        {
            if (!DictModbusRTU.ContainsKey(ComName))
                DictModbusRTU.Add(ComName, new ModbusClient());
            ModbusClient mdc = DictModbusRTU[ComName];
            mdc.connect_type = ModbusClient.CONNECTION_TYPE.RTU;
            if (!mdc.SlaveIDList.Contains(SlaveID))
                mdc.SlaveIDList.Add(SlaveID);
            if (mdc.Connected)
                return mdc;
            mdc.SerialPort = ComName;
            mdc.Baudrate = BaudRate;
            mdc.Parity = Parity.None;
            mdc.StopBits = StopBits.One;
            mdc.Connect();
            return mdc;
        }

        internal static ModbusClient.Request ReadHoldingRegisters(string slaveID,string ComeName, int RegIndex, int length)
        {
            ModbusClient RTUClient = DictModbusRTU[ComeName];
            var req = new ModbusClient.Request(byte.Parse(slaveID), ModbusClient.Request.REQUEST.READHOLDING, RegIndex, length,DateTime.Now.ToString("yyyyMMddHHmmssffff"));
            RTUClient.AddRequest(req);
            return req;
        }

        internal static void WriteSingleRegister(string slaveID, string ComeName, int RegIndex, int value)
        {
            ModbusClient RUTClient = DictModbusRTU[ComeName];
            var req = new ModbusClient.Request(byte.Parse(slaveID), ModbusClient.Request.REQUEST.WRITESIGNLE, RegIndex, value, DateTime.Now.ToString("yyyyMMddHHmmssffff"));
            RUTClient.AddRequest(req);
           
        }
    }
}
