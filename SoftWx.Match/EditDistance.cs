// Copyright ©2015-2018 SoftWx, Inc.
// Released under the MIT License the text of which appears at the end of this file.
// <authors> Steve Hatchett
using System.Runtime.CompilerServices;

namespace SoftWx.Match {
    /// <summary>
    /// Class providing optimized methods for computing the edit distance between two strings.
    /// </summary>
    /// <remarks>
    /// Copyright ©2015-2018 SoftWx, Inc.
    /// This class currently supports the Levenshtein and Damerau-Levenshtein edit distance
    /// measures. The inspiration for creating highly optimized edit distance functions was 
    /// from Sten Hjelmqvist's "Fast, memory efficient" algorithm, described at
    /// http://www.codeproject.com/Articles/13525/Fast-memory-efficient-Levenshtein-algorithm
    /// The Levenshtein algorithm computes the edit distance between two strings, i.e. the
    /// number of insertion, deletion, and substitution edits required to transform one
    /// string to the other. This value will be >= 0, where 0 indicates identical strings.
    /// Comparisons are case sensitive, so for example, "Fred" and "fred" will have a 
    /// distance of 1. The optimized algorithm is described in my post at
    /// http://blog.softwx.net/2014/12/optimizing-levenshtein-algorithm-in-c.html
    /// Also see http://en.wikipedia.org/wiki/Levenshtein_distance for general information.
    /// The Damerau-Levenshtein algorithm is basically the Levenshtein algorithm with a 
    /// modification that considers transposition of two adjacent characters as a single edit.
    /// The optimized algorithm is described in my post at 
    /// http://blog.softwx.net/2015/01/optimizing-damerau-levenshtein_15.html
    /// Also see http://en.wikipedia.org/wiki/Damerau%E2%80%93Levenshtein_distance
    /// Note that this implementation of Damerau-Levenshtein is the simpler and faster optimal
    /// string alignment (aka restricted edit) distance that difers slightly from the classic
    /// algorithm by imposing the restriction that no substring is edited more than once. So,
    /// for example, "CA" to "ABC" has an edit distance of 2 by a complete application of
    /// Damerau-Levenshtein, but has a distance of 3 by the method implemented here, that uses
    /// the optimal string alignment algorithm. See wikipedia article for more detail on this
    /// distinction.
    /// </remarks>
    public class EditDistance {
        private int[] baseV0;
        private int[] baseV2;

        /// <summary>Create a new instance of EditDistance.</summary>
        public EditDistance() {
            this.baseV0 = new int[0];
            this.baseV2 = new int[0];
        }

        /// <summary>Create a new instance of EditDistance using the specified expected
        /// maximum string length that will be encountered.</summary>
        /// <remarks>By specifying the max expected string length, better memory efficiency
        /// can be achieved.</remarks>
        /// <param name="expectedMaxStringLength">The expected maximum length of strings that will
        /// be passed to the edit distance functions.</param>
        public EditDistance(int expectedMaxStringLength) {
            this.baseV0 = new int[expectedMaxStringLength];
            this.baseV2 = new int[expectedMaxStringLength];
        }

        /// <summary>Compute and return the Levenshtein edit distance between two strings.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is threadsafe.</remarks>
        /// <param name="string1">The string being compared for edit distance.</param>
        /// <param name="string2">The other string being compared for edit distance.</param>
        /// <returns>The Levenshtein edit distance (>=0) between the two strings.</returns>
        public static int StaticLevenshtein(string string1, string string2) {
            if (string1 == null || string2 == null) return NullResults(string1, string2, int.MaxValue);

            // if strings of different lengths, ensure shorter string is in string1. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (string1.Length > string2.Length) {
                var temp = string1; string1 = string2; string2 = temp; // swap string1 and string2
            }

            int len1, len2, start;
            // identify common suffix and/or prefix that can be ignored
            PrefixSuffixPrep(string1, string2, out len1, out len2, out start);
            if (len1 == 0) return len2;

            return InternalLevenshtein(string1, string2, len1, len2, start, new int[len2]);
        }

