using System;

namespace gpm_vibration_module_api.Model.VibSensorParamSetting
{
    /// <summary>
    /// 一般版本感測器使用的設定物件
    /// </summary>
    public class ModuleSetting_GEN : ModuleSetting_HighClassVersion
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ModuleSetting_GEN()
        {
            //When use low sampling mode and data length set > 8192 , last index value must set 0x20 or higher.
            base._SettingBytes = new byte[8] { 0x00, 0x00, 0x9F, 0x00, 0x00, 0x00, 0x00, 0x20 };
        }

        /// <summary>
        /// 在資料長度設定下的封包總數
        /// </summary>
        public override int PackageTotalLen
        {
            get
            {
                if (_Mode == DAQMode.High_Sampling)
                    return 3072;
                else
                    return 3072 * Ratio;
            }
        }
        /// <summary>
        /// 要送給控制器的指令封包(包含頭(0x53)、尾(0x0d 0x0a))
        /// </summary>
        internal override byte[] SettingBytesWithHead
        {
            get
            {
                byte[] bytes = new byte[11];
                bytes[0] = 0x53;
                bytes[9] = 0x0d;
                bytes[10] = 0x0a;
                Array.Copy(_SettingBytes, 0, bytes, 1, 8);
                return bytes;
            }
        }

        /// <summary>
        /// 單軸數據長度設定
        /// </summary>
        public override int DataLength
        {
            get { return base.DataLength; }
            set { base.DataLength = value; }
        }

        /// <summary>
        /// 以512為基底，取得單軸數據長度是512的幾倍
        /// </summary>
        private int Ratio
        {
            get
            {
                return _DataLength / 512;
            }
        }
        /// <summary>
        /// 根據設定值更新參數陣列
        /// </summary>
        internal override void UpdateSettingBytes()
        {
            if (_Mode == DAQMode.Low_Sampling)
            {
                _SettingBytes[1] = (byte)Ratio;
            }
            else
            {
                _SettingBytes[1] = 0x00;
            }
            //量測範圍
            _SettingBytes[3] = _mEASURE_RANGE.ToGENByte();
        }
    }
}
