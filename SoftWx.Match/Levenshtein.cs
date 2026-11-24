// Copyright © 2015-2026 Steve Hatchett
// SPDX-License-Identifier: MIT
// Released under the MIT License the text of which appears at the end of this file.
// <authors> Steve Hatchett

using System;
using System.Runtime.CompilerServices;
#if NET8_0_OR_GREATER
using System.Buffers;
#endif

namespace SoftWx.Match {
    /// <summary>
    /// Class providing optimized methods for computing Levenshtein comparisons between two strings.
    /// </summary>
    /// <remarks>
    /// Copyright © 2015-2026 Steve Hatchett
    /// The inspiration for creating highly optimized edit distance functions was
    /// from Sten Hjelmqvist's "Fast, memory efficient" algorithm, described at
    /// https://www.codeproject.com/Articles/13525/Fast-memory-efficient-Levenshtein-algorithm
    /// The Levenshtein algorithm computes the edit distance metric between two strings, i.e.
    /// the number of insertion, deletion, and substitution edits required to transform one
    /// string to the other. This value will be >= 0, where 0 indicates identical strings.
    /// Comparisons are case sensitive, so for example, "Fred" and "fred" will have a
    /// distance of 1. The optimized algorithm was described in detail in my post at
    /// https://blog.softwx.net/2014/12/optimizing-levenshtein-algorithm-in-c.html
    /// Also see https://en.wikipedia.org/wiki/Levenshtein_distance for general information.
    /// The methods in this class are not threadsafe. Use the static versions in the Distance
    /// and Similarity classes if that is required.</remarks>
    public class Levenshtein : IDistance, ISimilarity {
        private const int DefaultMaxCachedLength = 512;
#if NET8_0_OR_GREATER
        private const int StackAllocThreshold = 256;
#else
        private const int ThreadCacheMaxLength = 256;
        [ThreadStatic]
        private static int[]? tlsChar1Costs;
#endif
        private readonly int maxCachedLength;
        private int[] baseChar1Costs;

        /// <summary>Create a new instance of Levenshtein.</summary>
        public Levenshtein() {
            this.maxCachedLength = DefaultMaxCachedLength;
            this.baseChar1Costs = Array.Empty<int>();
        }

        /// <summary>Create a new instance of Levenshtein using the specified expected
        /// maximum string length that will be encountered.</summary>
        /// <remarks>By specifying the max expected string length, better memory efficiency
        /// can be achieved.</remarks>
        /// <param name="expectedMaxStringLength">The expected maximum length of strings that will
        /// be passed to the Levenshtein methods.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when expectedMaxStringLength is negative.</exception>
        public Levenshtein(int expectedMaxStringLength) {
            if (expectedMaxStringLength < 0) {
                throw new ArgumentOutOfRangeException(nameof(expectedMaxStringLength), expectedMaxStringLength, "expectedMaxStringLength must be non-negative.");
            }
            this.maxCachedLength = Math.Max(expectedMaxStringLength, DefaultMaxCachedLength);
            this.baseChar1Costs = expectedMaxStringLength == 0 ? Array.Empty<int>() : new int[expectedMaxStringLength];
        }

        /// <summary>Compute and return the Levenshtein edit distance between two strings.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is not threadsafe.</remarks>
        /// <param name="string1">One of the strings to compare.</param>
        /// <param name="string2">The other string to compare.</param>
        /// <returns>0 if the strings are equivalent, otherwise a positive number whose
        /// magnitude increases as difference between the strings increases.</returns>
        public double Distance(string? string1, string? string2) {
            if (string1 == null) return (string2 ?? string.Empty).Length;
            if (string2 == null) return string1.Length;
#if NET8_0_OR_GREATER
            return Distance(string1.AsSpan(), string2.AsSpan());
#else
            // if strings of different lengths, ensure shorter string is in string1. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (string1.Length > string2.Length) (string1, string2) = (string2, string1);

            // identify common suffix and/or prefix that can be ignored
            Helpers.PrefixSuffixPrep(string1, string2, out int len1, out int len2, out int start);
            if (len1 == 0) return len2;
            if (len1 == 1) return Helpers.SingleCharDistance(string1, string2, len2, start);

            return DistanceWithBuffer(string1, string2, len1, len2, start);
#endif
        }

