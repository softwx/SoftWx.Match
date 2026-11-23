// Copyright ©2015-2026 SoftWx, Inc.
// Released under the MIT License the text of which appears at the end of this file.
// <authors> Steve Hatchett

using System;

namespace SoftWx.Match {
    /// <summary>The Similarity class provides threadsafe static methods that compute a
    /// normalized value of similarity between two strings.</summary>
    /// <remarks>The methods are static versions of methods from classes that implement
    /// the ISimilarity interface. On .NET 8 and later, these methods use stack allocation
    /// and ArrayPool for zero steady-state heap allocations.</remarks>
    public static class Similarity {
        /// <summary>Return Levenshtein similarity between two strings (1 - (levenshtein distance / len of longer string)).</summary>
        /// <param name="string1">One of the strings to compare.</param>
        /// <param name="string2">The other string to compare.</param>
        /// <returns>The degree of similarity 0 to 1.0, where 0 represents a lack of any
        /// notable similarity, and 1 represents equivalent strings.</returns>
        public static double Levenshtein(string? string1, string? string2) {
            if (string1 == null) return (string2 == null) ? 1 : 0;
            if (string2 == null) return 0;
#if NET8_0_OR_GREATER
            return Levenshtein(string1.AsSpan(), string2.AsSpan());
#else
            // if strings of different lengths, ensure shorter string is in string1. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (string1.Length > string2.Length) (string1, string2) = (string2, string1);

            // identify common suffix and/or prefix that can be ignored
            Helpers.PrefixSuffixPrep(string1, string2, out int len1, out int len2, out int start);
            if (len1 == 0) return (len2 == 0) ? 1.0 : len2.ToSimilarity(string2.Length);
            if (len1 == 1) return Helpers.SingleCharDistance(string1, string2, len2, start).ToSimilarity(string2.Length);

            return Match.Levenshtein.Distance(string1, string2, len1, len2, start, new int[len2])
                .ToSimilarity(string2.Length);
#endif
        }

        /// <summary>Return a measure of the similarity between two strings.</summary>
        /// <param name="string1">One of the strings to compare.</param>
        /// <param name="string2">The other string to compare.</param>
        /// <param name="minSimilarity">The minimum similarity that is of interest.</param>
        /// <returns>The degree of similarity 0 to 1.0, where -1 represents a similarity
        /// lower than minSimilarity, otherwise, a number between 0 and 1.0 where 0
        /// represents a lack of any notable similarity, and 1 represents equivalent
        /// strings.</returns>
        public static double Levenshtein(string? string1, string? string2, double minSimilarity) {
            if (double.IsNaN(minSimilarity) || minSimilarity < 0 || minSimilarity > 1) {
                throw new ArgumentOutOfRangeException(nameof(minSimilarity), minSimilarity, "minSimilarity must be in range 0 to 1.0");
            }
            if (string1 == null || string2 == null) return Helpers.NullSimilarityResults(string1, string2, minSimilarity);
#if NET8_0_OR_GREATER
            return Levenshtein(string1.AsSpan(), string2.AsSpan(), minSimilarity);
#else
            // if strings of different lengths, ensure shorter string is in string1. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (string1.Length > string2.Length) (string1, string2) = (string2, string1);

            int iMaxDistance = minSimilarity.ToDistance(string2.Length);
            if (string2.Length - string1.Length > iMaxDistance) return -1;
            if (iMaxDistance == 0) return (string1 == string2) ? 1 : -1;

            // identify common suffix and/or prefix that can be ignored
            Helpers.PrefixSuffixPrep(string1, string2, out int len1, out int len2, out int start);
            if (len1 == 0) return (len2 == 0) ? 1.0 : len2.ToSimilarity(string2.Length);
            if (iMaxDistance == 1) return (len2 == 1) ? 1.ToSimilarity(string2.Length) : -1;
            if (len1 == 1) {
                int dist = Helpers.SingleCharDistance(string1, string2, len2, start);
                return (dist <= iMaxDistance) ? dist.ToSimilarity(string2.Length) : -1;
            }

            if (iMaxDistance < len2) {
                return Match.Levenshtein.Distance(string1, string2, len1, len2, start, iMaxDistance, new int[len2])
                    .ToSimilarity(string2.Length);
            }
            return Match.Levenshtein.Distance(string1, string2, len1, len2, start, new int[len2])
                .ToSimilarity(string2.Length);
#endif
        }

