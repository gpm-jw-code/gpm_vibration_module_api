﻿using gpm_vibration_module_api.API.ThreeInOne;
using gpm_vibration_module_api.DataSets;
using gpm_vibration_module_api.GpmMath;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static gpm_vibration_module_api.API.ThreeInOne.Settings;
using static gpm_vibration_module_api.ThreeInOne.ThreeInOneModuleAPI;

namespace gpm_vibration_module_api.ThreeInOne
{
    public class ThreeInOneModuleAPI : SerialProtocolBase, IDisposable
    {
        /// <summary>
        /// 壓力量測模式列舉
        /// </summary>
        public enum PRESSURE_DATA_ACQ_MODE
        {
            /// <summary>
            /// 量測值為絕對大氣壓力值
            /// </summary>
            NON_ZERO_REST = 0,
            /// <summary>
            /// 量測值為相對大氣壓力值(相對於參數[7 or 8]設為2的當前壓力值)
            /// </summary>
            ZERO_REST = 1
        }

        /// <summary>
        /// 濕度值校正動作
        /// </summary>
        public enum HUMIDITY_CALIBRATION_ACTION
        {
            NONE = 0x03,
            MINUS_10RH = 0x4,
            MINUS_9RH = 0x5,
            MINUS_8RH = 0x6,
            MINUS_7RH = 0x7,
            MINUS_6RH = 0x8,
            MINUS_5RH = 0x9,
            MINUS_4RH = 0x0A,
            MINUS_3RH = 0x0B,
            MINUS_2RH = 0x0C,
            MINUS_1RH = 0x0D,
            ADD_1RH = 0x0E,
            ADD_2RH = 0x0F,
            ADD_3RH = 0x10,
            ADD_4RH = 0x11,
            ADD_5RH = 0x12,
            ADD_6RH = 0x13,
            ADD_7RH = 0x14,
            ADD_8RH = 0x15,
            ADD_9RH = 0x16,
            ADD_10RH = 0x17,

        }

        /// <summary>
        /// ODR列舉
        /// </summary>
        public enum ODR
        {
            Hz1344 = 1344,
            Hz5367 = 5367
        }

        /// <summary>
        /// 參數設定的原因
        /// </summary>
        private enum CURRENT_SETTING_TYPE
        {
            DEV,
            PRESSURE_MODE,
            HUMIDITY_MODE
        }

        public bool ShorDataTest = true;
        private readonly Configs globalConfig;
        private const int GetDataPacketLen = 3096;
        private const int ParametersPacketLen = 8;
        private ThreeInOneModuleDataSet _currentDataSet = new ThreeInOneModuleDataSet();
        private bool isGetDataRunning = false;
        private bool isParametersSettingRunning = false;
        private byte[] ParamsSendOutBytes = new byte[11] { 0x53, 0x01, 0x00, 0x97, 0x08, 0x00, 0x00, 0x00, 0x00, 0x0d, 0x0a }; // 8 + 前Header(1)+ 後結尾(2) >> 共11 byte
        private bool isMeasureRangeSettingReady = false;
        private CURRENT_SETTING_TYPE Extension_SETTING_TYPE = CURRENT_SETTING_TYPE.DEV;

        public const string RecordFolder = "Three-in-One-Device-Data";
        #region PUBLIC Methods and Properties


        public double Offset_T1 = 0.0;
        public double Offset_T2 = 0.0;
        public double Offset_H1 = 0.0;
        public double Offset_H2 = 0.0;
        public ThreeInOneModuleAPI()
        {
            globalConfig = LoadConfig();
            ShorDataTest = globalConfig.single_data_mode == 1;
            if (globalConfig.record_raw_data == 1)
                Directory.CreateDirectory(RecordFolder);
        }

        /// <summary>
        /// 目前的ODR設定值
        /// </summary>
        public ODR Odr { get; private set; } = ODR.Hz1344;
        /// <summary>
        /// 目前的量測範圍設定值
        /// </summary>
        public clsEnum.Module_Setting_Enum.MEASURE_RANGE MEASURE_RANGE { get; private set; } = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_2G;


        /// <summary>
        /// 目前Chanel 1壓力感測值模式設定
        /// </summary>
        public PRESSURE_DATA_ACQ_MODE Pressure_1_DataMode { get; private set; } = PRESSURE_DATA_ACQ_MODE.NON_ZERO_REST;
        /// <summary>
        /// 目前Chanel 2壓力感測值模式設定
        /// </summary>
        public PRESSURE_DATA_ACQ_MODE Pressure_2_DataMode { get; private set; } = PRESSURE_DATA_ACQ_MODE.NON_ZERO_REST;

