using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace gpm_vibration_module_api
{

    /// <summary>
    /// 對振動感測模組的所有控制類別
    /// </summary>
    public class clsModuleControl
    {
        public Module.clsModuleSettings moduleSettings = new Module.clsModuleSettings();
        public clsModuleControl()
        {

        }

        public Socket ModuleSocket { get; internal set; }
        /// <summary>
        /// 連線
        /// </summary>
        public int Connect(string ModuleIP, int ModulePort)
        {
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(ModuleIP), ModulePort);
            ModuleSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ModuleSocket.Connect(remoteEP);
            if (ModuleSocket.Connected)
                return 0;
            else
                return Convert.ToInt32(clsErrorCode.Error.ConnectFail);
        }
        public int Disconnect()
        {
            try
            {
                ModuleSocket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception)
            {
                throw;
            }
            try
            {
                ModuleSocket.Close();
            }
            catch (Exception)
            {

                throw;
            }
            return 0;
        }

        public void GetAccPacket()
        {

        }

        public byte[] WriteParameterToController(byte[] Parameters)
        {
            SocketBufferClear();
            byte[] ToWrite = new byte[11];
            ToWrite[0] = 0x53;
            ToWrite[9] = 0x0d;
            ToWrite[10] = 0x0a;
            Array.Copy(Parameters,0,ToWrite,1,Parameters.Length);
            return SendCommand(ToWrite, 8);
        }
        public byte[] WriteParameterToController(clsEnum.Module_Setting_Enum.SensorType? sensorType = clsEnum.Module_Setting_Enum.SensorType.Genernal,
            clsEnum.Module_Setting_Enum.DataLength? dataLength = clsEnum.Module_Setting_Enum.DataLength.x1,
            clsEnum.Module_Setting_Enum.MeasureRange? measureRange = clsEnum.Module_Setting_Enum.MeasureRange.MR_2G,
            clsEnum.Module_Setting_Enum.ODR? oDR = clsEnum.Module_Setting_Enum.ODR._9F)
        {
            if (sensorType != null)
                moduleSettings.SensorType = (clsEnum.Module_Setting_Enum.SensorType)sensorType;
            if (dataLength != null)
                moduleSettings.DataLength = (clsEnum.Module_Setting_Enum.DataLength)dataLength;
            if (measureRange != null)
                moduleSettings.MeasureRange = (clsEnum.Module_Setting_Enum.MeasureRange)measureRange;
            if (oDR != null)
                moduleSettings.ODR = (clsEnum.Module_Setting_Enum.ODR)oDR;
           return  WriteParameterToController(moduleSettings.ByteAryOfParameters);
        }


        public byte[] SendCommand(clsEnum.ControllerCommand command, int ExpectRetrunSize)
        {
            SocketBufferClear();
            byte[] returnData = new byte[ExpectRetrunSize];
            var cmdbytes = Encoding.ASCII.GetBytes(command.ToString() + "\r\n");
            ModuleSocket.Send(cmdbytes, 0, cmdbytes.Length, SocketFlags.None);
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


        public byte[] SendCommand(byte[] Data, int ExpectRetrunSize)
        {
            byte[] returnData = new byte[ExpectRetrunSize];
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

        private void SocketBufferClear()
        {
            if(ModuleSocket.Available!=0)
            {
                var size = ModuleSocket.Available;
                byte[] buffer = new byte[size];
                ModuleSocket.Receive(buffer,0,size, SocketFlags.None);
            }
        }
    }
}
