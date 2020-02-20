using System;
using System.Collections.Generic;
using System.Text;

namespace gpm_vibration_module_api
{
    /// <summary>
    /// For User using.
    /// </summary>
    public class GPMModuleAPI
    {
        /// <summary>
        /// 控制器底層控制
        /// </summary>
        private clsModuleBase module_base = new clsModuleBase();
        public GPMModuleAPI()
        {
        }

        /// <summary>
        /// 與控制器進行連線
        /// </summary>
        /// <param name="IP">控制器IP</param>
        /// <param name="Port">控制器Port</param>
        /// <returns></returns>
        public int Connect(string IP, int Port)
        {
            return module_base.Connect(IP, Port);
        }
        /// <summary>
        /// 斷開與控制器的連線
        /// </summary>
        public void Disconnect()
        {
            module_base.Disconnect();
        }

        /// <summary>
        /// 設定/取得量測範圍
        /// </summary>
        public clsEnum.Module_Setting_Enum.MeasureRange MeasureRange
        {
            set
            {
                module_base.WriteParameterToController(null, null, value, null);
            }
            get
            {
                return module_base.moduleSettings.MeasureRange;
            }
        }
        /// <summary>
        /// 設定/取得封包資料長度
        /// </summary>
        public clsEnum.Module_Setting_Enum.DataLength DataLength
        {
            set
            {
                module_base.WriteParameterToController(null, value, null, null);
            }
            get
            {
                return module_base.moduleSettings.DataLength;
            }
        }

        /// <summary>
        /// 設定感測器類型
        /// </summary>
        public clsEnum.Module_Setting_Enum.SensorType SensorType
        {
            set
            {
                module_base.WriteParameterToController(value, null, null, null);
            }
            get
            {
                return module_base.moduleSettings.SensorType;
            }
        }
        /// <summary>
        /// 設定加速規濾波設定
        /// </summary>
        public clsEnum.Module_Setting_Enum.ODR ODR
        {
            set
            {
                module_base.WriteParameterToController(null, null, null,, value);
            }
            get
            {
                return module_base.moduleSettings.ODR;
            }
        }

    }
}
