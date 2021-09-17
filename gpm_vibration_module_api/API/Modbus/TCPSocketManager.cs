using gpm_vibration_module_api.Modbus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gpm_vibration_module_api.API.Modbus
{
    public class TCPSocketManager
    {
        public static Dictionary<string, ModbusClient> DictModbusTCP = new Dictionary<string, ModbusClient>();
        public static Dictionary<string, Queue<Request>> Dict_TCPRequest = new Dictionary<string, Queue<Request>>();
        public static Dictionary<string, Dictionary<string, GPMModbusAPI>> Dict_IP_dict_ID_ModbusModule = new Dictionary<string, Dictionary<string, GPMModbusAPI>>();



        /// <summary>
        /// 註冊一個Comport來使用
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="BaudRate"></param>
        /// <param name="SlaveID"></param>
        /// <returns></returns>
        public static ModbusClient TCPSocketRegist(string IP, int Port, string SlaveID, GPMModbusAPI APIObject)
        {
            if (!DictModbusTCP.ContainsKey(IP))
            {
                DictModbusTCP.Add(IP, new ModbusClient());
                Dict_TCPRequest.Add(IP, new Queue<Request>());
                Dict_IP_dict_ID_ModbusModule.Add(IP, new Dictionary<string, GPMModbusAPI>());
            }
            if (!Dict_IP_dict_ID_ModbusModule[IP].ContainsKey(SlaveID))
            {
                Dict_IP_dict_ID_ModbusModule[IP].Add(SlaveID, APIObject);
            }

            ModbusClient mdc = DictModbusTCP[IP];
            mdc.connect_type = ModbusClient.CONNECTION_TYPE.TCP;
            if (!mdc.SlaveIDList.Contains(SlaveID))
                mdc.SlaveIDList.Add(SlaveID);
            if (mdc.Connected)
                return mdc;
            mdc.IPAddress = IP;
            mdc.Port = Port;
            mdc.Connect();
            Task.Run(() => QueueRequestHandle(IP));
            return mdc;
        }

        public static void QueueRequestHandle(string IP)
        {
            var ModbusClientModule = DictModbusTCP[IP];
            var RequestQueue = Dict_TCPRequest[IP];
            var Dict_ModbusModule = Dict_IP_dict_ID_ModbusModule[IP];
            int TimeoutCount = 0;
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
                            ModbusClientModule.UnitIdentifier = UnitIdentifier;
                            int[] Result = ModbusClientModule.ReadHoldingRegisters(CurrentRequest.StartIndex, CurrentRequest.ValueOrLength);

                            if (Result == null)
                                TimeoutCount += 1;
                            else
                                TimeoutCount = 0;
                            if (TimeoutCount>5)
                            {
                              //連線異常處理
                            }

                            Dict_ModbusModule[CurrentRequest.str_ID].GetRequestResult(Result);
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

        internal static Request SendReadHoldingRegistersRequest(string slaveID, string IP, int RegIndex, int length)
        {
            var req = new Request(slaveID, Request.REQUEST.READHOLDING, RegIndex, length, DateTime.Now.ToString("yyyyMMddHHmmssffff"));
            //RTUClient.AddRequest(req);
            var TargetRequestQueue = Dict_TCPRequest[IP];
            lock (TargetRequestQueue)
            {
                TargetRequestQueue.Enqueue(req);
            }
            return req;
        }

        internal static void SendWriteSingleRegisterRequest(string slaveID, string IP, int RegIndex, int value)
        {
            ModbusClient TcpClient = DictModbusTCP[IP];
            var req = new ModbusClient.Request(byte.Parse(slaveID), ModbusClient.Request.REQUEST.WRITESIGNLE, RegIndex, value, DateTime.Now.ToString("yyyyMMddHHmmssffff"));
            TcpClient.AddRequest(req);

        }
    }
}
