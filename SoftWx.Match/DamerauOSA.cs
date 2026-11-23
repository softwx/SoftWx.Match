// Copyright ©2015-2026 SoftWx, Inc.
// Released under the MIT License the text of which appears at the end of this file.
// <authors> Steve Hatchett

using System;
using System.Runtime.CompilerServices;
#if NET8_0_OR_GREATER
using System.Buffers;
#endif

namespace SoftWx.Match {
    /// <summary>
    /// Class providing optimized methods for computing Damerau-Levenshtein Optimal String
    /// Alignment (OSA) comparisons between two strings.
    /// </summary>
    /// <remarks>
    /// Copyright ©2015-2026 SoftWx, Inc.
    /// The inspiration for creating highly optimized edit distance functions was
    /// from Sten Hjelmqvist's "Fast, memory efficient" algorithm, described at
    /// https://www.codeproject.com/Articles/13525/Fast-memory-efficient-Levenshtein-algorithm
    /// The Damerau-Levenshtein algorithm is basically the Levenshtein algorithm with a
    /// modification that considers transposition of two adjacent characters as a single edit.
    /// The optimized algorithm was described in detail in my post at
    /// http://blog.softwx.net/2015/01/optimizing-damerau-levenshtein_15.html
    /// Also see https://en.wikipedia.org/wiki/Damerau%E2%80%93Levenshtein_distance
    /// Note that this implementation of Damerau-Levenshtein is the simpler and faster optimal
    /// string alignment (aka restricted edit) distance that differs slightly from the classic
    /// algorithm by imposing the restriction that no substring is edited more than once. So,
    /// for example, "CA" to "ABC" has an edit distance of 2 by a complete application of
    /// Damerau-Levenshtein, but has a distance of 3 by the method implemented here, that uses
    /// the optimal string alignment algorithm. This means that this algorithm is not a true
    /// metric since it does not uphold the triangle inequality. In real use though, this OSA
    /// version may be desired. Besides being faster, it does not give the lower distance score
    /// for transpositions that occur across long distances. Actual human error transpositions
    /// are most likely for adjacent characters. For example, the classic Damerau algorithm
    /// gives a distance of 1 for these two strings: "sated" and "dates" (it counts the 's' and
    /// 'd' as a single transposition). The optimal string alignment version of Damerau in this
    /// class gives a distance of 2 for these two strings (2 substitutions), as it only counts
    /// transpositions for adjacent characters.
    /// The methods in this class are not threadsafe. Use the static versions in the Distance
    /// and Similarity classes if that is required.</remarks>
    public class DamerauOSA : IDistance, ISimilarity {
#if NET8_0_OR_GREATER
        private const int StackAllocThreshold = 256;
#endif
        private int[] baseChar1Costs;
        private int[] basePrevChar1Costs;

        /// <summary>Create a new instance of DamerauOSA.</summary>
        public DamerauOSA() {
            this.baseChar1Costs = Array.Empty<int>();
            this.basePrevChar1Costs = Array.Empty<int>();
        }

