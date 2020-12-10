using gpm_module_api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using gpm_module_api.ParticalSensor;
using gpm_module_api.Visualize;
using System.Net.Sockets;
using gpm_vibration_module_api;
using gpm_vibration_module_api.Tools;

namespace gpm_module_api.ParticalSensor
{
    /// <summary>
    /// 讓使用者可以訪問粒子感測器
    /// </summary>
    public class ParticleModuleAPI : GPMModuleAPI
    {
        public struct DataTemporary
        {
            public Queue<DateTime> Queue_Timestamp;
            public Queue<double> Queue_Temperature;
            public Queue<double> Queue_Humidity;
            public Queue<double> Queue_Illuminance;
            public Queue<double> Queue_TypicalParticleSize;
            public Dictionary<PARTICLE_SIZE, Queue<double>> Dict_QueueMassConcentration;
            public Dictionary<PARTICLE_SIZE, Queue<double>> Dict_QueueNumberConcentration;
        }

        public ParticleModuleView View;
        internal ParticleDataSet _ParticleDataSet;
        internal ParticleDataSet PreDataSet = new ParticleDataSet();
        public DataTemporary TempData = new DataTemporary();
        public ParticleModuleView.DetailType NowDataType = ParticleModuleView.DetailType.None;

        public void DataTemporaryInitial()
        {
            TempData.Queue_Timestamp = new Queue<DateTime>();
            TempData.Queue_Temperature = new Queue<double>();
            TempData.Queue_Humidity = new Queue<double>();
            TempData.Queue_Illuminance = new Queue<double>();
            TempData.Queue_TypicalParticleSize = new Queue<double>();
            TempData.Dict_QueueMassConcentration = new Dictionary<PARTICLE_SIZE, Queue<double>>()
            {
                { PARTICLE_SIZE.PM0Dot5,new Queue<double>() },
                {PARTICLE_SIZE.PM1,new Queue<double>()},
                { PARTICLE_SIZE.PM2Dot5,new Queue<double>()},
                { PARTICLE_SIZE.PM4,new Queue<double>()},
                {PARTICLE_SIZE.PM10,new Queue<double>() }
            };
            TempData.Dict_QueueNumberConcentration = new Dictionary<PARTICLE_SIZE, Queue<double>>()
            {
                { PARTICLE_SIZE.PM0Dot5,new Queue<double>() },
                {PARTICLE_SIZE.PM1,new Queue<double>()},
                { PARTICLE_SIZE.PM2Dot5,new Queue<double>()},
                { PARTICLE_SIZE.PM4,new Queue<double>()},
                {PARTICLE_SIZE.PM10,new Queue<double>() }
            };
        }

        public ParticleModuleAPI(GPMModulesServer.ConnectInState _ConnectObj = null) : base(_ConnectObj)
        {
            base.module_base = new ParticleModuleBase();
            View = new ParticleModuleView();
            View.ModuleAPIBinding(this);
            DataTemporaryInitial();
            View.HumidityItemClickEvent += new Action<ParticleModuleAPI>(HumidityDetailShow);
            View.TemperatureItemClickEvent += new Action<ParticleModuleAPI>(TemperatureDetailShow);
            View.IlluminanceItemClickEvent += new Action<ParticleModuleAPI>(IlluminanceDetailShow);
            View.ParticleItemClickEvent += new Action<ParticleModuleAPI>(ParticleDetailShow);
        }

        private void ParticleDetailShow(ParticleModuleAPI obj)
        {
            UpdateDetailShow(ParticleModuleView.DetailType.Particle, View.DetailParticleDataType, View.DetailParticleSize);
        }

        private void IlluminanceDetailShow(ParticleModuleAPI obj)
        {
            UpdateDetailShow(ParticleModuleView.DetailType.Illuminance);
        }

        private void TemperatureDetailShow(ParticleModuleAPI obj)
        {
            UpdateDetailShow(ParticleModuleView.DetailType.Temperature);
        }

        private void HumidityDetailShow(ParticleModuleAPI obj)
        {
            UpdateDetailShow(ParticleModuleView.DetailType.Humidity);
        }

