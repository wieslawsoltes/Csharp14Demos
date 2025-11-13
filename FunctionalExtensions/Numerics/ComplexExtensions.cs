using System;
using System.Numerics;

namespace FunctionalExtensions.Numerics;

/// <summary>
/// Extension helpers for <see cref="Complex"/>.
/// </summary>
public static class ComplexExtensions
{
    extension(Complex value)
    {
        /// <summary>
        /// Magnitude squared of the complex number.
        /// </summary>
        public double MagnitudeSquared => (value.Real * value.Real) + (value.Imaginary * value.Imaginary);

        /// <summary>
        /// Magnitude (absolute value) of the complex number.
        /// </summary>
        public double Magnitude => Math.Sqrt((value.Real * value.Real) + (value.Imaginary * value.Imaginary));

        /// <summary>
        /// Complex conjugate.
        /// </summary>
        public Complex Conjugate => Complex.Conjugate(value);
    }
}
