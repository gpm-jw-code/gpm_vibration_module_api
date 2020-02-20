using System;
using System.Collections.Generic;
using System.Text;

namespace gpm_vibration_module_api.Module
{
    internal class clsModuleSettings
    {
        public byte[] ByteAryOfParameters = new byte[8] { 0x01, 0x00, 0x9F, 0x00, 0x00, 0x00, 0x00, 0x00 };
        private clsEnum.Module_Setting_Enum.DataLength pDataLength= clsEnum.Module_Setting_Enum.DataLength.x1;
        private clsEnum.Module_Setting_Enum.MeasureRange pMeasureRange = clsEnum.Module_Setting_Enum.MeasureRange.MR_2G;
        private clsEnum.Module_Setting_Enum.ODR pODR = clsEnum.Module_Setting_Enum.ODR._9F;
        private clsEnum.Module_Setting_Enum.SensorType pSensorType = clsEnum.Module_Setting_Enum.SensorType.Genernal;
        private bool IsWIFIControllUsingHighSpeedSensor = false;

        public clsEnum.Module_Setting_Enum.SensorType SensorType
        {
            get { return pSensorType; }
            set
            {
                byte byteval = 0x00;
                switch (value)
                {
                    case clsEnum.Module_Setting_Enum.SensorType.Genernal:
                        byteval = 0x01;
                        break;
                    case clsEnum.Module_Setting_Enum.SensorType.High:
                        byteval = 0x02;
                        break;
                    default:
                        break;
                }
                ByteAryOfParameters[0] = byteval;
                pSensorType = value;
            }
        }

        public clsEnum.Module_Setting_Enum.DataLength DataLength
        {
            get { return pDataLength; }
            set
            {
                byte byteval = 0x00;
                switch (value)
                {
                    case clsEnum.Module_Setting_Enum.DataLength.x1:
                        byteval = 0x00;
                        break;
                    case clsEnum.Module_Setting_Enum.DataLength.x2:
                        byteval = 0x01;
                        break;
                    case clsEnum.Module_Setting_Enum.DataLength.x4:
                        byteval = 0x02;
                        break;
                    case clsEnum.Module_Setting_Enum.DataLength.x8:
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
        public clsEnum.Module_Setting_Enum.MeasureRange MeasureRange
        {
            get { return pMeasureRange; }
            set
            {
                byte byteval = 0x00;
                switch (value)
                {
                    case clsEnum.Module_Setting_Enum.MeasureRange.MR_2G:
                        byteval = 0x00;
                        break;
                    case clsEnum.Module_Setting_Enum.MeasureRange.MR_4G:
                        byteval = 0x10;
                        break;
                    case clsEnum.Module_Setting_Enum.MeasureRange.MR_8G:
                        byteval = 0x20;
                        break;
                    case clsEnum.Module_Setting_Enum.MeasureRange.MR_16G:
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
