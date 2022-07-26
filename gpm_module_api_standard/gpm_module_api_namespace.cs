﻿using gpm_module_api.Data;
using gpm_module_api.ParticalSensor;
using gpm_module_api.UVSensor;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;
/// <summary>
/// 
/// </summary>
namespace gpm_module_api.VibrationSensor
{
    /// <summary>
    /// 數據封包擷取模式
    /// </summary>
    public enum DAQMode
    {
        /// <summary>
        /// 取樣率8K，With Gap
        /// </summary>
        High_Sampling = 8000,
        /// <summary>
        /// 取樣率4K(如果BR=460800), Without Gap
        /// </summary>
        Low_Sampling = 4000,
        BULK = 8001
    }
    public class GPMModuleAPI : gpm_vibration_module_api.GPMModuleAPI_HSR
    {
        public GPMModulesServer.ConnectInState Obj { get; }

        public async Task<int> Connect(string IP, int Port)
        {
            return await base.Connect(IP, Port);
        }

        public GPMModuleAPI(Socket socket) 
        {

        }

        public GPMModuleAPI() 
        {
           
        }

        public GPMModuleAPI(GPMModulesServer.ConnectInState obj)
        {
            Obj = obj;
        }

        public async Task<int> DAQModeSetting(DAQMode Mode)
        {
            return await base.DAQModeSetting(( DAQMode)Mode);
        }

        public async Task<int> Measure_Range_Setting(gpm_vibration_module_api.clsEnum.Module_Setting_Enum.MEASURE_RANGE range)
        {
            return await Measure_Range_Setting(range);
        }

        public async Task<int> Data_Length_Setting(int N)
        {
            return await Data_Length_Setting(N);
        }

        public async Task<DataSet> GetData(bool IsGetFFT, bool IsGetOtherFeatures)
        {
            var DataSet_base = await base.GetData(IsGetFFT, IsGetOtherFeatures);
            return new DataSet(base.SamplingRate)
            {
                AccData = DataSet_base.AccData,
                FFTData = DataSet_base.FFTData,
                Features = DataSet_base.Features,
                ErrorCode = DataSet_base.ErrorCode,
                TimeSpend = DataSet_base.TimeSpend,
                RecieveTime = DataSet_base.RecieveTime,
                IsReady = DataSet_base.IsReady,
                MelBankData = DataSet_base.MelBankData

            };
        }

        public static implicit operator GPMModuleAPI(UVSensorAPI v)
        {
            throw new NotImplementedException();
        }

        public static implicit operator GPMModuleAPI(ParticleModuleAPI v)
        {
            throw new NotImplementedException();
        }
    }
}

namespace gpm_module_api.Data
{

    public class DataSet : gpm_vibration_module_api.DataSet
    {
        double samplingRate = 0;
        public DataSet(double samplingRate) : base(samplingRate)
        {
            this.samplingRate = samplingRate;
        }
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}

namespace gpm_module_api
{
    public class WINDOW : gpm_vibration_module_api.GpmMath.Window
    {
    }
    public class clsEnum : gpm_vibration_module_api.clsEnum
    {

    }
}