        /// <summary>Compute and return the Levenshtein edit distance between two strings.</summary>
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
            int iMaxDistance = (maxDistance < int.MinValue) ? int.MinValue
                : (maxDistance > int.MaxValue) ? int.MaxValue
                : (int)Math.Floor(maxDistance);
            if (string1 == null || string2 == null) return Helpers.NullDistanceResults(string1, string2, iMaxDistance);
#if NET8_0_OR_GREATER
            return Distance(string1.AsSpan(), string2.AsSpan(), iMaxDistance);
#else
            if (iMaxDistance <= 0) return (string1 == string2) ? 0 : -1;

            // if strings of different lengths, ensure shorter string is in string1. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (string1.Length > string2.Length) (string1, string2) = (string2, string1);
            if (string2.Length - string1.Length > iMaxDistance) return -1;

            // identify common suffix and/or prefix that can be ignored
            Helpers.PrefixSuffixPrep(string1, string2, out int len1, out int len2, out int start);
            if (len1 == 0) return len2;
            if (iMaxDistance == 1) return (len2 == 1) ? 1 : -1;
            if (len1 == 1) {
                int dist = Helpers.SingleCharDistance(string1, string2, len2, start);
                return (dist <= iMaxDistance) ? dist : -1;
            }

            if (iMaxDistance < len2) {
                return DistanceWithBuffer(string1, string2, len1, len2, start, iMaxDistance);
            }
            return DistanceWithBuffer(string1, string2, len1, len2, start);
#endif
        }

        /// <summary>Return Levenshtein similarity between two strings
        /// (1 - (levenshtein distance / len of longer string)).</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is not threadsafe.</remarks>
        /// <param name="string1">One of the strings to compare.</param>
        /// <param name="string2">The other string to compare.</param>
        /// <returns>The degree of similarity 0 to 1.0, where 0 represents a lack of any
        /// notable similarity, and 1 represents equivalent strings.</returns>
        public double Similarity(string? string1, string? string2) {
            if (string1 == null) return (string2 == null) ? 1 : 0;
            if (string2 == null) return 0;
#if NET8_0_OR_GREATER
            return Similarity(string1.AsSpan(), string2.AsSpan());
#else
            // if strings of different lengths, ensure shorter string is in string1. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (string1.Length > string2.Length) (string1, string2) = (string2, string1);

            // identify common suffix and/or prefix that can be ignored
            Helpers.PrefixSuffixPrep(string1, string2, out int len1, out int len2, out int start);
            if (len1 == 0) return (len2 == 0) ? 1.0 : len2.ToSimilarity(string2.Length);
            if (len1 == 1) return Helpers.SingleCharDistance(string1, string2, len2, start).ToSimilarity(string2.Length);

            return DistanceWithBuffer(string1, string2, len1, len2, start)
                .ToSimilarity(string2.Length);
#endif
        }

        /// <summary>Return Levenshtein similarity between two strings
        /// (1 - (levenshtein distance / len of longer string)).</summary>
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
            if (double.IsNaN(minSimilarity) || minSimilarity < 0 || minSimilarity > 1) {
                throw new ArgumentOutOfRangeException(nameof(minSimilarity), minSimilarity, "minSimilarity must be in range 0 to 1.0");
            }
            if (string1 == null || string2 == null) return Helpers.NullSimilarityResults(string1, string2, minSimilarity);
#if NET8_0_OR_GREATER
            return Similarity(string1.AsSpan(), string2.AsSpan(), minSimilarity);
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
                return DistanceWithBuffer(string1, string2, len1, len2, start, iMaxDistance)
                    .ToSimilarity(string2.Length);
            }
            return DistanceWithBuffer(string1, string2, len1, len2, start)
                .ToSimilarity(string2.Length);
#endif
        }

