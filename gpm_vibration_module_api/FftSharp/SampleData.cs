﻿using System;
using System.Drawing;

namespace FftSharp
{
    internal static class SampleData
    {
        public static double[] Times(int sampleRate, int pointCount)
        {
            double periodSec = 1.0 / sampleRate;
            double[] times = new double[pointCount];
            for (int i = 0; i < pointCount; i++)
                times[i] = i * periodSec;
            return times;
        }

        public static double[] OddSines(int pointCount = 128, int sineCount = 2)
        {
            double[] values = new double[pointCount];
            for (int i = 0; i < pointCount; i++)
            {
                for (int s = 0; s < sineCount; s++)
                {
                    double mult = 1 + 2 * s;
                    values[i] += 1 / mult * Math.Sin(mult * i / Math.PI);
                }
            }
            return values;
        }

        public static void AddSin(double[] data, int sampleRate, double frequency, double magnitude = 1)
        {
            for (int i = 0; i < data.Length; i++)
                data[i] += Math.Sin(i * frequency / sampleRate * 2 * Math.PI) * magnitude;
        }

        public static void AddOffset(double[] data, double offset = 0)
        {
            for (int i = 0; i < data.Length; i++)
                data[i] += offset;
        }

        public static void AddWhiteNoise(double[] data, double magnitude = 1, double offset = 0, int? seed = 0)
        {
            Random rand = (seed.HasValue) ? new Random(seed.Value) : new Random();
            for (int i = 0; i < data.Length; i++)
                data[i] += (rand.NextDouble() - .5) * magnitude + offset;
        }

        public static double[] RandomNormal(int pointCount, double mean = .5, double stdDev = .5, int? seed = 0)
        {
            Random rand = (seed.HasValue) ? new Random(seed.Value) : new Random();
            double[] values = new double[pointCount];
            for (int i = 0; i < values.Length; i++)
            {
                double u1 = 1.0 - rand.NextDouble();
                double u2 = 1.0 - rand.NextDouble();
                double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
                values[i] = mean + stdDev * randStdNormal;
            }

            return values;
        }

