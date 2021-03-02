using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using gpm_module_api.ParticalSensor;
using System.Runtime;
using Microsoft.Win32;
using System.Windows.Forms.DataVisualization.Charting;
using System.Net;

namespace gpm_module_api.Visualize
{
    public partial class ParticleModuleView : UserControl
    {
        private ParticleModuleAPI _ParticleModuleAPI;
        public event Action<ParticleModuleAPI> ParticleItemClickEvent;
        public event Action<ParticleModuleAPI> TemperatureItemClickEvent;
        public event Action<ParticleModuleAPI> HumidityItemClickEvent;
        public event Action<ParticleModuleAPI> IlluminanceItemClickEvent;

        public event EventHandler ThresholdSettingEvent;
        public event EventHandler LoadThresholdSettingEvent;

        public DetailType DetailDataType = DetailType.None;
        public PARTICLE_SIZE DetailParticleSize = PARTICLE_SIZE.PM2Dot5;
        public PARTICLE_DATATYPE DetailParticleDataType = PARTICLE_DATATYPE.Mass;

        private bool IsThresholdSettingOpen = false;
        public enum DetailType
        {
            None,Temperature,Humidity,Illuminance,Particle
        }

        public ParticleModuleView()
        {
            InitializeComponent();
            MainShowTableLayoutPanel.BringToFront();
            TablePanel_DetailShow.ColumnStyles[3].Width = 0;
        }


        public void UpdateModuleInfo(string SensorID = "",string SpotName = "")
        {
            if (SensorID != "")
                ModuleIPLabel.Text = SensorID;
            if (SpotName != "")
                MonitorSpotNameLabel.Text = SpotName;
        }