#if NET8_0_OR_GREATER
        /// <summary>Compute and return the Levenshtein edit distance between two character spans.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is not threadsafe.</remarks>
        /// <param name="string1">One of the character spans to compare.</param>
        /// <param name="string2">The other character span to compare.</param>
        /// <returns>0 if the spans are equivalent, otherwise a positive number whose
        /// magnitude increases as difference between the spans increases.</returns>
        public double Distance(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2) {
            // Ensure shorter span is in string1 so the inner loop runs longer than the outer loop.
            if (string1.Length > string2.Length) {
                ReadOnlySpan<char> t = string1;
                string1 = string2;
                string2 = t;
            }

            // Strip common prefix/suffix; handle trivial 0-char and 1-char remainders without DP or buffers.
            Helpers.PrefixSuffixPrep(ref string1, ref string2);
            if (string1.Length == 0) return string2.Length;
            if (string1.Length == 1) return Helpers.SingleCharDistance(string1, string2);

            return DistanceWithBuffer(string1, string2);
        }

        /// <summary>Compute and return the Levenshtein edit distance between two character spans.</summary>
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
            int iMaxDistance = (maxDistance < int.MinValue) ? int.MinValue
                : (maxDistance > int.MaxValue) ? int.MaxValue
                : (int)Math.Floor(maxDistance);
            return Distance(string1, string2, iMaxDistance);
        }

        private int Distance(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2, int iMaxDistance) {
            if (iMaxDistance <= 0) return string1.SequenceEqual(string2) ? 0 : -1;

            if (string1.Length > string2.Length) {
                ReadOnlySpan<char> t = string1;
                string1 = string2;
                string2 = t;
            }
            if (string2.Length - string1.Length > iMaxDistance) return -1;

            Helpers.PrefixSuffixPrep(ref string1, ref string2);
            if (string1.Length == 0) return string2.Length;
            // After PrefixSuffixPrep both ends differ, so distance can only be 1 if both trimmed spans have length 1.
            if (iMaxDistance == 1) return (string2.Length == 1) ? 1 : -1;
            if (string1.Length == 1) {
                int dist = Helpers.SingleCharDistance(string1, string2);
                return (dist <= iMaxDistance) ? dist : -1;
            }

            if (iMaxDistance < string2.Length) {
                return DistanceWithBuffer(string1, string2, iMaxDistance);
            }
            return DistanceWithBuffer(string1, string2);
        }

        /// <summary>Return Levenshtein similarity between two character spans
        /// (1 - (levenshtein distance / len of longer span)).</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is not threadsafe.</remarks>
        /// <param name="string1">One of the character spans to compare.</param>
        /// <param name="string2">The other character span to compare.</param>
        /// <returns>The degree of similarity 0 to 1.0, where 0 represents a lack of any
        /// notable similarity, and 1 represents equivalent spans.</returns>
        public double Similarity(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2) {
            if (string1.Length > string2.Length) {
                ReadOnlySpan<char> t = string1;
                string1 = string2;
                string2 = t;
            }
            int maxLen = string2.Length;

            Helpers.PrefixSuffixPrep(ref string1, ref string2);
            if (string1.Length == 0) return (string2.Length == 0) ? 1.0 : string2.Length.ToSimilarity(maxLen);
            if (string1.Length == 1) return Helpers.SingleCharDistance(string1, string2).ToSimilarity(maxLen);

            return DistanceWithBuffer(string1, string2).ToSimilarity(maxLen);
        }

        /// <summary>Return Levenshtein similarity between two character spans
        /// (1 - (levenshtein distance / len of longer span)).</summary>
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
                return DistanceWithBuffer(string1, string2, iMaxDistance).ToSimilarity(maxLen);
            }
            return DistanceWithBuffer(string1, string2).ToSimilarity(maxLen);
        }

        // Reuses the instance buffer up to maxCachedLength; falls back to ArrayPool for oversized inputs so
        // one-off giant strings don't permanently pin large buffers on a long-lived instance.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int DistanceWithBuffer(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2) {
            int len2 = s2.Length;
            if (len2 <= this.baseChar1Costs.Length) {
                return Distance(s1, s2, this.baseChar1Costs);
            }
            if (len2 <= this.maxCachedLength) {
                int[] costs = this.baseChar1Costs = new int[len2];
                return Distance(s1, s2, costs);
            }
            return DistanceCore(s1, s2);
        }

        // Bounded counterpart to DistanceWithBuffer.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int DistanceWithBuffer(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, int maxDistance) {
            int len2 = s2.Length;
            if (len2 <= this.baseChar1Costs.Length) {
                return Distance(s1, s2, maxDistance, this.baseChar1Costs);
            }
            if (len2 <= this.maxCachedLength) {
                int[] costs = this.baseChar1Costs = new int[len2];
                return Distance(s1, s2, maxDistance, costs);
            }
            return DistanceCore(s1, s2, maxDistance);
        }

        // Threadsafe internal entry point using stackalloc for short spans and ArrayPool for long spans.
        // Expects s1.Length <= s2.Length and common prefix/suffix already removed.
        internal static int DistanceCore(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2) {
            // Avoid heap allocations: stackalloc up to 1 KB (256 ints), otherwise rent from ArrayPool.
            if (s2.Length <= StackAllocThreshold) {
                Span<int> char1Costs = stackalloc int[s2.Length];
                return Distance(s1, s2, char1Costs);
            }
            int[] rented = ArrayPool<int>.Shared.Rent(s2.Length);
            try {
                return Distance(s1, s2, rented);
            } finally {
                ArrayPool<int>.Shared.Return(rented);
            }
        }

        // Threadsafe internal bounded entry point using stackalloc for short spans and ArrayPool for long spans.
        // Expects s1.Length <= s2.Length, s2.Length >= maxDistance, s2.Length - s1.Length <= maxDistance, and common prefix/suffix already removed.
        internal static int DistanceCore(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, int maxDistance) {
            // Avoid heap allocations: stackalloc up to 1 KB (256 ints), otherwise rent from ArrayPool.
            if (s2.Length <= StackAllocThreshold) {
                Span<int> char1Costs = stackalloc int[s2.Length];
                return Distance(s1, s2, maxDistance, char1Costs);
            }
            int[] rented = ArrayPool<int>.Shared.Rent(s2.Length);
            try {
                return Distance(s1, s2, maxDistance, rented);
            } finally {
                ArrayPool<int>.Shared.Return(rented);
            }
        }

        // Core unbounded Levenshtein DP algorithm (expects s1.Length <= s2.Length and char1Costs.Length >= s2.Length).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Distance(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, Span<int> char1Costs) {
            int len1 = s1.Length;
            int len2 = s2.Length;
            // Slice buffer to exact len2 so its length matches s2.Length, helping RyuJIT elide bounds checks.
            char1Costs = char1Costs.Slice(0, len2);

            // Single 1D row buffer (O(len2) space) instead of a full 2D matrix; leftCharCost/aboveCharCost
            // carry the diagonal and previous-column values in registers.
            for (int j = 0; j < char1Costs.Length;) char1Costs[j] = ++j;
            int currentCharCost = 0;
            for (int i = 0; i < len1; ++i) {
                int leftCharCost = i;
                int aboveCharCost = i;
                char char1 = s1[i];
                for (int j = 0; j < s2.Length; ++j) {
                    currentCharCost = leftCharCost; // cost on diagonal (substitution)
                    leftCharCost = char1Costs[j];
                    if (s2[j] != char1) {
                        // substitution if neither of two conditions below
                        if (aboveCharCost < currentCharCost) currentCharCost = aboveCharCost; // deletion
                        if (leftCharCost < currentCharCost) currentCharCost = leftCharCost; // insertion
                        ++currentCharCost;
                    }
                    char1Costs[j] = aboveCharCost = currentCharCost;
                }
            }
            return currentCharCost;
        }

        // Core bounded Levenshtein DP algorithm (expects s1.Length <= s2.Length, s2.Length >= maxDistance,
        // s2.Length - s1.Length <= maxDistance, and char1Costs.Length >= s2.Length).
        private static int Distance(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, int maxDistance, Span<int> char1Costs) {
            int len1 = s1.Length;
            int len2 = s2.Length;
#if DEBUG
            if (len2 < maxDistance) throw new ArgumentException("len2 must be >= maxDistance", nameof(maxDistance));
            if (len2 - len1 > maxDistance) throw new ArgumentException("len2 - len1 must be <= maxDistance", nameof(maxDistance));
#endif
            // Slice buffer to exact len2 so bounds align with s2.
            char1Costs = char1Costs.Slice(0, len2);

            // Initialize only the first maxDistance cells upfront; remaining cells entering the band are
            // lazily initialized one per row below, keeping early-exit rejection O(maxDistance^2) instead of O(len2).
            for (int j = 0; j < maxDistance;) char1Costs[j] = ++j;
            int lenDiff = len2 - len1;
            int jStartOffset = maxDistance - lenDiff;
            int jStart = 0;
            int jEnd = maxDistance;
            int currentCost = 0;
            for (int i = 0; i < len1; ++i) {
                char char1 = s1[i];
                int prevChar1Cost = i;
                int aboveCharCost = i;
                // Only compute cells within the diagonal band [jStart, jEnd) that can still yield distance <= maxDistance.
                jStart += (i > jStartOffset) ? 1 : 0;
                if (jEnd < len2) char1Costs[jEnd++] = maxDistance + 1;
                for (int j = jStart; j < jEnd; ++j) {
                    currentCost = prevChar1Cost; // cost on diagonal (substitution)
                    prevChar1Cost = char1Costs[j];
                    if (s2[j] != char1) {
                        // substitution if neither of two conditions below
                        if (aboveCharCost < currentCost) currentCost = aboveCharCost; // deletion
                        if (prevChar1Cost < currentCost) currentCost = prevChar1Cost; // insertion
                        ++currentCost;
                    }
                    char1Costs[j] = aboveCharCost = currentCost;
                }
                // Values along any diagonal are monotonically non-decreasing; if the target diagonal
                // cell already exceeds maxDistance, the final distance must also exceed maxDistance.
                if (char1Costs[i + lenDiff] > maxDistance) return -1;
            }
            return currentCost;
        }
