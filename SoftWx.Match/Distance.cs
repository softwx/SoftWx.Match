// Copyright © 2015-2026 Steve Hatchett
// SPDX-License-Identifier: MIT
// Released under the MIT License the text of which appears at the end of this file.
// <authors> Steve Hatchett

#if NET8_0_OR_GREATER
using System;
#endif

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
#if NET8_0_OR_GREATER
            return Levenshtein(string1.AsSpan(), string2.AsSpan());
#else
            // if strings of different lengths, ensure shorter string is in string1. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (string1.Length > string2.Length) (string1, string2) = (string2, string1);

            // identify common suffix and/or prefix that can be ignored
            Helpers.PrefixSuffixPrep(string1, string2, out int len1, out int len2, out int start);
            if (len1 == 0) return len2;
            if (len1 == 1) return Helpers.SingleCharDistance(string1, string2, len2, start);

            return Match.Levenshtein.DistanceCore(string1, string2, len1, len2, start);
#endif
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
#if NET8_0_OR_GREATER
            return Levenshtein(string1.AsSpan(), string2.AsSpan(), maxDistance);
#else
            if (maxDistance <= 0) return (string1 == string2) ? 0 : -1;

            // if strings of different lengths, ensure shorter string is in string1. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (string1.Length > string2.Length) (string1, string2) = (string2, string1);
            if (string2.Length - string1.Length > maxDistance) return -1;

            // identify common suffix and/or prefix that can be ignored
            Helpers.PrefixSuffixPrep(string1, string2, out int len1, out int len2, out int start);
            if (len1 == 0) return len2;
            if (maxDistance == 1) return (len2 == 1) ? 1 : -1;
            if (len1 == 1) {
                int dist = Helpers.SingleCharDistance(string1, string2, len2, start);
                return (dist <= maxDistance) ? dist : -1;
            }

            if (maxDistance < len2) {
                return Match.Levenshtein.DistanceCore(string1, string2, len1, len2, start, maxDistance);
            }
            return Match.Levenshtein.DistanceCore(string1, string2, len1, len2, start);
#endif
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
#if NET8_0_OR_GREATER
            return DamerauOSA(string1.AsSpan(), string2.AsSpan());
#else
            // if strings of different lengths, ensure shorter string is in string1. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (string1.Length > string2.Length) (string1, string2) = (string2, string1);

            // identify common suffix and/or prefix that can be ignored
            Helpers.PrefixSuffixPrep(string1, string2, out int len1, out int len2, out int start);
            if (len1 == 0) return len2;
            if (len1 == 1) return Helpers.SingleCharDistance(string1, string2, len2, start);

            return Match.DamerauOSA.DistanceCore(string1, string2, len1, len2, start);
#endif
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
#if NET8_0_OR_GREATER
            return DamerauOSA(string1.AsSpan(), string2.AsSpan(), maxDistance);
#else
            if (maxDistance <= 0) return (string1 == string2) ? 0 : -1;

            // if strings of different lengths, ensure shorter string is in string1. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (string1.Length > string2.Length) (string1, string2) = (string2, string1);
            if (string2.Length - string1.Length > maxDistance) return -1;

            // identify common suffix and/or prefix that can be ignored
            Helpers.PrefixSuffixPrep(string1, string2, out int len1, out int len2, out int start);
            if (len1 == 0) return len2;
            if (maxDistance == 1) {
                return (len2 == 1 || Helpers.IsSingleTransposition(string1, string2, len1, len2, start)) ? 1 : -1;
            }
            if (len1 == 1) {
                int dist = Helpers.SingleCharDistance(string1, string2, len2, start);
                return (dist <= maxDistance) ? dist : -1;
            }

            if (maxDistance < len2) {
                return Match.DamerauOSA.DistanceCore(string1, string2, len1, len2, start, maxDistance);
            }
            return Match.DamerauOSA.DistanceCore(string1, string2, len1, len2, start);
#endif
        }

