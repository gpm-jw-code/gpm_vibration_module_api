﻿using System;
using System.Linq;
using static gpm_vibration_module_api.clsEnum.Module_Setting_Enum;

namespace gpm_vibration_module_api.Model.VibSensorParamSetting
{
    /// <summary>
    /// 高階版本感測器設定物件
    /// </summary>
    public class ModuleSetting_HighClassVersion
    {
        public int AccDataRevTimeout = 3000;
        public byte SlaveID = 0x00;


        public byte[] READParamCmdByteForModbus
        {
            get
            {
                byte[] cmd = { SlaveID, 0x03, 0x00, 0x10, 0x00, 0x04 };
                var crc = BitConverter.GetBytes(calculateCRC(cmd, 6, 0));
                return new byte[8] { SlaveID, 0x03, 0x00, 0x10, 0x00, 0x04, crc[0], crc[1] };
            }
        }

        public byte[] ModifyIDCmbByteForModbus(byte ID_ToChange)
        {
            byte[] cmd = { SlaveID, 0x06, 0x00, 0x20, 0x00, ID_ToChange };
            var crc = BitConverter.GetBytes(calculateCRC(cmd, 6, 0));
            return new byte[8] { SlaveID, 0x06, 0x00, 0x20, 0x00, ID_ToChange, crc[0], crc[1] };
        }

        public byte[] READRAWCmdByteForModbus
        {
            get
            {
                byte[] cmd = { SlaveID, 0x03, 0x00, 0xFF, 0x00, 0xFF };
                var crc = BitConverter.GetBytes(calculateCRC(cmd, 6, 0));
                return new byte[8] { SlaveID, 0x03, 0x00, 0xFF, 0x00, 0xFF, crc[0], crc[1] };
            }
        }
        internal enum SettingItem
        {
            SetDaqMode, SetDataLen, SetMeasureRange, NotSpecify
        }
        internal SettingItem settingItem = SettingItem.NotSpecify;

        protected double _SamplingRate = 10064;

        protected double BaseSamplingRate = 10064;
        internal double _downSamplingRatio = 1;
        virtual public double SamplingRate
        {
            get => _SamplingRate;
            set
            {
                _SamplingRate = value;
                _downSamplingRatio = value / BaseSamplingRate;
            }
        }

        /// <summary>
        /// [3] KX134: CNTL1  預設量測範圍 = +-8G ; 5K雙核 : 9F/87
        /// [4] ODCNTL 預設ODR = 12800           ; 5K雙核 : 量測範圍(2/4/8/16)
        /// </summary>
        public byte[] _SettingBytes = new byte[8] { 0x00, 0x02, 0x00, 0xC0, 0x2E, 0x00, 0x00, 0x00 };

        /// <summary>
        /// 是否為5K雙核版本
        /// </summary>
        internal bool Is5KDaulCPUVersion = false;

        virtual internal byte[] SettingBytesWithHead
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
        internal byte[] SettingBytes
        {
            get
            {
                UpdateSettingBytes();
                return _SettingBytes;
            }
        }


        /// <summary>
        /// 單軸資料長度
        /// </summary>
        internal int _DataOutputLength = 512;

        internal int DefaulDataOutPutLength = 256;
        /// <summary>
        /// 單軸資料長度
        /// </summary>
        virtual public int DataOuputLength
        {
            get { return _DataOutputLength; }
            set
            {
                _DataOutputLength = value;
                UpdateSettingBytes();
            }
        }
        /// <summary>
        /// 模組必須回傳的封包長度
        /// </summary>
        virtual public int PacketLengthOfDeviceShoultReturn
        {
            get
            {
                return _DataOutputLength * CompValueOfDownSample * 6;
            }
        }

