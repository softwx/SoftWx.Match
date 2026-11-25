// Copyright © 2015-2026 Steve Hatchett
// SPDX-License-Identifier: MIT
// Released under the MIT License the text of which appears at the end of this file.
// <authors> Steve Hatchett

using System;

namespace SoftWx.Match.AotTest {
    internal static class Program {
        public static int Main() {
            // Interface dispatch (IDistance / ISimilarity)
            VerifyDistance(new Levenshtein(), "flintstone", "hanson", 6.0, 2, -1.0, "IDistance Levenshtein");
            VerifySimilarity(new Levenshtein(128), "flintstone", "hanson", 0.4, 0.75, -1.0, "ISimilarity Levenshtein");
            VerifyDistance(new DamerauOSA(), "flintstone", "flintsotne", 1.0, 1, 1.0, "IDistance DamerauOSA");
            VerifySimilarity(new DamerauOSA(128), "flintstone", "flintsotne", 0.9, 0.75, 0.9, "ISimilarity DamerauOSA");

            // Concrete + ReadOnlySpan<char> overloads
            var lev = new Levenshtein();
            var dam = new DamerauOSA();
            ReadOnlySpan<char> span1 = "fred flintstone".AsSpan(5);
            ReadOnlySpan<char> span2 = "hanson".AsSpan();
            ReadOnlySpan<char> span3 = "flintsotne".AsSpan();

            AssertEqual(6.0, lev.Distance(span1, span2), "Levenshtein span Distance");
            AssertEqual(-1.0, lev.Distance(span1, span2, 2), "Levenshtein span bounded Distance");
            AssertEqual(0.4, lev.Similarity(span1, span2), "Levenshtein span Similarity");
            AssertEqual(0.4, lev.Similarity(span1, span2, 0.4), "Levenshtein span bounded Similarity");

            AssertEqual(1.0, dam.Distance(span1, span3), "DamerauOSA span Distance");
            AssertEqual(1.0, dam.Distance(span1, span3, 1), "DamerauOSA span bounded Distance");
            AssertEqual(0.9, dam.Similarity(span1, span3), "DamerauOSA span Similarity");
            AssertEqual(0.9, dam.Similarity(span1, span3, 0.75), "DamerauOSA span bounded Similarity");

            // Thread-safe static & extension methods (stackalloc path)
            AssertEqual(6, Distance.Levenshtein("flintstone", "hanson"), "Static Levenshtein");
            AssertEqual(-1, "flintstone".Levenshtein("hanson", 2), "Extension Levenshtein bounded");
            AssertEqual(6, span1.Levenshtein(span2), "Span extension Levenshtein");
            AssertEqual(1, Distance.DamerauOSA("flintstone", "flintsotne"), "Static DamerauOSA");
            AssertEqual(1, span1.DamerauOSA(span3, 1), "Span extension DamerauOSA bounded");
            AssertEqual(0.4, Similarity.Levenshtein(span1, span2), "Static Similarity.Levenshtein span");
            AssertEqual(0.9, Similarity.DamerauOSA(span1, span3, 0.75), "Static Similarity.DamerauOSA span bounded");

            // Oversized (> 512 chars) path exercising ArrayPool<int>.Shared
            string long1 = "x" + new string('a', 598) + "y";
            string long2 = "z" + new string('a', 598) + "w";
            AssertEqual(2.0, lev.Distance(long1, long2), "Levenshtein oversized");
            AssertEqual(2, Distance.Levenshtein(long1, long2, 2), "Static Levenshtein oversized bounded");
            AssertEqual(2.0, dam.Distance(long1, long2), "DamerauOSA oversized");
            AssertEqual(2, Distance.DamerauOSA(long1, long2, 2), "Static DamerauOSA oversized bounded");

            // Null handling & validation
            AssertEqual(0, Distance.Levenshtein(null, null), "Null Levenshtein");
            AssertEqual(1.0, Similarity.DamerauOSA(null, null), "Null DamerauOSA similarity");
            try {
                _ = Similarity.Levenshtein("a", "b", 1.5);
                throw new InvalidOperationException("Expected ArgumentOutOfRangeException was not thrown.");
            } catch (ArgumentOutOfRangeException) {
                // expected
            }

            Console.WriteLine("NativeAOT smoke test passed.");
            return 0;
        }

        private static void VerifyDistance(IDistance metric, string s1, string s2, double expected, double maxDist, double expectedBounded, string label) {
            AssertEqual(expected, metric.Distance(s1, s2), label);
            AssertEqual(expectedBounded, metric.Distance(s1, s2, maxDist), $"{label} bounded");
            AssertEqual(expected, metric.Distance(s1.AsSpan(), s2.AsSpan()), $"{label} span");
            AssertEqual(expectedBounded, metric.Distance(s1.AsSpan(), s2.AsSpan(), maxDist), $"{label} span bounded");
        }

        private static void VerifySimilarity(ISimilarity metric, string s1, string s2, double expected, double minSim, double expectedBounded, string label) {
            AssertEqual(expected, metric.Similarity(s1, s2), label);
            AssertEqual(expectedBounded, metric.Similarity(s1, s2, minSim), $"{label} bounded");
            AssertEqual(expected, metric.Similarity(s1.AsSpan(), s2.AsSpan()), $"{label} span");
            AssertEqual(expectedBounded, metric.Similarity(s1.AsSpan(), s2.AsSpan(), minSim), $"{label} span bounded");
        }

        private static void AssertEqual(int expected, int actual, string label) {
            if (expected != actual) {
                throw new InvalidOperationException($"{label}: expected {expected}, got {actual}.");
            }
        }

        private static void AssertEqual(double expected, double actual, string label) {
            if (Math.Abs(expected - actual) > 1e-12) {
                throw new InvalidOperationException($"{label}: expected {expected}, got {actual}.");
            }
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
