// Copyright ©2015-2026 SoftWx, Inc.
// Released under the MIT License the text of which appears at the end of this file.
// <authors> Steve Hatchett

using System;
using System.Runtime.CompilerServices;

namespace SoftWx.Match {
    internal static class Helpers {
        /// <summary>Determines the proper return value of an edit distance function when one or
        /// both strings are null.</summary>
        public static int NullDistanceResults(string? string1, string? string2, int maxDistance) {
            if (string1 == null) return (string2 == null) ? 0 : (string2.Length <= maxDistance) ? string2.Length : -1;
            return (string1.Length <= maxDistance) ? string1.Length : -1;
        }

        /// <summary>Determines the proper return value of a similarity function when one or
        /// both strings are null.</summary>
        public static int NullSimilarityResults(string? string1, string? string2, double minSimilarity) {
            return (string1 == null && string2 == null) ? 1 : (0 >= minSimilarity) ? 0 : -1;
        }

#if NET8_0_OR_GREATER
        /// <summary>Trims common prefix and suffix from two spans.</summary>
        /// <remarks>Expects span1.Length to be less than or equal to span2.Length.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PrefixSuffixPrep(ref ReadOnlySpan<char> span1, ref ReadOnlySpan<char> span2) {
            int len1 = span1.Length;
            int len2 = span2.Length;
            int start = 0;
            if (len1 != 0 && span1[0] == span2[0]) {
                start = span1.CommonPrefixLength(span2);
                if (start == len1) {
                    span1 = ReadOnlySpan<char>.Empty;
                    span2 = span2.Slice(start);
                    return;
                }
            }
            // suffix common to both spans can be ignored
            while (len1 > start && span1[len1 - 1] == span2[len2 - 1]) {
                len1--;
                len2--;
            }
            span1 = span1.Slice(start, len1 - start);
            span2 = span2.Slice(start, len2 - start);
        }

        /// <summary>Computes exact Levenshtein / DamerauOSA edit distance when span1.Length == 1 after PrefixSuffixPrep.</summary>
        /// <remarks>Assumes span1[0] != span2[0] and span1[0] != span2[^1].</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SingleCharDistance(ReadOnlySpan<char> span1, ReadOnlySpan<char> span2) {
            int len2 = span2.Length;
            if (len2 <= 2) return len2;
            return (span2.Slice(1, len2 - 2).IndexOf(span1[0]) >= 0) ? len2 - 1 : len2;
        }

        /// <summary>Returns true if the remaining trimmed spans represent a single adjacent transposition.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSingleTransposition(ReadOnlySpan<char> span1, ReadOnlySpan<char> span2) {
            return span1.Length == 2 && span2.Length == 2 && span1[0] == span2[1] && span1[1] == span2[0];
        }
#else
        /// <summary>Calculates starting position and lengths of two strings such that common
        /// prefix and suffix substrings are excluded.</summary>
        /// <remarks>Expects string1.Length to be less than or equal to string2.Length.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PrefixSuffixPrep(string string1, string string2, out int len1, out int len2, out int start) {
            len2 = string2.Length;
            len1 = string1.Length; // this is also the minimum length of the two strings
            // suffix common to both strings can be ignored
            while (len1 != 0 && string1[len1 - 1] == string2[len2 - 1]) {
                len1--;
                len2--;
            }
            // prefix common to both strings can be ignored
            start = 0;
            while (start != len1 && string1[start] == string2[start]) start++;
            if (start != 0) {
                len2 -= start; // length of the part excluding common prefix and suffix
                len1 -= start;
            }
        }

        /// <summary>Computes exact Levenshtein / DamerauOSA edit distance when len1 == 1 after PrefixSuffixPrep.</summary>
        /// <remarks>Assumes string1[start] != string2[start] and string1[start] != string2[start + len2 - 1].</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SingleCharDistance(string string1, string string2, int len2, int start) {
            if (len2 <= 2) return len2;
            return (string2.IndexOf(string1[start], start + 1, len2 - 2) >= 0) ? len2 - 1 : len2;
        }

        /// <summary>Returns true if the remaining trimmed substrings represent a single adjacent transposition.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSingleTransposition(string string1, string string2, int len1, int len2, int start) {
            return len1 == 2 && len2 == 2 && string1[start] == string2[start + 1] && string1[start + 1] == string2[start];
        }
#endif

        /// <summary>Calculate a similarity measure from an edit distance.</summary>
        /// <param name="length">The length of the longer of the two strings the edit distance is from.</param>
        /// <param name="distance">The edit distance between two strings.</param>
        /// <returns>A similarity value from 0 to 1.0 (1 - (distance / length)).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ToSimilarity(this int distance, int length) {
            return (distance < 0) ? -1 : (length == 0) ? 1.0 : 1 - (distance / (double)length);
        }

        /// <summary>Calculate an edit distance from a similarity measure.</summary>
        /// <param name="length">The length of the longer of the two strings the edit distance is from.</param>
        /// <param name="similarity">The similarity measure between two strings.</param>
        /// <returns>An edit distance from 0 to length (length * (1 - similarity)).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToDistance(this double similarity, int length) {
            return (int)((length * (1 - similarity)) + .0000000001);
        }
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