        #region 濕度模式

        public HUMIDITY_CALIBRATION_ACTION Humidity_1_Calibration_Action { get; private set; } = HUMIDITY_CALIBRATION_ACTION.NONE;
        public HUMIDITY_CALIBRATION_ACTION Humidity_2_Calibration_Action { get; private set; } = HUMIDITY_CALIBRATION_ACTION.NONE;

        #endregion


        /// <summary>
        /// 目前的加速規晶片取樣率設定
        /// </summary>
        public double SamplingRate { get; set; } = 8000;
        public string Port { get; private set; }
        /// <summary>
        /// 封包接收Timeout設定。
        /// 單位:ms 毫秒
        /// </summary>
        public int RecieveTimeout = 1000;
        private bool disposedValue;

        /// <summary>
        /// 透過Serial Port與模組連線
        /// </summary>
        /// <param name="PortName">COM 名</param>
        /// <param name="baudRate">鮑率(預設值 115200)</param>
        /// <returns>Error Code > 0:連線成功; Otherwise Error Code </returns>
        public int Connect(string PortName, int baudRate = 115200)
        {
            Port = PortName;
            return base.Open(PortName, baudRate) ? 0 : (int)clsErrorCode.Error.SerialPortOpenFail;
        }

        /// <summary>
        /// 嘗試斷開與模組的連線
        /// </summary>
        /// 
        public new void Close()
        {
            base.Close();
            Dispose();
        }

        public void TEST()
        {
            Console.WriteLine("Hi, I am function in framework 4.6.1 dll...");
        }

        /// <summary>
        /// (可等候)取得數據物件(包含三軸振動G值/ 溫度 / 壓力 / 濕度 * 2 SET)
        /// </summary>
        /// <returns>物件 : ThreeInOneModuleDataSet 
        /// (Namepace: gpm_vibration_module_api.DataSets) 
        /// </returns>
        public async Task<ThreeInOneModuleDataSet> GetData()
        {
            if (!isMeasureRangeSettingReady)
            {
                _currentDataSet.ErrorCode = (int)clsErrorCode.Error.VibrationMeasureRangeNotSetYet;
                return _currentDataSet;
            }
            if (isParametersSettingRunning)
            {
                _currentDataSet.ErrorCode = (int)clsErrorCode.Error.ModuleIsBusy;
                return _currentDataSet;
            }
            isGetDataRunning = true;
            TotalDataByteLen = ShorDataTest ? 22 : GetDataPacketLen;
            bool SendSuccess = await SendCommand("READVALUE\r\n");
            if (!SendSuccess)
            {
                isGetDataRunning = false;
                _currentDataSet.ErrorCode = (int)clsErrorCode.Error.NoConnection;
                return _currentDataSet;
            }
            bool isTimeout = await DataRecieveDone();
            isGetDataRunning = false;
            _currentDataSet.ErrorCode = (!isTimeout ? 0 : (int)clsErrorCode.Error.DATA_GET_TIMEOUT);

            if (isTimeout)
                return _currentDataSet;
            _ = ShorDataTest ? DataSetPrepareProcessing_ShortDataMode() : DataSetPrepareProcessing();

            _currentDataSet.Temperature1 = _currentDataSet.Temperature1 + Offset_T1;
            _currentDataSet.Temperature2 = _currentDataSet.Temperature2 + Offset_T2;
            _currentDataSet.Humidity1 = _currentDataSet.Humidity1 + Offset_H1;
            _currentDataSet.Humidity2 = _currentDataSet.Humidity2 + Offset_H2;

            if (globalConfig.record_raw_data == 1 && _currentDataSet.ErrorCode == 0)
                RecordData();

            return _currentDataSet;
        }

        private void RecordData()
        {
            if (!ShorDataTest)
                return;
            DateTime time = DateTime.Now;
            double x = _currentDataSet.VibrationData.X[0];
            double y = _currentDataSet.VibrationData.Y[0];
            double z = _currentDataSet.VibrationData.X[0];
            double t1 = _currentDataSet.Temperature1;
            double t2 = _currentDataSet.Temperature2;
            double h1 = _currentDataSet.Humidity1;
            double h2 = _currentDataSet.Humidity2;
            using (StreamWriter sw = new StreamWriter(RecordFolder + $"\\{Port}-{time.ToString("yyyy-MM-dd")}.csv", true))
            {
                sw.WriteLine(time.ToString("yyyy/MM/dd HH:mm:ss:ffff") + "," + x + "," + y + "," + z + "," + t1 + "," + h1 + "," + t2 + "," + h2 + ","
                    + Offset_T1 + "," + Offset_H1 + "," + Offset_T2 + "," + Offset_H2);
            }
        }


