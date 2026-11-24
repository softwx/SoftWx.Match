// Copyright © 2015-2026 Steve Hatchett
// SPDX-License-Identifier: MIT
// Released under the MIT License the text of which appears at the end of this file.
// <authors> Steve Hatchett

using System;

namespace SoftWx.Match {
    /// <summary>The Distance class provides threadsafe static methods that compute the
    /// distance between two strings.</summary>
    /// <remarks>The methods are static versions of methods from classes that implement
    /// the IDistance interface. In general (although not necessarily) these static
    /// methods will be about the same speed as the methods of IDistance instantiated
    /// classes. On .NET 8 and later, these methods use stack allocation and ArrayPool
    /// for zero steady-state heap allocations. Note that since the methods in this class
    /// are not implementations of the IDistance interface, some argument types and return
    /// types may differ from the interface. The types will be those that match most closely
    /// to the nature of the algorithm. For example, the Levenshtein methods take an int
    /// maxDistance and return an int value instead of double since it only deals with
    /// integral values.
    /// The methods in this class also can be used as string (and on .NET 8+, ReadOnlySpan&lt;char&gt;)
    /// extension methods as in this example:
    ///   <code>var distance = "hello".Levenshtein("goodbye");</code>
    /// </remarks>
    public static class Distance {
        /// <summary>Compute and return the Levenshtein edit distance between two strings.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is threadsafe.</remarks>
        /// <param name="string1">One of the strings to compare.</param>
        /// <param name="string2">The other string to compare.</param>
        /// <returns>0 if the strings are equivalent, otherwise a positive number whose
        /// magnitude increases as difference between the strings increases.</returns>
        public static int Levenshtein(this string? string1, string? string2) {
            if (string1 == null) return (string2 ?? string.Empty).Length;
            if (string2 == null) return string1.Length;
            return Match.Levenshtein.DistanceStatic(string1.AsSpan(), string2.AsSpan());
        }

        /// <summary>Compute and return the Levenshtein edit distance between two strings.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is threadsafe.</remarks>
        /// <param name="string1">One of the strings to compare.</param>
        /// <param name="string2">The other string to compare.</param>
        /// <param name="maxDistance">The maximum distance that is of interest.</param>
        /// <returns>-1 if the distance is greater than the maxDistance, 0 if the strings
        /// are equivalent, otherwise a positive number whose magnitude increases as
        /// difference between the strings increases.</returns>
        public static int Levenshtein(this string? string1, string? string2, int maxDistance) {
            if (string1 == null || string2 == null) return Helpers.NullDistanceResults(string1, string2, maxDistance);
            return Match.Levenshtein.DistanceStatic(string1.AsSpan(), string2.AsSpan(), maxDistance);
        }

        /// <summary>Compute and return the Damerau-Levenshtein optimal string
        /// alignment edit distance between two strings.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is threadsafe.</remarks>
        /// <param name="string1">One of the strings to compare.</param>
        /// <param name="string2">The other string to compare.</param>
        /// <returns>0 if the strings are equivalent, otherwise a positive number whose
        /// magnitude increases as difference between the strings increases.</returns>
        public static int DamerauOSA(this string? string1, string? string2) {
            if (string1 == null) return (string2 ?? string.Empty).Length;
            if (string2 == null) return string1.Length;
            return Match.DamerauOSA.DistanceStatic(string1.AsSpan(), string2.AsSpan());
        }

        /// <summary>Compute and return the Damerau-Levenshtein optimal string
        /// alignment edit distance between two strings.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is threadsafe.</remarks>
        /// <param name="string1">One of the strings to compare.</param>
        /// <param name="string2">The other string to compare.</param>
        /// <param name="maxDistance">The maximum distance that is of interest.</param>
        /// <returns>-1 if the distance is greater than the maxDistance, 0 if the strings
        /// are equivalent, otherwise a positive number whose magnitude increases as
        /// difference between the strings increases.</returns>
        public static int DamerauOSA(this string? string1, string? string2, int maxDistance) {
            if (string1 == null || string2 == null) return Helpers.NullDistanceResults(string1, string2, maxDistance);
            return Match.DamerauOSA.DistanceStatic(string1.AsSpan(), string2.AsSpan(), maxDistance);
        }

#if NET8_0_OR_GREATER
        /// <summary>Compute and return the Levenshtein edit distance between two character spans.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is threadsafe.</remarks>
        /// <param name="string1">One of the character spans to compare.</param>
        /// <param name="string2">The other character span to compare.</param>
        /// <returns>0 if the spans are equivalent, otherwise a positive number whose
        /// magnitude increases as difference between the spans increases.</returns>
        public static int Levenshtein(this ReadOnlySpan<char> string1, ReadOnlySpan<char> string2) =>
            Match.Levenshtein.DistanceStatic(string1, string2);

        /// <summary>Compute and return the Levenshtein edit distance between two character spans.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is threadsafe.</remarks>
        /// <param name="string1">One of the character spans to compare.</param>
        /// <param name="string2">The other character span to compare.</param>
        /// <param name="maxDistance">The maximum distance that is of interest.</param>
        /// <returns>-1 if the distance is greater than the maxDistance, 0 if the spans
        /// are equivalent, otherwise a positive number whose magnitude increases as
        /// difference between the spans increases.</returns>
        public static int Levenshtein(this ReadOnlySpan<char> string1, ReadOnlySpan<char> string2, int maxDistance) =>
            Match.Levenshtein.DistanceStatic(string1, string2, maxDistance);

        /// <summary>Compute and return the Damerau-Levenshtein optimal string
        /// alignment edit distance between two character spans.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is threadsafe.</remarks>
        /// <param name="string1">One of the character spans to compare.</param>
        /// <param name="string2">The other character span to compare.</param>
        /// <returns>0 if the spans are equivalent, otherwise a positive number whose
        /// magnitude increases as difference between the spans increases.</returns>
        public static int DamerauOSA(this ReadOnlySpan<char> string1, ReadOnlySpan<char> string2) =>
            Match.DamerauOSA.DistanceStatic(string1, string2);

        /// <summary>Compute and return the Damerau-Levenshtein optimal string
        /// alignment edit distance between two character spans.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is threadsafe.</remarks>
        /// <param name="string1">One of the character spans to compare.</param>
        /// <param name="string2">The other character span to compare.</param>
        /// <param name="maxDistance">The maximum distance that is of interest.</param>
        /// <returns>-1 if the distance is greater than the maxDistance, 0 if the spans
        /// are equivalent, otherwise a positive number whose magnitude increases as
        /// difference between the spans increases.</returns>
        public static int DamerauOSA(this ReadOnlySpan<char> string1, ReadOnlySpan<char> string2, int maxDistance) =>
            Match.DamerauOSA.DistanceStatic(string1, string2, maxDistance);
#endif
    }
}
/*
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