        internal MEASURE_RANGE _mEASURE_RANGE = MEASURE_RANGE.MR_8G;
        virtual public MEASURE_RANGE mEASURE_RANGE
        {
            get { return _mEASURE_RANGE; }
            set
            {
                _mEASURE_RANGE = value;
                UpdateSettingBytes();
            }
        }
        internal DAQMode _Mode = DAQMode.High_Sampling;
        virtual public DAQMode Mode
        {
            get { return _Mode; }
            set
            {
                SamplingRate = value == DAQMode.High_Sampling ? 5600 : 3500;
                _Mode = value;
                UpdateSettingBytes();
            }
        }

        /// <summary>
        /// 取得當前的LSB值(與量測範圍直接相關)
        /// </summary>
        internal int LSB
        {
            get
            {
                return (int)_mEASURE_RANGE;
            }
        }

        /// <summary>
        /// 量測時間估計(毫秒)
        /// </summary>
        public int Measure_Time
        {
            get
            {
                int meas_time = (int)Math.Floor((double)_DataOutputLength / SamplingRate * 1000);
                return meas_time;
            }
        }

        private double _ODR = 12800;
        public double ODR
        {
            get { return _ODR; }
            set
            {
                _ODR = value;
                UpdateSettingBytes();
            }
        }

        /// <summary>
        /// 因為Down Sample, 要多取幾倍的數據量
        /// </summary>
        internal int CompValueOfDownSample => Convert.ToInt32((1.0 / _downSamplingRatio + ""));