        /// <summary>
        /// (可等候)設定ODR 
        /// </summary>
        /// <param name="odr">ODR設定值列舉</param>
        /// <returns></returns>
        public async Task<int> ODRSetting(ODR odr)
        {
            TotalDataByteLen = ParametersPacketLen;
            MeasureRangeByteDefine(odr, MEASURE_RANGE);
            int errorCode = await WriteParametersAndDefine();
            isMeasureRangeSettingReady = errorCode == 0;

            return errorCode;
        }


        /// <summary>
        /// (可等候)設定量測範圍 
        /// </summary>
        /// <param name="mEASURE">量測範圍列舉</param>
        /// <returns></returns>
        public async Task<int> MeasureRangeSetting(clsEnum.Module_Setting_Enum.MEASURE_RANGE mEASURE)
        {
            if (mEASURE == clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_32G | mEASURE == clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_64G)
                return (int)clsErrorCode.Error.MRSettingOutOfRange;
            TotalDataByteLen = ParametersPacketLen;
            MeasureRangeByteDefine(Odr, mEASURE);
            int errorCode = await WriteParametersAndDefine();
            isMeasureRangeSettingReady = errorCode == 0;
            return errorCode;
        }

        /// <summary>
        /// (可等候)設定壓力數值顯示模式
        /// </summary>
        /// <param name="Channel">欲設定的壓力感測Channel</param>
        /// <param name="mode">壓力數值顯示模式</param>
        /// <returns></returns>
        public async Task<int> PresssureValueModeSetting(int Channel, PRESSURE_DATA_ACQ_MODE mode)
        {
            if (Channel != 1 && Channel != 2)
                return (int)clsErrorCode.Error.ChannelNotExist;

            Extension_SETTING_TYPE = CURRENT_SETTING_TYPE.PRESSURE_MODE;
            TotalDataByteLen = ParametersPacketLen;
            int indexOfPbyte = Channel == 1 ? 7 : 8;
            int byteOfModeSet = mode == PRESSURE_DATA_ACQ_MODE.ZERO_REST ? 1 : 2;
            ParamsSendOutBytes[indexOfPbyte] = (byte)byteOfModeSet;
            int errorCode = await WriteParametersAndDefine();
            Extension_SETTING_TYPE = CURRENT_SETTING_TYPE.DEV;
            return errorCode;
        }

        /// <summary>
        /// (可等候)設定壓力數值顯示模式
        /// </summary>
        /// <param name="Channel">欲設定的壓力感測Channel</param>
        /// <param name="mode">壓力數值顯示模式</param>
        /// <returns></returns>
        public async Task<int> HumidityValueModeSetting(int Channel, HUMIDITY_CALIBRATION_ACTION mode)
        {
            if (Channel != 1 && Channel != 2)
                return (int)clsErrorCode.Error.ChannelNotExist;
            Extension_SETTING_TYPE = CURRENT_SETTING_TYPE.HUMIDITY_MODE;
            TotalDataByteLen = ParametersPacketLen;
            /// 53 01 00 9f 00 00 00 00 00 0d 0a
            int indexOfPbyte = Channel == 1 ? 7 : 8;
            int byteOfModeSet = (int)mode;
            ParamsSendOutBytes[indexOfPbyte] = (byte)byteOfModeSet;
            int errorCode = await WriteParametersAndDefine();

            Extension_SETTING_TYPE = CURRENT_SETTING_TYPE.DEV;
            return errorCode;
        }



        /// <summary>
        /// (可等候)寫入參數組(8byte),如果不知道參數組的定義請不要輕易嘗試
        /// </summary>
        /// <param name="EightBytesParamesSet"></param>
        /// <returns></returns>
        public async Task<int> WriteParameters(byte[] EightBytesParamesSet)
        {
            if (EightBytesParamesSet.Length != ParametersPacketLen)
                throw new Exception("參數數據位元組長度不足 '8'");
            TotalDataByteLen = ParametersPacketLen;
            Array.Copy(EightBytesParamesSet, 0, ParamsSendOutBytes, 1, 8);
            return await WriteParametersAndDefine();
        }