        /// <summary>Create a new instance of DamerauOSA using the specified expected
        /// maximum string length that will be encountered.</summary>
        /// <remarks>By specifying the max expected string length, better memory efficiency
        /// can be achieved.</remarks>
        /// <param name="expectedMaxStringLength">The expected maximum length of strings that will
        /// be passed to the edit distance functions.</param>
        public DamerauOSA(int expectedMaxStringLength) {
            if (expectedMaxStringLength < 0) {
                throw new ArgumentOutOfRangeException(nameof(expectedMaxStringLength), expectedMaxStringLength, "expectedMaxStringLength must be non-negative.");
            }
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

            if (len2 > this.baseChar1Costs.Length) {
                this.baseChar1Costs = new int[len2];
                this.basePrevChar1Costs = new int[len2];
            }
            return Distance(string1, string2, len1, len2, start, this.baseChar1Costs, this.basePrevChar1Costs);
#endif
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
            if (len1 == 0) return (len2 <= iMaxDistance) ? len2 : -1;
            if (iMaxDistance == 1) {
                return (len2 == 1 || Helpers.IsSingleTransposition(string1, string2, len1, len2, start)) ? 1 : -1;
            }
            if (len1 == 1) {
                int dist = Helpers.SingleCharDistance(string1, string2, len2, start);
                return (dist <= iMaxDistance) ? dist : -1;
            }

            if (len2 > this.baseChar1Costs.Length) {
                this.baseChar1Costs = new int[len2];
                this.basePrevChar1Costs = new int[len2];
            }
            if (iMaxDistance < len2) {
                return Distance(string1, string2, len1, len2, start, iMaxDistance, this.baseChar1Costs, this.basePrevChar1Costs);
            }
            return Distance(string1, string2, len1, len2, start, this.baseChar1Costs, this.basePrevChar1Costs);
#endif
        }

        /// <summary>Return Damerau-Levenshtein optimal string alignment similarity
        /// between two strings (1 - (damerau distance / len of longer string)).</summary>
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

            if (len2 > this.baseChar1Costs.Length) {
                this.baseChar1Costs = new int[len2];
                this.basePrevChar1Costs = new int[len2];
            }
            return Distance(string1, string2, len1, len2, start, this.baseChar1Costs, this.basePrevChar1Costs)
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
            if (iMaxDistance <= 0) return (string1 == string2) ? 1 : -1;

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

            if (len2 > this.baseChar1Costs.Length) {
                this.baseChar1Costs = new int[len2];
                this.basePrevChar1Costs = new int[len2];
            }
            if (iMaxDistance < len2) {
                return Distance(string1, string2, len1, len2, start, iMaxDistance, this.baseChar1Costs, this.basePrevChar1Costs)
                    .ToSimilarity(string2.Length);
            }
            return Distance(string1, string2, len1, len2, start, this.baseChar1Costs, this.basePrevChar1Costs)
                .ToSimilarity(string2.Length);
#endif
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
        public double Distance(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2) {
            if (string1.Length > string2.Length) {
                ReadOnlySpan<char> t = string1;
                string1 = string2;
                string2 = t;
            }

            Helpers.PrefixSuffixPrep(ref string1, ref string2);
            if (string1.Length == 0) return string2.Length;
            if (string1.Length == 1) return Helpers.SingleCharDistance(string1, string2);

            int len2 = string2.Length;
            if (len2 > this.baseChar1Costs.Length) {
                this.baseChar1Costs = new int[len2];
                this.basePrevChar1Costs = new int[len2];
            }
            return Distance(string1, string2, this.baseChar1Costs, this.basePrevChar1Costs);
        }

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
            if (string1.Length == 0) return (string2.Length <= iMaxDistance) ? string2.Length : -1;
            if (iMaxDistance == 1) {
                return (string2.Length == 1 || Helpers.IsSingleTransposition(string1, string2)) ? 1 : -1;
            }
            if (string1.Length == 1) {
                int dist = Helpers.SingleCharDistance(string1, string2);
                return (dist <= iMaxDistance) ? dist : -1;
            }

            int len2 = string2.Length;
            if (len2 > this.baseChar1Costs.Length) {
                this.baseChar1Costs = new int[len2];
                this.basePrevChar1Costs = new int[len2];
            }
            if (iMaxDistance < len2) {
                return Distance(string1, string2, iMaxDistance, this.baseChar1Costs, this.basePrevChar1Costs);
            }
            return Distance(string1, string2, this.baseChar1Costs, this.basePrevChar1Costs);
        }

        /// <summary>Return Damerau-Levenshtein optimal string alignment similarity
        /// between two character spans (1 - (damerau distance / len of longer span)).</summary>
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

