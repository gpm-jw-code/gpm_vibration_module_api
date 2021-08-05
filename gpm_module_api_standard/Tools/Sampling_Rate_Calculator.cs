using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gpm_vibration_module_api.Tools
{
    public class Sampling_Rate_Calculator
    {
        public const int Datasets_minimum = 5;
        public class clsResult
        {
            public double Total_Period;
            public int Total_Points;
            public double X_SamplingRate;
            public double Y_SamplingRate;
            public double Z_SamplingRate;

        }
        /// <summary>
        /// 計算取樣率
        /// </summary>
        internal static clsResult Calibration(List<DataSet> datas, double dbExtFreq)
        {
            var result = new clsResult();
            var sum_sr_x = 0.0;
            var sum_sr_y = 0.0;
            var sum_sr_z = 0.0;
            var data_num = datas.Count;
            for (int i = 0; i < data_num; i++)
            {
                sum_sr_x += CalSamplingRate(datas[i].AccData.X, dbExtFreq, ref result);
                sum_sr_y += CalSamplingRate(datas[i].AccData.Y, dbExtFreq, ref result);
                sum_sr_z += CalSamplingRate(datas[i].AccData.Z, dbExtFreq, ref result);
            }
            result.X_SamplingRate = sum_sr_x / data_num;
            result.Y_SamplingRate = sum_sr_y / data_num;
            result.Z_SamplingRate = sum_sr_z / data_num;

            return result;
        }

        internal static clsResult Calibration(List<List<double>> DataX, List<List<double>> DataY, List<List<double>> DataZ, double dbExtFreq)
        {
            clsResult result = new clsResult();
            var sum_sr_x = 0.0;
            var sum_sr_y = 0.0;
            var sum_sr_z = 0.0;
            foreach (var data in DataX)
            {
                sum_sr_x += CalSamplingRate(data, dbExtFreq, ref result);
            }
            foreach (var data in DataY)
            {
                sum_sr_y += CalSamplingRate(data, dbExtFreq, ref result);
            }
            foreach (var data in DataZ)
            {
                sum_sr_z += CalSamplingRate(data, dbExtFreq, ref result);
            }

            result.X_SamplingRate = sum_sr_x / DataX.Count;
            result.Y_SamplingRate = sum_sr_y / DataY.Count;
            result.Z_SamplingRate = sum_sr_z / DataZ.Count;
            return result;
        }

        private static double CalSamplingRate(List<double> dataPack, double dbExtFreq, ref clsResult result)
        {
            //var zeroCalVal = (dataPack.Max() - dataPack.Min()) / 2;
            var zeroCalVal = dataPack.Average();
            int IndexHead = -1;
            int IndexTail = 0;
            var ZeroCrossNum = 0;
            for (int i = 1; i < dataPack.Count - 1; i++)
            {
                if ((dataPack[i] - zeroCalVal) * (dataPack[i - 1] - zeroCalVal) < 0)
                {
                    if (IndexHead == -1)
                        IndexHead = i;
                    IndexTail = i - 1;
                    ZeroCrossNum++;
                }
            }
            var Period = (double)(ZeroCrossNum - 1) / (double)2; //週期數

            var PtNum = IndexTail - IndexHead + 1;
            var SamplingRate = (double)dbExtFreq * ((double)PtNum / (double)Period);

            result.Total_Period += Period;
            result.Total_Points += PtNum;

            return SamplingRate;
        }
    }
}
