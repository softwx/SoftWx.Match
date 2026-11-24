// Copyright © 2015-2026 Steve Hatchett
// SPDX-License-Identifier: MIT
// Released under the MIT License the text of which appears at the end of this file.
// <authors> Steve Hatchett

using System;
using System.Runtime.CompilerServices;

namespace SoftWx.Match {
    // Internal helper methods shared across distance and similarity implementations.
    // CharSpan / IntSpan resolve to System.ReadOnlySpan<char> / System.Span<int> on .NET 8+
    // and to lightweight internal shims on netstandard2.0 (see SpanShim.cs).
    internal static class Helpers {
        // Determines the proper return value of an edit distance function when one or both strings are null.
        public static int NullDistanceResults(string? string1, string? string2, int maxDistance) {
            if (string1 == null) return (string2 == null) ? 0 : (string2.Length <= maxDistance) ? string2.Length : -1;
            return (string1.Length <= maxDistance) ? string1.Length : -1;
        }

        // Determines the proper return value of a similarity function when one or both strings are null.
        public static int NullSimilarityResults(string? string1, string? string2, double minSimilarity) {
            return (string1 == null && string2 == null) ? 1 : (0 >= minSimilarity) ? 0 : -1;
        }

        // Clamps a non-NaN double maxDistance to an int via floor (caller handles NaN).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ClampMaxDistance(double maxDistance) {
            return (maxDistance < int.MinValue) ? int.MinValue
                : (maxDistance > int.MaxValue) ? int.MaxValue
                : (int)Math.Floor(maxDistance);
        }

        // Validates that minSimilarity is in [0, 1.0].
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValidateMinSimilarity(double minSimilarity) {
            if (double.IsNaN(minSimilarity) || minSimilarity < 0 || minSimilarity > 1) {
                ThrowInvalidMinSimilarity(minSimilarity);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidMinSimilarity(double minSimilarity) {
            throw new ArgumentOutOfRangeException(nameof(minSimilarity), minSimilarity, "minSimilarity must be in range 0 to 1.0");
        }

        // Ensures the shorter span is in span1, strips common prefix/suffix, and resolves trivial
        // 0-char and 1-char remainders without allocating or entering DP.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryFastDistance(ref CharSpan span1, ref CharSpan span2, out int distance) {
            if (span1.Length > span2.Length) {
                CharSpan t = span1;
                span1 = span2;
                span2 = t;
            }
            PrefixSuffixPrep(ref span1, ref span2);
            if (span1.Length == 0) {
                distance = span2.Length;
                return true;
            }
            if (span1.Length == 1) {
                distance = SingleCharDistance(span1, span2);
                return true;
            }
            distance = 0;
            return false;
        }

        // Bounded counterpart to TryFastDistance: resolves maxDistance <= 0, length-diff rejection,
        // prefix/suffix, maxDistance == 1 (single substitution, or transposition when allowTransposition
        // is true), and 1-char remainders without allocating or entering DP.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryFastDistance(ref CharSpan span1, ref CharSpan span2, int maxDistance, bool allowTransposition, out int distance) {
            if (maxDistance <= 0) {
                distance = span1.SequenceEqual(span2) ? 0 : -1;
                return true;
            }
            if (span1.Length > span2.Length) {
                CharSpan t = span1;
                span1 = span2;
                span2 = t;
            }
            if (span2.Length - span1.Length > maxDistance) {
                distance = -1;
                return true;
            }
            PrefixSuffixPrep(ref span1, ref span2);
            if (span1.Length == 0) {
                distance = span2.Length;
                return true;
            }
            // After PrefixSuffixPrep both ends differ, so distance can only be 1 if len2 == 1
            // (1 substitution) or, for DamerauOSA, both trimmed spans are 2 transposed chars.
            if (maxDistance == 1) {
                distance = (span2.Length == 1 || (allowTransposition && IsSingleTransposition(span1, span2))) ? 1 : -1;
                return true;
            }
            if (span1.Length == 1) {
                int dist = SingleCharDistance(span1, span2);
                distance = (dist <= maxDistance) ? dist : -1;
                return true;
            }
            distance = 0;
            return false;
        }

        // Trims common prefix and suffix from two spans (expects span1.Length <= span2.Length).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PrefixSuffixPrep(ref CharSpan span1, ref CharSpan span2) {
            int len1 = span1.Length;
            int len2 = span2.Length;
            int start = 0;
            // Guard with a single-char check to avoid CommonPrefixLength call overhead when first chars differ,
            // while using SIMD-vectorized CommonPrefixLength on .NET 8+ whenever a shared prefix exists.
            if (len1 != 0 && span1[0] == span2[0]) {
                start = span1.CommonPrefixLength(span2);
                // If the entire shorter span matched as a prefix, skip suffix scanning.
                if (start == len1) {
                    span1 = CharSpan.Empty;
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

        // Computes exact Levenshtein / DamerauOSA distance when span1.Length == 1 after PrefixSuffixPrep
        // (assumes span1[0] != span2[0] and span1[0] != span2[len2 - 1]).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SingleCharDistance(CharSpan span1, CharSpan span2) {
            int len2 = span2.Length;
            // After PrefixSuffixPrep, span1[0] cannot match span2[0] or span2[len2 - 1], so for len2 <= 2
            // no match is possible (distance == len2). For len2 >= 3, only the interior can match.
            if (len2 <= 2) return len2;
            return (span2.Slice(1, len2 - 2).IndexOf(span1[0]) >= 0) ? len2 - 1 : len2;
        }

        // Returns true if the remaining trimmed spans represent a single adjacent transposition.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSingleTransposition(CharSpan span1, CharSpan span2) {
            // After PrefixSuffixPrep, first and last chars differ, so a single adjacent transposition
            // can only exist if both trimmed spans have length 2 and are reversed.
            return span1.Length == 2 && span2.Length == 2 && span1[0] == span2[1] && span1[1] == span2[0];
        }

        // Converts an edit distance to a normalized similarity in [0, 1.0], or -1 if distance is negative.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ToSimilarity(this int distance, int length) {
            return (distance < 0) ? -1 : (length == 0) ? 1.0 : 1 - (distance / (double)length);
        }

        // Converts a normalized similarity threshold in [0, 1.0] to the maximum edit distance for strings of the given length.
        // The small epsilon compensates for IEEE-754 rounding errors (e.g. 10 * (1 - 0.8) = 1.9999999999999996)
        // that would otherwise truncate down by 1 when cast to int.
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
