using gpm_vibration_module_api.DataSets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gpm_vibration_module_api.ThreeInOne
{
    public class ThreeInOneModuleAPI : SerialProtocolBase, IDisposable
    {
        private const int GetDataPacketLen = 3096;
        private const int ParametersPacketLen = 8;
        private ThreeInOneModuleDataSet _currentDataSet = new ThreeInOneModuleDataSet();
        private bool isGetDataRunning = false;
        private bool isParametersSettingRunning = false;
        private byte[] ParamsSendOutBytes = new byte[11] { 0x53, 0x01, 0x00, 0x9f, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0d, 0x0a }; // 8 + 前Header(1)+ 後結尾(2) >> 共11 byte

        public clsEnum.Module_Setting_Enum.MEASURE_RANGE MEASURE_RANGE { get; private set; } = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_2G;
        /// <summary>
        /// 封包接收Timeout設定。
        /// 單位:ms 毫秒
        /// </summary>
        public int RecieveTimeout = 1000;
        /// <summary>
        /// 透過Serial Port與模組連線
        /// </summary>
        /// <param name="PortName">COM 名</param>
        /// <param name="baudRate">鮑率(預設值 115200)</param>
        /// <returns>Error Code > 0:連線成功; Otherwise Error Code </returns>
        public int Connect(string PortName, int baudRate = 115200)
        {
            return base.Open(PortName, baudRate) ? 0 : (int)clsErrorCode.Error.SerialPortOpenFail;
        }

        /// <summary>
        /// 嘗試斷開與模組的連線
        /// </summary>
        public void Close()
        {
            base.Close();
            Dispose();
        }

        /// <summary>
        /// (可等候)取得數據物件(包含三軸振動G值/ 溫度 / 壓力 / 濕度 * 2 SET)
        /// </summary>
        /// <returns>物件 : ThreeInOneModuleDataSet 
        /// (Namepace: gpm_vibration_module_api.DataSets) 
        /// </returns>
        public async Task<ThreeInOneModuleDataSet> GetData()
        {
            if (isParametersSettingRunning)
            {
                _currentDataSet.ErrorCode = (int)clsErrorCode.Error.ModuleIsBusy;
                return _currentDataSet;
            }
            isGetDataRunning = true;
            TotalDataByteLen = GetDataPacketLen;
            bool SendSuccess = SendCommand("READVALUE\r\n");
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
            DataSetPrepareProcessing();
            return _currentDataSet;
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
            MeasureRangeByteDefine(mEASURE);
            return await WriteParameters();
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
            return await WriteParameters();
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
            bool SendSuccess = SendCommand("READSTVAL\r\n");
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

        public void Dispose()
        {
        }

        #region Private Methods

        private async Task<int> WriteParameters()
        {
            if (isGetDataRunning)
                return (int)clsErrorCode.Error.ModuleIsBusy;
            isParametersSettingRunning = true;
            TotalDataByteLen = 8;
            bool SendSuccess = SendCommand(ParamsSendOutBytes);
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
            MeasureReangDefineByByteVal();
            return 0; //done
        }

        private void MeasureReangDefineByByteVal()
        {
            byte mbyte = TempDataByteList[3];
            if (mbyte == 0x00)
                MEASURE_RANGE = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_2G;
            if (mbyte == 0x10)
                MEASURE_RANGE = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_4G;
            if (mbyte == 0x20)
                MEASURE_RANGE = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_8G;
            if (mbyte == 0x30)
                MEASURE_RANGE = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_16G;
        }

        private void MeasureRangeByteDefine(clsEnum.Module_Setting_Enum.MEASURE_RANGE mEASURE)
        {
            byte mByte = 0x00;
            if (mEASURE == clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_2G)
                mByte = 0x00;
            if (mEASURE == clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_4G)
                mByte = 0x10;
            if (mEASURE == clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_8G)
                mByte = 0x20;
            if (mEASURE == clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_16G)
                mByte = 0x30;
            ///       HERE ↓↓
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

        private void DataSetPrepareProcessing()
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
                    RawBytes = TempDataByteList,
                    ErrorCode = 0
                };
            }
            catch (Exception ex)
            {
                _currentDataSet = new ThreeInOneModuleDataSet
                {
                    ErrorCode = (int)clsErrorCode.Error.SYSTEM_ERROR
                };
            }


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


        #endregion
    }
}
