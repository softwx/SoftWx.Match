// Copyright © 2015-2026 Steve Hatchett
// SPDX-License-Identifier: MIT
// Released under the MIT License the text of which appears at the end of this file.
// <authors> Steve Hatchett

using System;
using System.Runtime.CompilerServices;
#if NET8_0_OR_GREATER
using CharSpan = System.ReadOnlySpan<char>;
using IntSpan = System.Span<int>;
#endif

namespace SoftWx.Match {
    /// <summary>
    /// Class providing optimized methods for computing Damerau-Levenshtein Optimal String
    /// Alignment (OSA) comparisons between two strings.
    /// </summary>
    /// <remarks>
    /// Copyright © 2015-2026 Steve Hatchett
    /// The inspiration for creating highly optimized edit distance functions was
    /// from Sten Hjelmqvist's "Fast, memory efficient" algorithm, described at
    /// https://www.codeproject.com/Articles/13525/Fast-memory-efficient-Levenshtein-algorithm
    /// The Damerau-Levenshtein algorithm is basically the Levenshtein algorithm with a
    /// modification that considers transposition of two adjacent characters as a single edit.
    /// The optimized algorithm was described in detail in my post at
    /// https://blog.softwx.net/2015/01/optimizing-damerau-levenshtein_15.html
    /// Also see https://en.wikipedia.org/wiki/Damerau%E2%80%93Levenshtein_distance
    /// Note that this implementation of Damerau-Levenshtein is the simpler and faster optimal
    /// string alignment (aka restricted edit) distance that differs slightly from the classic
    /// algorithm by imposing the restriction that no substring is edited more than once. So,
    /// for example, "CA" to "ABC" has an edit distance of 2 by a complete application of
    /// Damerau-Levenshtein (transpose CA to AC, then insert B), but has a distance of 3 by the
    /// method implemented here, which uses the optimal string alignment algorithm. This means
    /// that this algorithm is not a true metric since it does not uphold the triangle inequality.
    /// In real use though, this OSA version is often desired since it is faster and the cases
    /// where it differs from unrestricted Damerau-Levenshtein are rare in practice.
    /// The methods in this class are not threadsafe. Use the static versions in the Distance
    /// and Similarity classes if that is required.</remarks>
    public class DamerauOSA : IDistance, ISimilarity {
        private const int DefaultMaxCachedLength = 512;
        private const int MinBufferLength = 32;
        private readonly int maxCachedLength;
        private int[] baseChar1Costs;
        private int[] basePrevChar1Costs;

        /// <summary>Create a new instance of DamerauOSA.</summary>
        public DamerauOSA() {
            this.maxCachedLength = DefaultMaxCachedLength;
            this.baseChar1Costs = Array.Empty<int>();
            this.basePrevChar1Costs = Array.Empty<int>();
        }

        /// <summary>Create a new instance of DamerauOSA using the specified expected
        /// maximum string length that will be encountered.</summary>
        /// <remarks>By specifying the max expected string length, better memory efficiency
        /// can be achieved.</remarks>
        /// <param name="expectedMaxStringLength">The expected maximum length of strings that will
        /// be passed to the edit distance functions.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when expectedMaxStringLength is negative.</exception>
        public DamerauOSA(int expectedMaxStringLength) {
            if (expectedMaxStringLength < 0) {
                throw new ArgumentOutOfRangeException(nameof(expectedMaxStringLength), expectedMaxStringLength, "expectedMaxStringLength must be non-negative.");
            }
            this.maxCachedLength = Math.Max(expectedMaxStringLength, DefaultMaxCachedLength);
            if (expectedMaxStringLength == 0) {
                this.baseChar1Costs = Array.Empty<int>();
                this.basePrevChar1Costs = Array.Empty<int>();
            } else {
                this.baseChar1Costs = new int[expectedMaxStringLength];
                this.basePrevChar1Costs = new int[expectedMaxStringLength];
            }
        }

        /// <summary>Compute and return the Damerau-Levenshtein optimal string
        /// alignment edit distance between two strings.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is not threadsafe.</remarks>
        /// <param name="string1">One of the strings to compare.</param>
        /// <param name="string2">The other string to compare.</param>
        /// <returns>0 if the strings are equivalent, otherwise a positive number whose
        /// magnitude increases as difference between the strings increases.</returns>
        public double Distance(string? string1, string? string2) {
            if (string1 == null) return (string2 ?? string.Empty).Length;
            if (string2 == null) return string1.Length;
            return DistanceInternal(string1.AsSpan(), string2.AsSpan());
        }

