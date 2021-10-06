﻿using gpm_vibration_module_api.Modbus;
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

        public static Dictionary<string, bool> Dict_IsModbusClientRetry = new Dictionary<string, bool>();
        public static bool ConnectionRetry(string IP,string Port, string SlaveID)
        {
            if (Dict_IsModbusClientRetry[IP+"_"+Port] == true)
            {
                return false;
            }

            Dict_IsModbusClientRetry[IP + "_" + Port] = true;

            var TargetModbusClient = DictModbusTCP[IP + "_" + Port];
            bool ConnectResult = TargetModbusClient.Connect();

            foreach (var item in Dict_IP_dict_ID_ModbusModule[IP + "_" + Port].Values)
            {
                item.IsWaitingForTCPReconnectResult = false;
            }
            Dict_IsModbusClientRetry[IP + "_" + Port] = false; ;

            return ConnectResult;
        }


        /// <summary>
        /// 註冊一個TCP Socket來使用
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="Port"></param>
        /// <param name="SlaveID"></param>
        /// <returns></returns>
        public static ModbusClient TCPSocketRegist(string IP, int Port, string SlaveID, GPMModbusAPI APIObject)
        {
            if (!DictModbusTCP.ContainsKey(IP+"_"+Port))
            {
                DictModbusTCP.Add(IP + "_" + Port, new ModbusClient());
                Dict_IsModbusClientRetry.Add(IP + "_" + Port, false);
                Dict_TCPRequest.Add(IP + "_" + Port, new Queue<Request>());
                Dict_IP_dict_ID_ModbusModule.Add(IP + "_" + Port, new Dictionary<string, GPMModbusAPI>());
            }
            if (!Dict_IP_dict_ID_ModbusModule[IP + "_" + Port].ContainsKey(SlaveID))
            {
                Dict_IP_dict_ID_ModbusModule[IP + "_" + Port].Add(SlaveID, APIObject);
            }

            ModbusClient mdc = DictModbusTCP[IP + "_" + Port];
            mdc.connect_type = ModbusClient.CONNECTION_TYPE.TCP;
            if (!mdc.SlaveIDList.Contains(SlaveID))
                mdc.SlaveIDList.Add(SlaveID);
            if (mdc.Connected)
                return mdc;
            mdc.IPAddress = IP;
            mdc.Port = Port;
            mdc.Connect();
            Task.Run(() => QueueRequestHandle(IP + "_" + Port));
            return mdc;
        }

        public static void QueueRequestHandle(string IP_Port)
        {
            var ModbusClientModule = DictModbusTCP[IP_Port];
            var RequestQueue = Dict_TCPRequest[IP_Port];
            var Dict_ModbusModule = Dict_IP_dict_ID_ModbusModule[IP_Port];
            while (true)
            {
                if (Dict_IsModbusClientRetry[IP_Port])
                {
                    Thread.Sleep(1000);
                    continue;
                }
                Request CurrentRequest = null;
                lock (RequestQueue)
                {
                    Thread.Sleep(300);
                    if (RequestQueue.Count == 0)
                        continue;

                    CurrentRequest = RequestQueue.Dequeue();
                    if (CurrentRequest == null)
                        continue;
                }
                try
                {
                    ModbusClientModule.UnitIdentifier = CurrentRequest.SlaveID;
                    if (!ModbusClientModule.Connected)
                    {
                        Dict_ModbusModule[CurrentRequest.str_ID].GetRequestResult(new int[1] { -1 });
                        continue;
                    }
                    if (CurrentRequest.request == Request.REQUEST.READHOLDING)
                    {
                        int[] Result = ModbusClientModule.ReadHoldingRegisters(CurrentRequest.StartIndex, CurrentRequest.ValueOrLength);
                        Dict_ModbusModule[CurrentRequest.str_ID].GetRequestResult(Result);
                    }
                    else
                    {
                        ModbusClientModule.WriteSingleRegister(CurrentRequest.StartIndex, CurrentRequest.ValueOrLength);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        internal static Request SendReadHoldingRegistersRequest(string slaveID, string IP,string Port, int RegIndex, int length)
        {
            var req = new Request(slaveID, Request.REQUEST.READHOLDING, RegIndex, length, DateTime.Now.ToString("yyyyMMddHHmmssffff"));
            //RTUClient.AddRequest(req);
            var TargetRequestQueue = Dict_TCPRequest[IP+"_"+Port];
            lock (TargetRequestQueue)
            {
                TargetRequestQueue.Enqueue(req);
            }
            return req;
        }

        internal static void SendWriteSingleRegisterRequest(string slaveID, string IP, string Port, int RegIndex, int value)
        {
            var req = new Request(slaveID, Request.REQUEST.WRITESIGNLE, RegIndex, value, DateTime.Now.ToString("yyyyMMddHHmmssffff"));
            var TargetRequestQueue = Dict_TCPRequest[IP + "_" + Port];
            lock (TargetRequestQueue)
            {
                TargetRequestQueue.Enqueue(req);
            }
        }
    }
}