        /// <summary>
        /// (可等候)讀取振動加速規模組的參數組
        /// </summary>
        /// <returns> "Tuple" >> item1 : ErrorCode; item2 : 8byte參數位元組</returns>
        public async Task<Tuple<int, byte[]>> ReadParameters()
        {
            if (isParametersSettingRunning)
            {
                return new Tuple<int, byte[]>((int)clsErrorCode.Error.ModuleIsBusy, null);
            }
            isParametersSettingRunning = true;
            TotalDataByteLen = ParametersPacketLen;
            bool SendSuccess = await SendCommand("READSTVAL\r\n");
            if (!SendSuccess)
            {
                isParametersSettingRunning = false;
                return new Tuple<int, byte[]>((int)clsErrorCode.Error.NoConnection, null);
            }
            bool isTimeout = await DataRecieveDone();
            isParametersSettingRunning = false;
            var ErrorCode = !isTimeout ? 0 : (int)clsErrorCode.Error.DATA_GET_TIMEOUT;
            return new Tuple<int, byte[]>(ErrorCode, isTimeout ? null : TempDataByteList.ToArray());
        }

        #endregion

        #region Private Methods

        private async Task<int> WriteParametersAndDefine()
        {
            if (isGetDataRunning)
                return (int)clsErrorCode.Error.ModuleIsBusy;
            isParametersSettingRunning = true;
            TotalDataByteLen = 8;
            bool SendSuccess = await SendCommand(ParamsSendOutBytes,true,false);
            isParametersSettingRunning = false;
            if (!SendSuccess)
            {
                return (int)clsErrorCode.Error.NoConnection;
            }
            bool isTimeout = await DataRecieveDone();
            if (isTimeout)
                return (int)clsErrorCode.Error.PARAM_HS_TIMEOUT;
            if (IsParametersError())
                return (int)clsErrorCode.Error.ERROR_PARAM_RETURN_FROM_CONTROLLER;
            PropertiesDefineByByteVal();
            return 0; //done
        }

        private void PropertiesDefineByByteVal()
        {
            byte mbyte = TempDataByteList[3];

            Odr = TempDataByteList[2] == 0x9F ? ODR.Hz5367 : ODR.Hz1344;

            if (mbyte == (Odr == ODR.Hz1344 ? 0x08 : 0x00))
                MEASURE_RANGE = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_2G;
            if (mbyte == (Odr == ODR.Hz1344 ? 0x18 : 0x10))
                MEASURE_RANGE = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_4G;
            if (mbyte == (Odr == ODR.Hz1344 ? 0x28 : 0x20))
                MEASURE_RANGE = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_8G;
            if (mbyte == (Odr == ODR.Hz1344 ? 0x38 : 0x30))
                MEASURE_RANGE = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_16G;

            if (Extension_SETTING_TYPE == CURRENT_SETTING_TYPE.PRESSURE_MODE)
            {
                byte pressure_1_AcqModeByte = TempDataByteList[6];
                byte pressure_2_AcqModeByte = TempDataByteList[7];
                Pressure_1_DataMode = pressure_1_AcqModeByte == 1 ? PRESSURE_DATA_ACQ_MODE.ZERO_REST : PRESSURE_DATA_ACQ_MODE.NON_ZERO_REST;
                Pressure_2_DataMode = pressure_2_AcqModeByte == 1 ? PRESSURE_DATA_ACQ_MODE.ZERO_REST : PRESSURE_DATA_ACQ_MODE.NON_ZERO_REST;
            }
            if (Extension_SETTING_TYPE == CURRENT_SETTING_TYPE.HUMIDITY_MODE)
            {
                byte humidity_1_AcqModeByte = TempDataByteList[6];
                byte humidity_2_AcqModeByte = TempDataByteList[7];
                Humidity_1_Calibration_Action = humidity_1_AcqModeByte == 3 ? HUMIDITY_CALIBRATION_ACTION.NONE : humidity_1_AcqModeByte.ToHumidityCalAction();
                Humidity_2_Calibration_Action = humidity_2_AcqModeByte == 3 ? HUMIDITY_CALIBRATION_ACTION.NONE : humidity_2_AcqModeByte.ToHumidityCalAction();
            }

        }