        public void ModuleAPIBinding(ParticleModuleAPI particleModuleAPI)
        {
            _ParticleModuleAPI = particleModuleAPI;
        }
        public new void Update()
        {
            Invoke((MethodInvoker)delegate
            {
                try
                {
                    TemperatureShowLabel.Text = _ParticleModuleAPI.PreDataSet.Temperature + "";
                    HumidityShowlabel.Text = _ParticleModuleAPI.PreDataSet.Humidity + "";
                    IlluminanceShowlabel.Text = _ParticleModuleAPI.PreDataSet.Illuminance + "";

                    if (_ParticleModuleAPI.PreDataSet.ParticalValueDict.Count != 0)
                    {
                        //PM05MassLabel.Text = _ParticleModuleAPI.PreDataSet.ParticalValueDict[PARTICLE_SIZE.PM0Dot5].Mass + "";
                        PM1Dot0MassLabel.Text = _ParticleModuleAPI.PreDataSet.ParticalValueDict[PARTICLE_SIZE.PM1].Mass + "";
                        PM2Dot5MassLabel.Text = _ParticleModuleAPI.PreDataSet.ParticalValueDict[PARTICLE_SIZE.PM2Dot5].Mass + "";
                        PM4Dot0MassLabel.Text = _ParticleModuleAPI.PreDataSet.ParticalValueDict[PARTICLE_SIZE.PM4].Mass + "";
                        PM10Dot0MassLabel.Text = _ParticleModuleAPI.PreDataSet.ParticalValueDict[PARTICLE_SIZE.PM10].Mass + "";

                        PM05NumberLabel.Text = _ParticleModuleAPI.PreDataSet.ParticalValueDict[PARTICLE_SIZE.PM0Dot5].Number + "";
                        PM1Dot0NumberLabel.Text = _ParticleModuleAPI.PreDataSet.ParticalValueDict[PARTICLE_SIZE.PM1].Number + "";
                        PM2Dot5NumberLabel.Text = _ParticleModuleAPI.PreDataSet.ParticalValueDict[PARTICLE_SIZE.PM2Dot5].Number + "";
                        PM4Dot0NumberLabel.Text = _ParticleModuleAPI.PreDataSet.ParticalValueDict[PARTICLE_SIZE.PM4].Number + "";
                        PM10Dot0NumberLabel.Text = _ParticleModuleAPI.PreDataSet.ParticalValueDict[PARTICLE_SIZE.PM10].Number + "";
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            );
        }

        private void panel5_SizeChanged(object sender, EventArgs e)
        {
            TemperatureUnitLabel.Height = HumidityUnitLabel.Height = IlluUnitLabel.Height = panel5.Height / 2;
        }

        private void TemperatureItemClick(object sender, MouseEventArgs e)
        {
            DetailDataType = DetailType.Temperature;
            ShowDetailChart(DetailType.Temperature);
            TemperatureItemClickEvent.BeginInvoke(_ParticleModuleAPI, null, null);
            LoadThresholdSettingEvent(sender, e);
            Chart_HistoryData.Titles[0].Text = "Temperature";
        }

        public void ImportLimitToChart(double upLimit, double downLimit)
        {
            Invoke((MethodInvoker)delegate {
                Chart_HistoryData.ChartAreas[0].AxisY.StripLines[0].IntervalOffset = upLimit;
                Chart_HistoryData.ChartAreas[0].AxisY.StripLines[1].IntervalOffset = downLimit;
                NUM_UpperLimit.Value = (decimal)upLimit;
                NUM_LowerLimit.Value = (decimal)downLimit;
            });
        }

        private void HumidityItemClick(object sender, MouseEventArgs e)
        {
            DetailDataType = DetailType.Humidity;
            ShowDetailChart(DetailType.Humidity);
            HumidityItemClickEvent.BeginInvoke(_ParticleModuleAPI, null, null);
            LoadThresholdSettingEvent(sender, e);
            Chart_HistoryData.Titles[0].Text = "Humidity";
        }
        private void ParticleItemClick(object sender, MouseEventArgs e)
        {
            Label TargetLabel = sender as Label;
            DetailDataType = DetailType.Particle;
            string ChartTitle = "";
            if (TargetLabel== null)
            {
                ShowDetailChart(DetailType.Particle);
                return;
            }
            if (TargetLabel.Tag != null)
            {
                string[] TagInfo = TargetLabel.Tag.ToString().Split('_');
                switch (TagInfo[1])
                {
                    case "Num":
                        DetailParticleDataType = PARTICLE_DATATYPE.Number;
                        ChartTitle += "Number Concentration";
                        break;
                    case "Mass":
                        DetailParticleDataType = PARTICLE_DATATYPE.Mass;
                        ChartTitle += "Mass Concentration";
                        break;
                    default:
                        break;
                }
                switch (TagInfo[0])
                {
                    case "0.5":
                        DetailParticleSize = PARTICLE_SIZE.PM0Dot5;
                        ChartTitle += " PM 0.5";
                        break;
                    case "1":
                        DetailParticleSize = PARTICLE_SIZE.PM1;
                        ChartTitle += " PM 1";
                        break;
                    case "2.5":
                        DetailParticleSize = PARTICLE_SIZE.PM2Dot5;
                        ChartTitle += " PM 2.5";
                        break;
                    case "4":
                        DetailParticleSize = PARTICLE_SIZE.PM4;
                        ChartTitle += " PM 4";
                        break;
                    case "10":
                        DetailParticleSize = PARTICLE_SIZE.PM10;
                        ChartTitle += " PM 10";
                        break;
                    default:
                        break;
                }
            }
            string OriginDataTitle = Chart_HistoryData.Titles[0].Text;

            
            if (string.IsNullOrEmpty(ChartTitle))
            {
                ShowDetailChart(DetailType.Particle);
            }
            else if (ChartTitle == OriginDataTitle)
            {
                ShowDetailChart(DetailType.Particle);
            }
            else
            {
                Chart_HistoryData.Titles[0].Text = ChartTitle;

                if (!IsDetailShow)
                {
                    ShowDetailChart(DetailType.Particle);
                }
                else
                {
                 //   return;
                }
            }
            ParticleItemClickEvent.BeginInvoke(_ParticleModuleAPI, null, null);
            LoadThresholdSettingEvent(sender, e);
        }

        public void DataOutofThreshold(DetailType DataType, bool IsOutofThreshold, PARTICLE_SIZE ParticleSize = PARTICLE_SIZE.PM0Dot5, PARTICLE_DATATYPE ParticleDataType = PARTICLE_DATATYPE.Mass)
        {
            Label TargetLabel = new Label();
            switch (DataType)
            {
                case DetailType.None:
                    break;
                case DetailType.Temperature:
                    TargetLabel = TemperatureShowLabel;
                    break;
                case DetailType.Humidity:
                    TargetLabel = HumidityShowlabel;
                    break;
                case DetailType.Illuminance:
                    TargetLabel = IlluminanceShowlabel;
                    break;
                case DetailType.Particle:

                    switch (ParticleDataType)
                    {
                        case PARTICLE_DATATYPE.Mass:
                            switch (ParticleSize)
                            {
                                case PARTICLE_SIZE.PM0Dot5:
                                    break;
                                case PARTICLE_SIZE.PM1:
                                    TargetLabel = PM1Dot0MassLabel;
                                    break;
                                case PARTICLE_SIZE.PM2Dot5:
                                    TargetLabel = PM2Dot5MassLabel;
                                    break;
                                case PARTICLE_SIZE.PM4:
                                    TargetLabel = PM4Dot0MassLabel;
                                    break;
                                case PARTICLE_SIZE.PM10:
                                    TargetLabel = PM10Dot0MassLabel;
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case PARTICLE_DATATYPE.Number:
                            switch (ParticleSize)
                            {
                                case PARTICLE_SIZE.PM0Dot5:
                                    TargetLabel = PM05NumberLabel;
                                    break;
                                case PARTICLE_SIZE.PM1:
                                    TargetLabel = PM1Dot0NumberLabel;
                                    break;
                                case PARTICLE_SIZE.PM2Dot5:
                                    TargetLabel = PM2Dot5NumberLabel;
                                    break;
                                case PARTICLE_SIZE.PM4:
                                    TargetLabel = PM4Dot0NumberLabel;
                                    break;
                                case PARTICLE_SIZE.PM10:
                                    TargetLabel = PM10Dot0NumberLabel;
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case PARTICLE_DATATYPE.TypicalSize:
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
            Invoke((MethodInvoker)delegate
            {
                TargetLabel.ForeColor = IsOutofThreshold ? Color.Red : Color.White;
            });
        }

        private void IllumiueItemClick(object sender, MouseEventArgs e)
        {
            DetailDataType = DetailType.Illuminance;
            ShowDetailChart(DetailType.Illuminance);
            IlluminanceItemClickEvent.BeginInvoke(_ParticleModuleAPI,null,null);
            LoadThresholdSettingEvent(sender, e);
            Chart_HistoryData.Titles[0].Text = "Illuminance";
        }

        public void ImportDetailData(DateTime[] queue_Timestamp, double[] targetDataQueue)
        {
            Invoke((MethodInvoker)delegate
            { 
                Chart_HistoryData.Series[0].Points.DataBindXY(queue_Timestamp, targetDataQueue); 
            });
            
        }

        public bool IsDetailShow = false;

        private void ShowDetailChart(DetailType DataType)
        {
            IsDetailShow = !IsDetailShow;

            Panel TargetPanel = new Panel();
            switch (DataType)
            {
                case DetailType.None:
                    break;
                case DetailType.Temperature:
                    TargetPanel = TemperatureShowPanel;
                    break;
                case DetailType.Humidity:
                    TargetPanel = HumidityShowPanel;
                    break;
                case DetailType.Illuminance:
                    TargetPanel = IlluminancePanel;
                    break;
                case DetailType.Particle:
                    TargetPanel = ParticleShowPanel;
                    break;
                default:
                    break;
            }
            TargetPanel.Parent = (TargetPanel.Parent == MainShowTableLayoutPanel) ? Panel_DetailItem : MainShowTableLayoutPanel;
            TargetPanel.Dock = DockStyle.Fill;
            if (TargetPanel.Parent == MainShowTableLayoutPanel)
            {
                MainShowTableLayoutPanel.BringToFront();
            }
            else
            {
                Panel_DetailShow.BringToFront();
            }
        }

        private void BTN_SaveSetting_Click(object sender, EventArgs e)
        {
            if (NUM_UpperLimit.Value<=NUM_LowerLimit.Value)
            {
                MessageBox.Show("上限必須大於下限");
                return;
            }
            Chart_HistoryData.ChartAreas[0].AxisY.StripLines[0].IntervalOffset = (double)NUM_UpperLimit.Value;
            Chart_HistoryData.ChartAreas[0].AxisY.StripLines[1].IntervalOffset = (double)NUM_LowerLimit.Value;
            double[] Limit = new double[] { (double)NUM_UpperLimit.Value, (double)NUM_LowerLimit.Value };
            object SenderObject = Limit as object;
            
            ThresholdSettingEvent(SenderObject,e);

        }

        private void PictureBox_OpenCloseDetailSetting_Click(object sender, EventArgs e)
        {
            if (IsThresholdSettingOpen)
            {
                PictureBox_OpenCloseDetailSetting.Image = ImageList_ArrowImage.Images[1];
                TablePanel_DetailShow.ColumnStyles[3].Width = 0;
                IsThresholdSettingOpen = false;
            }
            else
            {
                PictureBox_OpenCloseDetailSetting.Image = ImageList_ArrowImage.Images[0];
                TablePanel_DetailShow.ColumnStyles[3].Width = 25;
                IsThresholdSettingOpen = true;
            }
        }
    }
}