        /// <summary>Return Damerau-Levenshtein optimal string alignment similarity
        /// between two strings (1 - (damerau distance / len of longer string)).</summary>
        /// <param name="string1">One of the strings to compare.</param>
        /// <param name="string2">The other string to compare.</param>
        /// <returns>The degree of similarity 0 to 1.0, where 0 represents a lack of any
        /// notable similarity, and 1 represents equivalent strings.</returns>
        public static double DamerauOSA(string? string1, string? string2) {
            if (string1 == null) return (string2 == null) ? 1 : 0;
            if (string2 == null) return 0;
#if NET8_0_OR_GREATER
            return DamerauOSA(string1.AsSpan(), string2.AsSpan());
#else
            // if strings of different lengths, ensure shorter string is in string1. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (string1.Length > string2.Length) (string1, string2) = (string2, string1);

            // identify common suffix and/or prefix that can be ignored
            Helpers.PrefixSuffixPrep(string1, string2, out int len1, out int len2, out int start);
            if (len1 == 0) return (len2 == 0) ? 1.0 : len2.ToSimilarity(string2.Length);
            if (len1 == 1) return Helpers.SingleCharDistance(string1, string2, len2, start).ToSimilarity(string2.Length);

            return Match.DamerauOSA.Distance(string1, string2, len1, len2, start, new int[len2], new int[len2])
                .ToSimilarity(string2.Length);
#endif
        }

        /// <summary>Return Damerau-Levenshtein optimal string alignment similarity
        /// between two strings (1 - (damerau distance / len of longer string)).</summary>
        /// <param name="string1">One of the strings to compare.</param>
        /// <param name="string2">The other string to compare.</param>
        /// <param name="minSimilarity">The minimum similarity that is of interest.</param>
        /// <returns>The degree of similarity 0 to 1.0, where -1 represents a similarity
        /// lower than minSimilarity, otherwise, a number between 0 and 1.0 where 0
        /// represents a lack of any notable similarity, and 1 represents equivalent
        /// strings.</returns>
        public static double DamerauOSA(string? string1, string? string2, double minSimilarity) {
            if (double.IsNaN(minSimilarity) || minSimilarity < 0 || minSimilarity > 1) {
                throw new ArgumentOutOfRangeException(nameof(minSimilarity), minSimilarity, "minSimilarity must be in range 0 to 1.0");
            }
            if (string1 == null || string2 == null) return Helpers.NullSimilarityResults(string1, string2, minSimilarity);
#if NET8_0_OR_GREATER
            return DamerauOSA(string1.AsSpan(), string2.AsSpan(), minSimilarity);
#else
            // if strings of different lengths, ensure shorter string is in string1. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (string1.Length > string2.Length) (string1, string2) = (string2, string1);

            int iMaxDistance = minSimilarity.ToDistance(string2.Length);
            if (string2.Length - string1.Length > iMaxDistance) return -1;
            if (iMaxDistance == 0) return (string1 == string2) ? 1 : -1;

            // identify common suffix and/or prefix that can be ignored
            Helpers.PrefixSuffixPrep(string1, string2, out int len1, out int len2, out int start);
            if (len1 == 0) return (len2 == 0) ? 1.0 : len2.ToSimilarity(string2.Length);
            if (iMaxDistance == 1) {
                return (len2 == 1 || Helpers.IsSingleTransposition(string1, string2, len1, len2, start))
                    ? 1.ToSimilarity(string2.Length)
                    : -1;
            }
            if (len1 == 1) {
                int dist = Helpers.SingleCharDistance(string1, string2, len2, start);
                return (dist <= iMaxDistance) ? dist.ToSimilarity(string2.Length) : -1;
            }

            if (iMaxDistance < len2) {
                return Match.DamerauOSA.Distance(string1, string2, len1, len2, start, iMaxDistance, new int[len2], new int[len2])
                    .ToSimilarity(string2.Length);
            }
            return Match.DamerauOSA.Distance(string1, string2, len1, len2, start, new int[len2], new int[len2])
                .ToSimilarity(string2.Length);
#endif
        }

#if NET8_0_OR_GREATER
        /// <summary>Return Levenshtein similarity between two character spans (1 - (levenshtein distance / len of longer span)).</summary>
        /// <param name="string1">One of the character spans to compare.</param>
        /// <param name="string2">The other character span to compare.</param>
        /// <returns>The degree of similarity 0 to 1.0, where 0 represents a lack of any
        /// notable similarity, and 1 represents equivalent spans.</returns>
        public static double Levenshtein(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2) {
            if (string1.Length > string2.Length) {
                ReadOnlySpan<char> t = string1;
                string1 = string2;
                string2 = t;
            }
            int maxLen = string2.Length;

            Helpers.PrefixSuffixPrep(ref string1, ref string2);
            if (string1.Length == 0) return (string2.Length == 0) ? 1.0 : string2.Length.ToSimilarity(maxLen);
            if (string1.Length == 1) return Helpers.SingleCharDistance(string1, string2).ToSimilarity(maxLen);

            return Match.Levenshtein.DistanceCore(string1, string2).ToSimilarity(maxLen);
        }