        virtual internal void UpdateSettingBytes()
        {
            ///長度;先計算倍率(要考慮DownSampling,所以要得數據要N倍)
            var ratio = _DataOutputLength * 6 * CompValueOfDownSample / 1536;
            var DLHLBytes = ratio.ToHLBytes();
            _SettingBytes[0] = DLHLBytes[0];
            _SettingBytes[1] = DLHLBytes[1];
            if (Is5KDaulCPUVersion)
            {
                _SettingBytes[3] = 0x9F; //Fix ususally
                //量測範圍設定
                _SettingBytes[4] = _mEASURE_RANGE.ToGENByte();
            }
            else
            {
                #region Measurement Range Setting
                _SettingBytes[3] = _mEASURE_RANGE.ToKXByte();
                #endregion

                #region ODR Setting
                var bitStringODCNTL = Convert.ToString(_SettingBytes[4], 2).To8BitString();

                if (_ODR == 50)
                    bitStringODCNTL = bitStringODCNTL.Substring(0, 4) + "0110";
                if (_ODR == 12800)
                    bitStringODCNTL = bitStringODCNTL.Substring(0, 4) + "1110";
                var _byte = Enumerable.Range(0, bitStringODCNTL.Length / 8)
                    .Select(pos => Convert.ToByte(bitStringODCNTL.Substring(pos * 8, 8), 2)).ToArray()[0];
                _SettingBytes[4] = _byte;
                #endregion
            }

        }
        /// <summary>
        /// Calculates the CRC16 for Modbus-RTU
        /// </summary>
        /// <param name="data">Byte buffer to send</param>
        /// <param name="numberOfBytes">Number of bytes to calculate CRC</param>
        /// <param name="startByte">First byte in buffer to start calculating CRC</param>
        internal UInt16 calculateCRC(byte[] data, UInt16 numberOfBytes, int startByte)
        {
            byte[] auchCRCHi = {
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
            0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
            0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01,
            0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81,
            0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
            0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01,
            0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
            0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
            0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01,
            0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
            0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
            0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01,
            0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
            0x40
            };

            byte[] auchCRCLo = {
            0x00, 0xC0, 0xC1, 0x01, 0xC3, 0x03, 0x02, 0xC2, 0xC6, 0x06, 0x07, 0xC7, 0x05, 0xC5, 0xC4,
            0x04, 0xCC, 0x0C, 0x0D, 0xCD, 0x0F, 0xCF, 0xCE, 0x0E, 0x0A, 0xCA, 0xCB, 0x0B, 0xC9, 0x09,
            0x08, 0xC8, 0xD8, 0x18, 0x19, 0xD9, 0x1B, 0xDB, 0xDA, 0x1A, 0x1E, 0xDE, 0xDF, 0x1F, 0xDD,
            0x1D, 0x1C, 0xDC, 0x14, 0xD4, 0xD5, 0x15, 0xD7, 0x17, 0x16, 0xD6, 0xD2, 0x12, 0x13, 0xD3,
            0x11, 0xD1, 0xD0, 0x10, 0xF0, 0x30, 0x31, 0xF1, 0x33, 0xF3, 0xF2, 0x32, 0x36, 0xF6, 0xF7,
            0x37, 0xF5, 0x35, 0x34, 0xF4, 0x3C, 0xFC, 0xFD, 0x3D, 0xFF, 0x3F, 0x3E, 0xFE, 0xFA, 0x3A,
            0x3B, 0xFB, 0x39, 0xF9, 0xF8, 0x38, 0x28, 0xE8, 0xE9, 0x29, 0xEB, 0x2B, 0x2A, 0xEA, 0xEE,
            0x2E, 0x2F, 0xEF, 0x2D, 0xED, 0xEC, 0x2C, 0xE4, 0x24, 0x25, 0xE5, 0x27, 0xE7, 0xE6, 0x26,
            0x22, 0xE2, 0xE3, 0x23, 0xE1, 0x21, 0x20, 0xE0, 0xA0, 0x60, 0x61, 0xA1, 0x63, 0xA3, 0xA2,
            0x62, 0x66, 0xA6, 0xA7, 0x67, 0xA5, 0x65, 0x64, 0xA4, 0x6C, 0xAC, 0xAD, 0x6D, 0xAF, 0x6F,
            0x6E, 0xAE, 0xAA, 0x6A, 0x6B, 0xAB, 0x69, 0xA9, 0xA8, 0x68, 0x78, 0xB8, 0xB9, 0x79, 0xBB,
            0x7B, 0x7A, 0xBA, 0xBE, 0x7E, 0x7F, 0xBF, 0x7D, 0xBD, 0xBC, 0x7C, 0xB4, 0x74, 0x75, 0xB5,
            0x77, 0xB7, 0xB6, 0x76, 0x72, 0xB2, 0xB3, 0x73, 0xB1, 0x71, 0x70, 0xB0, 0x50, 0x90, 0x91,
            0x51, 0x93, 0x53, 0x52, 0x92, 0x96, 0x56, 0x57, 0x97, 0x55, 0x95, 0x94, 0x54, 0x9C, 0x5C,
            0x5D, 0x9D, 0x5F, 0x9F, 0x9E, 0x5E, 0x5A, 0x9A, 0x9B, 0x5B, 0x99, 0x59, 0x58, 0x98, 0x88,
            0x48, 0x49, 0x89, 0x4B, 0x8B, 0x8A, 0x4A, 0x4E, 0x8E, 0x8F, 0x4F, 0x8D, 0x4D, 0x4C, 0x8C,
            0x44, 0x84, 0x85, 0x45, 0x87, 0x47, 0x46, 0x86, 0x82, 0x42, 0x43, 0x83, 0x41, 0x81, 0x80,
            0x40
            };
            UInt16 usDataLen = numberOfBytes;
            byte uchCRCHi = 0xFF;
            byte uchCRCLo = 0xFF;
            int i = 0;
            int uIndex;
            while (usDataLen > 0)
            {
                usDataLen--;
                if ((i + startByte) < data.Length)
                {
                    uIndex = uchCRCLo ^ data[i + startByte];
                    uchCRCLo = (byte)(uchCRCHi ^ auchCRCHi[uIndex]);
                    uchCRCHi = auchCRCLo[uIndex];
                }
                i++;
            }
            return (UInt16)((UInt16)uchCRCHi << 8 | uchCRCLo);
        }

    }



}

