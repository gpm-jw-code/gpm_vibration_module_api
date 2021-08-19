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

        internal static int[] ReadHoldingRegisters(string slaveID,string ComeName, int RegIndex, int length)
        {
            ModbusClient RUTClient = DictModbusRTU[ComeName];
            while (RUTClient.IsBusy)
            {
                Thread.Sleep(1);
            }
            RUTClient.UnitIdentifier = byte.Parse(slaveID);
            return RUTClient.ReadHoldingRegisters(RegIndex, length);
        }

        internal static void WriteSingleRegister(string slaveID, string ComeName, int RegIndex, int value)
        {
            ModbusClient RUTClient = DictModbusRTU[ComeName];
            while (RUTClient.IsBusy)
            {
                Thread.Sleep(1);
            }
            RUTClient.UnitIdentifier = byte.Parse(slaveID);
            RUTClient.WriteSingleRegister(RegIndex, value);
        }
    }
}