        private void UpdateDetailShow(ParticleModuleView.DetailType TargetDataType, PARTICLE_DATATYPE ParticleData = PARTICLE_DATATYPE.Mass, PARTICLE_SIZE ParticleSize = PARTICLE_SIZE.PM2Dot5)
        {
            if (!View.IsDetailShow)
            {
                NowDataType = ParticleModuleView.DetailType.None;
                return;
            }
            else
            {
                NowDataType = TargetDataType;
                Queue<double> TargetDataQueue = new Queue<double>();
                switch (NowDataType)
                {
                    case ParticleModuleView.DetailType.None:
                        break;
                    case ParticleModuleView.DetailType.Temperature:
                        TargetDataQueue = TempData.Queue_Temperature;
                        break;
                    case ParticleModuleView.DetailType.Humidity:
                        TargetDataQueue = TempData.Queue_Humidity;
                        break;
                    case ParticleModuleView.DetailType.Illuminance:
                        TargetDataQueue = TempData.Queue_Illuminance;
                        break;
                    case ParticleModuleView.DetailType.Particle:
                        switch (ParticleData)
                        {
                            case PARTICLE_DATATYPE.Mass:
                                TargetDataQueue = TempData.Dict_QueueMassConcentration[ParticleSize];
                                break;
                            case PARTICLE_DATATYPE.Number:
                                TargetDataQueue = TempData.Dict_QueueNumberConcentration[ParticleSize];
                                break;
                            case PARTICLE_DATATYPE.TypicalSize:
                                TargetDataQueue = TempData.Queue_TypicalParticleSize;
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }
                View.ImportDetailData(TempData.Queue_Timestamp.ToArray(), TargetDataQueue.ToArray());
            }
        }

        private void UpdateDetailShow()
        {
            if (!View.IsDetailShow)
            {
                return;
            }
            Queue<double> TargetDataQueue = new Queue<double>();
            switch (NowDataType)
            {
                case ParticleModuleView.DetailType.None:
                    return;
                    break;
                case ParticleModuleView.DetailType.Temperature:
                    TargetDataQueue = TempData.Queue_Temperature;
                    break;
                case ParticleModuleView.DetailType.Humidity:
                    TargetDataQueue = TempData.Queue_Humidity;
                    break;
                case ParticleModuleView.DetailType.Illuminance:
                    TargetDataQueue = TempData.Queue_Illuminance;
                    break;
                case ParticleModuleView.DetailType.Particle:
                    switch (View.DetailParticleDataType)
                    {
                        case PARTICLE_DATATYPE.Mass:
                            TargetDataQueue = TempData.Dict_QueueMassConcentration[View.DetailParticleSize];
                            break;
                        case PARTICLE_DATATYPE.Number:
                            TargetDataQueue = TempData.Dict_QueueNumberConcentration[View.DetailParticleSize];
                            break;
                        case PARTICLE_DATATYPE.TypicalSize:
                            TargetDataQueue = TempData.Queue_TypicalParticleSize;
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
            View.ImportDetailData(TempData.Queue_Timestamp.ToArray(), TargetDataQueue.ToArray());
        }

        public new event Action<DateTime> DisconnectEvent;

        internal override void GetDataTask()
        {
            _ParticleDataSet = new ParticleDataSet();
            try
            {
                byte[] dataByteAry;
                bool IsTimeout;
                var SCK_STATE = module_base.GetAccData_HighSpeedWay(out _ParticleDataSet.TimeSpend, out IsTimeout);
                dataByteAry = SCK_STATE.data_rev_;
               _ParticleDataSet.ErrorCode = IsTimeout ? Convert.ToInt32(clsErrorCode.Error.DATA_GET_TIMEOUT) : 0;
                module_base.isBusy = false;
                if (dataByteAry.Length < 62)
                {
                    _ParticleDataSet.ErrorCode = Convert.ToInt32(clsErrorCode.Error.DATA_GET_TIMEOUT);
                    _ParticleDataSet = PreDataSet;
                    WaitAsyncForGetDataTask.Set();
                    return;
                }
                if (_ParticleDataSet.ErrorCode != 0)
                {
                    WaitAsyncForGetDataTask.Set();
                    return;
                }
                var ParticleDataSet = ConverterTools.ParticalPacketToDataSet(dataByteAry);
                _ParticleDataSet.Humidity = ParticleDataSet.Humidity;
                _ParticleDataSet.Temperature = ParticleDataSet.Temperature;
                _ParticleDataSet.Illuminance = ParticleDataSet.Illuminance;
                _ParticleDataSet.ParticalValueDict = ParticleDataSet.ParticalValueDict;
                _ParticleDataSet.TypicalParticleSize = ParticleDataSet.TypicalParticleSize;
                _ParticleDataSet.Res1 = ParticleDataSet.Res1;
                _ParticleDataSet.Res2 = ParticleDataSet.Res2;
                PreDataSet = _ParticleDataSet;
                AddNewDataToTempData(ParticleDataSet);
                UpdateDetailShow();
            }
            catch (SocketException exp)
            {
                if (DisconnectEvent != null)
                    DisconnectEvent.Invoke(DateTime.Now);
            }
            catch (Exception exp)
            {

            }

            WaitAsyncForGetDataTask.Set();
        }

        public new async Task<ParticleDataSet> GetData()
        {
            if (module_base.isBusy)
            {
                return new ParticleDataSet
                {
                    Humidity = _ParticleDataSet.Humidity,
                    Illuminance = _ParticleDataSet.Illuminance,
                    ParticalValueDict = _ParticleDataSet.ParticalValueDict,
                    Res1 = _ParticleDataSet.Res1,
                    Res2 = _ParticleDataSet.Res2,
                    Temperature = _ParticleDataSet.Temperature,
                    TypicalParticleSize = _ParticleDataSet.TypicalParticleSize,
                    ErrorCode = Convert.ToInt32(clsErrorCode.Error.ModuleIsBusy)
                };
            }

            // if (Connected == false)
            //     return new DataSet(module_base.SamplingRate) { ErrorCode = Convert.ToInt32(clsErrorCode.Error.NoConnection) };
            WaitAsyncForParametersSet.Set();
            WaitAsyncForGetDataTask.Reset();
            await Task.Run(() => GetDataTask());
            WaitAsyncForGetDataTask.WaitOne();
            View.Update();
            return PreDataSet;
        }

        public void AddNewDataToTempData(ParticleDataSet NewDataSet)
        {
            if (NewDataSet.ErrorCode != 0)
            {
                return;
            }

            if (NewDataSet.ParticalValueDict.Count != 5)
            {
                return;
            }

            TempData.Queue_Timestamp.Enqueue(DateTime.Now);
            TempData.Queue_Temperature.Enqueue(NewDataSet.Temperature);
            TempData.Queue_Humidity.Enqueue(NewDataSet.Humidity);
            TempData.Queue_Illuminance.Enqueue(NewDataSet.Illuminance);
            TempData.Dict_QueueMassConcentration[PARTICLE_SIZE.PM0Dot5].Enqueue(NewDataSet.ParticalValueDict[PARTICLE_SIZE.PM0Dot5].Mass);
            TempData.Dict_QueueMassConcentration[PARTICLE_SIZE.PM1].Enqueue(NewDataSet.ParticalValueDict[PARTICLE_SIZE.PM1].Mass);
            TempData.Dict_QueueMassConcentration[PARTICLE_SIZE.PM2Dot5].Enqueue(NewDataSet.ParticalValueDict[PARTICLE_SIZE.PM2Dot5].Mass);
            TempData.Dict_QueueMassConcentration[PARTICLE_SIZE.PM4].Enqueue(NewDataSet.ParticalValueDict[PARTICLE_SIZE.PM4].Mass);
            TempData.Dict_QueueMassConcentration[PARTICLE_SIZE.PM10].Enqueue(NewDataSet.ParticalValueDict[PARTICLE_SIZE.PM10].Mass);
            TempData.Dict_QueueNumberConcentration[PARTICLE_SIZE.PM0Dot5].Enqueue(NewDataSet.ParticalValueDict[PARTICLE_SIZE.PM0Dot5].Number);
            TempData.Dict_QueueNumberConcentration[PARTICLE_SIZE.PM1].Enqueue(NewDataSet.ParticalValueDict[PARTICLE_SIZE.PM1].Number);
            TempData.Dict_QueueNumberConcentration[PARTICLE_SIZE.PM2Dot5].Enqueue(NewDataSet.ParticalValueDict[PARTICLE_SIZE.PM2Dot5].Number);
            TempData.Dict_QueueNumberConcentration[PARTICLE_SIZE.PM4].Enqueue(NewDataSet.ParticalValueDict[PARTICLE_SIZE.PM4].Number);
            TempData.Dict_QueueNumberConcentration[PARTICLE_SIZE.PM10].Enqueue(NewDataSet.ParticalValueDict[PARTICLE_SIZE.PM10].Number);
            TempData.Queue_TypicalParticleSize.Enqueue(NewDataSet.TypicalParticleSize);

            if (TempData.Queue_Timestamp.Count > 100)
            {
                TempData.Queue_Timestamp.Dequeue();
                TempData.Queue_Temperature.Dequeue();
                TempData.Queue_Humidity.Dequeue();
                TempData.Queue_Illuminance.Dequeue();
                TempData.Dict_QueueMassConcentration[PARTICLE_SIZE.PM0Dot5].Dequeue();
                TempData.Dict_QueueMassConcentration[PARTICLE_SIZE.PM1].Dequeue();
                TempData.Dict_QueueMassConcentration[PARTICLE_SIZE.PM2Dot5].Dequeue();
                TempData.Dict_QueueMassConcentration[PARTICLE_SIZE.PM4].Dequeue();
                TempData.Dict_QueueMassConcentration[PARTICLE_SIZE.PM10].Dequeue();
                TempData.Dict_QueueNumberConcentration[PARTICLE_SIZE.PM0Dot5].Dequeue();
                TempData.Dict_QueueNumberConcentration[PARTICLE_SIZE.PM1].Dequeue();
                TempData.Dict_QueueNumberConcentration[PARTICLE_SIZE.PM2Dot5].Dequeue();
                TempData.Dict_QueueNumberConcentration[PARTICLE_SIZE.PM4].Dequeue();
                TempData.Dict_QueueNumberConcentration[PARTICLE_SIZE.PM10].Dequeue();
                TempData.Queue_TypicalParticleSize.Dequeue();
            }
        }

    }
}
