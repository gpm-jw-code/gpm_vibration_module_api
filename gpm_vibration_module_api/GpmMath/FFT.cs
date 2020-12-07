using FftSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static gpm_vibration_module_api.GpmMath.Numeric;

namespace gpm_vibration_module_api.GpmMath
{
    internal class Stastify
    {
        internal static double GetMean(List<double> data)
        {
            var sum = 0.0;
            foreach (var val in data)
            {
                sum += val;
            }
            return sum / data.Count;
        }
        public static double FindMax(double[] arr)
        {
            double max = arr[0];
            for (int i = 1; i < arr.Length; i++)
                if (arr[i] > max)
                    max = arr[i];
            return max;
        }
        private static double FindMin(double[] arr)
        {
            double min = arr[0];
            for (int i = 1; i < arr.Length; i++)
                if (arr[i] < min)
                    min = arr[i];
            return min;
        }
        public static double RMS(double[] DataVec)
        {
            double SumOfSquare = 0;
            foreach (double Si in DataVec)
                SumOfSquare += Math.Pow(Si, 2);
            return Math.Sqrt(SumOfSquare);
        }
        public static double RMS(List<double> DataVec)
        {
            double SumOfSquare = 0;
            foreach (double Si in DataVec)
                SumOfSquare += Math.Pow(Si, 2);
            return Math.Sqrt(SumOfSquare);
        }

        public static double GetOA(List<double> data)
        {
            if (data == null)
                return -1;
            return RMS(data);
        }
        public static double GetOA(double[] data)
        {
            return GetOA(data.ToList());
        }

        public static double GetPP(double[] data)
        {
            return PPValCal(data);
        }
        public static double GetPP(List<double> data)
        {
            return GetPP(data.ToArray());
        }
        private static double PPValCal(double[] data)
        {
            if (data.Length == 0)
                return -1;
            return FindMax(data) - FindMin(data); ;
        }
    }
    internal class FFT
    {
        public static double[] GetFFT(double[] TD)
        {
            int N = TD.Length;
            double[] FFT = new double[N / 2];
            Numeric.Complex[] Cpl_TD = new Numeric.Complex[N];
            for (int i = 0; i < N; i++)
                Cpl_TD[i] = TD[i];
            Transform.FFT(Cpl_TD);
            for (int i = 0; i < N / 2; i++)
                FFT[i] = Cpl_TD[i].Magnitude;
            FFT[0] = 0;
            return FFT;
        }

        public static List<double> GetFFT(List<double> TD, bool IsZeroAdd = false, int FFTWindow = 2048)
        {

            List<double> toConvertTimeData = new List<double>();
            toConvertTimeData.AddRange(TD);
            if (IsZeroAdd)
            {

                var numToAdd = FFTWindow - TD.Count;
                toConvertTimeData.AddRange(new double[numToAdd]);
                //var _window = WindowFun.Hamming(toConvertTimeData.Count);
                for (int i = 0; i < toConvertTimeData.Count; i++)
                {
                    //toConvertTimeData[i] = toConvertTimeData[i] * _window[i];
                    toConvertTimeData[i] = toConvertTimeData[i];
                }

            }
            int N = toConvertTimeData.Count;
            List<double> FFT = new List<double>();
            Numeric.Complex[] Cpl_TD = new Numeric.Complex[N];
            for (int i = 0; i < N; i++)
                Cpl_TD[i] = toConvertTimeData[i];
            Transform.FFT(Cpl_TD);
            for (int i = 0; i < N / 2; i++)
            {
                FFT.Add(Cpl_TD[i].Magnitude);
            }   // FFT.Add(Cpl_TD[i].Magnitude / (N / 2));
            FFT[0] = 0;
            return FFT;
        }



        /// <summary>
        /// 計算OA值
        /// </summary>
        /// <param name="DataVec"></param>
        /// <returns></returns>

    }
}