        private void MeasureRangeByteDefine(ODR odr, clsEnum.Module_Setting_Enum.MEASURE_RANGE mEASURE)
        {
            byte mByte = 0x00;

            if (odr == ODR.Hz5367)
            {
                //   HERE ↓↓
                //53 01 00 ◯ 10 00 00 00 00 0d 0a
                ParamsSendOutBytes[3] = 0x9F;
                if (mEASURE == clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_2G)
                    mByte = 0x00;
                if (mEASURE == clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_4G)
                    mByte = 0x10;
                if (mEASURE == clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_8G)
                    mByte = 0x20;
                if (mEASURE == clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_16G)
                    mByte = 0x30;
            }
            else
            {
                //   HERE ↓↓
                //53 01 00 ◯ 10 00 00 00 00 0d 0a
                ParamsSendOutBytes[3] = 0x97;
                if (mEASURE == clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_2G)
                    mByte = 0x08;
                if (mEASURE == clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_4G)
                    mByte = 0x18;
                if (mEASURE == clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_8G)
                    mByte = 0x28;
                if (mEASURE == clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_16G)
                    mByte = 0x38;
            }
            //       HERE ↓↓
            //53 01 00 9f  ◯ 00 00 00 00 0d 0a
            ParamsSendOutBytes[4] = mByte;
        }

        private bool IsParametersError()
        {
            byte[] parametersBytes = TempDataByteList.ToArray();
            //回傳值檢查
            for (int i = 1; i < ParamsSendOutBytes.Length - 2; i++)
            {
                if (parametersBytes[i - 1] != ParamsSendOutBytes[i])
                    return true;
            }
            return false;
        }

        private async Task<bool> DataRecieveDone()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            while (!_isDataRecieveDone)
            {
                if (timer.ElapsedMilliseconds > RecieveTimeout)
                {
                    timer.Stop();
                    return true;
                }
                Thread.Sleep(1);
            }
            return false;
        }

        private bool DataSetPrepareProcessing_ShortDataMode()
        {
            //TempDataByteList
            try
            {
                byte[] AccDataBytes = new byte[6];
                byte[] temperature1 = new byte[4];
                byte[] humidity1 = new byte[4];
                byte[] temperature2 = new byte[4];
                byte[] humidity2 = new byte[4];
                byte[] tmpeDataByteListAry = TempDataByteList.ToArray();
                Array.Copy(tmpeDataByteListAry, 0, AccDataBytes, 0, 6);
                Array.Copy(tmpeDataByteListAry, 6, temperature1, 0, 4);
                Array.Copy(tmpeDataByteListAry, 10, humidity1, 0, 4);
                Array.Copy(tmpeDataByteListAry, 14, temperature2, 0, 4);
                Array.Copy(tmpeDataByteListAry, 18, humidity2, 0, 4);

                List<List<double>> axisDatasList = Tools.ConverterTools.AccPacketToListDouble(AccDataBytes, MEASURE_RANGE, DAQMode.High_Sampling);

                //List<double> fft_x = FFT.GetFFT(axisDatasList[0]);
                //List<double> fft_y = FFT.GetFFT(axisDatasList[1]);
                //List<double> fft_z = FFT.GetFFT(axisDatasList[2]);

                double t1 = GetDoubleByIEEE754(temperature1);
                double h1 = GetDoubleByIEEE754(humidity1);
                double t2 = GetDoubleByIEEE754(temperature2);
                double h2 = GetDoubleByIEEE754(humidity2);

                _currentDataSet = new ThreeInOneModuleDataSet
                {
                    Temperature1 = t1,
                    Temperature2 = t2,
                    Humidity1 = h1,
                    Humidity2 = h2,
                    VibrationData = new DataSet.clsAcc
                    {
                        X = axisDatasList[0],
                        Y = axisDatasList[1],
                        Z = axisDatasList[2],
                    },
                    //FFTData = new DataSet.clsFFTData
                    //{
                    //    SamplingRate = SamplingRate,
                    //    FreqVec = FreqVecCal(fft_x.Count, SamplingRate),
                    //    X = fft_x,
                    //    Y = fft_y,
                    //    Z = fft_z
                    //},
                    RawBytes = TempDataByteList,
                    ErrorCode = 0
                };
                return true;
            }
            catch (Exception ex)
            {
                _currentDataSet = new ThreeInOneModuleDataSet
                {
                    ErrorCode = (int)clsErrorCode.Error.SYSTEM_ERROR
                };
                return false;
            }

        }

