// Copyright © 2015-2026 Steve Hatchett
// SPDX-License-Identifier: MIT
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
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is threadsafe.</remarks>
        /// <param name="string1">One of the strings to compare.</param>
        /// <param name="string2">The other string to compare.</param>
        /// <returns>The degree of similarity 0 to 1.0, where 0 represents a lack of any
        /// notable similarity, and 1 represents equivalent strings.</returns>
        public static double Levenshtein(string? string1, string? string2) {
            if (string1 == null) return (string2 == null) ? 1 : 0;
            if (string2 == null) return 0;
            return Match.Levenshtein.DistanceStatic(string1.AsSpan(), string2.AsSpan())
                .ToSimilarity(Math.Max(string1.Length, string2.Length));
        }

        /// <summary>Return Levenshtein similarity between two strings (1 - (levenshtein distance / len of longer string)).</summary>
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
        public static double Levenshtein(string? string1, string? string2, double minSimilarity) {
            Helpers.ValidateMinSimilarity(minSimilarity);
            if (string1 == null || string2 == null) return Helpers.NullSimilarityResults(string1, string2, minSimilarity);
            int maxLen = Math.Max(string1.Length, string2.Length);
            return Match.Levenshtein.DistanceStatic(string1.AsSpan(), string2.AsSpan(), minSimilarity.ToDistance(maxLen))
                .ToSimilarity(maxLen);
        }

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
        /// <summary>Return Levenshtein similarity between two character spans (1 - (levenshtein distance / len of longer span)).</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is threadsafe.</remarks>
        /// <param name="string1">One of the character spans to compare.</param>
        /// <param name="string2">The other character span to compare.</param>
        /// <returns>The degree of similarity 0 to 1.0, where 0 represents a lack of any
        /// notable similarity, and 1 represents equivalent spans.</returns>
        public static double Levenshtein(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2) =>
            Match.Levenshtein.DistanceStatic(string1, string2)
                .ToSimilarity(Math.Max(string1.Length, string2.Length));

        /// <summary>Return Levenshtein similarity between two character spans (1 - (levenshtein distance / len of longer span)).</summary>
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
        public static double Levenshtein(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2, double minSimilarity) {
            Helpers.ValidateMinSimilarity(minSimilarity);
            int maxLen = Math.Max(string1.Length, string2.Length);
            return Match.Levenshtein.DistanceStatic(string1, string2, minSimilarity.ToDistance(maxLen))
                .ToSimilarity(maxLen);
        }

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