        /// <summary>Compute and return the Levenshtein edit distance between two strings.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is threadsafe.</remarks>
        /// <param name="string1">The string being compared for edit distance.</param>
        /// <param name="string2">The other string being compared for edit distance.</param>
        /// <param name="maxDistance">The maximum edit distance of interest.</param>
        /// <returns>The Levenshtein edit distance (>=0) between the two strings, or -1 if
        /// the maxDistance is exceeded.</returns>
        public static int StaticLevenshtein(string string1, string string2, int maxDistance) {
            if (string1 == null || string2 == null) return NullResults(string1, string2, maxDistance);
            if (maxDistance <= 0) return (string1 == string2) ? 0 : -1;

            // if strings of different lengths, ensure shorter string is in string1. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (string1.Length > string2.Length) {
                var temp = string1; string1 = string2; string2 = temp; // swap string1 and string2
            }
            if (string2.Length - string1.Length > maxDistance) return -1;

            // identify common suffix and/or prefix that can be ignored
            int len1, len2, start;
            PrefixSuffixPrep(string1, string2, out len1, out len2, out start);
            if (len1 == 0) return (len2 <= maxDistance) ? len2 : -1;

            if (maxDistance < len2) {
                return InternalLevenshtein(string1, string2, len1, len2, start, maxDistance, new int[len2]);
            }
            return InternalLevenshtein(string1, string2, len1, len2, start, new int[len2]);
        }


        /// <summary>Compute and return the Damerau-Levenshtein edit distance between two strings.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is threadsafe.</remarks>
        /// <param name="string1">The string being compared for edit distance.</param>
        /// <param name="string2">The other string being compared for edit distance.</param>
        /// <returns>The Damerau-Levenshtein edit distance (>=0) between the two strings.</returns>
        public static int StaticDamLev(string string1, string string2) {
            if (string1 == null || string2 == null) return NullResults(string1, string2, int.MaxValue);

            // if strings of different lengths, ensure shorter string is in string1. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (string1.Length > string2.Length) {
                var temp = string1; string1 = string2; string2 = temp; // swap string1 and string2
            }

            int len1, len2, start;
            // identify common suffix and/or prefix that can be ignored
            PrefixSuffixPrep(string1, string2, out len1, out len2, out start);
            if (len1 == 0) return len2;

            return InternalDamLevOpt(string1, string2, len1, len2, start, new int[len2], new int[len2]);
        }

        /// <summary>Compute and return the Damerau-Levenshtein edit distance between two strings.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is threadsafe.</remarks>
        /// <param name="string1">The string being compared for edit distance.</param>
        /// <param name="string2">The other string being compared for edit distance.</param>
        /// <param name="maxDistance">The maximum edit distance of interest.</param>
        /// <returns>The Damerau-Levenshtein edit distance (>=0) between the two strings, or -1 if
        /// the maxDistance is exceeded.</returns>
        public static int StaticDamLev(string string1, string string2, int maxDistance) {
            if (string1 == null || string2 == null) return NullResults(string1, string2, maxDistance);
            if (maxDistance <= 0) return (string1 == string2) ? 0 : -1;

            // if strings of different lengths, ensure shorter string is in string1. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (string1.Length > string2.Length) {
                var temp = string1; string1 = string2; string2 = temp; // swap string1 and string2
            }
            if (string2.Length - string1.Length > maxDistance) return -1;

            // identify common suffix and/or prefix that can be ignored
            int len1, len2, start;
            PrefixSuffixPrep(string1, string2, out len1, out len2, out start);
            if (len1 == 0) return (len2 <= maxDistance) ? len2 : -1;

            if (maxDistance < len2) {
                return InternalDamLev(string1, string2, len1, len2, start, maxDistance, new int[len2], new int[len2]);
            }
            return InternalDamLevOpt(string1, string2, len1, len2, start, new int[len2], new int[len2]);
        }

        /// <summary>Compute and return the Levenshtein edit distance between two strings.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is not threadsafe.</remarks>
        /// <param name="string1">The string being compared for edit distance.</param>
        /// <param name="string2">The other string being compared for edit distance.</param>
        /// <returns>The Levenshtein edit distance (>=0) between the two strings.</returns>
        public int Levenshtein(string string1, string string2) {
            if (string1 == null || string2 == null) return NullResults(string1, string2, int.MaxValue);

            // if strings of different lengths, ensure shorter string is in string1. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (string1.Length > string2.Length) {
                var temp = string1; string1 = string2; string2 = temp; // swap string1 and string2
            }

            // identify common suffix and/or prefix that can be ignored
            int len1, len2, start;
            PrefixSuffixPrep(string1, string2, out len1, out len2, out start);
            if (len1 == 0) return len2;

            if (len2 > this.baseV0.Length) this.baseV0 = new int[len2];

            return InternalLevenshtein(string1, string2, len1, len2, start, this.baseV0);
        }

        /// <summary>Compute and return the Levenshtein edit distance between two strings.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is not threadsafe.</remarks>
        /// <param name="string1">The string being compared for edit distance.</param>
        /// <param name="string2">The other string being compared for edit distance.</param>
        /// <param name="maxDistance">The maximum edit distance of interest.</param>
        /// <returns>The Levenshtein edit distance (>=0) between the two strings, or -1 if
        /// the maxDistance is exceeded.</returns>
        public int Levenshtein(string string1, string string2, int maxDistance) {
            if (string1 == null || string2 == null) return NullResults(string1, string2, maxDistance);
            if (maxDistance <= 0) return (string1 == string2) ? 0 : -1;

            // if strings of different lengths, ensure shorter string is in string1. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (string1.Length > string2.Length) {
                var temp = string1; string1 = string2; string2 = temp; // swap string1 and string2
            }
            if (string2.Length - string1.Length > maxDistance) return -1;

            // identify common suffix and/or prefix that can be ignored
            int len1, len2, start;
            PrefixSuffixPrep(string1, string2, out len1, out len2, out start);
            if (len1 == 0) return (len2 <= maxDistance) ? len2 : -1;

            if (len2 > this.baseV0.Length) this.baseV0 = new int[len2];
            if (maxDistance < len2) {
                return InternalLevenshtein(string1, string2, len1, len2, start, maxDistance, this.baseV0);
            }
            return InternalLevenshtein(string1, string2, len1, len2, start, this.baseV0);
        }

        /// <summary>Compute and return the Damerau-Levenshtein edit distance between two strings.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is not threadsafe.</remarks>
        /// <param name="string1">The string being compared for edit distance.</param>
        /// <param name="string2">The other string being compared for edit distance.</param>
        /// <returns>The Damerau-Levenshtein edit distance (>=0) between the two strings.</returns>
        public int DamLev(string string1, string string2) {
            if (string1 == null || string2 == null) return NullResults(string1, string2, int.MaxValue);

            // if strings of different lengths, ensure shorter string is in string1. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (string1.Length > string2.Length) {
                var temp = string1; string1 = string2; string2 = temp; // swap string1 and string2
            }

            // identify common suffix and/or prefix that can be ignored
            int len1, len2, start;
            PrefixSuffixPrep(string1, string2, out len1, out len2, out start);
            if (len1 == 0) return len2;

            if (len2 > this.baseV0.Length) {
                this.baseV0 = new int[len2];
                this.baseV2 = new int[len2];
            }
            return InternalDamLevOpt(string1, string2, len1, len2, start, this.baseV0, this.baseV2);
        }

        /// <summary>Compute and return the Damerau-Levenshtein edit distance between two strings.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is not threadsafe.</remarks>
        /// <param name="string1">The string being compared for edit distance.</param>
        /// <param name="string2">The other string being compared for edit distance.</param>
        /// <param name="maxDistance">The maximum edit distance of interest.</param>
        /// <returns>The Damerau-Levenshtein edit distance (>=0) between the two strings, or -1 if
        /// the maxDistance is exceeded.</returns>
        public int DamLev(string string1, string string2, int maxDistance) {
            if (string1 == null || string2 == null) return NullResults(string1, string2, maxDistance);
            if (maxDistance <= 0) return (string1 == string2) ? 0 : -1;

            // if strings of different lengths, ensure shorter string is in string1. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (string1.Length > string2.Length) {
                var temp = string1; string1 = string2; string2 = temp; // swap string1 and string2
            }
            if (string2.Length - string1.Length > maxDistance) return -1;

            // identify common suffix and/or prefix that can be ignored
            int len1, len2, start;
            PrefixSuffixPrep(string1, string2, out len1, out len2, out start);
            if (len1 == 0) return (len2 <= maxDistance) ? len2 : -1;

            if (len2 > this.baseV0.Length) {
                this.baseV0 = new int[len2];
                this.baseV2 = new int[len2];
            }
            if (maxDistance < len2) {
                return InternalDamLev(string1, string2, len1, len2, start, maxDistance, this.baseV0, this.baseV2);
            }
            return InternalDamLevOpt(string1, string2, len1, len2, start, this.baseV0, this.baseV2);
        }

        /// <summary>Internal implementation of the core Levenshtein algorithm.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int InternalLevenshtein(string string1, string string2, int len1, int len2, int start, int[] v0) {
            int j;
            for (j = 0; j < len2;) v0[j] = ++j;
            int current = 0;
            for (int i = 0; i < len1; i++) {
                char char1 = string1[i+start];
                int left = current = i;
                for (j = 0; j < len2; j++) {
                    int above = current;
                    current = left; // cost on diagonal (substitution)
                    left = v0[j];
                    if (string2[j+start] != char1) {
                        // substitution if neither of two conditions below
                        if (above < current) current = above; // deletion
                        if (left < current) current = left;   // insertion
                        current++;
                    }
                    v0[j] = current;
                }
            }
            return current;
        }

        /// <summary>Internal implementation of the core Levenshtein algorithm that accepts a maxDistance.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match</remarks>
        private static int InternalLevenshtein(string string1, string string2, int len1, int len2, int start, int maxDistance, int[] v0) {
#if DEBUG
            if (len2 > maxDistance) throw new ArgumentException();
#endif
            int i, j;
            for (j = 0; j < maxDistance;) v0[j] = ++j;
            for (; j < len2;) v0[j++] = maxDistance + 1;
            int lenDiff = len2 - len1;
            int jStartOffset = maxDistance - lenDiff;
            int jStart = 0;
            int jEnd = maxDistance;
            int current = 0;
            for (i = 0; i < len1; i++) {
                char char1 = string1[start + i];
                int left = current = i;
                // no need to look beyond window of lower right diagonal - maxDistance cells (lower right diag is i - lenDiff)
                // and the upper left diagonal + maxDistance cells (upper left is i)
                jStart += (i > jStartOffset) ? 1 : 0;
                jEnd += (jEnd < len2) ? 1 : 0;
                for (j = jStart; j < jEnd; j++) {
                    int above = current;
                    current = left; // cost on diagonal (substitution)
                    left = v0[j];
                    if (char1 != string2[j+start]) {
                        // substitution if neither of two conditions below
                        if (above < current) current = above; // deletion
                        if (left < current) current = left;   // insertion
                        current++;
                    }
                    v0[j] = current;
                }
                if (v0[i + lenDiff] > maxDistance) return -1;
            }
            return (current <= maxDistance) ? current : -1;
        }

        /// <summary>Internal implementation of the core Damerau-Levenshtein, optimal string alignment algorithm.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int InternalDamLevOpt(string string1, string string2, int len1, int len2, int start, int[] v0, int[] v2) {
            int j;
            for (j = 0; j < len2;) v0[j] = ++j;
            char char1 = string1[0];
            int current = 0;
            for (int i = 0; i < len1; i++) {
                char prevChar1 = char1;
                char1 = string1[i + start];
                char char2 = string2[0];
                int left = current = i;
                int nextTransCost = 0;
                for (j = 0; j < len2; j++) {
                    int above = current;
                    int thisTransCost = nextTransCost;
                    nextTransCost = v2[j];
                    v2[j] = current = left; // cost of diagonal (substitution)
                    left = v0[j];    // left now equals current cost (which will be diagonal at next iteration)
                    char prevChar2 = char2;
                    char2 = string2[j + start];
                    if (char1 != char2) {
                        //substitution if neither of two conditions below
                        if (above < current) current = above; // deletion
                        if (left < current) current = left;   // insertion
                        current++;
                        if ((i != 0) && (j != 0)
                            && (char1 == prevChar2)
                            && (prevChar1 == char2)) {
                            if (++thisTransCost < current) current = thisTransCost; // transposition
                        }
                    }
                    v0[j] = current;
                }
            }
            return current;
        }

        /// <summary>Internal implementation of the core Damerau-Levenshtein, optimal string alignment algorithm
        /// that accepts a maxDistance.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match</remarks>
        private static int InternalDamLev(string string1, string string2, int len1, int len2, int start, int maxDistance, int[] v0, int[] v2) {
#if DEBUG
            if (len2 > maxDistance) throw new ArgumentException();
#endif
            int i, j;
            for (j = 0; j < maxDistance;) v0[j] = ++j;
            for (; j < len2;) v0[j++] = maxDistance + 1;
            int lenDiff = len2 - len1;
            int jStartOffset = maxDistance - lenDiff;
            int jStart = 0;
            int jEnd = maxDistance;
            char char1 = string1[0];
            int current = 0;
            for (i = 0; i < len1; i++) {
                char prevChar1 = char1;
                char1 = string1[start + i];
                char char2 = string2[0];
                int left = current = i;
                int nextTransCost = 0;
                // no need to look beyond window of lower right diagonal - maxDistance cells (lower right diag is i - lenDiff)
                // and the upper left diagonal + maxDistance cells (upper left is i)
                jStart += (i > jStartOffset) ? 1 : 0;
                jEnd += (jEnd < len2) ? 1 : 0;
                for (j = jStart; j < jEnd; j++) {
                    int above = current;
                    int thisTransCost = nextTransCost;
                    nextTransCost = v2[j];
                    v2[j] = current = left; // cost on diagonal (substitution)
                    left = v0[j];     // left now equals current cost (which will be diagonal at next iteration)
                    char prevChar2 = char2;
                    char2 = string2[j + start];
                    if (char1 != char2) {
                        // substitution if neither of two conditions below
                        if (above < current) current = above; // deletion
                        if (left < current) current = left;   // insertion
                        current++;
                        if ((i != 0) && (j != 0)
                            && (char1 == prevChar2)
                            && (prevChar1 == char2)) {
                            if (++thisTransCost < current) current = thisTransCost; // transposition
                        }
                    }
                    v0[j] = current;
                }
                if (v0[i + lenDiff] > maxDistance) return -1;
            }
            return (current <= maxDistance) ? current : -1;
        }

        /// <summary>Determines the proper return value of an edit distance function when one or
        /// both strings are null.</summary>
        private static int NullResults(string string1, string string2, int maxDistance) {
            if (string1 == null) return (string2 == null) ? 0 : (string2.Length <= maxDistance) ? string2.Length : -1;
            return (string1.Length <= maxDistance) ? string1.Length : -1;
        }

        /// <summary>Calculates starting position and lengths of two strings such that common
        /// prefix and suffix substrings are excluded.</summary>
        /// <remarks>Expects string1.Length to be less than or equal to string2.Length</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PrefixSuffixPrep(string string1, string string2, out int len1, out int len2, out int start) {
            len2 = string2.Length;
            len1 = string1.Length; // this is also the minimun length of the two strings
            // suffix common to both strings can be ignored
            while (len1 != 0 && string1[len1 - 1] == string2[len2 - 1]) {
                len2--; len1--;
            }
            // prefix common to both strings can be ignored
            start = 0;
            while (start != len1 && string1[start] == string2[start]) start++;
            len2 -= start; // length of the part excluding common prefix and suffix
            len1 -= start;
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
