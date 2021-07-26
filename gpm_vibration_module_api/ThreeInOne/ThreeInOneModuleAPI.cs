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
    public class ThreeInOneModuleAPI : SerialProtocolBase
    {
        private ThreeInOneModuleDataSet _currentDataSet;
        private bool isGetDataRunning = false;
        private bool isMeasureRangeSetRunning = false;
        public clsEnum.Module_Setting_Enum.MEASURE_RANGE MEASURE_RANGE { get; private set; } = clsEnum.Module_Setting_Enum.MEASURE_RANGE.MR_2G;
        private byte[] ParamsSendOutBytes = new byte[11] { 0x53, 0x01, 0x00, 0x9f, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0d, 0x0a }; // 8 + 前Header(1)+ 後結尾(2) >> 共11 byte



        public int Connect(string PortName)
        {
            return base.Open(PortName, 115200) ? 0 : (int)clsErrorCode.Error.SerialPortOpenFail;
        }
        public void Close()
        {
            base.Close();
        }


        public async Task<ThreeInOneModuleDataSet> GetData()
        {
            if (isMeasureRangeSetRunning)
            {
                _currentDataSet.ErrorCode = (int)clsErrorCode.Error.ModuleIsBusy;
                return _currentDataSet;
            }
            isGetDataRunning = true;
            TotalDataByteLen = 3092;
            SendCommand("READVALUE\r\n");
            bool isTimeout = await DataRecieveDone();
            DataSetPrepareProcessing();
            _currentDataSet.ErrorCode = !isTimeout ? 0 : (int)clsErrorCode.Error.DATA_GET_TIMEOUT;
            isGetDataRunning = false;
            return _currentDataSet;
        }

        public async Task<int> MeasureRangeSetting(clsEnum.Module_Setting_Enum.MEASURE_RANGE mEASURE)
        {
            if (isGetDataRunning)
                return (int)clsErrorCode.Error.ModuleIsBusy;

            isMeasureRangeSetRunning = true;
            TotalDataByteLen = 8;
            MeasureRangeByteDefine(mEASURE);
            SendCommand(ParamsSendOutBytes);
            bool isTimeout = await DataRecieveDone();
            if (isTimeout)
                return (int)clsErrorCode.Error.PARAM_HS_TIMEOUT;
            if (IsParametersError())
                return (int)clsErrorCode.Error.ERROR_PARAM_RETURN_FROM_CONTROLLER;
            MeasureReangDefineByByteVal();
            isMeasureRangeSetRunning = false;
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
                if (timer.ElapsedMilliseconds > 10000)
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
                    Y = axisDatasList[0],
                    Z = axisDatasList[0],
                },
                RawBytes = TempDataByteList,
                ErrorCode = 0
            };

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
    }
}
