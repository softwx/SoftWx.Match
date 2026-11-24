// Copyright © 2015-2026 Steve Hatchett
// SPDX-License-Identifier: MIT
// Released under the MIT License the text of which appears at the end of this file.
// <authors> Steve Hatchett

using System;

namespace SoftWx.Match.Test {
    /// <summary>
    /// Literal implementation of algorithms as described in Wikipedia entries for
    /// Levenshtein and Damerau-Levenshtein OSA edit distance, with trivial addition
    /// of maxDistance and normalized similarity helpers.
    /// </summary>
    internal static class EditDistanceReference {
        public static int RefLevenshtein(string s, string t, int maxDistance = int.MaxValue) {
            if (maxDistance < 0) maxDistance = 0;
            var d = new int[s.Length + 1, t.Length + 1];
            for (int i = 0; i <= s.Length; i++) d[i, 0] = i;
            for (int i = 0; i <= t.Length; i++) d[0, i] = i;
            for (int j = 1; j <= t.Length; j++) {
                for (int i = 1; i <= s.Length; i++) {
                    if (s[i - 1] == t[j - 1]) {
                        d[i, j] = d[i - 1, j - 1]; // no operation
                    } else {
                        d[i, j] = Math.Min(Math.Min(
                            d[i - 1, j] + 1,     // a deletion
                            d[i, j - 1] + 1),    // an insertion
                            d[i - 1, j - 1] + 1  // a substitution
                        );
                    }
                }
            }
            var distance = d[s.Length, t.Length];
            return (distance <= maxDistance) ? distance : -1;
        }

        public static double RefLevenshteinSimilarity(string s, string t, double minSimilarity = 0) {
            int maxLen = Math.Max(s.Length, t.Length);
            if (maxLen == 0) return 1.0;
            int distance = RefLevenshtein(s, t);
            double similarity = 1.0 - (distance / (double)maxLen);
            return (similarity + 1e-12 >= minSimilarity) ? similarity : -1;
        }

        public static int RefDamerauOSA(string s, string t, int maxDistance = int.MaxValue) {
            if (maxDistance < 0) maxDistance = 0;
            var d = new int[s.Length + 1, t.Length + 1];
            for (int i = 0; i <= s.Length; i++) d[i, 0] = i;
            for (int i = 0; i <= t.Length; i++) d[0, i] = i;
            for (int i = 1; i <= s.Length; i++) {
                for (int j = 1; j <= t.Length; j++) {
                    int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(
                        d[i - 1, j] + 1,       // a deletion
                        d[i, j - 1] + 1),      // an insertion
                        d[i - 1, j - 1] + cost // a substitution
                    );
                    if (i > 1 && j > 1 && s[i - 1] == t[j - 2] && s[i - 2] == t[j - 1]) {
                        d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + cost);
                    }
                }
            }
            var distance = d[s.Length, t.Length];
            return (distance <= maxDistance) ? distance : -1;
        }

        public static double RefDamerauOSASimilarity(string s, string t, double minSimilarity = 0) {
            int maxLen = Math.Max(s.Length, t.Length);
            if (maxLen == 0) return 1.0;
            int distance = RefDamerauOSA(s, t);
            double similarity = 1.0 - (distance / (double)maxLen);
            return (similarity + 1e-12 >= minSimilarity) ? similarity : -1;
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
