using System;
using System.Collections.Generic;
using static gpm_vibration_module_api.clsEnum.Module_Setting_Enum;

namespace gpm_vibration_module_api.Model.VibSensorParamSetting
{
    /// <summary>
    /// 485感測模組使用的設定物件
    /// </summary>
    public class ModuleSetting_485 : ModuleSetting_HighClassVersion
    {

        public ModuleSetting_485()
        {
            base._SettingBytes = new byte[6] { 0x00, 0x06, 0x00, 0x00, 0x00, 0x00 };
            base.Mode = DAQMode.High_Sampling;
        }
        public override int PackageTotalLen
        {
            get
            {
                if (_Mode == DAQMode.High_Sampling)
                    return 3072 + 3; //包含ID(Head) 跟 oxod oxoa(Tail)
                else
                    return 3072 * (_DataLength / 512); //待確認?
            }
        }
        internal override byte[] SettingBytesWithHead
        {
            get
            {
                byte[] cmd = _SettingBytes;
                var crc = BitConverter.GetBytes(calculateCRC(cmd, 6, 0));
                List<byte> cmdlist = new List<byte>();
                cmdlist.AddRange(cmd);
                cmdlist.AddRange(crc);
                return cmdlist.ToArray();
            }
        }
        public override int DataLength
        {
            get
            {
                return base.DataLength;
            }
            set
            {
                settingItem = SettingItem.SetDataLen;
                base.DataLength = value;
            }
        }

        public override MEASURE_RANGE mEASURE_RANGE
        {
            get
            {
                return base.mEASURE_RANGE;
            }
            set
            {
                settingItem = SettingItem.SetMeasureRange;
                base.mEASURE_RANGE = value;
            }
        }
        public override DAQMode Mode
        {
            get
            {
                return base.Mode;
            }
            set
            {
                settingItem = SettingItem.SetDaqMode;
                base.Mode = value;
            }
        }
        internal override void UpdateSettingBytes()
        {
            _SettingBytes[0] = SlaveID;
            _SettingBytes[1] = 0x06; //function code
            _SettingBytes[2] = 0x00;
            switch (settingItem)
            {
                case SettingItem.SetDaqMode:
                    _SettingBytes[3] = 0x10; _SettingBytes[4] = 0;
                    if (_Mode == DAQMode.Low_Sampling)
                    {
                        int _ratio = _DataLength / 512;  //倍數
                        _SettingBytes[5] = (byte)_ratio;
                    }
                    else
                        _SettingBytes[5] = 0x00;
                    break;
                case SettingItem.SetDataLen:
                    _SettingBytes[3] = 0x10; _SettingBytes[4] = 0;
                    if (_Mode == DAQMode.Low_Sampling)
                    {
                        int _ratio = _DataLength / 512;  //倍數
                        _SettingBytes[5] = (byte)_ratio;
                    }
                    else
                        _SettingBytes[5] = 0x00;
                    break;
                case SettingItem.SetMeasureRange:
                    _SettingBytes[3] = 0x11;
                    _SettingBytes[4] = 0x9F;
                    _SettingBytes[5] = _mEASURE_RANGE.ToGENByte();//量測範圍
                    break;
                case SettingItem.NotSpecify:
                    break;
                default:
                    break;
            }
        }
    }
}