        /// <summary>Compute and return the Damerau-Levenshtein optimal string
        /// alignment edit distance between two strings.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is not threadsafe.</remarks>
        /// <param name="string1">One of the strings to compare.</param>
        /// <param name="string2">The other string to compare.</param>
        /// <param name="maxDistance">The maximum distance that is of interest.</param>
        /// <returns>-1 if the distance is greater than the maxDistance, 0 if the strings
        /// are equivalent, otherwise a positive number whose magnitude increases as
        /// difference between the strings increases.</returns>
        public double Distance(string? string1, string? string2, double maxDistance) {
            if (double.IsNaN(maxDistance)) return -1;
            int iMaxDistance = Helpers.ClampMaxDistance(maxDistance);
            if (string1 == null || string2 == null) return Helpers.NullDistanceResults(string1, string2, iMaxDistance);
            return DistanceInternal(string1.AsSpan(), string2.AsSpan(), iMaxDistance);
        }

        /// <summary>Return Damerau-Levenshtein optimal string alignment similarity
        /// between two strings (1 - (damerau distance / len of longer string)).</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is not threadsafe.</remarks>
        /// <param name="string1">One of the strings to compare.</param>
        /// <param name="string2">The other string to compare.</param>
        /// <returns>The degree of similarity 0 to 1.0, where 0 represents a lack of any
        /// notable similarity, and 1 represents equivalent strings.</returns>
        public double Similarity(string? string1, string? string2) {
            if (string1 == null) return (string2 == null) ? 1 : 0;
            if (string2 == null) return 0;
            return DistanceInternal(string1.AsSpan(), string2.AsSpan())
                .ToSimilarity(Math.Max(string1.Length, string2.Length));
        }

        /// <summary>Return Damerau-Levenshtein optimal string alignment similarity
        /// between two strings (1 - (damerau distance / len of longer string)).</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is not threadsafe.</remarks>
        /// <param name="string1">One of the strings to compare.</param>
        /// <param name="string2">The other string to compare.</param>
        /// <param name="minSimilarity">The minimum similarity that is of interest.</param>
        /// <returns>The degree of similarity 0 to 1.0, where -1 represents a similarity
        /// lower than minSimilarity, otherwise, a number between 0 and 1.0 where 0
        /// represents a lack of any notable similarity, and 1 represents equivalent
        /// strings.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when minSimilarity is NaN or outside the range 0 to 1.0.</exception>
        public double Similarity(string? string1, string? string2, double minSimilarity) {
            Helpers.ValidateMinSimilarity(minSimilarity);
            if (string1 == null || string2 == null) return Helpers.NullSimilarityResults(string1, string2, minSimilarity);
            int maxLen = Math.Max(string1.Length, string2.Length);
            return DistanceInternal(string1.AsSpan(), string2.AsSpan(), minSimilarity.ToDistance(maxLen))
                .ToSimilarity(maxLen);
        }

#if NET8_0_OR_GREATER
        /// <summary>Compute and return the Damerau-Levenshtein optimal string
        /// alignment edit distance between two character spans.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is not threadsafe.</remarks>
        /// <param name="string1">One of the character spans to compare.</param>
        /// <param name="string2">The other character span to compare.</param>
        /// <returns>0 if the spans are equivalent, otherwise a positive number whose
        /// magnitude increases as difference between the spans increases.</returns>
        public double Distance(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2) =>
            DistanceInternal(string1, string2);

        /// <summary>Compute and return the Damerau-Levenshtein optimal string
        /// alignment edit distance between two character spans.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is not threadsafe.</remarks>
        /// <param name="string1">One of the character spans to compare.</param>
        /// <param name="string2">The other character span to compare.</param>
        /// <param name="maxDistance">The maximum distance that is of interest.</param>
        /// <returns>-1 if the distance is greater than the maxDistance, 0 if the spans
        /// are equivalent, otherwise a positive number whose magnitude increases as
        /// difference between the spans increases.</returns>
        public double Distance(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2, double maxDistance) {
            if (double.IsNaN(maxDistance)) return -1;
            return DistanceInternal(string1, string2, Helpers.ClampMaxDistance(maxDistance));
        }

