using gpm_vibration_module_api.Modbus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public static Dictionary<string, Queue<Request>> Dict_RTURequest = new Dictionary<string, Queue<Request>>();

        public static Dictionary<string, Dictionary<string, GPMModbusAPI>> Dict_Com_dict_ID_ModbusModule = new Dictionary<string, Dictionary<string, GPMModbusAPI>>();


        /// <summary>
        /// 註冊一個Comport來使用
        /// </summary>
        /// <param name="ComName"></param>
        /// <param name="BaudRate"></param>
        /// <param name="SlaveID"></param>
        /// <returns></returns>
        public static ModbusClient SerialPortRegist(string ComName, int BaudRate, string SlaveID, Parity parity, StopBits stopBits, GPMModbusAPI APIObject)
        {
            if (!DictModbusRTU.ContainsKey(ComName))
            {
                DictModbusRTU.Add(ComName, new ModbusClient());
                Dict_RTURequest.Add(ComName, new Queue<Request>());
                Dict_Com_dict_ID_ModbusModule.Add(ComName, new Dictionary<string, GPMModbusAPI>());
            }
            if (!Dict_Com_dict_ID_ModbusModule[ComName].ContainsKey(SlaveID))
            {
                Dict_Com_dict_ID_ModbusModule[ComName].Add(SlaveID, APIObject);
            }

            ModbusClient mdc = DictModbusRTU[ComName];
            mdc.connect_type = ModbusClient.CONNECTION_TYPE.RTU;
            if (!mdc.SlaveIDList.Contains(SlaveID))
                mdc.SlaveIDList.Add(SlaveID);
            if (mdc.Connected)
                return mdc;
            mdc.SerialPort = ComName;
            mdc.Baudrate = BaudRate;
            mdc.Parity = parity;
            mdc.StopBits = stopBits;
            mdc.Connect();
            Task.Run(() => QueueRequestHandle(ComName));
            return mdc;
        }

        public static void SerialPortCancelRegist(string ComName,string SlaveID)
        {
            Dict_Com_dict_ID_ModbusModule[ComName].Remove(SlaveID);
            if (Dict_Com_dict_ID_ModbusModule[ComName].Count == 0)
            {
                Dict_Com_dict_ID_ModbusModule.Remove(ComName);
                Dict_RTURequest.Remove(ComName);
                DictModbusRTU[ComName].Disconnect();
                DictModbusRTU.Remove(ComName);
            }
        }

        public static void QueueRequestHandle(string ComportName)
        {
            var ModbusClientModule = DictModbusRTU[ComportName];
            var RequestQueue = Dict_RTURequest[ComportName];
            var Dict_ModbusModule = Dict_Com_dict_ID_ModbusModule[ComportName];
            while (true)
            {
                lock (RequestQueue)
                {

                }
                try
                {
                    Thread.Sleep(300);
                    if (RequestQueue.Count != 0)
                    {
                        //Console.WriteLine("待處理柱列:"+RequestQueue.Count);
                        //lock (RequestQueue)
                        //{
                        var CurrentRequest = RequestQueue.Dequeue();
                        if (CurrentRequest == null)
                            continue;
                        var UnitIdentifier = CurrentRequest.SlaveID;
                        if (UnitIdentifier == 3)
                        {

                        }
                        if (CurrentRequest.request == Request.REQUEST.READHOLDING)
                        {
                            Stopwatch sw = new Stopwatch();
                            sw.Start();
                            ModbusClientModule.UnitIdentifier = UnitIdentifier;
                            int[] Result = ModbusClientModule.ReadHoldingRegisters(CurrentRequest.StartIndex, CurrentRequest.ValueOrLength);
                            sw.Stop();
                            Dict_ModbusModule[CurrentRequest.str_ID].GetRequestResult(Result,(int)sw.ElapsedMilliseconds);
                            Console.WriteLine($"[RTU] ReadHoldingRegisters Time spend:{sw.ElapsedMilliseconds} ms");
                        }
                        else
                            ModbusClientModule.WriteSingleRegister(CurrentRequest.StartIndex, CurrentRequest.ValueOrLength);

                        //}
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }
        }

        internal static Request SendReadHoldingRegistersRequest(string slaveID, string ComeName, int RegIndex, int length)
        {
            var req = new Request(slaveID, Request.REQUEST.READHOLDING, RegIndex, length, DateTime.Now.ToString("yyyyMMddHHmmssffff"));
            //RTUClient.AddRequest(req);
            var TargetRequestQueue = Dict_RTURequest[ComeName];
            lock (TargetRequestQueue)
            {
                TargetRequestQueue.Enqueue(req);
            }
            return req;
        }

        internal static void SendWriteSingleRegisterRequest(string slaveID, string ComeName, int RegIndex, int value)
        {
            var req = new Request(slaveID, Request.REQUEST.WRITESIGNLE, RegIndex, value, DateTime.Now.ToString("yyyyMMddHHmmssffff"));
            var TargetRequestQueue = Dict_RTURequest[ComeName];
            lock (TargetRequestQueue)
            {
                TargetRequestQueue.Enqueue(req);
            }
        }
    }

    public class Request
    {
        public Request()
        {

        }
        public Request(string SlaveID, REQUEST request, int StartIndex, int ValueOrLength, string key)
        {
            this.str_ID = SlaveID;
            this.SlaveID = byte.Parse(SlaveID);
            this.request = request;
            this.StartIndex = StartIndex;
            this.ValueOrLength = ValueOrLength;
            this.key = key;
        }
        public enum REQUEST
        {
            READHOLDING, WRITESIGNLE
        }
        public string str_ID;
        public byte SlaveID;
        public readonly REQUEST request = REQUEST.READHOLDING;
        public readonly int StartIndex, ValueOrLength;
        public int[] ReadHoldingRegisterData;
        public bool IsReachDone = false;
        public readonly string key;
    }
}
