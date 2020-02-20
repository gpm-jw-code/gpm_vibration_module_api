using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace gpm_vibration_module_api
{

    /// <summary>
    /// 振動感測模組底層控制類別
    /// </summary>
    internal class clsModuleBase
    {
        public Module.clsModuleSettings moduleSettings = new Module.clsModuleSettings();
        private ManualResetEvent pausesignal;
        private bool IsPauseReady = true;
        internal bool IsOldFWUsing = false;
        public clsModuleBase()
        {
            pausesignal = new ManualResetEvent(true);
        }

        public Socket ModuleSocket { get; internal set; }
        /// <summary>
        /// 連線
        /// </summary>
        public int Connect(string ModuleIP, int ModulePort)
        {
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(ModuleIP), ModulePort);
                ModuleSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                ModuleSocket.Connect(remoteEP);
                if (ModuleSocket.Connected)
                    return 0;
                else
                    return Convert.ToInt32(clsErrorCode.Error.ConnectFail);
            }
            catch (SocketException exp)
            {
                return Convert.ToInt32(clsErrorCode.Error.ConnectFail);
            }
        }
        /// <summary>
        /// 斷線,釋放資源
        /// </summary>
        /// <returns></returns>
        public int Disconnect()
        {
            try
            {
                ModuleSocket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception)
            {
            }
            try
            {
                ModuleSocket.Close();
            }
            catch (Exception)
            {

            }
            return 0;
        }

        /// <summary>
        /// 寫入參數至控制器
        /// </summary>
        /// <param name="Parameters"></param>
        /// <returns></returns>
        public byte[] WriteParameterToController(byte[] Parameters, int returnBytes)
        {
            SocketBufferClear();
            byte[] ToWrite = new byte[11];
            ToWrite[0] = 0x53;
            ToWrite[9] = 0x0d;
            ToWrite[10] = 0x0a;
            Array.Copy(Parameters, 0, ToWrite, 1, Parameters.Length);
            return SendCommand(ToWrite, returnBytes);
        }
        /// <summary>
        /// 寫入參數至控制器(function)
        /// </summary>
        /// <param name="sensorType"></param>
        /// <param name="dataLength"></param>
        /// <param name="measureRange"></param>
        /// <param name="oDR"></param>
        /// <returns></returns>
        public byte[] WriteParameterToController(clsEnum.Module_Setting_Enum.SensorType? sensorType,
            clsEnum.Module_Setting_Enum.DataLength? dataLength,
            clsEnum.Module_Setting_Enum.MeasureRange? measureRange,
            clsEnum.Module_Setting_Enum.ODR? oDR)
        {
            var returnBytes = moduleSettings.SensorType == clsEnum.Module_Setting_Enum.SensorType.High | sensorType == clsEnum.Module_Setting_Enum.SensorType.High ? 1 : 8;
            if (moduleSettings.WifiControllUseHighSppedSensor)
                returnBytes = 8;
            moduleSettings.SensorType = sensorType != null ? (clsEnum.Module_Setting_Enum.SensorType)sensorType : moduleSettings.SensorType;
            moduleSettings.DataLength = dataLength != null ? (clsEnum.Module_Setting_Enum.DataLength)dataLength : moduleSettings.DataLength;
            moduleSettings.MeasureRange = measureRange != null ? (clsEnum.Module_Setting_Enum.MeasureRange)measureRange : moduleSettings.MeasureRange;
            moduleSettings.ODR = oDR != null ? (clsEnum.Module_Setting_Enum.ODR)oDR : moduleSettings.ODR;
            var ParamReturn = WriteParameterToController(moduleSettings.ByteAryOfParameters, returnBytes);
            DefineSettingByParameters(ParamReturn);
            return moduleSettings.ByteAryOfParameters;
        }

        private void DefineSettingByParameters(byte[] Parameters)
        {
            var ParametersToDefine = (Parameters.Length != 8 | Parameters[0] != 0x02) ? new byte[] { 0x01, 0x00, 0x9f, 0x00, 0x00, 0x00, 0x00, 0x00 } : Parameters;
            if (Parameters.Length == 1 && Parameters[0] == 0x02)
                return;
            ParametersToDefine[0] = Parameters[0];
            var TypeByte = ParametersToDefine[0];
            var DataLengthByte = ParametersToDefine[1];
            var ODRByte = ParametersToDefine[2];
            var MeasureRangeByte = ParametersToDefine[3];

            switch (TypeByte)
            {
                case 0x01:
                    moduleSettings.SensorType = clsEnum.Module_Setting_Enum.SensorType.Genernal;
                    break;
                case 0x02:
                    moduleSettings.SensorType = clsEnum.Module_Setting_Enum.SensorType.High;
                    break;
                default:
                    moduleSettings.SensorType = clsEnum.Module_Setting_Enum.SensorType.Genernal;
                    break;
            }

            switch (DataLengthByte)
            {
                case 0x00:
                    moduleSettings.DataLength = clsEnum.Module_Setting_Enum.DataLength.x1;
                    break;
                case 0x01:
                    moduleSettings.DataLength = clsEnum.Module_Setting_Enum.DataLength.x2;
                    break;
                case 0x02:
                    moduleSettings.DataLength = clsEnum.Module_Setting_Enum.DataLength.x4;
                    break;
                case 0x03:
                    moduleSettings.DataLength = clsEnum.Module_Setting_Enum.DataLength.x8;
                    break;
                default:
                    moduleSettings.DataLength = clsEnum.Module_Setting_Enum.DataLength.x1;
                    break;
            }

            switch (ODRByte)
            {
                case 0x9F:
                    moduleSettings.ODR = clsEnum.Module_Setting_Enum.ODR._9F;
                    break;
                case 0x87:
                    moduleSettings.ODR = clsEnum.Module_Setting_Enum.ODR._87;
                    break;
                default:
                    moduleSettings.ODR = clsEnum.Module_Setting_Enum.ODR._9F;
                    break;
            }

            switch (MeasureRangeByte)
            {
                case 0x00:
                    moduleSettings.MeasureRange = clsEnum.Module_Setting_Enum.MeasureRange.MR_2G;
                    break;
                case 0x10:
                    moduleSettings.MeasureRange = clsEnum.Module_Setting_Enum.MeasureRange.MR_4G;
                    break;
                case 0x20:
                    moduleSettings.MeasureRange = clsEnum.Module_Setting_Enum.MeasureRange.MR_8G;
                    break;
                case 0x30:
                    moduleSettings.MeasureRange = clsEnum.Module_Setting_Enum.MeasureRange.MR_16G;
                    break;
                default:
                    moduleSettings.MeasureRange = clsEnum.Module_Setting_Enum.MeasureRange.MR_2G;
                    break;
            }
        }
        private void WaitPause()
        {
            pausesignal.Reset();
            while (IsPauseReady == false)
            {
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// 發送指令至控制器
        /// </summary>
        /// <param name="Timespend">傳回耗時</param>
        /// <returns></returns>

        private List<byte> AccDataBuffer = new List<byte>();

        private class SocketState
        {
            public Socket workSocket = null;
            public int BufferSize = 256;
            public byte[] buffer;
            public StringBuilder sb = new StringBuilder();

        }

        internal byte[] GetAccData_HighSpeedWay(out long timespend)
        {
            try
            {
                WaitForBufferRecieveDone = new ManualResetEvent(false);
                AccDataBuffer.Clear();
                SocketBufferClear();
                var cmdbytes = Encoding.ASCII.GetBytes(clsEnum.ControllerCommand.READVALUE + "\r\n");
                ModuleSocket.Send(cmdbytes, 0, cmdbytes.Length, SocketFlags.None);
                var Datalength = Convert.ToInt32(moduleSettings.DataLength) * 6;
                byte[] Datas = new byte[Datalength];
                var st_time = DateTime.Now;
                SocketState state = new SocketState() { buffer = new byte[Datalength], workSocket = ModuleSocket, BufferSize = Datalength };
                ModuleSocket.BeginReceive(state.buffer, 0, state.BufferSize, 0, new AsyncCallback(receiveCallBack), state);
                WaitForBufferRecieveDone.WaitOne();
                var ed_time = DateTime.Now;
                timespend = (ed_time - st_time).Ticks / 10000; //1 tick = 100 nanosecond  = 0.0001 毫秒
                return AccDataBuffer.ToArray();
            }
            catch (Exception exp)
            {
                timespend = -1;
                AccDataBuffer.Clear();
                WaitForBufferRecieveDone.Set();
                return new byte[0];
            }
        }
        private ManualResetEvent WaitForBufferRecieveDone;
        private void receiveCallBack(IAsyncResult ar)
        {
            try
            {
                SocketState state = (SocketState)ar.AsyncState;
                var client = state.workSocket;
                int bytesRead = client.EndReceive(ar);
                if (bytesRead > 0)
                {
                    var rev = new byte[bytesRead];
                    Array.Copy(state.buffer, 0, rev, 0, bytesRead);
                    AccDataBuffer.AddRange(rev);
                    if (AccDataBuffer.Count == state.BufferSize)
                    {
                        WaitForBufferRecieveDone.Set();
                    }
                    else
                        client.BeginReceive(state.buffer, 0, state.BufferSize, 0,
                            new AsyncCallback(receiveCallBack), state);
                }
                else
                {

                }

            }
            catch
            {
                AccDataBuffer.Clear();
                WaitForBufferRecieveDone.Set();
            }
            //            AccDataBuffer.AddRange(state.buffer);
        }

        /// <summary>
        /// 發送任意bytes給控制器
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="ExpectRetrunSize"></param>
        /// <returns></returns>
        private byte[] SendCommand(byte[] Data, int ExpectRetrunSize)
        {
            byte[] returnData = new byte[ExpectRetrunSize];
            try
            {
                SocketBufferClear();
                ModuleSocket.Send(Data, 0, Data.Length, SocketFlags.None);
                int RecieveByteNum = 0;
                int timespend = 0;
                while (RecieveByteNum < ExpectRetrunSize)
                {
                    timespend++;
                    if (timespend > 5000)
                        return new byte[0];
                    int avaliable = ModuleSocket.Available;
                    ModuleSocket.Receive(returnData, RecieveByteNum, avaliable, 0);
                    RecieveByteNum += avaliable;
                }
                return returnData;
            }
            catch (SocketException exp)
            {
                return returnData;
            }
        }

        /// <summary>
        /// 清空socket buffer
        /// </summary>
        private void SocketBufferClear()
        {
            try
            {
                if (ModuleSocket == null)
                    return;
                if (ModuleSocket.Available != 0)
                {
                    var size = ModuleSocket.Available;
                    byte[] buffer = new byte[size];
                    ModuleSocket.Receive(buffer, 0, size, SocketFlags.None);
                }
            }
            catch
            {

            }
        }


    }
}