        /// <summary>Return Damerau-Levenshtein optimal string alignment similarity
        /// between two character spans (1 - (damerau distance / len of longer span)).</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is not threadsafe.</remarks>
        /// <param name="string1">One of the character spans to compare.</param>
        /// <param name="string2">The other character span to compare.</param>
        /// <returns>The degree of similarity 0 to 1.0, where 0 represents a lack of any
        /// notable similarity, and 1 represents equivalent spans.</returns>
        public double Similarity(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2) =>
            DistanceInternal(string1, string2).ToSimilarity(Math.Max(string1.Length, string2.Length));

        /// <summary>Return Damerau-Levenshtein optimal string alignment similarity
        /// between two character spans (1 - (damerau distance / len of longer span)).</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is not threadsafe.</remarks>
        /// <param name="string1">One of the character spans to compare.</param>
        /// <param name="string2">The other character span to compare.</param>
        /// <param name="minSimilarity">The minimum similarity that is of interest.</param>
        /// <returns>The degree of similarity 0 to 1.0, where -1 represents a similarity
        /// lower than minSimilarity, otherwise, a number between 0 and 1.0 where 0
        /// represents a lack of any notable similarity, and 1 represents equivalent
        /// spans.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when minSimilarity is NaN or outside the range 0 to 1.0.</exception>
        public double Similarity(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2, double minSimilarity) {
            Helpers.ValidateMinSimilarity(minSimilarity);
            int maxLen = Math.Max(string1.Length, string2.Length);
            return DistanceInternal(string1, string2, minSimilarity.ToDistance(maxLen))
                .ToSimilarity(maxLen);
        }
#endif

        // CharSpan / IntSpan resolve to System.ReadOnlySpan<char> / System.Span<int> on .NET 8+
        // and to lightweight internal shims on netstandard2.0 (see SpanShim.cs).

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int DistanceInternal(CharSpan s1, CharSpan s2) {
            if (Helpers.TryFastDistance(ref s1, ref s2, out int distance)) return distance;
            return DistanceWithBuffer(s1, s2);
        }