        public static double[] SampleAudio1()
        {
            /* This sample data was created to serve as a standard sample to test
             * this library against other similar libraries, including libraries for
             * other programming languages.
             * 
             * These data simulate a sample of audio with the following parameters:
             *   sample rate: 48 kHz
             *   points: 512 (2^9)
             *   offset: 0.1 (above zero)
             *   tone: 2 kHz (amplitude 2)
             *   tone: 10 kHz (amplitude 10)
             *   tone: 20 kHz (amplitude .5)
             *   
             * The sum of these points should be: 71.52
             */

            double[] audio =
            {
                0.330, 2.150, 1.440, 1.370, 0.240, 2.600, 3.510, 1.980, 1.880, 0.080,
                1.820, 1.300, 0.230, -1.160, -1.350, -0.580, -0.840, -1.350, -2.720, -2.530,
                -0.020, -0.760, -0.480, -2.100, 0.300, 1.860, 1.600, 1.490, 0.580, 2.120,
                2.790, 1.990, 1.200, 0.800, 2.180, 1.600, -0.370, -1.250, -1.990, 0.350,
                -1.190, -1.620, -3.280, -2.570, 0.070, -0.810, -1.130, -1.680, -0.250, 1.550,
                1.080, 1.530, 0.650, 2.530, 2.790, 2.420, 1.720, 0.540, 2.390, 1.510,
                0.220, -1.420, -1.440, 0.290, -1.610, -1.500, -3.230, -2.200, -0.010, -1.390,
                -0.470, -1.650, 0.250, 2.050, 1.480, 0.910, 0.760, 2.760, 2.730, 2.450,
                1.090, 0.280, 2.070, 1.160, 0.270, -1.170, -1.500, 0.200, -0.910, -1.580,
                -2.460, -2.550, -0.310, -0.940, -1.130, -1.850, 0.420, 1.560, 0.850, 0.880,
                0.660, 2.730, 3.230, 2.470, 1.120, 0.740, 1.600, 1.730, 0.280, -1.540,
                -2.180, -0.500, -1.090, -1.390, -2.910, -2.690, -0.160, -1.040, -1.240, -1.520,
                -0.390, 1.690, 1.520, 0.870, 0.310, 2.750, 3.560, 2.530, 1.290, 0.330,
                1.810, 1.340, 0.130, -1.580, -2.050, -0.110, -0.850, -1.730, -3.300, -2.100,
                -0.430, -0.670, -1.340, -1.430, 0.220, 2.160, 1.350, 1.380, 0.210, 2.230,
                3.210, 1.790, 1.900, 0.380, 1.600, 1.100, 0.440, -1.070, -1.690, -0.090,
                -0.730, -2.260, -2.890, -2.680, -0.020, -0.960, -0.890, -1.580, 0.270, 2.330,
                0.970, 0.870, 0.500, 2.520, 2.820, 1.610, 1.130, -0.040, 1.980, 1.280,
                -0.380, -1.240, -1.520, -0.400, -0.790, -2.310, -2.890, -1.880, 0.160, -1.590,
                -0.810, -1.860, 0.570, 1.920, 1.440, 1.130, 0.450, 3.020, 3.490, 2.510,
                1.150, -0.060, 2.430, 1.010, 0.480, -1.090, -1.550, -0.090, -1.350, -1.350,
                -3.370, -2.150, -0.710, -1.410, -0.970, -1.550, -0.140, 1.640, 0.910, 1.590,
                0.170, 2.650, 3.160, 2.200, 1.240, -0.170, 1.630, 1.710, 0.310, -0.740,
                -1.680, -0.350, -1.430, -1.870, -3.200, -1.950, -0.340, -0.970, -1.150, -1.760,
                -0.160, 2.330, 1.280, 0.810, 1.020, 3.000, 2.760, 2.310, 0.990, -0.000,
                1.600, 0.940, 0.330, -1.530, -1.490, 0.040, -1.130, -2.100, -2.560, -1.980,
                -0.390, -0.700, -0.660, -1.670, -0.060, 2.110, 1.090, 1.450, 1.030, 2.650,
                2.690, 2.160, 1.890, 0.680, 2.070, 0.970, -0.400, -1.080, -1.660, -0.230,
                -0.830, -2.020, -2.610, -2.320, -0.000, -1.070, -0.940, -1.970, 0.230, 1.890,
                0.980, 1.060, 0.830, 2.500, 3.520, 1.880, 1.090, -0.040, 2.190, 1.040,
                0.130, -1.120, -1.560, -0.120, -1.600, -1.900, -3.280, -1.980, -0.270, -0.900,
                -0.830, -2.120, 0.170, 1.790, 1.660, 0.930, 0.150, 2.320, 3.230, 2.340,
                1.150, 0.070, 1.550, 1.280, -0.110, -0.790, -1.510, -0.080, -0.750, -2.140,
                -2.450, -1.990, 0.060, -1.140, -0.620, -1.780, 0.150, 1.640, 1.090, 1.200,
                0.450, 2.700, 3.200, 2.470, 1.810, 0.110, 2.210, 1.180, 0.070, -0.830,
                -2.120, 0.300, -1.180, -1.480, -2.450, -2.570, -0.340, -1.280, -1.280, -1.870,
                0.220, 1.920, 1.580, 1.170, 0.790, 2.830, 2.720, 1.640, 1.510, 0.440,
                2.100, 1.650, 0.460, -1.390, -1.960, -0.010, -1.040, -2.260, -2.870, -1.850,
                -0.670, -1.130, -1.400, -1.980, 0.590, 1.370, 1.000, 0.840, 0.550, 2.610,
                3.460, 1.760, 1.020, -0.040, 2.310, 1.670, 0.350, -1.390, -2.160, -0.480,
                -1.520, -1.760, -2.670, -2.010, -0.600, -1.210, -1.420, -1.850, 0.080, 1.690,
                1.270, 1.220, 0.830, 2.230, 2.700, 1.680, 1.420, 0.560, 1.910, 1.550,
                0.060, -1.550, -1.750, -0.570, -0.920, -1.990, -2.700, -2.130, -0.370, -1.060,
                -0.630, -1.710, 0.510, 1.740, 1.480, 1.390, 0.780, 2.270, 3.520, 2.130,
                1.890, -0.140, 2.080, 0.990, 0.570, -1.190, -1.900, 0.320, -1.640, -1.700,
                -3.090, -1.840, 0.030, -1.150, -0.800, -2.040, 0.590, 2.020, 0.720, 1.690,
                0.730, 2.380, 3.420, 2.480, 1.420, -0.010, 2.040, 1.220, -0.020, -1.110,
                -1.950, -0.320, -0.870, -1.550, -2.670, -2.440, -0.300, -1.180, -1.390, -1.800,
                0.520, 2.110, 1.320, 1.630, 0.270, 2.880, 3.160, 1.990, 1.640, 0.530,
                2.120, 0.900, -0.220, -1.590, -1.450, 0.050, -1.460, -1.730, -2.760, -2.060,
                0.100, -1.560, -0.920, -1.600, -0.140, 1.350, 0.830, 0.880, 0.760, 2.300,
                3.160, 2.110,
            };

            return audio;
        }
    }
}
