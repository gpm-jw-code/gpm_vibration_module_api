using System;
using System.Collections.Generic;
using System.Text;

namespace gpm_vibration_module_api.Module
{
    public class clsModuleSettings
    {
        private byte[] byteAryOfParameters  = new byte[] { 0x01 , 0x00, 0x9F, 0x00, 0x00, 0x00, 0x00, 0x00 };
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
        public string ParametersStringType = "1,0,159,0,0,0,0,0";


        private clsEnum.Module_Setting_Enum.DATA_LENGTH pDataLength = clsEnum.Module_Setting_Enum.DATA_LENGTH.x1;
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

        public clsEnum.Module_Setting_Enum.DATA_LENGTH DataLength
        {
            get { return pDataLength; }
            set
            {
                byte byteval = 0x00;
                switch (value)
                {
                    case clsEnum.Module_Setting_Enum.DATA_LENGTH.x1:
                        byteval = 0x00;
                        break;
                    case clsEnum.Module_Setting_Enum.DATA_LENGTH.x2:
                        byteval = 0x01;
                        break;
                    case clsEnum.Module_Setting_Enum.DATA_LENGTH.x4:
                        byteval = 0x02;
                        break;
                    case clsEnum.Module_Setting_Enum.DATA_LENGTH.x8:
                        byteval = 0x03;
                        break;
                    default:
                        break;
                }
                ByteAryOfParameters[1] = byteval;
                pDataLength = value;
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
                        byteval = 0x20;
                        break;
                    case clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_16G:
                        byteval = 0x30;
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
}
