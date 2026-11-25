// Copyright © 2015-2026 Steve Hatchett
// SPDX-License-Identifier: MIT
// Released under the MIT License the text of which appears at the end of this file.
// <authors> Steve Hatchett

#if NET8_0_OR_GREATER
using System;
#endif

namespace SoftWx.Match {
    /// <summary>Types implementing the ISimilarity interface provide methods
    /// for computing a normalized value of similarity between two strings.</summary>
    public interface ISimilarity {
        /// <summary>Return a measure of the similarity between two strings.</summary>
        /// <param name="string1">One of the strings to compare.</param>
        /// <param name="string2">The other string to compare.</param>
        /// <returns>The degree of similarity 0 to 1.0, where 0 represents a lack of any
        /// notable similarity, and 1 represents equivalent strings.</returns>
        double Similarity(string? string1, string? string2);

        /// <summary>Return a measure of the similarity between two strings.</summary>
        /// <param name="string1">One of the strings to compare.</param>
        /// <param name="string2">The other string to compare.</param>
        /// <param name="minSimilarity">The minimum similarity that is of interest.</param>
        /// <returns>The degree of similarity 0 to 1.0, where -1 represents a similarity
        /// lower than minSimilarity, otherwise, a number between 0 and 1.0 where 0
        /// represents a lack of any notable similarity, and 1 represents equivalent
        /// strings.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when minSimilarity is NaN or outside the range 0 to 1.0.</exception>
        double Similarity(string? string1, string? string2, double minSimilarity);

#if NET8_0_OR_GREATER
        /// <summary>Return a measure of the similarity between two character spans.</summary>
        /// <remarks>The default implementation converts the spans to strings and delegates to
        /// <see cref="Similarity(string, string)"/>. Implementers should override this method
        /// to avoid string allocations.</remarks>
        /// <param name="string1">One of the character spans to compare.</param>
        /// <param name="string2">The other character span to compare.</param>
        /// <returns>The degree of similarity 0 to 1.0, where 0 represents a lack of any
        /// notable similarity, and 1 represents equivalent spans.</returns>
        double Similarity(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2) =>
            Similarity(string1.ToString(), string2.ToString());

        /// <summary>Return a measure of the similarity between two character spans.</summary>
        /// <remarks>The default implementation converts the spans to strings and delegates to
        /// <see cref="Similarity(string, string, double)"/>. Implementers should override this method
        /// to avoid string allocations.</remarks>
        /// <param name="string1">One of the character spans to compare.</param>
        /// <param name="string2">The other character span to compare.</param>
        /// <param name="minSimilarity">The minimum similarity that is of interest.</param>
        /// <returns>The degree of similarity 0 to 1.0, where -1 represents a similarity
        /// lower than minSimilarity, otherwise, a number between 0 and 1.0 where 0
        /// represents a lack of any notable similarity, and 1 represents equivalent
        /// spans.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when minSimilarity is NaN or outside the range 0 to 1.0.</exception>
        double Similarity(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2, double minSimilarity) =>
            Similarity(string1.ToString(), string2.ToString(), minSimilarity);
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
