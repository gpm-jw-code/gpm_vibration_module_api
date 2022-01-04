using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JMAlgorithm
{
    /// <summary>
    /// 微積分
    /// </summary>
    public class Class_Calculus
    {
        /// <summary>
        /// 積分
        /// </summary>
        public class Class_Integral
        {
            // 參考資料: https://blog.jiangyayu.cn/archives/Integrals.html/


            /// <summary>
            /// 蒙地卡羅法積分
            /// TimeSeriesData: (ｍ/s^2)
            /// SamplingRate: (Hz)
            /// return 速度 (m/s) 
            /// </summary>
            public static double MonteCarloMethod(double[] TimeSeriesData, double SamplingRate, int RandomSeed = -1)
            {
                Random Rand = new Random();
                if (RandomSeed != -1)
                    Rand = new Random(RandomSeed);

                int N = 100000; // 預計投多少隨機點 
                int count = 0; // 初始狀態

                double Y_upLimit = TimeSeriesData.Max();
                double Y_lowLimit = TimeSeriesData.Min();

                double X_upLimit = TimeSeriesData.Count() / SamplingRate;
                double X_lowLimit = 0;

                int X_upLimit_index = TimeSeriesData.Count();
                int X_lowLimit_index = 0;

                double[] xlist = new double[N];
                double[] ylist = new double[N];

                for (int i = 0; i < N; i++)
                {
                    double y = Y_lowLimit + (Y_upLimit - Y_lowLimit) * Rand.NextDouble();
                    int x_ind = X_lowLimit_index + Rand.Next(X_lowLimit_index, X_upLimit_index);
                    count += Is_UnderTheCurve(TimeSeriesData, y, x_ind);

                    xlist[i] = x_ind;
                    ylist[i] = y;

                }

                double Reg = (Y_upLimit - Y_lowLimit) * (X_upLimit - X_lowLimit);

                double result = Math.Abs(((double)count / N) * ((Y_upLimit - Y_lowLimit) * (X_upLimit - X_lowLimit)));
                return result;
            }

            /// <summary>
            /// 判斷座標數值是否在曲線與X軸之間
            /// 若在曲線與X軸之間 且都在X軸上 回傳1 
            /// 若在曲線與X軸之間 且都在X軸下 回傳-1 
            /// 其他回傳0 
            /// </summary>
            /// <param name="TimeSeriesData"></param>
            /// <param name="y"></param>
            /// <param name="x_ind"></param>
            /// <returns></returns>
            private static int Is_UnderTheCurve(double[] TimeSeriesData, double y, int x_ind)
            {
                var xx = TimeSeriesData[x_ind];
                if (TimeSeriesData[x_ind] > 0)
                {
                    if (TimeSeriesData[x_ind] >= y)
                        if (y >= 0)
                            return 1;
                }
                else if (TimeSeriesData[x_ind] < 0)
                {
                    if (TimeSeriesData[x_ind] <= y)
                        if (y <= 0)
                            return -1;
                }
                return 0;
            }



            /// <summary>
            /// 梯形面積法積分
            /// TimeSeriesData: (ｍ/s^2)
            /// SamplingRate: (Hz)
            /// return 速度 (m/s) 
            /// </summary>
            /// <param name="TimeSeriesData"></param>
            /// <param name="SamplingRate"></param>
            /// <returns></returns>
            public static double TrapezoidalAreaMethod(double[] TimeSeriesData, double SamplingRate)
            {
                double step = 1 / SamplingRate;

                double Area = 0;
                for (int i = 1; i < TimeSeriesData.Length; i++)
                {
                    Area += (TimeSeriesData[i - 1] + TimeSeriesData[i]) * step / 2; // 梯形面積
                }
                return Area;
            }


            public static double[] TrapezoidalAreaMethod_SeriesOut(double[] TimeSeriesData, double SamplingRate)
            {
                double step = 1 / SamplingRate;
                double Area0 = 0;
                double[] Out_TimeSeriesData = new double[TimeSeriesData.Length];
                Out_TimeSeriesData[0] = 0;
                for (int i = 1; i < TimeSeriesData.Length; i++)
                {
                    double SetpArea = (TimeSeriesData[i - 1] + TimeSeriesData[i]) * step / 2; // 梯形面積
                    Area0 += SetpArea;
                    Out_TimeSeriesData[i] = Area0;
                }

                return Out_TimeSeriesData;
            }
        }
    }
}