        private int DistanceInternal(CharSpan s1, CharSpan s2, int maxDistance) {
            if (Helpers.TryFastDistance(ref s1, ref s2, maxDistance, allowTransposition: true, out int distance)) return distance;
            return (maxDistance < s2.Length)
                ? DistanceWithBuffer(s1, s2, maxDistance)
                : DistanceWithBuffer(s1, s2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int DistanceStatic(CharSpan s1, CharSpan s2) {
            if (Helpers.TryFastDistance(ref s1, ref s2, out int distance)) return distance;
            return DistanceCore(s1, s2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int DistanceStatic(CharSpan s1, CharSpan s2, int maxDistance) {
            if (Helpers.TryFastDistance(ref s1, ref s2, maxDistance, allowTransposition: true, out int distance)) return distance;
            return (maxDistance < s2.Length)
                ? DistanceCore(s1, s2, maxDistance)
                : DistanceCore(s1, s2);
        }

        // Computes next buffer capacity via geometric growth from MinBufferLength, capped at maxLength.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GrowBufferLength(int required, int current, int maxLength) {
            int doubled = (current <= maxLength / 2) ? current * 2 : maxLength;
            int grown = Math.Max(MinBufferLength, doubled);
            return Math.Min(maxLength, Math.Max(required, grown));
        }

        // Reuses instance buffers up to maxCachedLength; falls back to DistanceCore for oversized inputs
        // so one-off giant strings don't permanently pin large buffers on a long-lived instance.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int DistanceWithBuffer(CharSpan s1, CharSpan s2) {
            int len2 = s2.Length;
            int[] costs = this.baseChar1Costs;
            if (len2 <= costs.Length) {
                return Distance(s1, s2, costs, this.basePrevChar1Costs);
            }
            if (len2 <= this.maxCachedLength) {
                int newLength = GrowBufferLength(len2, costs.Length, this.maxCachedLength);
                costs = this.baseChar1Costs = new int[newLength];
                int[] prevCosts = this.basePrevChar1Costs = new int[newLength];
                return Distance(s1, s2, costs, prevCosts);
            }
            return DistanceCore(s1, s2);
        }

        // Bounded counterpart to DistanceWithBuffer.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int DistanceWithBuffer(CharSpan s1, CharSpan s2, int maxDistance) {
            int len2 = s2.Length;
            int[] costs = this.baseChar1Costs;
            if (len2 <= costs.Length) {
                return Distance(s1, s2, maxDistance, costs, this.basePrevChar1Costs);
            }
            if (len2 <= this.maxCachedLength) {
                int newLength = GrowBufferLength(len2, costs.Length, this.maxCachedLength);
                costs = this.baseChar1Costs = new int[newLength];
                int[] prevCosts = this.basePrevChar1Costs = new int[newLength];
                return Distance(s1, s2, maxDistance, costs, prevCosts);
            }
            return DistanceCore(s1, s2, maxDistance);
        }

#if NET8_0_OR_GREATER
        private const int StackAllocThreshold = 256;

        // Threadsafe buffer acquisition: stackalloc up to 2 KB total (2 x 256 ints), otherwise rent a single
        // double-sized buffer from ArrayPool and slice it into both row buffers (one Rent/Return pair).
        private static int DistanceCore(CharSpan s1, CharSpan s2) {
            if (s2.Length <= StackAllocThreshold) {
                Span<int> char1Costs = stackalloc int[s2.Length];
                Span<int> prevChar1Costs = stackalloc int[s2.Length];
                return Distance(s1, s2, char1Costs, prevChar1Costs);
            }
            int[] rented = System.Buffers.ArrayPool<int>.Shared.Rent(s2.Length * 2);
            try {
                return Distance(s1, s2, rented.AsSpan(0, s2.Length), rented.AsSpan(s2.Length, s2.Length));
            } finally {
                System.Buffers.ArrayPool<int>.Shared.Return(rented);
            }
        }

        // Bounded counterpart to DistanceCore.
        private static int DistanceCore(CharSpan s1, CharSpan s2, int maxDistance) {
            if (s2.Length <= StackAllocThreshold) {
                Span<int> char1Costs = stackalloc int[s2.Length];
                Span<int> prevChar1Costs = stackalloc int[s2.Length];
                return Distance(s1, s2, maxDistance, char1Costs, prevChar1Costs);
            }
            int[] rented = System.Buffers.ArrayPool<int>.Shared.Rent(s2.Length * 2);
            try {
                return Distance(s1, s2, maxDistance, rented.AsSpan(0, s2.Length), rented.AsSpan(s2.Length, s2.Length));
            } finally {
                System.Buffers.ArrayPool<int>.Shared.Return(rented);
            }
        }
#else
        private const int ThreadCacheMaxLength = 256;
        [ThreadStatic]
        private static int[]? tlsChar1Costs;
        [ThreadStatic]
        private static int[]? tlsPrevChar1Costs;

        // Threadsafe buffer acquisition using a capped per-thread cache on netstandard2.0.
        private static int DistanceCore(CharSpan s1, CharSpan s2) {
            GetThreadCosts(s2.Length, out int[] costs, out int[] prevCosts);
            return Distance(s1, s2, costs, prevCosts);
        }

        // Bounded counterpart to DistanceCore.
        private static int DistanceCore(CharSpan s1, CharSpan s2, int maxDistance) {
            GetThreadCosts(s2.Length, out int[] costs, out int[] prevCosts);
            return Distance(s1, s2, maxDistance, costs, prevCosts);
        }

        // Returns reusable per-thread scratch buffers up to ThreadCacheMaxLength, or transient arrays if larger.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GetThreadCosts(int len2, out int[] costs, out int[] prevCosts) {
            int[]? cachedCosts = tlsChar1Costs;
            int[]? cachedPrev = tlsPrevChar1Costs;
            if (cachedCosts != null && cachedPrev != null && len2 <= cachedCosts.Length) {
                costs = cachedCosts;
                prevCosts = cachedPrev;
                return;
            }
            if (len2 <= ThreadCacheMaxLength) {
                int currentLen = (cachedCosts != null) ? cachedCosts.Length : 0;
                int newLength = GrowBufferLength(len2, currentLen, ThreadCacheMaxLength);
                costs = new int[newLength];
                prevCosts = new int[newLength];
                tlsChar1Costs = costs;
                tlsPrevChar1Costs = prevCosts;
                return;
            }
            costs = new int[len2];
            prevCosts = new int[len2];
        }
#endif

        // Core unbounded Damerau-Levenshtein OSA DP algorithm (expects s1.Length <= s2.Length
        // and both scratch buffers of length at least s2.Length).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Distance(CharSpan s1, CharSpan s2, IntSpan char1Costs, IntSpan prevChar1Costs) {
            int len1 = s1.Length;
            int len2 = s2.Length;
            // Slice both row buffers to exact len2 so lengths match s2.Length, helping RyuJIT elide bounds checks.
            char1Costs = char1Costs.Slice(0, len2);
            prevChar1Costs = prevChar1Costs.Slice(0, len2);

