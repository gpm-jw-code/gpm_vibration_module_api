using System;
using System.Collections.Generic;
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
            return RMS(data);
        }
        public static double GetOA(double[] data)
        {
            return RMS(data);
        }

        public static double GetPP(double[] data)
        {
            return PPValCal(data);
        }
        public static double GetPP(List<double> data)
        {
            return PPValCal(data.ToArray());
        }
        private static double PPValCal(double[] data)
        {
            var ppval = 0.0;
            ppval = FindMax(data) - FindMin(data);
            return ppval;
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
            FourierTransform.FFT(Cpl_TD, FourierTransform.Direction.Forward);
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
            FourierTransform.FFT(Cpl_TD, FourierTransform.Direction.Forward);
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


        internal static class FourierTransform
        {
            /// <summary>
            /// Fourier transformation direction.
            /// </summary>
            internal enum Direction
            {
                /// <summary>
                ///   Forward direction of Fourier transformation.
                /// </summary>
                /// 
                Forward = 1,

                /// <summary>
                ///   Backward direction of Fourier transformation.
                /// </summary>
                /// 
                Backward = -1
            };

            /// <summary>
            /// One dimensional Discrete Fourier Transform.
            /// </summary>
            /// 
            /// <param name="data">Data to transform.</param>
            /// <param name="direction">Transformation direction.</param>
            /// 

            internal static void DFT(Numeric.Complex[] data, Direction direction)
            {
                int n = data.Length;
                double arg, cos, sin;
                var dst = new Numeric.Complex[n];

                // for each destination element
                for (int i = 0; i < dst.Length; i++)
                {
                    dst[i] = Numeric.Complex.Zero;

                    arg = -(int)direction * 2.0 * System.Math.PI * (double)i / (double)n;

                    // sum source elements
                    for (int j = 0; j < data.Length; j++)
                    {
                        cos = System.Math.Cos(j * arg);
                        sin = System.Math.Sin(j * arg);

                        double re = data[j].Real * cos - data[j].Imaginary * sin;
                        double im = data[j].Real * sin + data[j].Imaginary * cos;

                        dst[i] += new Numeric.Complex(re, im);
                    }
                }

                // copy elements
                if (direction == Direction.Forward)
                {
                    // devide also for forward transform
                    for (int i = 0; i < data.Length; i++)
                        data[i] /= n;
                }
                else
                {
                    for (int i = 0; i < data.Length; i++)
                        data[i] = dst[i];
                }
            }

            /// <summary>
            /// Two dimensional Discrete Fourier Transform.
            /// </summary>
            /// 
            /// <param name="data">Data to transform.</param>
            /// <param name="direction">Transformation direction.</param>
            /// 
            internal static void DFT2(Numeric.Complex[,] data, Direction direction)
            {
                int n = data.GetLength(0);	// rows
                int m = data.GetLength(1);	// columns
                double arg, cos, sin;
                var dst = new Numeric.Complex[System.Math.Max(n, m)];

                // process rows
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < dst.Length; j++)
                    {
                        dst[j] = Numeric.Complex.Zero;

                        arg = -(int)direction * 2.0 * System.Math.PI * (double)j / (double)m;

                        // sum source elements
                        for (int k = 0; k < m; k++)
                        {
                            cos = System.Math.Cos(k * arg);
                            sin = System.Math.Sin(k * arg);

                            double re = data[i, k].Real * cos - data[i, k].Imaginary * sin;
                            double im = data[i, k].Real * sin + data[i, k].Imaginary * cos;

                            dst[j] += new Numeric.Complex(re, im);
                        }
                    }

                    // copy elements
                    if (direction == Direction.Forward)
                    {
                        // devide also for forward transform
                        for (int j = 0; j < dst.Length; j++)
                            data[i, j] = dst[j] / m;
                    }
                    else
                    {
                        for (int j = 0; j < dst.Length; j++)
                            data[i, j] = dst[j];
                    }
                }

                // process columns
                for (int j = 0; j < m; j++)
                {
                    for (int i = 0; i < n; i++)
                    {
                        dst[i] = Numeric.Complex.Zero;

                        arg = -(int)direction * 2.0 * System.Math.PI * (double)i / (double)n;

                        // sum source elements
                        for (int k = 0; k < n; k++)
                        {
                            cos = System.Math.Cos(k * arg);
                            sin = System.Math.Sin(k * arg);

                            double re = data[k, j].Real * cos - data[k, j].Imaginary * sin;
                            double im = data[k, j].Real * sin + data[k, j].Imaginary * cos;

                            dst[i] += new Numeric.Complex(re, im);
                        }
                    }

                    // copy elements
                    if (direction == Direction.Forward)
                    {
                        // devide also for forward transform
                        for (int i = 0; i < dst.Length; i++)
                            data[i, j] = dst[i] / n;
                    }
                    else
                    {
                        for (int i = 0; i < dst.Length; i++)
                            data[i, j] = dst[i];
                    }
                }
            }


            /// <summary>
            /// One dimensional Fast Fourier Transform.
            /// </summary>
            /// 
            /// <param name="data">Data to transform.</param>
            /// <param name="direction">Transformation direction.</param>
            /// 
            /// <remarks><para><note>The method accepts <paramref name="data"/> array of 2<sup>n</sup> size
            /// only, where <b>n</b> may vary in the [1, 14] range.</note></para></remarks>
            /// 
            /// <exception cref="ArgumentException">Incorrect data length.</exception>
            /// 
            internal static void FFT(Numeric.Complex[] data, Direction direction)
            {
                int n = data.Length;
                int m = Numeric.Tools.Log2(n);

                // reorder data first
                ReorderData(data);

                // compute FFT
                int tn = 1, tm;

                for (int k = 1; k <= m; k++)
                {
                    Numeric.Complex[] rotation = FourierTransform.GetComplexRotation(k, direction);

                    tm = tn;
                    tn <<= 1;

                    for (int i = 0; i < tm; i++)
                    {
                        Numeric.Complex t = rotation[i];

                        for (int even = i; even < n; even += tn)
                        {
                            int odd = even + tm;
                            Numeric.Complex ce = data[even];
                            Numeric.Complex co = data[odd];

                            double tr = co.Real * t.Real - co.Imaginary * t.Imaginary;
                            double ti = co.Real * t.Imaginary + co.Imaginary * t.Real;

                            data[even] += new Numeric.Complex(tr, ti);
                            data[odd] = new Numeric.Complex(ce.Real - tr, ce.Imaginary - ti);
                        }
                    }
                }

                if (direction == Direction.Forward)
                {
                    for (int i = 0; i < data.Length; i++)
                    {
                        data[i] = data[i] / (double)n;
                    }
                }
            }

            /// <summary>
            /// Two dimensional Fast Fourier Transform.
            /// </summary>
            /// 
            /// <param name="data">Data to transform.</param>
            /// <param name="direction">Transformation direction.</param>
            /// 
            /// <remarks><para><note>The method accepts <paramref name="data"/> array of 2<sup>n</sup> size
            /// only in each dimension, where <b>n</b> may vary in the [1, 14] range. For example, 16x16 array
            /// is valid, but 15x15 is not.</note></para></remarks>
            /// 
            /// <exception cref="ArgumentException">Incorrect data length.</exception>
            /// 
            internal static void FFT2(Numeric.Complex[,] data, Direction direction)
            {
                int k = data.GetLength(0);
                int n = data.GetLength(1);

                // check data size
                if (!Numeric.Tools.IsPowerOf2(k) || !Numeric.Tools.IsPowerOf2(n))
                    throw new ArgumentException("The matrix rows and columns must be a power of 2.");

                if (k < minLength || k > maxLength || n < minLength || n > maxLength)
                    throw new ArgumentException("Incorrect data length.");

                // process rows
                var row = new Numeric.Complex[n];

                for (int i = 0; i < k; i++)
                {
                    // copy row
                    for (int j = 0; j < row.Length; j++)
                        row[j] = data[i, j];

                    // transform it
                    FourierTransform.FFT(row, direction);

                    // copy back
                    for (int j = 0; j < row.Length; j++)
                        data[i, j] = row[j];
                }

                // process columns
                var col = new Numeric.Complex[k];

                for (int j = 0; j < n; j++)
                {
                    // copy column
                    for (int i = 0; i < k; i++)
                        col[i] = data[i, j];

                    // transform it
                    FourierTransform.FFT(col, direction);

                    // copy back
                    for (int i = 0; i < k; i++)
                        data[i, j] = col[i];
                }
            }


            private const int minLength = 2;
            private const int maxLength = 16384;
            private const int minBits = 1;
            private const int maxBits = 14;
            private static int[][] reversedBits = new int[maxBits][];
            private static Numeric.Complex[,][] complexRotation = new Numeric.Complex[maxBits, 2][];

            // Get array, indicating which data members should be swapped before FFT
            private static int[] GetReversedBits(int numberOfBits)
            {
                if ((numberOfBits < minBits) || (numberOfBits > maxBits))
                    throw new ArgumentOutOfRangeException();

                // check if the array is already calculated
                if (reversedBits[numberOfBits - 1] == null)
                {
                    int n = Numeric.Tools.Pow2(numberOfBits);
                    int[] rBits = new int[n];

                    // calculate the array
                    for (int i = 0; i < n; i++)
                    {
                        int oldBits = i;
                        int newBits = 0;

                        for (int j = 0; j < numberOfBits; j++)
                        {
                            newBits = (newBits << 1) | (oldBits & 1);
                            oldBits = (oldBits >> 1);
                        }
                        rBits[i] = newBits;
                    }
                    reversedBits[numberOfBits - 1] = rBits;
                }
                return reversedBits[numberOfBits - 1];
            }

            // Get rotation of complex number
            private static Numeric.Complex[] GetComplexRotation(int numberOfBits, Direction direction)
            {
                int directionIndex = (direction == Direction.Forward) ? 0 : 1;

                // check if the array is already calculated
                if (complexRotation[numberOfBits - 1, directionIndex] == null)
                {
                    int n = 1 << (numberOfBits - 1);
                    double uR = 1.0;
                    double uI = 0.0;
                    double angle = System.Math.PI / n * (int)direction;
                    double wR = System.Math.Cos(angle);
                    double wI = System.Math.Sin(angle);
                    double t;
                    Numeric.Complex[] rotation = new Numeric.Complex[n];

                    for (int i = 0; i < n; i++)
                    {
                        rotation[i] = new Numeric.Complex(uR, uI);
                        t = uR * wI + uI * wR;
                        uR = uR * wR - uI * wI;
                        uI = t;
                    }

                    complexRotation[numberOfBits - 1, directionIndex] = rotation;
                }
                return complexRotation[numberOfBits - 1, directionIndex];
            }

            // Reorder data for FFT using
            private static void ReorderData(Numeric.Complex[] data)
            {
                int len = data.Length;

                // check data length
                if ((len < minLength) || (len > maxLength) || (!Numeric.Tools.IsPowerOf2(len)))
                    throw new ArgumentException("Incorrect data length.");

                int[] rBits = GetReversedBits(Numeric.Tools.Log2(len));

                for (int i = 0; i < len; i++)
                {
                    int s = rBits[i];

                    if (s > i)
                    {
                        Numeric.Complex t = data[i];
                        data[i] = data[s];
                        data[s] = t;
                    }
                }
            }
        }

        internal static class WindowFun
        {
            /// <summary>
            /// Hamming window. Named after Richard Hamming.
            /// Symmetric version, useful e.g. for filter design purposes.
            /// </summary>
            public static double[] Hamming(int width)
            {
                const double a = 0.53836;
                const double b = -0.46164;

                double phaseStep = (2.0 * Math.PI) / (width - 1.0);

                var w = new double[width];
                for (int i = 0; i < w.Length; i++)
                {
                    w[i] = a + b * Math.Cos(i * phaseStep);
                }
                return w;
            }

            /// <summary>
            /// Hamming window. Named after Richard Hamming.
            /// Periodic version, useful e.g. for FFT purposes.
            /// </summary>
            public static double[] HammingPeriodic(int width)
            {
                const double a = 0.53836;
                const double b = -0.46164;

                double phaseStep = (2.0 * Math.PI) / width;

                var w = new double[width];
                for (int i = 0; i < w.Length; i++)
                {
                    w[i] = a + b * Math.Cos(i * phaseStep);
                }
                return w;
            }

            /// <summary>
            /// Hann window. Named after Julius von Hann.
            /// Symmetric version, useful e.g. for filter design purposes.
            /// </summary>
            public static double[] Hann(int width)
            {
                double phaseStep = (2.0 * Math.PI) / (width - 1.0);

                var w = new double[width];
                for (int i = 0; i < w.Length; i++)
                {
                    w[i] = 0.5 - 0.5 * Math.Cos(i * phaseStep);
                }
                return w;
            }

            /// <summary>
            /// Hann window. Named after Julius von Hann.
            /// Periodic version, useful e.g. for FFT purposes.
            /// </summary>
            public static double[] HannPeriodic(int width)
            {
                double phaseStep = (2.0 * Math.PI) / width;

                var w = new double[width];
                for (int i = 0; i < w.Length; i++)
                {
                    w[i] = 0.5 - 0.5 * Math.Cos(i * phaseStep);
                }
                return w;
            }

            /// <summary>
            /// Cosine window.
            /// Symmetric version, useful e.g. for filter design purposes.
            /// </summary>
            public static double[] Cosine(int width)
            {
                double phaseStep = Math.PI / (width - 1.0);

                var w = new double[width];
                for (int i = 0; i < w.Length; i++)
                {
                    w[i] = Math.Sin(i * phaseStep);
                }
                return w;
            }

            /// <summary>
            /// Cosine window.
            /// Periodic version, useful e.g. for FFT purposes.
            /// </summary>
            public static double[] CosinePeriodic(int width)
            {
                double phaseStep = Math.PI / width;

                var w = new double[width];
                for (int i = 0; i < w.Length; i++)
                {
                    w[i] = Math.Sin(i * phaseStep);
                }
                return w;
            }

            /// <summary>
            /// Lanczos window.
            /// Symmetric version, useful e.g. for filter design purposes.
            /// </summary>
            //public static double[] Lanczos(int width)
            //{
            //    double phaseStep = 2.0 / (width - 1.0);

            //    var w = new double[width];
            //    for (int i = 0; i < w.Length; i++)
            //    {
            //        w[i] = Trig.Sinc(i * phaseStep - 1.0);
            //    }
            //    return w;
            //}

            /// <summary>
            /// Lanczos window.
            /// Periodic version, useful e.g. for FFT purposes.
            /// </summary>
            //public static double[] LanczosPeriodic(int width)
            //{
            //    double phaseStep = 2.0 / width;

            //    var w = new double[width];
            //    for (int i = 0; i < w.Length; i++)
            //    {
            //        w[i] = Trig.Sinc(i * phaseStep - 1.0);
            //    }
            //    return w;
            //}

            /// <summary>
            /// Gauss window.
            /// </summary>
            public static double[] Gauss(int width, double sigma)
            {
                double a = (width - 1) / 2.0;

                var w = new double[width];
                for (int i = 0; i < w.Length; i++)
                {
                    double exponent = (i - a) / (sigma * a);
                    w[i] = Math.Exp(-0.5 * exponent * exponent);
                }
                return w;
            }

            /// <summary>
            /// Blackman window.
            /// </summary>
            public static double[] Blackman(int width)
            {
                const double alpha = 0.16;
                const double a = 0.5 - 0.5 * alpha;
                const double b = 0.5 * alpha;

                int last = width - 1;
                double c = 2.0 * Math.PI / last;
                double d = 2.0 * c;

                var w = new double[width];
                for (int i = 0; i < w.Length; i++)
                {
                    w[i] = a
                           - 0.5 * Math.Cos(i * c)
                           + b * Math.Cos(i * d);
                }
                return w;
            }

            /// <summary>
            /// Blackman-Harris window.
            /// </summary>
            public static double[] BlackmanHarris(int width)
            {
                const double a = 0.35875;
                const double b = -0.48829;
                const double c = 0.14128;
                const double d = -0.01168;

                int last = width - 1;
                double e = 2.0 * Math.PI / last;
                double f = 2.0 * e;
                double g = 3.0 * e;

                var w = new double[width];
                for (int i = 0; i < w.Length; i++)
                {
                    w[i] = a
                           + b * Math.Cos(e * i)
                           + c * Math.Cos(f * i)
                           + d * Math.Cos(g * i);
                }
                return w;
            }

            /// <summary>
            /// Blackman-Nuttall window.
            /// </summary>
            public static double[] BlackmanNuttall(int width)
            {
                const double a = 0.3635819;
                const double b = -0.4891775;
                const double c = 0.1365995;
                const double d = -0.0106411;

                int last = width - 1;
                double e = 2.0 * Math.PI / last;
                double f = 2.0 * e;
                double g = 3.0 * e;

                var w = new double[width];
                for (int i = 0; i < w.Length; i++)
                {
                    w[i] = a
                           + b * Math.Cos(e * i)
                           + c * Math.Cos(f * i)
                           + d * Math.Cos(g * i);
                }
                return w;
            }

            /// <summary>
            /// Bartlett window.
            /// </summary>
            public static double[] Bartlett(int width)
            {
                int last = width - 1;
                double a = 2.0 / last;
                double b = last / 2.0;

                var w = new double[width];
                for (int i = 0; i < w.Length; i++)
                {
                    w[i] = a * (b - Math.Abs(i - b));
                }
                return w;
            }

            /// <summary>
            /// Bartlett-Hann window.
            /// </summary>
            public static double[] BartlettHann(int width)
            {
                const double a = 0.62;
                const double b = -0.48;
                const double c = -0.38;

                int last = width - 1;
                double d = 1.0 / last;
                double e = 2.0 * Math.PI / last;

                var w = new double[width];
                for (int i = 0; i < w.Length; i++)
                {
                    w[i] = a
                           + b * Math.Abs(i * d - 0.5)
                           + c * Math.Cos(i * e);
                }
                return w;
            }

            /// <summary>
            /// Nuttall window.
            /// </summary>
            public static double[] Nuttall(int width)
            {
                const double a = 0.355768;
                const double b = -0.487396;
                const double c = 0.144232;
                const double d = -0.012604;

                int last = width - 1;
                double e = 2.0 * Math.PI / last;
                double f = 2.0 * e;
                double g = 3.0 * e;

                var w = new double[width];
                for (int i = 0; i < w.Length; i++)
                {
                    w[i] = a
                           + b * Math.Cos(e * i)
                           + c * Math.Cos(f * i)
                           + d * Math.Cos(g * i);
                }
                return w;
            }

            /// <summary>
            /// Flat top window.
            /// </summary>
            public static double[] FlatTop(int width)
            {
                const double a = 1.0;
                const double b = -1.93;
                const double c = 1.29;
                const double d = -0.388;
                const double e = 0.032;

                int last = width - 1;
                double f = 2.0 * Math.PI / last;
                double g = 2.0 * f;
                double h = 3.0 * f;
                double k = 4.0 * f;

                double[] w = new double[width];
                for (int i = 0; i < w.Length; i++)
                {
                    w[i] = a
                           + b * Math.Cos(f * i)
                           + c * Math.Cos(g * i)
                           + d * Math.Cos(h * i)
                           + e * Math.Cos(k * i);
                }
                return w;
            }

            /// <summary>
            /// Uniform rectangular (Dirichlet) window.
            /// </summary>
            public static double[] Dirichlet(int width)
            {
                var w = new double[width];
                for (int i = 0; i < w.Length; i++)
                {
                    w[i] = 1.0;
                }
                return w;
            }

            /// <summary>
            /// Triangular window.
            /// </summary>
            public static double[] Triangular(int width)
            {
                double a = 2.0 / width;
                double b = width / 2.0;
                double c = (width - 1) / 2.0;

                var w = new double[width];
                for (int i = 0; i < w.Length; i++)
                {
                    w[i] = a * (b - Math.Abs(i - c));
                }
                return w;
            }

            /// <summary>
            /// Tukey tapering window. A rectangular window bounded
            /// by half a cosine window on each side.
            /// </summary>
            /// <param name="width">Width of the window</param>
            /// <param name="r">Fraction of the window occupied by the cosine parts</param>
            //public static double[] Tukey(int width, double r = 0.5)
            //{

            //    if (r <= 0)
            //    {
            //        return Generate.Repeat(width, 1.0);
            //    }
            //    else if (r >= 1)
            //    {
            //        return Hann(width);
            //    }

            //    var w = new double[width];
            //    var period = (width - 1) * r;
            //    var step = 2 * Math.PI / period;
            //    var b1 = (int)Math.Floor((width - 1) * r * 0.5 + 1);
            //    var b2 = width - b1;
            //    for (var i = 0; i < b1; i++)
            //    {
            //        w[i] = (1 - Math.Cos(i * step)) * 0.5;
            //    }
            //    for (var i = b1; i < b2; i++)
            //    {
            //        w[i] = 1;
            //    }
            //    for (var i = b2; i < width; i++)
            //    {
            //        w[i] = w[width - i - 1];
            //    }
            //    return w;
            //}
        }
    }
}
