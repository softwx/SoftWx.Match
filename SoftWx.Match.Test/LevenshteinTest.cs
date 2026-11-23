// Copyright ©2015-2026 SoftWx, Inc.
// Released under the MIT License the text of which appears at the end of this file.
// <authors> Steve Hatchett

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SoftWx.Match.Test {
    [TestClass]
    public class LevenshteinTest {
        private static readonly List<string> testStrings = TestHelper.BuildTestStrings(0, 4);

        [TestMethod]
        public void LevShouldImplementInterfaces() {
            var ed = new Levenshtein();
            Assert.IsInstanceOfType<IDistance>(ed);
            Assert.IsInstanceOfType<ISimilarity>(ed);
        }

        [TestMethod]
        public void LevConstructorShouldValidateExpectedMaxStringLength() {
            var ed0 = new Levenshtein(0);
            Assert.AreEqual(1, ed0.Distance("a", "b"));

            var ed10 = new Levenshtein(10);
            Assert.AreEqual(1, ed10.Distance("a", "b"));

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Levenshtein(-1));
        }

        [TestMethod]
        public void LevDistanceShouldMatchReferenceImplementationNoMax() {
            var ed = new Levenshtein();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = (int)ed.Distance(s1, s2);
                    int d2 = EditDistanceReference.RefLevenshtein(s1, s2);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void StaticLevDistanceShouldMatchReferenceImplementationNoMax() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = Distance.Levenshtein(s1, s2);
                    int d2 = EditDistanceReference.RefLevenshtein(s1, s2);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(3)]
        [DataRow(int.MaxValue)]
        public void LevDistanceShouldMatchReferenceImplementationWithMax(int maxDistance) {
            var ed = new Levenshtein();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = (int)ed.Distance(s1, s2, maxDistance);
                    int d2 = EditDistanceReference.RefLevenshtein(s1, s2, maxDistance);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(3)]
        [DataRow(int.MaxValue)]
        public void StaticLevDistanceShouldMatchReferenceImplementationWithMax(int maxDistance) {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = Distance.Levenshtein(s1, s2, maxDistance);
                    int d2 = EditDistanceReference.RefLevenshtein(s1, s2, maxDistance);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void LevDistanceShouldHandleNulls() {
            var ed = new Levenshtein();
            Assert.AreEqual(0, ed.Distance(null, null));
            Assert.AreEqual(2, ed.Distance(null, "hi"));
            Assert.AreEqual(2, ed.Distance("hi", null));

            Assert.AreEqual(0, ed.Distance(null, null, 0));
            Assert.AreEqual(-1, ed.Distance(null, "hi", 1));
            Assert.AreEqual(2, ed.Distance(null, "hi", 2));
            Assert.AreEqual(-1, ed.Distance("hi", null, 1));
            Assert.AreEqual(2, ed.Distance("hi", null, 2));
        }

        [TestMethod]
        public void StaticLevDistanceShouldHandleNulls() {
            Assert.AreEqual(0, Distance.Levenshtein(null, null));
            Assert.AreEqual(2, Distance.Levenshtein(null, "hi"));
            Assert.AreEqual(2, Distance.Levenshtein("hi", null));

            Assert.AreEqual(0, Distance.Levenshtein(null, null, 0));
            Assert.AreEqual(-1, Distance.Levenshtein(null, "hi", 1));
            Assert.AreEqual(2, Distance.Levenshtein(null, "hi", 2));
            Assert.AreEqual(-1, Distance.Levenshtein("hi", null, 1));
            Assert.AreEqual(2, Distance.Levenshtein("hi", null, 2));
        }

        [TestMethod]
        public void LevDistanceShouldHandleFractionalAndSpecialMaxDistance() {
            var ed = new Levenshtein();
            Assert.AreEqual(0, ed.Distance("abc", "abc", 0.5));
            Assert.AreEqual(-1, ed.Distance("abc", "abd", 0.5));
            Assert.AreEqual(1, ed.Distance("a", "b", 1.5));
            Assert.AreEqual(-1, ed.Distance("a", "bc", 1.5));
            Assert.AreEqual(2, ed.Distance("a", "bc", 2.9));
            Assert.AreEqual(2, ed.Distance("a", "bc", double.PositiveInfinity));
            Assert.AreEqual(-1, ed.Distance("a", "a", double.NaN));
            Assert.AreEqual(-1, ed.Distance("a", "b", double.NaN));
            Assert.AreEqual(-1, ed.Distance(null, null, double.NaN));
        }

        [TestMethod]
        public void LevDistanceExtensionMethodsShouldWork() {
            Assert.AreEqual(6, "flintstone".Levenshtein("hanson"));
            Assert.AreEqual(-1, "flintstone".Levenshtein("hanson", 2));
            Assert.AreEqual(1, "hello".Levenshtein("hallo", 2));
        }

        [TestMethod]
        public void LevDistanceShouldBeCommutative() {
            var ed = new Levenshtein();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    Assert.AreEqual(ed.Distance(s1, s2), ed.Distance(s2, s1));
                    Assert.AreEqual(ed.Distance(s1, s2, 2), ed.Distance(s2, s1, 2));
                }
            }
        }

        [TestMethod]
        public void StaticLevDistanceShouldBeCommutative() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    Assert.AreEqual(Distance.Levenshtein(s1, s2), Distance.Levenshtein(s2, s1));
                    Assert.AreEqual(Distance.Levenshtein(s1, s2, 2), Distance.Levenshtein(s2, s1, 2));
                }
            }
        }

        [TestMethod]
        public void StaticLevDistanceShouldBeThreadsafe() {
            Parallel.For(0, testStrings.Count, i => {
                foreach (var s2 in testStrings) {
                    int d1 = Distance.Levenshtein(testStrings[i], s2);
                    int d2 = EditDistanceReference.RefLevenshtein(testStrings[i], s2);
                    Assert.AreEqual(d2, d1);
                }
            });
        }

        [TestMethod]
        public void LevSimilarityShouldMatchReferenceImplementationNoMin() {
            var ed = new Levenshtein();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    double actual = ed.Similarity(s1, s2);
                    double expected = EditDistanceReference.RefLevenshteinSimilarity(s1, s2);
                    Assert.AreEqual(expected, actual, 1e-12, $"s1='{s1}', s2='{s2}'");
                }
            }
        }

        [TestMethod]
        public void StaticLevSimilarityShouldMatchReferenceImplementationNoMin() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    double actual = Similarity.Levenshtein(s1, s2);
                    double expected = EditDistanceReference.RefLevenshteinSimilarity(s1, s2);
                    Assert.AreEqual(expected, actual, 1e-12, $"s1='{s1}', s2='{s2}'");
                }
            }
        }

        [TestMethod]
        [DataRow(0.0)]
        [DataRow(0.25)]
        [DataRow(0.5)]
        [DataRow(0.75)]
        [DataRow(1.0)]
        public void LevSimilarityShouldMatchReferenceImplementationWithMin(double minSimilarity) {
            var ed = new Levenshtein();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    double actual = ed.Similarity(s1, s2, minSimilarity);
                    double expected = EditDistanceReference.RefLevenshteinSimilarity(s1, s2, minSimilarity);
                    Assert.AreEqual(expected, actual, 1e-12, $"s1='{s1}', s2='{s2}', min={minSimilarity}");
                }
            }
        }

        [TestMethod]
        [DataRow(0.0)]
        [DataRow(0.25)]
        [DataRow(0.5)]
        [DataRow(0.75)]
        [DataRow(1.0)]
        public void StaticLevSimilarityShouldMatchReferenceImplementationWithMin(double minSimilarity) {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    double actual = Similarity.Levenshtein(s1, s2, minSimilarity);
                    double expected = EditDistanceReference.RefLevenshteinSimilarity(s1, s2, minSimilarity);
                    Assert.AreEqual(expected, actual, 1e-12, $"s1='{s1}', s2='{s2}', min={minSimilarity}");
                }
            }
        }

        [TestMethod]
        public void LevSimilarityShouldHandlePrefixAndSuffixPairs() {
            var ed = new Levenshtein();
            Assert.AreEqual(1.0, ed.Similarity("", ""));
            Assert.AreEqual(0.0, ed.Similarity("", "abc"));
            Assert.AreEqual(0.5, ed.Similarity("a", "ab"));
            Assert.AreEqual(2 / 3.0, ed.Similarity("bc", "abc"), 1e-12);
            Assert.AreEqual(0.5, ed.Similarity("a", "ab", 0.5));
            Assert.AreEqual(-1, ed.Similarity("a", "ab", 0.75));

            Assert.AreEqual(1.0, Similarity.Levenshtein("", ""));
            Assert.AreEqual(0.0, Similarity.Levenshtein("", "abc"));
            Assert.AreEqual(0.5, Similarity.Levenshtein("a", "ab"));
            Assert.AreEqual(2 / 3.0, Similarity.Levenshtein("bc", "abc"), 1e-12);
            Assert.AreEqual(0.5, Similarity.Levenshtein("a", "ab", 0.5));
            Assert.AreEqual(-1, Similarity.Levenshtein("a", "ab", 0.75));
        }

        [TestMethod]
        public void LevSimilarityShouldHandleRoundUpDownMin() {
            var ed = new Levenshtein();
            Assert.AreEqual(-1, ed.Similarity("123456789", "a2345aaaa", .5));
            Assert.AreEqual(5 / 9.0, ed.Similarity("123456789", "a23456aaa", .5));
        }

        [TestMethod]
        public void StaticLevSimilarityShouldHandleRoundUpDownMin() {
            Assert.AreEqual(-1, Similarity.Levenshtein("123456789", "a2345aaaa", .5));
            Assert.AreEqual(5 / 9.0, Similarity.Levenshtein("123456789", "a23456aaa", .5));
        }

        [TestMethod]
        public void LevSimilarityShouldGiveRange0To1() {
            var ed = new Levenshtein();
            Assert.AreEqual(0, ed.Similarity("1234", "aaaa"));
            Assert.AreEqual(.25, ed.Similarity("1234", "a2aa"));
            Assert.AreEqual(.5, ed.Similarity("1234", "a23a"));
            Assert.AreEqual(.75, ed.Similarity("1234", "1a34"));
            Assert.AreEqual(1, ed.Similarity("1234", "1234"));
        }

        [TestMethod]
        public void StaticLevSimilarityShouldGiveRange0To1() {
            Assert.AreEqual(0, Similarity.Levenshtein("1234", "aaaa"));
            Assert.AreEqual(.25, Similarity.Levenshtein("1234", "a2aa"));
            Assert.AreEqual(.5, Similarity.Levenshtein("1234", "a23a"));
            Assert.AreEqual(.75, Similarity.Levenshtein("1234", "1a34"));
            Assert.AreEqual(1, Similarity.Levenshtein("1234", "1234"));
        }

        [TestMethod]
        public void LevSimilarityShouldHandleNulls() {
            var ed = new Levenshtein();
            Assert.AreEqual(1.0, ed.Similarity(null, null));
            Assert.AreEqual(0, ed.Similarity(null, "hi"));
            Assert.AreEqual(0, ed.Similarity("hi", null));

            Assert.AreEqual(1.0, ed.Similarity(null, null, 0.5));
            Assert.AreEqual(0, ed.Similarity(null, "hi", 0.0));
            Assert.AreEqual(-1, ed.Similarity(null, "hi", 0.5));
            Assert.AreEqual(0, ed.Similarity("hi", null, 0.0));
            Assert.AreEqual(-1, ed.Similarity("hi", null, 0.5));
        }

        [TestMethod]
        public void StaticLevSimilarityShouldHandleNulls() {
            Assert.AreEqual(1.0, Similarity.Levenshtein(null, null));
            Assert.AreEqual(0, Similarity.Levenshtein(null, "hi"));
            Assert.AreEqual(0, Similarity.Levenshtein("hi", null));

            Assert.AreEqual(1.0, Similarity.Levenshtein(null, null, 0.5));
            Assert.AreEqual(0, Similarity.Levenshtein(null, "hi", 0.0));
            Assert.AreEqual(-1, Similarity.Levenshtein(null, "hi", 0.5));
            Assert.AreEqual(0, Similarity.Levenshtein("hi", null, 0.0));
            Assert.AreEqual(-1, Similarity.Levenshtein("hi", null, 0.5));
        }

        [TestMethod]
        [DataRow(-0.1)]
        [DataRow(1.1)]
        [DataRow(double.NaN)]
        public void LevSimilarityShouldThrowOnInvalidMinSimilarity(double invalidMin) {
            var ed = new Levenshtein();
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => ed.Similarity("a", "b", invalidMin));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => Similarity.Levenshtein("a", "b", invalidMin));
        }

        [TestMethod]
        public void LevSimilarityShouldBeCommutative() {
            var ed = new Levenshtein();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    Assert.AreEqual(ed.Similarity(s1, s2), ed.Similarity(s2, s1));
                    Assert.AreEqual(ed.Similarity(s1, s2, .5), ed.Similarity(s2, s1, .5));
                }
            }
        }

        [TestMethod]
        public void StaticLevSimilarityShouldBeCommutative() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    Assert.AreEqual(Similarity.Levenshtein(s1, s2), Similarity.Levenshtein(s2, s1));
                    Assert.AreEqual(Similarity.Levenshtein(s1, s2, .5), Similarity.Levenshtein(s2, s1, .5));
                }
            }
        }

        [TestMethod]
        public void StaticLevSimilarityShouldBeThreadsafe() {
            Parallel.For(0, testStrings.Count, i => {
                foreach (var s2 in testStrings) {
                    double actual = Similarity.Levenshtein(testStrings[i], s2);
                    double expected = EditDistanceReference.RefLevenshteinSimilarity(testStrings[i], s2);
                    Assert.AreEqual(expected, actual, 1e-12);
                }
            });
        }

        [TestMethod]
        public void LevSpanOverloadsShouldMatchStringOverloads() {
            var ed = new Levenshtein();
            ReadOnlySpan<char> slice1 = "xxflintstoneyy".AsSpan(2, 10);
            ReadOnlySpan<char> slice2 = "zzhansonww".AsSpan(2, 6);

            Assert.AreEqual(6, ed.Distance(slice1, slice2));
            Assert.AreEqual(6, Distance.Levenshtein(slice1, slice2));
            Assert.AreEqual(6, slice1.Levenshtein(slice2));
            Assert.AreEqual(6, slice1.Levenshtein("hanson"));
            Assert.AreEqual(-1, ed.Distance(slice1, slice2, 2));
            Assert.AreEqual(-1, Distance.Levenshtein(slice1, slice2, 2));
            Assert.AreEqual(1, ed.Distance("a".AsSpan(), "b".AsSpan(), 1.5));
            Assert.AreEqual(-1, ed.Distance("a".AsSpan(), "bc".AsSpan(), 1.5));
            Assert.AreEqual(-1, ed.Distance("a".AsSpan(), "a".AsSpan(), double.NaN));

            Assert.AreEqual(0.4, ed.Similarity(slice1, slice2), 1e-12);
            Assert.AreEqual(0.4, Similarity.Levenshtein(slice1, slice2), 1e-12);
            Assert.AreEqual(0.4, ed.Similarity(slice1, slice2, 0.4), 1e-12);
            Assert.AreEqual(-1, ed.Similarity(slice1, slice2, 0.5));
            Assert.AreEqual(0.4, Similarity.Levenshtein(slice1, slice2, 0.4), 1e-12);
            Assert.AreEqual(-1, Similarity.Levenshtein(slice1, slice2, 0.5));

            Assert.AreEqual(0, ed.Distance(ReadOnlySpan<char>.Empty, ReadOnlySpan<char>.Empty));
            Assert.AreEqual(1.0, ed.Similarity(ReadOnlySpan<char>.Empty, ReadOnlySpan<char>.Empty));
            Assert.AreEqual(0.0, ed.Similarity(ReadOnlySpan<char>.Empty, "abc".AsSpan()));
            Assert.AreEqual(0.5, ed.Similarity("a".AsSpan(), "ab".AsSpan()));

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => ed.Similarity("a".AsSpan(), "b".AsSpan(), -0.1));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => Similarity.Levenshtein("a".AsSpan(), "b".AsSpan(), double.NaN));
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
