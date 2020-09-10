using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace gpm_vibration_module_api.GpmMath
{
    internal class Numeric
    {

        internal static class Tools
        {



            public static DateTime LicenseDate;
            /// <summary>
            ///   Gets the angle formed by the vector [x,y].
            /// </summary>
            /// 
            public static float Angle(float x, float y)
            {
                if (y >= 0)
                {
                    if (x >= 0)
                        return (float)Math.Atan(y / x);
                    return (float)(Math.PI - Math.Atan(-y / x));
                }
                else
                {
                    if (x >= 0)
                        return (float)(2 * Math.PI - Math.Atan(-y / x));
                    return (float)(Math.PI + Math.Atan(y / x));
                }
            }

            /// <summary>
            ///   Gets the angle formed by the vector [x,y].
            /// </summary>
            /// 
            public static double Angle(double x, double y)
            {
                if (y >= 0)
                {
                    if (x >= 0)
                        return Math.Atan2(y, x);
                    return Math.PI - Math.Atan(-y / x);
                }
                else
                {
                    if (x >= 0)
                        return 2.0 * Math.PI - Math.Atan2(-y, x);
                    return Math.PI + Math.Atan(y / x);
                }
            }

            /// <summary>
            ///   Gets the displacement angle between two points.
            /// </summary>
            /// 


            /// <summary>
            ///   Gets the displacement angle between two points, coded
            ///   as an integer varying from 0 to 20.
            /// </summary>
            /// 


            /// <summary>
            ///   Gets the greatest common divisor between two integers.
            /// </summary>
            /// 
            /// <param name="a">First value.</param>
            /// <param name="b">Second value.</param>
            /// 
            /// <returns>The greatest common divisor.</returns>
            /// 
            public static int GreatestCommonDivisor(int a, int b)
            {
                int x = a - b * (int)Math.Floor(a / (double)b);
                while (x != 0)
                {
                    a = b;
                    b = x;
                    x = a - b * (int)Math.Floor(a / (double)b);
                }
                return b;
            }

            /// <summary>
            ///   Returns the next power of 2 after the input value x.
            /// </summary>
            /// 
            /// <param name="x">Input value x.</param>
            /// 
            /// <returns>Returns the next power of 2 after the input value x.</returns>
            /// 
            public static int NextPowerOf2(int x)
            {
                --x;
                x |= x >> 1;
                x |= x >> 2;
                x |= x >> 4;
                x |= x >> 8;
                x |= x >> 16;
                return ++x;
            }

            /// <summary>
            ///   Returns the previous power of 2 after the input value x.
            /// </summary>
            /// 
            /// <param name="x">Input value x.</param>
            /// 
            /// <returns>Returns the previous power of 2 after the input value x.</returns>
            /// 
            public static int PreviousPowerOf2(int x)
            {
                return NextPowerOf2(x + 1) / 2;
            }


            /// <summary>
            ///   Hypotenuse calculus without overflow/underflow
            /// </summary>
            /// 
            /// <param name="a">First value</param>
            /// <param name="b">Second value</param>
            /// 
            /// <returns>The hypotenuse Sqrt(a^2 + b^2)</returns>
            /// 
            public static double Hypotenuse(double a, double b)
            {
                double r = 0.0;
                double absA = System.Math.Abs(a);
                double absB = System.Math.Abs(b);

                if (absA > absB)
                {
                    r = b / a;
                    r = absA * System.Math.Sqrt(1 + r * r);
                }
                else if (b != 0)
                {
                    r = a / b;
                    r = absB * System.Math.Sqrt(1 + r * r);
                }

                return r;
            }

            /// <summary>
            ///   Hypotenuse calculus without overflow/underflow
            /// </summary>
            /// 
            /// <param name="a">first value</param>
            /// <param name="b">second value</param>
            /// 
            /// <returns>The hypotenuse Sqrt(a^2 + b^2)</returns>
            /// 
            public static decimal Hypotenuse(decimal a, decimal b)
            {
                decimal r = 0;
                decimal absA = System.Math.Abs(a);
                decimal absB = System.Math.Abs(b);

                if (absA > absB)
                {
                    r = b / a;
                    r = absA * (decimal)System.Math.Sqrt((double)(1 + r * r));
                }
                else if (b != 0)
                {
                    r = a / b;
                    r = absB * (decimal)System.Math.Sqrt((double)(1 + r * r));
                }

                return r;
            }

            /// <summary>
            ///   Hypotenuse calculus without overflow/underflow
            /// </summary>
            /// 
            /// <param name="a">first value</param>
            /// <param name="b">second value</param>
            /// 
            /// <returns>The hypotenuse Sqrt(a^2 + b^2)</returns>
            /// 
            public static float Hypotenuse(float a, float b)
            {
                double r = 0;
                float absA = System.Math.Abs(a);
                float absB = System.Math.Abs(b);

                if (absA > absB)
                {
                    r = b / a;
                    r = absA * System.Math.Sqrt(1 + r * r);
                }
                else if (b != 0)
                {
                    r = a / b;
                    r = absB * System.Math.Sqrt(1 + r * r);
                }

                return (float)r;
            }

            /// <summary>
            ///   Gets the proper modulus operation for
            ///   an integer value x and modulo m.
            /// </summary>
            /// 
            public static int Mod(int x, int m)
            {
                if (m < 0)
                    m = -m;

                int r = x % m;

                return r < 0 ? r + m : r;
            }

            /// <summary>
            ///   Gets the proper modulus operation for
            ///   a real value x and modulo m.
            /// </summary>
            /// 
            public static double Mod(double x, double m)
            {
                if (m < 0)
                    m = -m;

                double r = x % m;

                return r < 0 ? r + m : r;
            }


            #region Scaling functions








            /// <summary>
            ///   Converts the value x (which is measured in the scale
            ///   'from') to another value measured in the scale 'to'.
            /// </summary>
            /// 

            /// <summary>
            ///   Converts the value x (which is measured in the scale
            ///   'from') to another value measured in the scale 'to'.
            /// </summary>
            /// 
            [Obsolete("Please use Vector.Scale instead.")]
            public static double[][] Scale(double[] fromMin, double[] fromMax, double[] toMin, double[] toMax, double[][] x)
            {
                int rows = x.Length;
                int cols = fromMin.Length;

                double[][] result = new double[rows][];
                for (int i = 0; i < rows; i++)
                {
                    result[i] = new double[cols];
                    for (int j = 0; j < cols; j++)
                    {
                        result[i][j] = (toMax[j] - toMin[j]) * (x[i][j] - fromMin[j]) / (fromMax[j] - fromMin[j]) + toMin[j];
                    }
                }

                return result;
            }

            /// <summary>
            ///   Converts the value x (which is measured in the scale
            ///   'from') to another value measured in the scale 'to'.
            /// </summary>
            /// 
            [Obsolete("Please use Vector.Scale instead.")]
            public static double[][] Scale(double fromMin, double fromMax, double toMin, double toMax, double[][] x)
            {
                int rows = x.Length;

                double[][] result = new double[rows][];
                for (int i = 0; i < rows; i++)
                {
                    result[i] = new double[x[i].Length];
                    for (int j = 0; j < result[i].Length; j++)
                    {
                        result[i][j] = (toMax - toMin) * (x[i][j] - fromMin) / (fromMax - fromMin) + toMin;
                    }
                }

                return result;
            }

            /// <summary>
            ///   Converts the value x (which is measured in the scale
            ///   'from') to another value measured in the scale 'to'.
            /// </summary>
            /// 
            [Obsolete("Please use Vector.Scale instead.")]
            public static double[][] Scale(double[] fromMin, double[] fromMax, double toMin, double toMax, double[][] x)
            {
                int rows = x.Length;
                int cols = fromMin.Length;

                double[][] result = new double[rows][];
                for (int i = 0; i < rows; i++)
                {
                    result[i] = new double[cols];
                    for (int j = 0; j < cols; j++)
                    {
                        result[i][j] = (toMax - toMin) * (x[i][j] - fromMin[j]) / (fromMax[j] - fromMin[j]) + toMin;
                    }
                }

                return result;
            }




            #endregion


            /// <summary>
            ///   Returns the hyperbolic arc cosine of the specified value.
            /// </summary>
            /// 
            public static double Acosh(double x)
            {
                if (x < 1.0)
                    throw new ArgumentOutOfRangeException("x");
                return System.Math.Log(x + System.Math.Sqrt(x * x - 1));
            }

            /// <summary>
            /// Returns the hyperbolic arc sine of the specified value.
            /// </summary>
            /// 
            public static double Asinh(double d)
            {
                double x;
                int sign;

                if (d == 0.0)
                    return d;

                if (d < 0.0)
                {
                    sign = -1;
                    x = -d;
                }
                else
                {
                    sign = 1;
                    x = d;
                }
                return sign * System.Math.Log(x + System.Math.Sqrt(x * x + 1));
            }

            /// <summary>
            /// Returns the hyperbolic arc tangent of the specified value.
            /// </summary>
            /// 
            public static double Atanh(double d)
            {
                if (d > 1.0 || d < -1.0)
                    throw new ArgumentOutOfRangeException("d");
                return 0.5 * System.Math.Log((1.0 + d) / (1.0 - d));
            }



            /// <summary>
            ///   Returns the factorial falling power of the specified value.
            /// </summary>
            /// 
            public static int FactorialPower(int value, int degree)
            {
                int t = value;
                for (int i = 0; i < degree; i++)
                    t *= degree--;
                return t;
            }

            /// <summary>
            ///   Truncated power function.
            /// </summary>
            /// 
            public static double TruncatedPower(double value, double degree)
            {
                double x = System.Math.Pow(value, degree);
                return (x > 0) ? x : 0.0;
            }



            /// <summary>
            ///   Interpolates data using a piece-wise linear function.
            /// </summary>
            /// 
            /// <param name="value">The value to be calculated.</param>
            /// <param name="x">The input data points <c>x</c>. Those values need to be sorted.</param>
            /// <param name="y">The output data points <c>y</c>.</param>
            /// <param name="lower">
            ///   The value to be returned for values before the first point in <paramref name="x"/>.</param>
            /// <param name="upper">
            ///   The value to be returned for values after the last point in <paramref name="x"/>.</param>
            ///   
            /// <returns>Computes the output for f(value) by using a piecewise linear
            ///   interpolation of the data points <paramref name="x"/> and <paramref name="y"/>.</returns>
            /// 
            public static double Interpolate1D(double value, double[] x, double[] y, double lower, double upper)
            {
                for (int i = 0; i < x.Length; i++)
                {
                    if (value < x[i])
                    {
                        if (i == 0)
                            return lower;

                        int start = i - 1;
                        int next = i;

                        double m = (value - x[start]) / (x[next] - x[start]);
                        return y[start] + (y[next] - y[start]) * m;
                    }
                }

                return upper;
            }

            /// <summary>
            ///   Gets the maximum value among three values.
            /// </summary>
            /// 
            /// <param name="a">The first value <c>a</c>.</param>
            /// <param name="b">The second value <c>b</c>.</param>
            /// <param name="c">The third value <c>c</c>.</param>
            /// 
            /// <returns>The maximum value among <paramref name="a"/>, 
            ///   <paramref name="b"/> and <paramref name="c"/>.</returns>
            /// 
            public static double Max(double a, double b, double c)
            {
                if (a > b)
                {
                    if (c > a)
                        return c;
                    return a;
                }
                else
                {
                    if (c > b)
                        return c;
                    return b;
                }
            }

            /// <summary>
            ///   Gets the minimum value among three values.
            /// </summary>
            /// 
            /// <param name="a">The first value <c>a</c>.</param>
            /// <param name="b">The second value <c>b</c>.</param>
            /// <param name="c">The third value <c>c</c>.</param>
            /// 
            /// <returns>The minimum value among <paramref name="a"/>, 
            ///   <paramref name="b"/> and <paramref name="c"/>.</returns>
            /// 
            public static double Min(double a, double b, double c)
            {
                if (a < b)
                {
                    if (c < a)
                        return c;
                    return a;
                }
                else
                {
                    if (c < b)
                        return c;
                    return b;
                }
            }

            /// <summary>
            /// Calculates power of 2.
            /// </summary>
            /// 
            /// <param name="power">Power to raise in.</param>
            /// 
            /// <returns>Returns specified power of 2 in the case if power is in the range of
            /// [0, 30]. Otherwise returns 0.</returns>
            /// 
            public static int Pow2(int power)
            {
                return ((power >= 0) && (power <= 30)) ? (1 << power) : 0;
            }

            /// <summary>
            /// Checks if the specified integer is power of 2.
            /// </summary>
            /// 
            /// <param name="x">Integer number to check.</param>
            /// 
            /// <returns>Returns <b>true</b> if the specified number is power of 2.
            /// Otherwise returns <b>false</b>.</returns>
            /// 
            public static bool IsPowerOf2(int x)
            {
                return (x > 0) ? ((x & (x - 1)) == 0) : false;
            }

            /// <summary>
            /// Get base of binary logarithm.
            /// </summary>
            /// 
            /// <param name="x">Source integer number.</param>
            /// 
            /// <returns>Power of the number (base of binary logarithm).</returns>
            /// 
            public static int Log2(int x)
            {
                if (x <= 65536)
                {
                    if (x <= 256)
                    {
                        if (x <= 16)
                        {
                            if (x <= 4)
                            {
                                if (x <= 2)
                                {
                                    if (x <= 1)
                                        return 0;
                                    return 1;
                                }
                                return 2;
                            }
                            if (x <= 8)
                                return 3;
                            return 4;
                        }
                        if (x <= 64)
                        {
                            if (x <= 32)
                                return 5;
                            return 6;
                        }
                        if (x <= 128)
                            return 7;
                        return 8;
                    }
                    if (x <= 4096)
                    {
                        if (x <= 1024)
                        {
                            if (x <= 512)
                                return 9;
                            return 10;
                        }
                        if (x <= 2048)
                            return 11;
                        return 12;
                    }
                    if (x <= 16384)
                    {
                        if (x <= 8192)
                            return 13;
                        return 14;
                    }
                    if (x <= 32768)
                        return 15;
                    return 16;
                }

                if (x <= 16777216)
                {
                    if (x <= 1048576)
                    {
                        if (x <= 262144)
                        {
                            if (x <= 131072)
                                return 17;
                            return 18;
                        }
                        if (x <= 524288)
                            return 19;
                        return 20;
                    }
                    if (x <= 4194304)
                    {
                        if (x <= 2097152)
                            return 21;
                        return 22;
                    }
                    if (x <= 8388608)
                        return 23;
                    return 24;
                }
                if (x <= 268435456)
                {
                    if (x <= 67108864)
                    {
                        if (x <= 33554432)
                            return 25;
                        return 26;
                    }
                    if (x <= 134217728)
                        return 27;
                    return 28;
                }
                if (x <= 1073741824)
                {
                    if (x <= 536870912)
                        return 29;
                    return 30;
                }
                return 31;
            }

            /// <summary>
            ///   Returns the square root of the specified <see cref="decimal"/> number.
            /// </summary>
            /// 


        }
#if !SILVERLIGHT
        [Serializable]
#endif // !SILVERLIGHT
        public struct Complex : IEquatable<Complex>, IFormattable
        {

            // --------------SECTION: Private Data members ----------- //

            private Double m_real;
            private Double m_imaginary;

            // ---------------SECTION: Necessary Constants ----------- //

            private const Double LOG_10_INV = 0.43429448190325;


            // --------------SECTION: Public Properties -------------- //

            public Double Real
            {
                get
                {
                    return m_real;
                }
            }

            public Double Imaginary
            {
                get
                {
                    return m_imaginary;
                }
            }

            public Double Magnitude
            {
                get
                {
                    return Complex.Abs(this);
                }
            }

            public Double Phase
            {
                get
                {
                    return Math.Atan2(m_imaginary, m_real);
                }
            }

            // --------------SECTION: Attributes -------------- //

            public static readonly Complex Zero = new Complex(0.0, 0.0);
            public static readonly Complex One = new Complex(1.0, 0.0);
            public static readonly Complex ImaginaryOne = new Complex(0.0, 1.0);

            // --------------SECTION: Constructors and factory methods -------------- //

            public Complex(Double real, Double imaginary)  /* Constructor to create a complex number with rectangular co-ordinates  */
            {
                this.m_real = real;
                this.m_imaginary = imaginary;
            }

            public static Complex FromPolarCoordinates(Double magnitude, Double phase) /* Factory method to take polar inputs and create a Complex object */
            {
                return new Complex((magnitude * Math.Cos(phase)), (magnitude * Math.Sin(phase)));
            }

            public static Complex Negate(Complex value)
            {
                return -value;
            }

            public static Complex Add(Complex left, Complex right)
            {
                return left + right;
            }

            public static Complex Subtract(Complex left, Complex right)
            {
                return left - right;
            }

            public static Complex Multiply(Complex left, Complex right)
            {
                return left * right;
            }

            public static Complex Divide(Complex dividend, Complex divisor)
            {
                return dividend / divisor;
            }

            // --------------SECTION: Arithmetic Operator(unary) Overloading -------------- //
            public static Complex operator -(Complex value)  /* Unary negation of a complex number */
            {

                return (new Complex((-value.m_real), (-value.m_imaginary)));
            }

            // --------------SECTION: Arithmetic Operator(binary) Overloading -------------- //       
            public static Complex operator +(Complex left, Complex right)
            {
                return (new Complex((left.m_real + right.m_real), (left.m_imaginary + right.m_imaginary)));

            }

            public static Complex operator -(Complex left, Complex right)
            {
                return (new Complex((left.m_real - right.m_real), (left.m_imaginary - right.m_imaginary)));
            }

            public static Complex operator *(Complex left, Complex right)
            {
                // Multiplication:  (a + bi)(c + di) = (ac -bd) + (bc + ad)i
                Double result_Realpart = (left.m_real * right.m_real) - (left.m_imaginary * right.m_imaginary);
                Double result_Imaginarypart = (left.m_imaginary * right.m_real) + (left.m_real * right.m_imaginary);
                return (new Complex(result_Realpart, result_Imaginarypart));
            }

            public static Complex operator /(Complex left, Complex right)
            {
                // Division : Smith's formula.
                double a = left.m_real;
                double b = left.m_imaginary;
                double c = right.m_real;
                double d = right.m_imaginary;

                if (Math.Abs(d) < Math.Abs(c))
                {
                    double doc = d / c;
                    return new Complex((a + b * doc) / (c + d * doc), (b - a * doc) / (c + d * doc));
                }
                else
                {
                    double cod = c / d;
                    return new Complex((b + a * cod) / (d + c * cod), (-a + b * cod) / (d + c * cod));
                }
            }


            // --------------SECTION: Other arithmetic operations  -------------- //

            public static Double Abs(Complex value)
            {

                if (Double.IsInfinity(value.m_real) || Double.IsInfinity(value.m_imaginary))
                {
                    return double.PositiveInfinity;
                }

                // |value| == sqrt(a^2 + b^2)
                // sqrt(a^2 + b^2) == a/a * sqrt(a^2 + b^2) = a * sqrt(a^2/a^2 + b^2/a^2)
                // Using the above we can factor out the square of the larger component to dodge overflow.


                double c = Math.Abs(value.m_real);
                double d = Math.Abs(value.m_imaginary);

                if (c > d)
                {
                    double r = d / c;
                    return c * Math.Sqrt(1.0 + r * r);
                }
                else if (d == 0.0)
                {
                    return c;  // c is either 0.0 or NaN
                }
                else
                {
                    double r = c / d;
                    return d * Math.Sqrt(1.0 + r * r);
                }
            }
            public static Complex Conjugate(Complex value)
            {
                // Conjugate of a Complex number: the conjugate of x+i*y is x-i*y 

                return (new Complex(value.m_real, (-value.m_imaginary)));

            }
            public static Complex Reciprocal(Complex value)
            {
                // Reciprocal of a Complex number : the reciprocal of x+i*y is 1/(x+i*y)
                if ((value.m_real == 0) && (value.m_imaginary == 0))
                {
                    return Complex.Zero;
                }

                return Complex.One / value;
            }

            // --------------SECTION: Comparison Operator(binary) Overloading -------------- //

            public static bool operator ==(Complex left, Complex right)
            {
                return ((left.m_real == right.m_real) && (left.m_imaginary == right.m_imaginary));


            }
            public static bool operator !=(Complex left, Complex right)
            {
                return ((left.m_real != right.m_real) || (left.m_imaginary != right.m_imaginary));

            }

            // --------------SECTION: Comparison operations (methods implementing IEquatable<ComplexNumber>,IComparable<ComplexNumber>) -------------- //

            public override bool Equals(object obj)
            {
                if (!(obj is Complex)) return false;
                return this == ((Complex)obj);
            }
            public bool Equals(Complex value)
            {
                return ((this.m_real.Equals(value.m_real)) && (this.m_imaginary.Equals(value.m_imaginary)));

            }

            // --------------SECTION: Type-casting basic numeric data-types to ComplexNumber  -------------- //

            public static implicit operator Complex(Int16 value)
            {
                return (new Complex(value, 0.0));
            }
            public static implicit operator Complex(Int32 value)
            {
                return (new Complex(value, 0.0));
            }
            public static implicit operator Complex(Int64 value)
            {
                return (new Complex(value, 0.0));
            }
            [CLSCompliant(false)]
            public static implicit operator Complex(UInt16 value)
            {
                return (new Complex(value, 0.0));
            }
            [CLSCompliant(false)]
            public static implicit operator Complex(UInt32 value)
            {
                return (new Complex(value, 0.0));
            }
            [CLSCompliant(false)]
            public static implicit operator Complex(UInt64 value)
            {
                return (new Complex(value, 0.0));
            }
            [CLSCompliant(false)]
            public static implicit operator Complex(SByte value)
            {
                return (new Complex(value, 0.0));
            }
            public static implicit operator Complex(Byte value)
            {
                return (new Complex(value, 0.0));
            }
            public static implicit operator Complex(Single value)
            {
                return (new Complex(value, 0.0));
            }
            public static implicit operator Complex(Double value)
            {
                return (new Complex(value, 0.0));
            }
            //public static explicit operator Complex(BigInteger value)
            //{
            //    return (new Complex((Double)value, 0.0));
            //}
            public static explicit operator Complex(Decimal value)
            {
                return (new Complex((Double)value, 0.0));
            }

            public static bool IsPowerOf2(int x)
            {
                return (x > 0) ? ((x & (x - 1)) == 0) : false;
            }
            // --------------SECTION: Formattig/Parsing options  -------------- //

            public override String ToString()
            {
                return (String.Format(CultureInfo.CurrentCulture, "({0}, {1})", this.m_real, this.m_imaginary));
            }

            public String ToString(String format)
            {
                return (String.Format(CultureInfo.CurrentCulture, "({0}, {1})", this.m_real.ToString(format, CultureInfo.CurrentCulture), this.m_imaginary.ToString(format, CultureInfo.CurrentCulture)));
            }

            public String ToString(IFormatProvider provider)
            {
                return (String.Format(provider, "({0}, {1})", this.m_real, this.m_imaginary));
            }

            public String ToString(String format, IFormatProvider provider)
            {
                return (String.Format(provider, "({0}, {1})", this.m_real.ToString(format, provider), this.m_imaginary.ToString(format, provider)));
            }


            public override Int32 GetHashCode()
            {
                Int32 n1 = 99999997;
                Int32 hash_real = this.m_real.GetHashCode() % n1;
                Int32 hash_imaginary = this.m_imaginary.GetHashCode();
                Int32 final_hashcode = hash_real ^ hash_imaginary;
                return (final_hashcode);
            }



            // --------------SECTION: Trigonometric operations (methods implementing ITrigonometric)  -------------- //

            public static Complex Sin(Complex value)
            {
                double a = value.m_real;
                double b = value.m_imaginary;
                return new Complex(Math.Sin(a) * Math.Cosh(b), Math.Cos(a) * Math.Sinh(b));
            }

            [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sinh", Justification = "Microsoft: Existing Name")]
            public static Complex Sinh(Complex value) /* Hyperbolic sin */
            {
                double a = value.m_real;
                double b = value.m_imaginary;
                return new Complex(Math.Sinh(a) * Math.Cos(b), Math.Cosh(a) * Math.Sin(b));

            }
            public static Complex Asin(Complex value) /* Arcsin */
            {
                return (-ImaginaryOne) * Log(ImaginaryOne * value + Sqrt(One - value * value));
            }

            public static Complex Cos(Complex value)
            {
                double a = value.m_real;
                double b = value.m_imaginary;
                return new Complex(Math.Cos(a) * Math.Cosh(b), -(Math.Sin(a) * Math.Sinh(b)));
            }

            [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cosh", Justification = "Microsoft: Existing Name")]
            public static Complex Cosh(Complex value) /* Hyperbolic cos */
            {
                double a = value.m_real;
                double b = value.m_imaginary;
                return new Complex(Math.Cosh(a) * Math.Cos(b), Math.Sinh(a) * Math.Sin(b));
            }
            public static Complex Acos(Complex value) /* Arccos */
            {
                return (-ImaginaryOne) * Log(value + ImaginaryOne * Sqrt(One - (value * value)));

            }
            public static Complex Tan(Complex value)
            {
                return (Sin(value) / Cos(value));
            }

            [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Tanh", Justification = "Microsoft: Existing Name")]
            public static Complex Tanh(Complex value) /* Hyperbolic tan */
            {
                return (Sinh(value) / Cosh(value));
            }
            public static Complex Atan(Complex value) /* Arctan */
            {
                Complex Two = new Complex(2.0, 0.0);
                return (ImaginaryOne / Two) * (Log(One - ImaginaryOne * value) - Log(One + ImaginaryOne * value));
            }

            // --------------SECTION: Other numerical functions  -------------- //        

            public static Complex Log(Complex value) /* Log of the complex number value to the base of 'e' */
            {
                return (new Complex((Math.Log(Abs(value))), (Math.Atan2(value.m_imaginary, value.m_real))));

            }
            public static Complex Log(Complex value, Double baseValue) /* Log of the complex number to a the base of a double */
            {
                return (Log(value) / Log(baseValue));
            }
            public static Complex Log10(Complex value) /* Log to the base of 10 of the complex number */
            {

                Complex temp_log = Log(value);
                return (Scale(temp_log, (Double)LOG_10_INV));

            }
            public static Complex Exp(Complex value) /* The complex number raised to e */
            {
                Double temp_factor = Math.Exp(value.m_real);
                Double result_re = temp_factor * Math.Cos(value.m_imaginary);
                Double result_im = temp_factor * Math.Sin(value.m_imaginary);
                return (new Complex(result_re, result_im));
            }

            [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sqrt", Justification = "Microsoft: Existing Name")]
            public static Complex Sqrt(Complex value) /* Square root ot the complex number */
            {
                return Complex.FromPolarCoordinates(Math.Sqrt(value.Magnitude), value.Phase / 2.0);
            }

            public static Complex Pow(Complex value, Complex power) /* A complex number raised to another complex number */
            {

                if (power == Complex.Zero)
                {
                    return Complex.One;
                }

                if (value == Complex.Zero)
                {
                    return Complex.Zero;
                }

                double a = value.m_real;
                double b = value.m_imaginary;
                double c = power.m_real;
                double d = power.m_imaginary;

                double rho = Complex.Abs(value);
                double theta = Math.Atan2(b, a);
                double newRho = c * theta + d * Math.Log(rho);

                double t = Math.Pow(rho, c) * Math.Pow(Math.E, -d * theta);

                return new Complex(t * Math.Cos(newRho), t * Math.Sin(newRho));
            }

            public static Complex Pow(Complex value, Double power) // A complex number raised to a real number 
            {
                return Pow(value, new Complex(power, 0));
            }



            //--------------- SECTION: Private member functions for internal use -----------------------------------//

            private static Complex Scale(Complex value, Double factor)
            {

                Double result_re = factor * value.m_real;
                Double result_im = factor * value.m_imaginary;
                return (new Complex(result_re, result_im));
            }

        }
    }
}
