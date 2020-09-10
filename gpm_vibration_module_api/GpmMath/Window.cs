using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gpm_vibration_module_api.GpmMath
{
    public enum WINDOW
    {
        Hamming, Hanning, FlatTop, none
    }
    internal static class Window
    {
        internal static double[] WindowGen(WINDOW window, int width)
        {
            switch (window)
            {
                case WINDOW.Hamming:
                return Window.Hamming(width);
                case WINDOW.Hanning:
                return Window.Hann(width);
                case WINDOW.FlatTop:
                return Window.FlatTop(width);
                case WINDOW.none:
                return BoxWindow(width);
                default:
                return BoxWindow(width);
            }

        }
        private static double[] BoxWindow(int width)
        {
            double[] box = new double[width];
            for (int i = 0; i < width; i++)
                box[i] = 1;
            return box;
        }
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
       

        /// <summary>
        /// Lanczos window.
        /// Periodic version, useful e.g. for FFT purposes.
        /// </summary>
       
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
       
    }
}
