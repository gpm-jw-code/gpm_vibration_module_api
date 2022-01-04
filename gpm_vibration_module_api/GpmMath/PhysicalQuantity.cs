using gpm_vibration_module_api.DataSets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JMAlgorithm
{
    public partial class PhysicalQuantity
    {

        /// <summary>
        /// 計算速度 
        /// 輸入為加速度序列(double[]), 單位G   
        /// </summary>
        /// <param name="G_array"></param>
        /// <returns></returns>
        public static Series_Data_Properties GetVelocity(double[] G_array, double SamplingRate)
        {
            G_array = Cancel_offset(G_array); // 消除重力加速度的影響 

            G_array = G_array.Select(x => x * 9.81).ToArray(); // G 換算成 m/s^2

            double[] Velocity_series = Class_Calculus.Class_Integral.TrapezoidalAreaMethod_SeriesOut(G_array, SamplingRate);
            Velocity_series = Velocity_series.Select(x => x * 1000).ToArray();  // m/s 換算成 mm/s 
            Velocity_series = Cancel_offset(Velocity_series); // 速度數據對正, 消除加速度振動相位差造成的影響 

            Series_Data_Properties Velocity_Data = new Series_Data_Properties();
            Velocity_Data.RMS = Velocity_series.RootMeanSquare();
            Velocity_Data.P2P = Velocity_series.Max() - Velocity_series.Min();
            Velocity_Data.series = Velocity_series;
            Velocity_Data.Physical_unit = "mm/s";

            return Velocity_Data;
        }

        /// <summary>
        /// 計算位移量 
        /// 輸入為加速度序列(double[]), 單位 m/s 
        /// </summary>
        /// <param name="G_array"></param>
        /// <param name="SamplingRate"></param>
        /// <returns></returns>
        public static Series_Data_Properties GetDisplacement(double[] Vel_array, double SamplingRate)
        {
            //// 速度數據要消除offset, 因為有可能因為加速度的相位差造成兩次積分後出現巨大的誤差,
            //// 大部分振動, 淨位移量不會太大 
            //Vel_array = Cancel_offset(Vel_array);
            // 位移計算已經消除offset, 不用再做一次

            double[] Displacement_series = Class_Calculus.Class_Integral.TrapezoidalAreaMethod_SeriesOut(Vel_array, SamplingRate);
            Displacement_series = Displacement_series.Select(x => x * 1000).ToArray();  // mm 換算成 ㎛ 

            Displacement_series = Cancel_offset(Displacement_series); // 位移量對正, 以確保RMS正確性 

            Series_Data_Properties Displacement_Data = new Series_Data_Properties();
            Displacement_Data.RMS = Displacement_series.RootMeanSquare();
            Displacement_Data.P2P = Displacement_series.Max() - Displacement_series.Min();
            Displacement_Data.series = Displacement_series;
            Displacement_Data.Physical_unit = "µm";

            return Displacement_Data;
        }

        /// <summary>
        /// 消除數列的offset, 以平均值為中心校正 
        /// 主要用來消除地球引力造成的offset 
        /// </summary>
        /// <returns></returns>
        private static double[] Cancel_offset(double[] G_array)
        {
            double Offset = G_array.Average();
            G_array = G_array.Select(x => x - Offset).ToArray();

            return G_array;
        }

    }

    public static class Extension
    {
        public static double RootMeanSquare(this double[] datas)
        {
            double squareSum = datas.ToList().Sum(d => d * d);

            return Math.Sqrt(squareSum / datas.Length);
        }
    }
}
