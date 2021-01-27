#define ETH468
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using static gpm_vibration_module_api.ClsModuleBase;

namespace gpm_vibration_module_api.Module
{
    [Serializable]
    public class clsModuleSettings
    {
        [Serializable]
        public class PassFilterParam
        {
            public bool Active { get; set; } = false;
            public double CutOffFreq { get; set; } = 1700;
        }

        public clsModuleSettings()
        {
            lowPassFilter = new PassFilterParam();
        }

        /// <summary>
        /// 控制器回傳的資料封包總長度
        /// </summary>
        public int DataBytesSize = 6000;

        public byte[] DataByteSizeSettingValueBytes
        {
            get
            {
                var n = DataBytesSize / 1536; //倍數
                return BitConverter.GetBytes(n);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        internal int Min_Single_Axis_Sapmle_Num { get; set; } = 100;
        public PassFilterParam lowPassFilter { get; set; } = new PassFilterParam();
        public PassFilterParam highPassFilter { get; set; } = new PassFilterParam();
        public int Packet_Receive_Size
        {
            get
            {
                return SocketState.Packet_Receive_Size;
            }
            set
            {
                SocketState.Packet_Receive_Size = value;
            }
        }

        public int MCU_Delay_tune
        {
            get
            {
                return ClsModuleBase.delay_;
            }
            set
            {
                ClsModuleBase.delay_ = value;
            }
        }
        private bool _IsDaul_MCU_Mode;
        public bool Is_Daul_MCU_Mode
        {
            get { return _IsDaul_MCU_Mode; }
            set
            {
                _IsDaul_MCU_Mode = value;
                if (value)
                    dAQMode = DAQMode.Low_Sampling;
            }
        }


        public DAQMode dAQMode = DAQMode.High_Sampling;

        public double sampling_rate_ = 5000;
#if (ETH468)
        private byte[] byteAryOfParameters = new byte[8] { 0x01, 0x00, 0x9F, 0x00, 0x00, 0x00, 0x00, 0x00 };
#else
         private byte[] byteAryOfParameters = new byte[] { 0x01, 0x01, 0x9F, 0x00, 0x00, 0x00, 0x00, 0x00 };
#endif

        /// <summary>
        /// 額外多收的資料量(單位3072)
        /// </summary>
        public int comp_len = 0;
        public byte[] ByteAryOfParameters
        {
            set
            {
                byteAryOfParameters = value;
                string str = "";
                foreach (var item in value)
                {
                    str += item + ",";
                }
                ParametersStringType = str;
            }
            get
            {
                return byteAryOfParameters;
            }
        }

#if (ETH468)
        public string ParametersStringType = "1,0,159,0,0,0,0,0";

#else
          public string ParametersStringType = "1,0,159,0,0,0,0,0";
#endif

        private int pDataLength = 1;
        private clsEnum.Module_Setting_Enum.MEASURE_RANGE pMeasureRange = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_2G;
        private clsEnum.Module_Setting_Enum.ODR pODR = clsEnum.Module_Setting_Enum.ODR._9F;
        private clsEnum.Module_Setting_Enum.SENSOR_TYPE pSensorType = clsEnum.Module_Setting_Enum.SENSOR_TYPE.Genernal;
        private bool IsWIFIControllUsingHighSpeedSensor = false;

        public clsEnum.Module_Setting_Enum.SENSOR_TYPE SensorType
        {
            get { return pSensorType; }
            set
            {
                byte byteval = 0x00;
                switch (value)
                {
                    case clsEnum.Module_Setting_Enum.SENSOR_TYPE.Genernal:
                        byteval = 0x01;
                        break;
                    case clsEnum.Module_Setting_Enum.SENSOR_TYPE.High:
                        byteval = 0x02;
                        break;
                    default:
                        break;
                }
                var b = ByteAryOfParameters;
                b[0] = byteval;
                ByteAryOfParameters = b;
                pSensorType = value;
            }
        }





        public int DataLength
        {
            get { return pDataLength; }
            set
            {
                if (!Is_Daul_MCU_Mode)
                {
                    ByteAryOfParameters[1] = (byte)(dAQMode == DAQMode.High_Sampling ? 0x00 : value);
                    pDataLength = value;
                }
                else
                {
                    var n = value / 1536;//倍率
                    ByteAryOfParameters[0] = BitConverter.GetBytes(n)[1];
                    ByteAryOfParameters[1] = BitConverter.GetBytes(n)[0];
                    pDataLength = value / 6;
                }
            }
        }
        public clsEnum.Module_Setting_Enum.ODR ODR
        {
            get { return pODR; }
            set
            {
                byte byteval = (byte)value;
                ByteAryOfParameters[2] = byteval;
                pODR = value;
            }
        }
        public clsEnum.Module_Setting_Enum.MEASURE_RANGE MeasureRange
        {
            get { return pMeasureRange; }
            set
            {
                byte byteval = 0x00;
                switch (value)
                {
                    case clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_2G:
                        byteval = 0x00;
                        break;
                    case clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_4G:
                        byteval = 0x10;
                        break;
                    case clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_8G:
                        if (!Is_Daul_MCU_Mode)
                            byteval = 0x20;
                        else
                            byteval = 0xc0;
                        break;

                    case clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_16G:
                        if (!Is_Daul_MCU_Mode)
                            byteval = 0x30;
                        else
                            byteval = 0xC8;
                        break;
                    case clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_32G: //KX134 ONLY
                        byteval = 0xD0;
                        break;
                    case clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_64G: //KX134 ONLY
                        byteval = 0xD8;
                        break;
                }
                ByteAryOfParameters[3] = byteval;
                pMeasureRange = value;

            }
        }

        public bool WifiControllUseHighSppedSensor
        {
            set
            {
                IsWIFIControllUsingHighSpeedSensor = value;
            }
            get
            {
                return IsWIFIControllUsingHighSpeedSensor;
            }
        }
    }

    public static class Extensions
    {
        public static T DeepClone<T>(this T obj)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, obj);
                stream.Position = 0;

                return (T)formatter.Deserialize(stream);
            }
        }
    }

}