            int len2 = string2.Length;
            if (len2 > this.baseChar1Costs.Length) {
                this.baseChar1Costs = new int[len2];
                this.basePrevChar1Costs = new int[len2];
            }
            return Distance(string1, string2, this.baseChar1Costs, this.basePrevChar1Costs).ToSimilarity(maxLen);
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
            if (iMaxDistance <= 0) return string1.SequenceEqual(string2) ? 1 : -1;

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

            int len2 = string2.Length;
            if (len2 > this.baseChar1Costs.Length) {
                this.baseChar1Costs = new int[len2];
                this.basePrevChar1Costs = new int[len2];
            }
            if (iMaxDistance < len2) {
                return Distance(string1, string2, iMaxDistance, this.baseChar1Costs, this.basePrevChar1Costs).ToSimilarity(maxLen);
            }
            return Distance(string1, string2, this.baseChar1Costs, this.basePrevChar1Costs).ToSimilarity(maxLen);
        }

        /// <summary>Threadsafe internal entry point using stackalloc for short strings and ArrayPool for long strings.</summary>
        internal static int DistanceCore(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2) {
            if (s2.Length <= StackAllocThreshold) {
                Span<int> char1Costs = stackalloc int[s2.Length];
                Span<int> prevChar1Costs = stackalloc int[s2.Length];
                return Distance(s1, s2, char1Costs, prevChar1Costs);
            }
            int[] rented = ArrayPool<int>.Shared.Rent(s2.Length * 2);
            try {
                return Distance(s1, s2, rented.AsSpan(0, s2.Length), rented.AsSpan(s2.Length, s2.Length));
            } finally {
                ArrayPool<int>.Shared.Return(rented);
            }
        }

        /// <summary>Threadsafe internal bounded entry point using stackalloc for short strings and ArrayPool for long strings.</summary>
        internal static int DistanceCore(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, int maxDistance) {
            if (s2.Length <= StackAllocThreshold) {
                Span<int> char1Costs = stackalloc int[s2.Length];
                Span<int> prevChar1Costs = stackalloc int[s2.Length];
                return Distance(s1, s2, maxDistance, char1Costs, prevChar1Costs);
            }
            int[] rented = ArrayPool<int>.Shared.Rent(s2.Length * 2);
            try {
                return Distance(s1, s2, maxDistance, rented.AsSpan(0, s2.Length), rented.AsSpan(s2.Length, s2.Length));
            } finally {
                ArrayPool<int>.Shared.Return(rented);
            }
        }

        /// <summary>Internal implementation of the core Damerau-Levenshtein, optimal string alignment algorithm.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int Distance(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, Span<int> char1Costs, Span<int> prevChar1Costs) {
            int len1 = s1.Length;
            int len2 = s2.Length;
            char1Costs = char1Costs.Slice(0, len2);
            prevChar1Costs = prevChar1Costs.Slice(0, len2);

            for (int j = 0; j < char1Costs.Length;) char1Costs[j] = ++j;
            char char1 = ' ';
            int currentCost = 0;
            for (int i = 0; i < len1; ++i) {
                char prevChar1 = char1;
                char1 = s1[i];
                char char2 = ' ';
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

        /// <summary>Internal implementation of the core Damerau-Levenshtein, optimal string alignment algorithm
        /// that accepts a maxDistance.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match</remarks>
        internal static int Distance(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, int maxDistance, Span<int> char1Costs, Span<int> prevChar1Costs) {
            int len1 = s1.Length;
            int len2 = s2.Length;
#if DEBUG
            if (len2 < maxDistance) throw new ArgumentException("len2 must be >= maxDistance", nameof(maxDistance));
            if (len2 - len1 > maxDistance) throw new ArgumentException("len2 - len1 must be <= maxDistance", nameof(maxDistance));
#endif
            char1Costs = char1Costs.Slice(0, len2);
            prevChar1Costs = prevChar1Costs.Slice(0, len2);

            for (int j = 0; j < maxDistance;) char1Costs[j] = ++j;
            int lenDiff = len2 - len1;
            int jStartOffset = maxDistance - lenDiff;
            int jStart = 0;
            int jEnd = maxDistance;
            char char1 = ' ';
            int currentCost = 0;
            for (int i = 0; i < len1; ++i) {
                char prevChar1 = char1;
                char1 = s1[i];
                char char2 = ' ';
                int leftCharCost = i;
                int aboveCharCost = i;
                int nextTransCost = 0;
                // no need to look beyond window of lower right diagonal - maxDistance cells (lower right diag is i - lenDiff)
                // and the upper left diagonal + maxDistance cells (upper left is i)
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
                if (char1Costs[i + lenDiff] > maxDistance) return -1;
            }
            return (currentCost <= maxDistance) ? currentCost : -1;
        }
#else
        /// <summary>Internal implementation of the core Damerau-Levenshtein, optimal string alignment algorithm.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int Distance(string string1, string string2, int len1, int len2, int start, int[] char1Costs, int[] prevChar1Costs) {
            int j;
            for (j = 0; j < len2;) char1Costs[j] = ++j;
            char char1 = ' ';
            int currentCost = 0;
            for (int i = 0; i < len1; ++i) {
                char prevChar1 = char1;
                char1 = string1[start + i];
                char char2 = ' ';
                int leftCharCost, aboveCharCost;
                leftCharCost = aboveCharCost = i;
                int nextTransCost = 0;
                for (j = 0; j < len2; ++j) {
                    int thisTransCost = nextTransCost;
                    nextTransCost = prevChar1Costs[j];
                    prevChar1Costs[j] = currentCost = leftCharCost; // cost of diagonal (substitution)
                    leftCharCost = char1Costs[j];    // left now equals current cost (which will be diagonal at next iteration)
                    char prevChar2 = char2;
                    char2 = string2[start + j];
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

        /// <summary>Internal implementation of the core Damerau-Levenshtein, optimal string alignment algorithm
        /// that accepts a maxDistance.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match</remarks>
        internal static int Distance(string string1, string string2, int len1, int len2, int start, int maxDistance, int[] char1Costs, int[] prevChar1Costs) {
#if DEBUG
            if (len2 < maxDistance) throw new ArgumentException("len2 must be >= maxDistance", nameof(maxDistance));
            if (len2 - len1 > maxDistance) throw new ArgumentException("len2 - len1 must be <= maxDistance", nameof(maxDistance));
#endif
            int i, j;
            for (j = 0; j < maxDistance;) char1Costs[j] = ++j;
            int lenDiff = len2 - len1;
            int jStartOffset = maxDistance - lenDiff;
            int jStart = 0;
            int jEnd = maxDistance;
            char char1 = ' ';
            int currentCost = 0;
            for (i = 0; i < len1; ++i) {
                char prevChar1 = char1;
                char1 = string1[start + i];
                char char2 = ' ';
                int leftCharCost, aboveCharCost;
                leftCharCost = aboveCharCost = i;
                int nextTransCost = 0;
                // no need to look beyond window of lower right diagonal - maxDistance cells (lower right diag is i - lenDiff)
                // and the upper left diagonal + maxDistance cells (upper left is i)
                jStart += (i > jStartOffset) ? 1 : 0;
                if (jEnd < len2) char1Costs[jEnd++] = maxDistance + 1;
                for (j = jStart; j < jEnd; ++j) {
                    int thisTransCost = nextTransCost;
                    nextTransCost = prevChar1Costs[j];
                    prevChar1Costs[j] = currentCost = leftCharCost; // cost on diagonal (substitution)
                    leftCharCost = char1Costs[j];     // left now equals current cost (which will be diagonal at next iteration)
                    char prevChar2 = char2;
                    char2 = string2[start + j];
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
                if (char1Costs[i + lenDiff] > maxDistance) return -1;
            }
            return (currentCost <= maxDistance) ? currentCost : -1;
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