#if NET8_0_OR_GREATER
        /// <summary>Compute and return the Levenshtein edit distance between two character spans.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is threadsafe.</remarks>
        /// <param name="string1">One of the character spans to compare.</param>
        /// <param name="string2">The other character span to compare.</param>
        /// <returns>0 if the spans are equivalent, otherwise a positive number whose
        /// magnitude increases as difference between the spans increases.</returns>
        public static int Levenshtein(this ReadOnlySpan<char> string1, ReadOnlySpan<char> string2) {
            if (string1.Length > string2.Length) {
                ReadOnlySpan<char> t = string1;
                string1 = string2;
                string2 = t;
            }

            Helpers.PrefixSuffixPrep(ref string1, ref string2);
            if (string1.Length == 0) return string2.Length;
            if (string1.Length == 1) return Helpers.SingleCharDistance(string1, string2);

            return Match.Levenshtein.DistanceCore(string1, string2);
        }

        /// <summary>Compute and return the Levenshtein edit distance between two character spans.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is threadsafe.</remarks>
        /// <param name="string1">One of the character spans to compare.</param>
        /// <param name="string2">The other character span to compare.</param>
        /// <param name="maxDistance">The maximum distance that is of interest.</param>
        /// <returns>-1 if the distance is greater than the maxDistance, 0 if the spans
        /// are equivalent, otherwise a positive number whose magnitude increases as
        /// difference between the spans increases.</returns>
        public static int Levenshtein(this ReadOnlySpan<char> string1, ReadOnlySpan<char> string2, int maxDistance) {
            if (maxDistance <= 0) return string1.SequenceEqual(string2) ? 0 : -1;

            if (string1.Length > string2.Length) {
                ReadOnlySpan<char> t = string1;
                string1 = string2;
                string2 = t;
            }
            if (string2.Length - string1.Length > maxDistance) return -1;

            Helpers.PrefixSuffixPrep(ref string1, ref string2);
            if (string1.Length == 0) return string2.Length;
            // After PrefixSuffixPrep both ends differ, so distance == 1 iff both trimmed spans have length 1.
            if (maxDistance == 1) return (string2.Length == 1) ? 1 : -1;
            if (string1.Length == 1) {
                int dist = Helpers.SingleCharDistance(string1, string2);
                return (dist <= maxDistance) ? dist : -1;
            }

            if (maxDistance < string2.Length) {
                return Match.Levenshtein.DistanceCore(string1, string2, maxDistance);
            }
            return Match.Levenshtein.DistanceCore(string1, string2);
        }

        /// <summary>Compute and return the Damerau-Levenshtein optimal string
        /// alignment edit distance between two character spans.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is threadsafe.</remarks>
        /// <param name="string1">One of the character spans to compare.</param>
        /// <param name="string2">The other character span to compare.</param>
        /// <returns>0 if the spans are equivalent, otherwise a positive number whose
        /// magnitude increases as difference between the spans increases.</returns>
        public static int DamerauOSA(this ReadOnlySpan<char> string1, ReadOnlySpan<char> string2) {
            if (string1.Length > string2.Length) {
                ReadOnlySpan<char> t = string1;
                string1 = string2;
                string2 = t;
            }

            Helpers.PrefixSuffixPrep(ref string1, ref string2);
            if (string1.Length == 0) return string2.Length;
            if (string1.Length == 1) return Helpers.SingleCharDistance(string1, string2);

            return Match.DamerauOSA.DistanceCore(string1, string2);
        }

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
        public static int DamerauOSA(this ReadOnlySpan<char> string1, ReadOnlySpan<char> string2, int maxDistance) {
            if (maxDistance <= 0) return string1.SequenceEqual(string2) ? 0 : -1;

            if (string1.Length > string2.Length) {
                ReadOnlySpan<char> t = string1;
                string1 = string2;
                string2 = t;
            }
            if (string2.Length - string1.Length > maxDistance) return -1;

            Helpers.PrefixSuffixPrep(ref string1, ref string2);
            if (string1.Length == 0) return string2.Length;
            // After PrefixSuffixPrep both ends differ, so distance == 1 iff len2 == 1 or a 2-char transposition.
            if (maxDistance == 1) {
                return (string2.Length == 1 || Helpers.IsSingleTransposition(string1, string2)) ? 1 : -1;
            }
            if (string1.Length == 1) {
                int dist = Helpers.SingleCharDistance(string1, string2);
                return (dist <= maxDistance) ? dist : -1;
            }

            if (maxDistance < string2.Length) {
                return Match.DamerauOSA.DistanceCore(string1, string2, maxDistance);
            }
            return Match.DamerauOSA.DistanceCore(string1, string2);
        }
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