            // Two rolling 1D row buffers (O(len2) space) instead of a full 2D matrix; prevChar1Costs
            // holds the prior row's diagonals needed for adjacent-character transposition checks.
            for (int j = 0; j < len2;) char1Costs[j] = ++j;
            char char1 = default;
            int currentCost = 0;
            for (int i = 0; i < len1; ++i) {
                char prevChar1 = char1;
                char1 = s1[i];
                char char2 = default;
                int leftCharCost = i;
                int aboveCharCost = i;
                int nextTransCost = 0;
                for (int j = 0; j < s2.Length; ++j) {
                    int thisTransCost = nextTransCost;
                    nextTransCost = prevChar1Costs[j];
                    prevChar1Costs[j] = currentCost = leftCharCost; // cost of diagonal (substitution)
                    leftCharCost = char1Costs[j]; // left now equals current cost (which will be diagonal at next iteration)
                    char prevChar2 = char2;
                    char2 = s2[j];
                    if (char1 != char2) {
                        // substitution if neither of two conditions below
                        if (aboveCharCost < currentCost) currentCost = aboveCharCost; // deletion
                        if (leftCharCost < currentCost) currentCost = leftCharCost;   // insertion
                        ++currentCost;
                        if ((i != 0) && (j != 0)
                            && (char1 == prevChar2)
                            && (prevChar1 == char2)
                            && (thisTransCost + 1 < currentCost)) {
                            currentCost = thisTransCost + 1; // transposition
                        }
                    }
                    char1Costs[j] = aboveCharCost = currentCost;
                }
            }
            return currentCost;
        }

