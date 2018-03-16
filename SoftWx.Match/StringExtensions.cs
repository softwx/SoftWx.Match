// Copyright ©2015-2018 SoftWx, Inc.
// Released under the MIT License the text of which appears at the end of this file.
// <authors> Steve Hatchett

namespace SoftWx.Match {
    /// <summary>String extension methods exposing the Static methods of the SoftWx.EditDistance class.</summary>
    /// <remarks>See the SoftWx.EditDistance class for more details. This class is not really neccessary,
    /// but was included to retain some of the interface exposed in earlier versions.
    /// https://github.com/softwx/SoftWx.Match
    /// These extension methods are threadsafe.
    /// </remarks>
    public static class StringExtensions {
        /// <summary>Compute and return the Levenshtein edit distance between two strings.</summary>
        /// <param name="string1">The string being compared for edit distance.</param>
        /// <param name="string2">The other string being compared for edit distance.</param>
        /// <returns>The Levenshtein edit distance (>=0) between the two strings.</returns>
        public static int Levenshtein(this string string1, string string2) {
            return EditDistance.StaticLevenshtein(string1, string2);
        }
        /// <summary>Compute and return the Levenshtein edit distance between two strings.</summary>
        /// <param name="string1">The string being compared for edit distance.</param>
        /// <param name="string2">The other string being compared for edit distance.</param>
        /// <param name="maxDistance">The maximum edit distance of interest.</param>
        /// <returns>The Levenshtein edit distance (>=0) between the two strings, or -1 if
        /// the maxDistance is exceeded.</returns>
        public static int Levenshtein(this string string1, string string2, int maxDistance) {
            return EditDistance.StaticLevenshtein(string1, string2, maxDistance);
        }
        /// <summary>Compute and return the Damerau-Levenshtein edit distance between two strings.</summary>
        /// <param name="string1">The string being compared for edit distance.</param>
        /// <param name="string2">The other string being compared for edit distance.</param>
        /// <returns>The Damerau-Levenshtein edit distance (>=0) between the two strings.</returns>
        public static int DamLev(this string string1, string string2) {
            return EditDistance.StaticDamLev(string1, string2);
        }
        /// <summary>Compute and return the Damerau-Levenshtein edit distance between two strings.</summary>
        /// <param name="string1">The string being compared for edit distance.</param>
        /// <param name="string2">The other string being compared for edit distance.</param>
        /// <param name="maxDistance">The maximum edit distance of interest.</param>
        /// <returns>The Damerau-Levenshtein edit distance (>=0) between the two strings, or -1 if
        /// the maxDistance is exceeded.</returns>
        public static int DamLev(this string string1, string string2, int maxDistance) {
            return EditDistance.StaticDamLev(string1, string2, maxDistance);
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