        private bool DataSetPrepareProcessing()
        {
            //TempDataByteList
            try
            {
                byte[] AccDataBytes = new byte[3072];
                byte[] temperature1 = new byte[4];
                byte[] pressure1 = new byte[4];
                byte[] humidity1 = new byte[4];
                byte[] temperature2 = new byte[4];
                byte[] pressure2 = new byte[4];
                byte[] humidity2 = new byte[4];
                byte[] tmpeDataByteListAry = TempDataByteList.ToArray();
                Array.Copy(tmpeDataByteListAry, 0, AccDataBytes, 0, 3072);
                Array.Copy(tmpeDataByteListAry, 3072, temperature1, 0, 4);
                Array.Copy(tmpeDataByteListAry, 3076, pressure1, 0, 4);
                Array.Copy(tmpeDataByteListAry, 3080, humidity1, 0, 4);
                Array.Copy(tmpeDataByteListAry, 3084, temperature2, 0, 4);
                Array.Copy(tmpeDataByteListAry, 3088, pressure2, 0, 4);
                Array.Copy(tmpeDataByteListAry, 3092, humidity2, 0, 4);

                List<List<double>> axisDatasList = Tools.ConverterTools.AccPacketToListDouble(AccDataBytes, MEASURE_RANGE, DAQMode.High_Sampling);

                List<double> fft_x = FFT.GetFFT(axisDatasList[0]);
                List<double> fft_y = FFT.GetFFT(axisDatasList[1]);
                List<double> fft_z = FFT.GetFFT(axisDatasList[2]);

                double t1 = GetDoubleByIEEE754(temperature1);
                double p1 = GetDoubleByIEEE754(pressure1);
                double h1 = GetDoubleByIEEE754(humidity1);
                double t2 = GetDoubleByIEEE754(temperature2);
                double p2 = GetDoubleByIEEE754(pressure2);
                double h2 = GetDoubleByIEEE754(humidity2);

                _currentDataSet = new ThreeInOneModuleDataSet
                {
                    Temperature1 = t1,
                    Temperature2 = t2,
                    Humidity1 = h1,
                    Humidity2 = h2,
                    Pressure1 = p1,
                    Pressure2 = p2,
                    VibrationData = new DataSet.clsAcc
                    {
                        X = axisDatasList[0],
                        Y = axisDatasList[1],
                        Z = axisDatasList[2],
                    },
                    FFTData = new DataSet.clsFFTData
                    {
                        SamplingRate = SamplingRate,
                        FreqVec = FreqVecCal(fft_x.Count, SamplingRate),
                        X = fft_x,
                        Y = fft_y,
                        Z = fft_z
                    },
                    RawBytes = TempDataByteList,
                    ErrorCode = 0
                };
                return true;
            }
            catch (Exception ex)
            {
                _currentDataSet = new ThreeInOneModuleDataSet
                {
                    ErrorCode = (int)clsErrorCode.Error.SYSTEM_ERROR
                };
                return false;
            }


        }
        internal List<double> FreqVecCal(int FFTWindowSize, double samplingRate)
        {
            var freqVec = new List<double>();
            var NysFreq = samplingRate / 2;
            for (int i = 0; i < FFTWindowSize; i++)
            {
                freqVec.Add((NysFreq / (double)FFTWindowSize) * (double)i);
            }
            return freqVec;
        }
        private double GetDoubleByIEEE754(byte[] byteAry)
        {
            if (byteAry == null)
                return -1;
            List<double> valuesList = new List<double>();
            var hexstring = byteAry[0].ToString("X2") + byteAry[1].ToString("X2") + byteAry[2].ToString("X2") + byteAry[3].ToString("X2");
            double dVal = ToFloat(hexstring);
            return dVal;
        }

        float ToFloat(string Hex32Input)
        {
            double doubleout = 0.0;
            UInt64 bigendian;
            bool success = UInt64.TryParse(Hex32Input,
                System.Globalization.NumberStyles.HexNumber, null, out bigendian);
            if (success)
            {
                double fractionDivide = Math.Pow(2, 23);

                int sign = (bigendian & 0x80000000) == 0 ? 1 : -1;
                Int64 exponent = ((Int64)(bigendian & 0x7F800000) >> 23) - (Int64)127;
                UInt64 fraction = (bigendian & 0x007FFFFF);
                if (fraction == 0)
                    doubleout = sign * Math.Pow(2, exponent);
                else
                    doubleout = sign * (1 + (fraction / fractionDivide)) * Math.Pow(2, exponent);
            }
            return (float)doubleout;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 處置受控狀態 (受控物件)
                }
                _currentDataSet.Dispose();
                disposedValue = true;
            }
        }

        // // TODO: 僅有當 'Dispose(bool disposing)' 具有會釋出非受控資源的程式碼時，才覆寫完成項
        // ~ThreeInOneModuleAPI()
        // {
        //     // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }


        #endregion
    }

}