        // Core bounded Damerau-Levenshtein OSA DP algorithm (expects s1.Length <= s2.Length, s2.Length >= maxDistance,
        // s2.Length - s1.Length <= maxDistance, and both scratch buffers of length at least s2.Length).
        private static int Distance(CharSpan s1, CharSpan s2, int maxDistance, IntSpan char1Costs, IntSpan prevChar1Costs) {
            int len1 = s1.Length;
            int len2 = s2.Length;
#if DEBUG
            if (len2 < maxDistance) throw new ArgumentException("len2 must be >= maxDistance", nameof(maxDistance));
            if (len2 - len1 > maxDistance) throw new ArgumentException("len2 - len1 must be <= maxDistance", nameof(maxDistance));
#endif
            // Slice both row buffers to exact len2 so bounds align with s2.
            char1Costs = char1Costs.Slice(0, len2);
            prevChar1Costs = prevChar1Costs.Slice(0, len2);

            // Initialize only the first maxDistance cells upfront; each newly entering cell at jEnd is
            // lazily initialized per row below, keeping early-exit rejection O(maxDistance^2) instead of O(len2).
            // prevChar1Costs[jEnd - 1] is written on this row before being read as thisTransCost on the next row.
            for (int j = 0; j < maxDistance;) char1Costs[j] = ++j;
            int lenDiff = len2 - len1;
            int jStartOffset = maxDistance - lenDiff;
            int jStart = 0;
            int jEnd = maxDistance;
            char char1 = default;
            int currentCost = 0;
            for (int i = 0; i < len1; ++i) {
                char prevChar1 = char1;
                char1 = s1[i];
                char char2 = default;
                int leftCharCost = i;
                int aboveCharCost = i;
                int nextTransCost = 0;
                // Only compute cells within the diagonal band [jStart, jEnd) that can still yield distance <= maxDistance.
                jStart += (i > jStartOffset) ? 1 : 0;
                if (jEnd < len2) char1Costs[jEnd++] = maxDistance + 1;
                for (int j = jStart; j < jEnd; ++j) {
                    int thisTransCost = nextTransCost;
                    nextTransCost = prevChar1Costs[j];
                    prevChar1Costs[j] = currentCost = leftCharCost; // cost on diagonal (substitution)
                    leftCharCost = char1Costs[j]; // left now equals current cost (which will be diagonal at next iteration)
                    char prevChar2 = char2;
                    char2 = s2[j];
                    if (char1 != char2) {
                        // substitution if neither of two conditions below
                        if (aboveCharCost < currentCost) currentCost = aboveCharCost; // deletion
                        if (leftCharCost < currentCost) currentCost = leftCharCost;   // insertion
                        ++currentCost;
                        if ((i != 0) && (j != 0)
                            && (char1 == prevChar2)
                            && (prevChar1 == char2)
                            && (thisTransCost + 1 < currentCost)) {
                            currentCost = thisTransCost + 1; // transposition
                        }
                    }
                    char1Costs[j] = aboveCharCost = currentCost;
                }
                // Values along any diagonal are monotonically non-decreasing; if the target diagonal
                // cell already exceeds maxDistance, the final distance must also exceed maxDistance.
                if (char1Costs[i + lenDiff] > maxDistance) return -1;
            }
            return currentCost;
        }
    }

    public static partial class Distance {
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

    public static partial class Similarity {
        /// <summary>Return Damerau-Levenshtein optimal string alignment similarity
        /// between two strings (1 - (damerau distance / len of longer string)).</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is threadsafe.</remarks>
        /// <param name="string1">One of the strings to compare.</param>
        /// <param name="string2">The other string to compare.</param>
        /// <returns>The degree of similarity 0 to 1.0, where 0 represents a lack of any
        /// notable similarity, and 1 represents equivalent strings.</returns>
        public static double DamerauOSA(string? string1, string? string2) {
            if (string1 == null) return (string2 == null) ? 1 : 0;
            if (string2 == null) return 0;
            return Match.DamerauOSA.DistanceStatic(string1.AsSpan(), string2.AsSpan())
                .ToSimilarity(Math.Max(string1.Length, string2.Length));
        }

        /// <summary>Return Damerau-Levenshtein optimal string alignment similarity
        /// between two strings (1 - (damerau distance / len of longer string)).</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is threadsafe.</remarks>
        /// <param name="string1">One of the strings to compare.</param>
        /// <param name="string2">The other string to compare.</param>
        /// <param name="minSimilarity">The minimum similarity that is of interest.</param>
        /// <returns>The degree of similarity 0 to 1.0, where -1 represents a similarity
        /// lower than minSimilarity, otherwise, a number between 0 and 1.0 where 0
        /// represents a lack of any notable similarity, and 1 represents equivalent
        /// strings.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when minSimilarity is NaN or outside the range 0 to 1.0.</exception>
        public static double DamerauOSA(string? string1, string? string2, double minSimilarity) {
            Helpers.ValidateMinSimilarity(minSimilarity);
            if (string1 == null || string2 == null) return Helpers.NullSimilarityResults(string1, string2, minSimilarity);
            int maxLen = Math.Max(string1.Length, string2.Length);
            return Match.DamerauOSA.DistanceStatic(string1.AsSpan(), string2.AsSpan(), minSimilarity.ToDistance(maxLen))
                .ToSimilarity(maxLen);
        }

#if NET8_0_OR_GREATER
        /// <summary>Return Damerau-Levenshtein optimal string alignment similarity
        /// between two character spans (1 - (damerau distance / len of longer span)).</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is threadsafe.</remarks>
        /// <param name="string1">One of the character spans to compare.</param>
        /// <param name="string2">The other character span to compare.</param>
        /// <returns>The degree of similarity 0 to 1.0, where 0 represents a lack of any
        /// notable similarity, and 1 represents equivalent spans.</returns>
        public static double DamerauOSA(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2) =>
            Match.DamerauOSA.DistanceStatic(string1, string2)
                .ToSimilarity(Math.Max(string1.Length, string2.Length));

        /// <summary>Return Damerau-Levenshtein optimal string alignment similarity
        /// between two character spans (1 - (damerau distance / len of longer span)).</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is threadsafe.</remarks>
        /// <param name="string1">One of the character spans to compare.</param>
        /// <param name="string2">The other character span to compare.</param>
        /// <param name="minSimilarity">The minimum similarity that is of interest.</param>
        /// <returns>The degree of similarity 0 to 1.0, where -1 represents a similarity
        /// lower than minSimilarity, otherwise, a number between 0 and 1.0 where 0
        /// represents a lack of any notable similarity, and 1 represents equivalent
        /// spans.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when minSimilarity is NaN or outside the range 0 to 1.0.</exception>
        public static double DamerauOSA(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2, double minSimilarity) {
            Helpers.ValidateMinSimilarity(minSimilarity);
            int maxLen = Math.Max(string1.Length, string2.Length);
            return Match.DamerauOSA.DistanceStatic(string1, string2, minSimilarity.ToDistance(maxLen))
                .ToSimilarity(maxLen);
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