        /// <summary>Return a measure of the similarity between two character spans.</summary>
        /// <param name="string1">One of the character spans to compare.</param>
        /// <param name="string2">The other character span to compare.</param>
        /// <param name="minSimilarity">The minimum similarity that is of interest.</param>
        /// <returns>The degree of similarity 0 to 1.0, where -1 represents a similarity
        /// lower than minSimilarity, otherwise, a number between 0 and 1.0 where 0
        /// represents a lack of any notable similarity, and 1 represents equivalent
        /// spans.</returns>
        public static double Levenshtein(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2, double minSimilarity) {
            if (double.IsNaN(minSimilarity) || minSimilarity < 0 || minSimilarity > 1) {
                throw new ArgumentOutOfRangeException(nameof(minSimilarity), minSimilarity, "minSimilarity must be in range 0 to 1.0");
            }

            if (string1.Length > string2.Length) {
                ReadOnlySpan<char> t = string1;
                string1 = string2;
                string2 = t;
            }
            int maxLen = string2.Length;

            int iMaxDistance = minSimilarity.ToDistance(maxLen);
            if (maxLen - string1.Length > iMaxDistance) return -1;
            if (iMaxDistance == 0) return string1.SequenceEqual(string2) ? 1 : -1;

            Helpers.PrefixSuffixPrep(ref string1, ref string2);
            if (string1.Length == 0) return (string2.Length == 0) ? 1.0 : string2.Length.ToSimilarity(maxLen);
            if (iMaxDistance == 1) return (string2.Length == 1) ? 1.ToSimilarity(maxLen) : -1;
            if (string1.Length == 1) {
                int dist = Helpers.SingleCharDistance(string1, string2);
                return (dist <= iMaxDistance) ? dist.ToSimilarity(maxLen) : -1;
            }

            if (iMaxDistance < string2.Length) {
                return Match.Levenshtein.DistanceCore(string1, string2, iMaxDistance).ToSimilarity(maxLen);
            }
            return Match.Levenshtein.DistanceCore(string1, string2).ToSimilarity(maxLen);
        }

        /// <summary>Return Damerau-Levenshtein optimal string alignment similarity
        /// between two character spans (1 - (damerau distance / len of longer span)).</summary>
        /// <param name="string1">One of the character spans to compare.</param>
        /// <param name="string2">The other character span to compare.</param>
        /// <returns>The degree of similarity 0 to 1.0, where 0 represents a lack of any
        /// notable similarity, and 1 represents equivalent spans.</returns>
        public static double DamerauOSA(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2) {
            if (string1.Length > string2.Length) {
                ReadOnlySpan<char> t = string1;
                string1 = string2;
                string2 = t;
            }
            int maxLen = string2.Length;

            Helpers.PrefixSuffixPrep(ref string1, ref string2);
            if (string1.Length == 0) return (string2.Length == 0) ? 1.0 : string2.Length.ToSimilarity(maxLen);
            if (string1.Length == 1) return Helpers.SingleCharDistance(string1, string2).ToSimilarity(maxLen);

            return Match.DamerauOSA.DistanceCore(string1, string2).ToSimilarity(maxLen);
        }

        /// <summary>Return Damerau-Levenshtein optimal string alignment similarity
        /// between two character spans (1 - (damerau distance / len of longer span)).</summary>
        /// <param name="string1">One of the character spans to compare.</param>
        /// <param name="string2">The other character span to compare.</param>
        /// <param name="minSimilarity">The minimum similarity that is of interest.</param>
        /// <returns>The degree of similarity 0 to 1.0, where -1 represents a similarity
        /// lower than minSimilarity, otherwise, a number between 0 and 1.0 where 0
        /// represents a lack of any notable similarity, and 1 represents equivalent
        /// spans.</returns>
        public static double DamerauOSA(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2, double minSimilarity) {
            if (double.IsNaN(minSimilarity) || minSimilarity < 0 || minSimilarity > 1) {
                throw new ArgumentOutOfRangeException(nameof(minSimilarity), minSimilarity, "minSimilarity must be in range 0 to 1.0");
            }

            if (string1.Length > string2.Length) {
                ReadOnlySpan<char> t = string1;
                string1 = string2;
                string2 = t;
            }
            int maxLen = string2.Length;

            int iMaxDistance = minSimilarity.ToDistance(maxLen);
            if (maxLen - string1.Length > iMaxDistance) return -1;
            if (iMaxDistance == 0) return string1.SequenceEqual(string2) ? 1 : -1;

            Helpers.PrefixSuffixPrep(ref string1, ref string2);
            if (string1.Length == 0) return (string2.Length == 0) ? 1.0 : string2.Length.ToSimilarity(maxLen);
            if (iMaxDistance == 1) {
                return (string2.Length == 1 || Helpers.IsSingleTransposition(string1, string2))
                    ? 1.ToSimilarity(maxLen)
                    : -1;
            }
            if (string1.Length == 1) {
                int dist = Helpers.SingleCharDistance(string1, string2);
                return (dist <= iMaxDistance) ? dist.ToSimilarity(maxLen) : -1;
            }

            if (iMaxDistance < string2.Length) {
                return Match.DamerauOSA.DistanceCore(string1, string2, iMaxDistance).ToSimilarity(maxLen);
            }
            return Match.DamerauOSA.DistanceCore(string1, string2).ToSimilarity(maxLen);
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
