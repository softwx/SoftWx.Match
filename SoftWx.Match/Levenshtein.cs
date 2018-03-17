// Copyright ©2015-2018 SoftWx, Inc.
// Released under the MIT License the text of which appears at the end of this file.
// <authors> Steve Hatchett
using System;
using System.Runtime.CompilerServices;

namespace SoftWx.Match {
    /// <summary>
    /// Class providing optimized methods for computing Levenshtein comparisons between two strings.
    /// </summary>
    /// <remarks>
    /// Copyright ©2015-2018 SoftWx, Inc.
    /// The inspiration for creating highly optimized edit distance functions was 
    /// from Sten Hjelmqvist's "Fast, memory efficient" algorithm, described at
    /// http://www.codeproject.com/Articles/13525/Fast-memory-efficient-Levenshtein-algorithm
    /// The Levenshtein algorithm computes the edit distance metric between two strings, i.e.
    /// the number of insertion, deletion, and substitution edits required to transform one
    /// string to the other. This value will be >= 0, where 0 indicates identical strings.
    /// Comparisons are case sensitive, so for example, "Fred" and "fred" will have a 
    /// distance of 1. The optimized algorithm was described in detail in my post at
    /// http://blog.softwx.net/2014/12/optimizing-levenshtein-algorithm-in-c.html
    /// Also see http://en.wikipedia.org/wiki/Levenshtein_distance for general information.
    /// The methods in this class are not threadsafe. Use the static versions in the Distance
    /// class if that is required.</remarks>
    public class Levenshtein : IDistance {
        private int[] baseV0;

        /// <summary>Create a new instance of Levenshtein.</summary>
        public Levenshtein() {
            this.baseV0 = new int[0];
        }

        /// <summary>Create a new instance of Levenshtein using the specified expected
        /// maximum string length that will be encountered.</summary>
        /// <remarks>By specifying the max expected string length, better memory efficiency
        /// can be achieved.</remarks>
        /// <param name="expectedMaxStringLength">The expected maximum length of strings that will
        /// be passed to the Levenshtein methods.</param>
        public Levenshtein(int expectedMaxStringLength) {
            this.baseV0 = new int[expectedMaxStringLength];
        }

        /// <summary>Compute and return the Levenshtein edit distance between two strings.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is not threadsafe.</remarks>
        /// <param name="string1">One of the strings to compare.</param>
        /// <param name="string2">The other string to compare.</param>
        /// <returns>0 if the strings are equivalent, otherwise a positive number whose
        /// magnitude increases as difference between the strings increases.</returns>
        public double Distance(string string1, string string2) {
            if (string1 == null || string2 == null) return StringHelper.NullDistanceResults(string1, string2, int.MaxValue);

            // if strings of different lengths, ensure shorter string is in string1. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (string1.Length > string2.Length) {
                var temp = string1; string1 = string2; string2 = temp; // swap string1 and string2
            }

            // identify common suffix and/or prefix that can be ignored
            int len1, len2, start;
            StringHelper.PrefixSuffixPrep(string1, string2, out len1, out len2, out start);
            if (len1 == 0) return len2;

            return InternalLevenshtein(string1, string2, len1, len2, start,
                (this.baseV0 = (len2 <= this.baseV0.Length) ? this.baseV0 : new int[len2]));
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
        public double Distance(string string1, string string2, double maxDistance) {
            if (string1 == null || string2 == null) return StringHelper.NullDistanceResults(string1, string2, maxDistance);
            if (maxDistance <= 0) return (string1 == string2) ? 0 : -1;
            maxDistance = Math.Ceiling(maxDistance);
            int iMaxDistance = (maxDistance <= int.MaxValue) ? (int)maxDistance : int.MaxValue;

            // if strings of different lengths, ensure shorter string is in string1. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (string1.Length > string2.Length) {
                var temp = string1; string1 = string2; string2 = temp; // swap string1 and string2
            }
            if (string2.Length - string1.Length > iMaxDistance) return -1;

            // identify common suffix and/or prefix that can be ignored
            int len1, len2, start;
            StringHelper.PrefixSuffixPrep(string1, string2, out len1, out len2, out start);
            if (len1 == 0) return (len2 <= iMaxDistance) ? len2 : -1;

            if (iMaxDistance < len2) {
                return InternalLevenshtein(string1, string2, len1, len2, start, iMaxDistance,
                    (this.baseV0 = (len2 <= this.baseV0.Length) ? this.baseV0 : new int[len2]));
            }
            return InternalLevenshtein(string1, string2, len1, len2, start,
                (this.baseV0 = (len2 <= this.baseV0.Length) ? this.baseV0 : new int[len2]));
        }

        /// <summary>Internal implementation of the core Levenshtein algorithm.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int InternalLevenshtein(string string1, string string2, int len1, int len2, int start, int[] v0) {
            int j;
            for (j = 0; j < len2;) v0[j] = ++j;
            int current = 0;
            for (int i = 0; i < len1; i++) {
                char char1 = string1[i + start];
                int left, above;
                left = above = current = i;
                for (j = 0; j < len2; j++) {
                    current = left; // cost on diagonal (substitution)
                    left = v0[j];
                    if (string2[j + start] != char1) {
                        // substitution if neither of two conditions below
                        if (above < current) current = above; // deletion
                        if (left < current) current = left;   // insertion
                        current++;
                    }
                    v0[j] = above = current;
                }
            }
            return current;
        }

        /// <summary>Internal implementation of the core Levenshtein algorithm that accepts a maxDistance.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match</remarks>
        internal static int InternalLevenshtein(string string1, string string2, int len1, int len2, int start, int maxDistance, int[] v0) {
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
                int left, above;
                left = above = current = i;
                // no need to look beyond window of lower right diagonal - maxDistance cells (lower right diag is i - lenDiff)
                // and the upper left diagonal + maxDistance cells (upper left is i)
                jStart += (i > jStartOffset) ? 1 : 0;
                jEnd += (jEnd < len2) ? 1 : 0;
                for (j = jStart; j < jEnd; j++) {
                    current = left; // cost on diagonal (substitution)
                    left = v0[j];
                    if (char1 != string2[j + start]) {
                        // substitution if neither of two conditions below
                        if (above < current) current = above; // deletion
                        if (left < current) current = left;   // insertion
                        current++;
                    }
                    v0[j] = above = current;
                }
                if (v0[i + lenDiff] > maxDistance) return -1;
            }
            return (current <= maxDistance) ? current : -1;
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