#else
        // Reuses the instance buffer up to maxCachedLength; uses a transient array for oversized inputs.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int DistanceWithBuffer(string string1, string string2, int len1, int len2, int start) {
            if (len2 <= this.baseChar1Costs.Length) {
                return Distance(string1, string2, len1, len2, start, this.baseChar1Costs);
            }
            int[] costs = new int[len2];
            if (len2 <= this.maxCachedLength) this.baseChar1Costs = costs;
            return Distance(string1, string2, len1, len2, start, costs);
        }

        // Bounded counterpart to DistanceWithBuffer.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int DistanceWithBuffer(string string1, string string2, int len1, int len2, int start, int maxDistance) {
            if (len2 <= this.baseChar1Costs.Length) {
                return Distance(string1, string2, len1, len2, start, maxDistance, this.baseChar1Costs);
            }
            int[] costs = new int[len2];
            if (len2 <= this.maxCachedLength) this.baseChar1Costs = costs;
            return Distance(string1, string2, len1, len2, start, maxDistance, costs);
        }

        // Threadsafe internal entry point using a capped per-thread cache on netstandard2.0.
        // Expects len1 <= len2 and common prefix/suffix already removed.
        internal static int DistanceCore(string string1, string string2, int len1, int len2, int start) {
            return Distance(string1, string2, len1, len2, start, GetThreadCosts(len2));
        }

        // Threadsafe internal bounded entry point using a capped per-thread cache on netstandard2.0.
        // Expects len1 <= len2, len2 >= maxDistance, len2 - len1 <= maxDistance, and common prefix/suffix already removed.
        internal static int DistanceCore(string string1, string string2, int len1, int len2, int start, int maxDistance) {
            return Distance(string1, string2, len1, len2, start, maxDistance, GetThreadCosts(len2));
        }

        // Returns a reusable per-thread scratch buffer up to ThreadCacheMaxLength, or a transient array if larger.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int[] GetThreadCosts(int len2) {
            int[]? costs = tlsChar1Costs;
            if (costs != null && len2 <= costs.Length) return costs;
            int[] fresh = new int[len2];
            if (len2 <= ThreadCacheMaxLength) tlsChar1Costs = fresh;
            return fresh;
        }

        // Core unbounded Levenshtein DP algorithm (expects len1 <= len2 and char1Costs.Length >= len2).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Distance(string string1, string string2, int len1, int len2, int start, int[] char1Costs) {
            for (int j = 0; j < len2;) char1Costs[j] = ++j;
            int currentCharCost = 0;
            // Separate start == 0 loop avoids adding start offset inside the inner loop when no prefix was stripped.
            if (start == 0) {
                for (int i = 0; i < len1; ++i) {
                    int leftCharCost, aboveCharCost;
                    leftCharCost = aboveCharCost = i;
                    char char1 = string1[i];
                    for (int j = 0; j < len2; ++j) {
                        currentCharCost = leftCharCost; // cost on diagonal (substitution)
                        leftCharCost = char1Costs[j];
                        if (string2[j] != char1) {
                            // substitution if neither of two conditions below
                            if (aboveCharCost < currentCharCost) currentCharCost = aboveCharCost; // deletion
                            if (leftCharCost < currentCharCost) currentCharCost = leftCharCost; // insertion
                            ++currentCharCost;
                        }
                        char1Costs[j] = aboveCharCost = currentCharCost;
                    }
                }
            } else {
                for (int i = 0; i < len1; ++i) {
                    int leftCharCost, aboveCharCost;
                    leftCharCost = aboveCharCost = i;
                    char char1 = string1[start + i];
                    for (int j = 0; j < len2; ++j) {
                        currentCharCost = leftCharCost; // cost on diagonal (substitution)
                        leftCharCost = char1Costs[j];
                        if (string2[start + j] != char1) {
                            // substitution if neither of two conditions below
                            if (aboveCharCost < currentCharCost) currentCharCost = aboveCharCost; // deletion
                            if (leftCharCost < currentCharCost) currentCharCost = leftCharCost; // insertion
                            ++currentCharCost;
                        }
                        char1Costs[j] = aboveCharCost = currentCharCost;
                    }
                }
            }
            return currentCharCost;
        }

        // Core bounded Levenshtein DP algorithm (expects len1 <= len2, len2 >= maxDistance,
        // len2 - len1 <= maxDistance, and char1Costs.Length >= len2).
        private static int Distance(string string1, string string2, int len1, int len2, int start, int maxDistance, int[] char1Costs) {
#if DEBUG
            if (len2 < maxDistance) throw new ArgumentException("len2 must be >= maxDistance", nameof(maxDistance));
            if (len2 - len1 > maxDistance) throw new ArgumentException("len2 - len1 must be <= maxDistance", nameof(maxDistance));
#endif
            int i, j;
            // Lazily initialize cells beyond maxDistance as they enter the band below.
            for (j = 0; j < maxDistance;) char1Costs[j] = ++j;
            int lenDiff = len2 - len1;
            int jStartOffset = maxDistance - lenDiff;
            int jStart = 0;
            int jEnd = maxDistance;
            int currentCost = 0;
            if (start == 0) {
                for (i = 0; i < len1; ++i) {
                    char char1 = string1[i];
                    int prevChar1Cost, aboveCharCost;
                    prevChar1Cost = aboveCharCost = i;
                    // no need to look beyond window of lower right diagonal - maxDistance cells (lower right diag is i + lenDiff)
                    // and the upper left diagonal + maxDistance cells (upper left is i)
                    jStart += (i > jStartOffset) ? 1 : 0;
                    if (jEnd < len2) char1Costs[jEnd++] = maxDistance + 1;
                    for (j = jStart; j < jEnd; ++j) {
                        currentCost = prevChar1Cost; // cost on diagonal (substitution)
                        prevChar1Cost = char1Costs[j];
                        if (string2[j] != char1) {
                            // substitution if neither of two conditions below
                            if (aboveCharCost < currentCost) currentCost = aboveCharCost; // deletion
                            if (prevChar1Cost < currentCost) currentCost = prevChar1Cost; // insertion
                            ++currentCost;
                        }
                        char1Costs[j] = aboveCharCost = currentCost;
                    }
                    // Diagonal values are monotonically non-decreasing, so exceeding maxDistance here guarantees failure.
                    if (char1Costs[i + lenDiff] > maxDistance) return -1;
                }
            } else {
                for (i = 0; i < len1; ++i) {
                    char char1 = string1[start + i];
                    int prevChar1Cost, aboveCharCost;
                    prevChar1Cost = aboveCharCost = i;
                    // no need to look beyond window of lower right diagonal - maxDistance cells (lower right diag is i + lenDiff)
                    // and the upper left diagonal + maxDistance cells (upper left is i)
                    jStart += (i > jStartOffset) ? 1 : 0;
                    if (jEnd < len2) char1Costs[jEnd++] = maxDistance + 1;
                    for (j = jStart; j < jEnd; ++j) {
                        currentCost = prevChar1Cost; // cost on diagonal (substitution)
                        prevChar1Cost = char1Costs[j];
                        if (string2[start + j] != char1) {
                            // substitution if neither of two conditions below
                            if (aboveCharCost < currentCost) currentCost = aboveCharCost; // deletion
                            if (prevChar1Cost < currentCost) currentCost = prevChar1Cost; // insertion
                            ++currentCost;
                        }
                        char1Costs[j] = aboveCharCost = currentCost;
                    }
                    // Diagonal values are monotonically non-decreasing, so exceeding maxDistance here guarantees failure.
                    if (char1Costs[i + lenDiff] > maxDistance) return -1;
                }
            }
            return currentCost;
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
