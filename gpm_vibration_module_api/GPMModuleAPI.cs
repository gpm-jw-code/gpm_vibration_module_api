#define YCM
//define KeyproEnable

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace gpm_vibration_module_api
{
    /// <summary>
    /// For User using.
    /// </summary>
    public class GPMModuleAPI
    {
#if KeyproEnable
        private clsEnum.KeyPro.KeyProExisStatus KeyProExisStatus = clsEnum.KeyPro.KeyProExisStatus.NoInsert;
#else
        private clsEnum.KeyPro.KeyProExisStatus KeyProExisStatus = clsEnum.KeyPro.KeyProExisStatus.Exist;

#endif
        /// <summary>
        /// 存放所有連線socket
        /// </summary>
        private static Dictionary<string, Socket> SCKConectionList = new Dictionary<string, Socket>();
        /// <summary>
        /// 斷開所有的模組連線並釋放資源
        /// </summary>
        public static void Dispose()
        {
            foreach (var sock in SCKConectionList)
            {
                try
                {
                    sock.Value.Shutdown(SocketShutdown.Both);
                }
                catch
                {

                }
                try
                {
                    sock.Value.Close();
                }
                catch
                {

                }
            }
        }

        public Socket ModuleSocket
        {
            get
            {
                return module_base.ModuleSocket;
            }
            set
            {
                module_base.ModuleSocket = value;
            }
        }

        private class clsParamSetTaskObj
        {
            public object SettingItem;
            public object SettingValue;
        }
        private DataSet DataSetRet = new DataSet();
        private clsParamSetTaskObj setTaskObj = new clsParamSetTaskObj();
        private bool IsGetFFT = false;
        private bool IsGetOtherFeatures = false;
        private Thread getDataThread;
        private Thread paramSetThread;
        private ManualResetEvent WaitAsyncForGetDataTask;
        private ManualResetEvent WaitAsyncForParametersSet;
        private event Action<string> FunctionCalled;
        /// <summary>
        /// 斷線事件
        /// </summary>
        public event Action<DateTime> DisconnectEvent;
        /// <summary>
        /// 控制器底層控制
        /// </summary>
        private clsModuleBase module_base = new clsModuleBase();
        public GPMModuleAPI(clsEnum.Module_Setting_Enum.SensorType sensorType)
        {
            KeyproMdule.API.KeyProInsertEvent += API_KeyProInsertEvent;
            KeyproMdule.API.KeyProRemoveEvent += API_KeyProRemoveEvent;
#if KeyproEnable
            var ret = KeyproMdule.API.IsKeyInsert();
#endif
            WaitAsyncForGetDataTask = new ManualResetEvent(false);
            WaitAsyncForParametersSet = new ManualResetEvent(true);
            GetDataTaskPause = new ManualResetEvent(true);
            getDataThread = new Thread(GetDataTask) { IsBackground = true };
            module_base.moduleSettings.SensorType = sensorType;

        }

        private void API_KeyProRemoveEvent(DateTime obj)
        {
            KeyProExisStatus = clsEnum.KeyPro.KeyProExisStatus.NoInsert;
        }

        private void API_KeyProInsertEvent(DateTime obj)
        {
            KeyProExisStatus = clsEnum.KeyPro.KeyProExisStatus.Exist;
        }

        public GPMModuleAPI()
        {
            KeyproMdule.API.KeyProInsertEvent += API_KeyProInsertEvent;
            KeyproMdule.API.KeyProRemoveEvent += API_KeyProRemoveEvent;
#if KeyproEnable
            var ret = KeyproMdule.API.IsKeyInsert();
#endif
            WaitAsyncForGetDataTask = new ManualResetEvent(false);
            WaitAsyncForParametersSet = new ManualResetEvent(true);
            GetDataTaskPause = new ManualResetEvent(true);
            getDataThread = new Thread(GetDataTask) { IsBackground = true };
#if YCM
            module_base.moduleSettings.SensorType = clsEnum.Module_Setting_Enum.SensorType.High;
            WifiSensorUsing = false;
#else
            module_base.moduleSettings.SensorType = clsEnum.Module_Setting_Enum.SensorType.Genernal;
            WifiSensorUsing = true;
#endif

        }
        public string SensorIP { get; private set; }

        public enum Enum_AccGetMethod
        {
            Auto, Manual
        }

        public bool WifiSensorUsing
        {
            set
            {
                module_base.moduleSettings.WifiControllUseHighSppedSensor = value;
            }
            get
            {
                return module_base.moduleSettings.WifiControllUseHighSppedSensor;
            }
        }
        public event Action<string> ConnectEvent;
        public Enum_AccGetMethod NowAccGetMethod = Enum_AccGetMethod.Manual;
        /// <summary>
        /// 與控制器進行連線
        /// </summary>
        /// <param name="IP">控制器IP</param>
        /// <param name="Port">控制器Port</param>
        /// <returns></returns>
        public int Connect(string IP, int Port)
        {
            if (KeyProExisStatus == clsEnum.KeyPro.KeyProExisStatus.NoInsert)
                return Convert.ToInt32(clsErrorCode.Error.KeyproNotFound);
            if (IP.Split('.').Length != 4 | IP == "")
                return Convert.ToInt32(clsErrorCode.Error.IPIllegal);
            if (Port <= 0)
                return Convert.ToInt32(clsErrorCode.Error.PortIllegal);

            try
            {

                if (SCKConectionList.ContainsKey(IP))
                {
                    Disconnect();
                }
                SensorIP = IP;
                var ret = module_base.Connect(IP, Port);
                if (ret == 0)
                {
                    if (ConnectEvent != null)
                        ConnectEvent.Invoke(IP);
                    if (!SCKConectionList.ContainsKey(IP))
                        SCKConectionList.Add(IP, module_base.ModuleSocket);
                    else
                        SCKConectionList[IP] = module_base.ModuleSocket;
                }
                return ret;
            }
            catch (Exception exp)
            {
                return -69;
            }
        }
        /// <summary>
        /// 斷開與控制器的連線
        /// </summary>
        public int Disconnect()
        {
            return module_base.Disconnect();
        }

        private void StartParamSetTask()
        {
            if (Connected == false)
                return;
            WaitAsyncForParametersSet.Reset();
            if (module_base.moduleSettings.DataLength == clsEnum.Module_Setting_Enum.DataLength.x8)
                Thread.Sleep(2000);
            else
                Thread.Sleep(1500);
            paramSetThread = new Thread(ParamSetTask) { IsBackground = true };
            paramSetThread.Start();
            WaitAsyncForParametersSet.WaitOne();
        }

        private void ParamSetTask()
        {
            switch (Convert.ToInt32(setTaskObj.SettingItem))
            {
                case 0:
                    module_base.WriteParameterToController((clsEnum.Module_Setting_Enum.SensorType)setTaskObj.SettingValue, null, null, null);
                    break;
                case 1:
                    module_base.WriteParameterToController(null, (clsEnum.Module_Setting_Enum.DataLength)setTaskObj.SettingValue, null, null);
                    break;
                case 2:
                    module_base.WriteParameterToController(null, null, (clsEnum.Module_Setting_Enum.MeasureRange)setTaskObj.SettingValue, null);
                    break;
                case 3:
                    module_base.WriteParameterToController(null, null, null, (clsEnum.Module_Setting_Enum.ODR)setTaskObj.SettingValue);
                    break;
            }
            WaitAsyncForParametersSet.Set();
        }
        /// <summary>
        /// 設定/取得量測範圍
        /// </summary>
        public clsEnum.Module_Setting_Enum.MeasureRange MeasureRange
        {
            set
            {
                setTaskObj.SettingItem = 2;
                setTaskObj.SettingValue = value;
                StartParamSetTask();
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
                if (SensorType != clsEnum.Module_Setting_Enum.SensorType.High)
                    return;
                setTaskObj.SettingItem = 1;
                setTaskObj.SettingValue = value;
                StartParamSetTask();
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
                setTaskObj.SettingItem = 0;
                setTaskObj.SettingValue = value;
                StartParamSetTask();
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
                setTaskObj.SettingItem = 3;
                setTaskObj.SettingValue = value;
                StartParamSetTask();
            }
            get
            {
                return module_base.moduleSettings.ODR;
            }
        }

        /// <summary>
        /// 取得連線狀態
        /// </summary>
        public bool Connected
        {
            get
            {
                if (module_base.ModuleSocket == null)
                    return false;
                return module_base.ModuleSocket.Connected;
            }
        }

        /// <summary>
        /// 設定感測器安裝位置名稱
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// 取得三軸加速度量測值
        /// </summary>
        public DataSet GetData(bool IsGetFFT, bool IsGetOtherFeatures)
        {
            if (KeyProExisStatus == clsEnum.KeyPro.KeyProExisStatus.NoInsert)
                return new DataSet() { ErrorCode = Convert.ToInt32(clsErrorCode.Error.KeyproNotFound) };
            if (Connected == false)
                return new DataSet() { ErrorCode = Convert.ToInt32(clsErrorCode.Error.NoConnection) };
            WaitAsyncForParametersSet.Set();
            WaitAsyncForGetDataTask.Reset();
            this.IsGetFFT = IsGetFFT;
            this.IsGetOtherFeatures = IsGetOtherFeatures;
            getDataThread = new Thread(GetDataTask) { IsBackground = true };
            getDataThread.Start();
            WaitAsyncForGetDataTask.WaitOne();
            return DataSetRet;
            //DataSet Datas = new DataSet();
            //try
            //{
            //    byte[] AccPacket;
            //    if (module_base.moduleSettings.SensorType == clsEnum.Module_Setting_Enum.SensorType.Genernal)
            //    {
            //        AccPacket = module_base.SendGetDataCommand(out Datas.TimeSpend);
            //    }
            //    else
            //        AccPacket = module_base.GetAccData_HighSpeedWay(out Datas.TimeSpend);
            //    var datas = Tools.ConverterTools.AccPacketToListDouble(AccPacket, MeasureRange, module_base.moduleSettings.SensorType == clsEnum.Module_Setting_Enum.SensorType.Genernal ? clsEnum.FWSetting_Enum.AccConvertAlgrium.Old : clsEnum.FWSetting_Enum.AccConvertAlgrium.New);
            //    Datas.AccData.X = datas[0];
            //    Datas.AccData.Y = datas[1];
            //    Datas.AccData.Z = datas[2];

            //    if (IsGetFFT)
            //    {
            //        Datas.FFTData.X = GpmMath.FFT.GetFFT(Datas.AccData.X);
            //        Datas.FFTData.Y = GpmMath.FFT.GetFFT(Datas.AccData.Y);
            //        Datas.FFTData.Z = GpmMath.FFT.GetFFT(Datas.AccData.Z);
            //        Datas.FFTData.FreqsVec = FreqVecCal(Datas.FFTData.X.Count);
            //    }

            //    if (IsGetOtherFeatures)
            //    {
            //        if (IsGetFFT)
            //        {
            //            Datas.Features.VibrationEnergy.X = GpmMath.Stastify.GetOA(Datas.FFTData.X);
            //            Datas.Features.VibrationEnergy.Y = GpmMath.Stastify.GetOA(Datas.FFTData.Y);
            //            Datas.Features.VibrationEnergy.Z = GpmMath.Stastify.GetOA(Datas.FFTData.Z);
            //        }
            //        Datas.Features.AccP2P.X = GpmMath.Stastify.GetPP(Datas.AccData.X);
            //        Datas.Features.AccP2P.Y = GpmMath.Stastify.GetPP(Datas.AccData.Y);
            //        Datas.Features.AccP2P.Z = GpmMath.Stastify.GetPP(Datas.AccData.Z);

            //        Datas.Features.AccRMS.X = GpmMath.Stastify.RMS(Datas.AccData.X);
            //        Datas.Features.AccRMS.Y = GpmMath.Stastify.RMS(Datas.AccData.Y);
            //        Datas.Features.AccRMS.Z = GpmMath.Stastify.RMS(Datas.AccData.Z);
            //    }
            //}
            //catch (Exception exp)
            //{
            //    Datas.AccData.X.Clear();
            //    Datas.AccData.Y.Clear();
            //    Datas.AccData.Z.Clear();
            //    Datas.AccData.X.Add(-99999);
            //    Datas.AccData.Y.Add(-99999);
            //    Datas.AccData.Z.Add(-99999);
            //    throw;
            //}
            //return Datas;
        }

        private ManualResetEvent GetDataTaskPause;
        public void GetDataResume()
        {
            GetDataTaskPause.Set();
        }
        /// <summary>
        /// 暫停收數據
        /// </summary>
        public void GetDataPause()
        {
            GetDataTaskPause.Reset();
        }

        private bool IsGetDataTaskPaused = true;
        private void GetDataTask()
        {
            IsGetDataTaskPaused = true;
            //GetDataTaskPause.WaitOne();
            WaitAsyncForParametersSet.WaitOne();
            IsGetDataTaskPaused = false;
            DataSetRet = new DataSet();
            try
            {
                byte[] AccPacket;
                AccPacket = module_base.GetAccData_HighSpeedWay(out DataSetRet.TimeSpend);
                if (AccPacket.Length == 0)
                {
                    DataSetRet.ErrorCode = Convert.ToInt32(clsErrorCode.Error.AccDataGetTimeout);
                    WaitAsyncForGetDataTask.Set();
                    return;
                }
                var datas = Tools.ConverterTools.AccPacketToListDouble(AccPacket, MeasureRange, module_base.moduleSettings.SensorType == clsEnum.Module_Setting_Enum.SensorType.Genernal ? clsEnum.FWSetting_Enum.AccConvertAlgrium.Old : clsEnum.FWSetting_Enum.AccConvertAlgrium.New);
                DataSetRet.AccData.X = datas[0];
                DataSetRet.AccData.Y = datas[1];
                DataSetRet.AccData.Z = datas[2];

                if (IsGetFFT)
                {
                    DataSetRet.FFTData.X = GpmMath.FFT.GetFFT(DataSetRet.AccData.X);
                    DataSetRet.FFTData.Y = GpmMath.FFT.GetFFT(DataSetRet.AccData.Y);
                    DataSetRet.FFTData.Z = GpmMath.FFT.GetFFT(DataSetRet.AccData.Z);
                    DataSetRet.FFTData.FreqsVec = FreqVecCal(DataSetRet.FFTData.X.Count);
                }

                if (IsGetOtherFeatures)
                {
                    if (IsGetFFT)
                    {
                        DataSetRet.Features.VibrationEnergy.X = GpmMath.Stastify.GetOA(DataSetRet.FFTData.X);
                        DataSetRet.Features.VibrationEnergy.Y = GpmMath.Stastify.GetOA(DataSetRet.FFTData.Y);
                        DataSetRet.Features.VibrationEnergy.Z = GpmMath.Stastify.GetOA(DataSetRet.FFTData.Z);
                    }
                    DataSetRet.Features.AccP2P.X = GpmMath.Stastify.GetPP(DataSetRet.AccData.X);
                    DataSetRet.Features.AccP2P.Y = GpmMath.Stastify.GetPP(DataSetRet.AccData.Y);
                    DataSetRet.Features.AccP2P.Z = GpmMath.Stastify.GetPP(DataSetRet.AccData.Z);

                    DataSetRet.Features.AccRMS.X = GpmMath.Stastify.RMS(DataSetRet.AccData.X);
                    DataSetRet.Features.AccRMS.Y = GpmMath.Stastify.RMS(DataSetRet.AccData.Y);
                    DataSetRet.Features.AccRMS.Z = GpmMath.Stastify.RMS(DataSetRet.AccData.Z);
                }
            }
            catch (SocketException exp)
            {
                if (DisconnectEvent != null)
                    DisconnectEvent.Invoke(DateTime.Now);
            }
            catch (Exception exp)
            {
                DataSetRet.AccData.X.Clear();
                DataSetRet.AccData.Y.Clear();
                DataSetRet.AccData.Z.Clear();
                DataSetRet.AccData.X.Add(-99999);
                DataSetRet.AccData.Y.Add(-99999);
                DataSetRet.AccData.Z.Add(-99999);
            }

            WaitAsyncForGetDataTask.Set();
        }

        private List<double> FreqVecCal(int FFTWindowSize)
        {
            var freqVec = new List<double>();
            var NysFreq = DataSet.clsFFTData.SamplingRate / 2;
            for (int i = 0; i < FFTWindowSize; i++)
            {
                freqVec.Add((NysFreq / (double)FFTWindowSize) * (double)i);
            }
            return freqVec;
        }


    }
}
