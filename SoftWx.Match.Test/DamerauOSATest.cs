// Copyright ©2015-2026 SoftWx, Inc.
// Released under the MIT License the text of which appears at the end of this file.
// <authors> Steve Hatchett

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SoftWx.Match.Test {
    [TestClass]
    public class DamerauOSATest {
        private static readonly List<string> testStrings = TestHelper.BuildTestStrings(0, 4);

        [TestMethod]
        public void DamerauOSAShouldImplementInterfaces() {
            var ed = new DamerauOSA();
            Assert.IsInstanceOfType<IDistance>(ed);
            Assert.IsInstanceOfType<ISimilarity>(ed);
        }

        [TestMethod]
        public void DamerauOSAConstructorShouldValidateExpectedMaxStringLength() {
            var ed0 = new DamerauOSA(0);
            Assert.AreEqual(1, ed0.Distance("ab", "ba"));

            var ed10 = new DamerauOSA(10);
            Assert.AreEqual(1, ed10.Distance("ab", "ba"));

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new DamerauOSA(-1));
        }

        [TestMethod]
        public void DamerauOSAShouldMatchReferenceImplementationNoMax() {
            var ed = new DamerauOSA();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = (int)ed.Distance(s1, s2);
                    int d2 = EditDistanceReference.RefDamerauOSA(s1, s2);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void StaticDamerauOSAShouldMatchReferenceImplementationNoMax() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = Distance.DamerauOSA(s1, s2);
                    int d2 = EditDistanceReference.RefDamerauOSA(s1, s2);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(3)]
        [DataRow(int.MaxValue)]
        public void DamerauOSAShouldMatchReferenceImplementationWithMax(int maxDistance) {
            var ed = new DamerauOSA();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = (int)ed.Distance(s1, s2, maxDistance);
                    int d2 = EditDistanceReference.RefDamerauOSA(s1, s2, maxDistance);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(3)]
        [DataRow(int.MaxValue)]
        public void StaticDamerauOSAShouldMatchReferenceImplementationWithMax(int maxDistance) {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = Distance.DamerauOSA(s1, s2, maxDistance);
                    int d2 = EditDistanceReference.RefDamerauOSA(s1, s2, maxDistance);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void DamerauOSAShouldHandleNulls() {
            var ed = new DamerauOSA();
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
        public void StaticDamerauOSAShouldHandleNulls() {
            Assert.AreEqual(0, Distance.DamerauOSA(null, null));
            Assert.AreEqual(2, Distance.DamerauOSA(null, "hi"));
            Assert.AreEqual(2, Distance.DamerauOSA("hi", null));

            Assert.AreEqual(0, Distance.DamerauOSA(null, null, 0));
            Assert.AreEqual(-1, Distance.DamerauOSA(null, "hi", 1));
            Assert.AreEqual(2, Distance.DamerauOSA(null, "hi", 2));
            Assert.AreEqual(-1, Distance.DamerauOSA("hi", null, 1));
            Assert.AreEqual(2, Distance.DamerauOSA("hi", null, 2));
        }

        [TestMethod]
        public void DamerauOSADistanceShouldHandleFractionalAndSpecialMaxDistance() {
            var ed = new DamerauOSA();
            Assert.AreEqual(0, ed.Distance("abc", "abc", 0.5));
            Assert.AreEqual(-1, ed.Distance("ab", "ba", 0.5));
            Assert.AreEqual(1, ed.Distance("ab", "ba", 1.5));
            Assert.AreEqual(-1, ed.Distance("a", "bc", 1.5));
            Assert.AreEqual(2, ed.Distance("a", "bc", 2.9));
            Assert.AreEqual(2, ed.Distance("a", "bc", double.PositiveInfinity));
            Assert.AreEqual(-1, ed.Distance("a", "a", double.NaN));
            Assert.AreEqual(-1, ed.Distance("a", "b", double.NaN));
            Assert.AreEqual(-1, ed.Distance(null, null, double.NaN));
        }

        [TestMethod]
        public void DamerauOSAExtensionMethodsShouldWork() {
            Assert.AreEqual(1, "ca".DamerauOSA("ac"));
            Assert.AreEqual(1, "ca".DamerauOSA("ac", 1));
            Assert.AreEqual(-1, "flintstone".DamerauOSA("hanson", 2));
        }

        [TestMethod]
        public void DamerauOSAShouldBeCommutative() {
            var ed = new DamerauOSA();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    Assert.AreEqual(ed.Distance(s1, s2), ed.Distance(s2, s1));
                    Assert.AreEqual(ed.Distance(s1, s2, 2), ed.Distance(s2, s1, 2));
                }
            }
        }

        [TestMethod]
        public void StaticDamerauOSAShouldBeCommutative() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    Assert.AreEqual(Distance.DamerauOSA(s1, s2), Distance.DamerauOSA(s2, s1));
                    Assert.AreEqual(Distance.DamerauOSA(s1, s2, 2), Distance.DamerauOSA(s2, s1, 2));
                }
            }
        }

        [TestMethod]
        public void StaticDamerauOSAShouldBeThreadsafe() {
            Parallel.For(0, testStrings.Count, i => {
                foreach (var s2 in testStrings) {
                    int d1 = Distance.DamerauOSA(testStrings[i], s2);
                    int d2 = EditDistanceReference.RefDamerauOSA(testStrings[i], s2);
                    Assert.AreEqual(d2, d1);
                }
            });
        }

        [TestMethod]
        public void DamerauOSASimilarityShouldMatchReferenceImplementationNoMin() {
            var ed = new DamerauOSA();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    double actual = ed.Similarity(s1, s2);
                    double expected = EditDistanceReference.RefDamerauOSASimilarity(s1, s2);
                    Assert.AreEqual(expected, actual, 1e-12, $"s1='{s1}', s2='{s2}'");
                }
            }
        }

        [TestMethod]
        public void StaticDamerauOSASimilarityShouldMatchReferenceImplementationNoMin() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    double actual = Similarity.DamerauOSA(s1, s2);
                    double expected = EditDistanceReference.RefDamerauOSASimilarity(s1, s2);
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
        public void DamerauOSASimilarityShouldMatchReferenceImplementationWithMin(double minSimilarity) {
            var ed = new DamerauOSA();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    double actual = ed.Similarity(s1, s2, minSimilarity);
                    double expected = EditDistanceReference.RefDamerauOSASimilarity(s1, s2, minSimilarity);
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
        public void StaticDamerauOSASimilarityShouldMatchReferenceImplementationWithMin(double minSimilarity) {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    double actual = Similarity.DamerauOSA(s1, s2, minSimilarity);
                    double expected = EditDistanceReference.RefDamerauOSASimilarity(s1, s2, minSimilarity);
                    Assert.AreEqual(expected, actual, 1e-12, $"s1='{s1}', s2='{s2}', min={minSimilarity}");
                }
            }
        }

        [TestMethod]
        public void DamerauOSASimilarityShouldHandlePrefixAndSuffixPairs() {
            var ed = new DamerauOSA();
            Assert.AreEqual(1.0, ed.Similarity("", ""));
            Assert.AreEqual(0.0, ed.Similarity("", "abc"));
            Assert.AreEqual(0.5, ed.Similarity("a", "ab"));
            Assert.AreEqual(2 / 3.0, ed.Similarity("bc", "abc"), 1e-12);
            Assert.AreEqual(0.5, ed.Similarity("a", "ab", 0.5));
            Assert.AreEqual(-1, ed.Similarity("a", "ab", 0.75));

            Assert.AreEqual(1.0, Similarity.DamerauOSA("", ""));
            Assert.AreEqual(0.0, Similarity.DamerauOSA("", "abc"));
            Assert.AreEqual(0.5, Similarity.DamerauOSA("a", "ab"));
            Assert.AreEqual(2 / 3.0, Similarity.DamerauOSA("bc", "abc"), 1e-12);
            Assert.AreEqual(0.5, Similarity.DamerauOSA("a", "ab", 0.5));
            Assert.AreEqual(-1, Similarity.DamerauOSA("a", "ab", 0.75));
        }

        [TestMethod]
        public void DamerauOSASimilarityShouldHandleRoundUpDownMin() {
            var ed = new DamerauOSA();
            Assert.AreEqual(-1, ed.Similarity("123456789", "a2345aaaa", .5));
            Assert.AreEqual(5 / 9.0, ed.Similarity("123456789", "a23456aaa", .5));
        }

        [TestMethod]
        public void StaticDamerauOSASimilarityShouldHandleRoundUpDownMin() {
            Assert.AreEqual(-1, Similarity.DamerauOSA("123456789", "a2345aaaa", .5));
            Assert.AreEqual(5 / 9.0, Similarity.DamerauOSA("123456789", "a23456aaa", .5));
        }

        [TestMethod]
        public void DamerauOSASimilarityShouldGiveRange0To1() {
            var ed = new DamerauOSA();
            Assert.AreEqual(0, ed.Similarity("1234", "aaaa"));
            Assert.AreEqual(.25, ed.Similarity("1234", "a2aa"));
            Assert.AreEqual(.5, ed.Similarity("1234", "a23a"));
            Assert.AreEqual(.75, ed.Similarity("1234", "1a34"));
            Assert.AreEqual(.75, ed.Similarity("1234", "1324")); // transposition = 1 edit
            Assert.AreEqual(1, ed.Similarity("1234", "1234"));
        }

        [TestMethod]
        public void StaticDamerauOSASimilarityShouldGiveRange0To1() {
            Assert.AreEqual(0, Similarity.DamerauOSA("1234", "aaaa"));
            Assert.AreEqual(.25, Similarity.DamerauOSA("1234", "a2aa"));
            Assert.AreEqual(.5, Similarity.DamerauOSA("1234", "a23a"));
            Assert.AreEqual(.75, Similarity.DamerauOSA("1234", "1a34"));
            Assert.AreEqual(.75, Similarity.DamerauOSA("1234", "1324")); // transposition = 1 edit
            Assert.AreEqual(1, Similarity.DamerauOSA("1234", "1234"));
        }

        [TestMethod]
        public void DamerauOSASimilarityShouldHandleNulls() {
            var ed = new DamerauOSA();
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
        public void StaticDamerauOSASimilarityShouldHandleNulls() {
            Assert.AreEqual(1.0, Similarity.DamerauOSA(null, null));
            Assert.AreEqual(0, Similarity.DamerauOSA(null, "hi"));
            Assert.AreEqual(0, Similarity.DamerauOSA("hi", null));

            Assert.AreEqual(1.0, Similarity.DamerauOSA(null, null, 0.5));
            Assert.AreEqual(0, Similarity.DamerauOSA(null, "hi", 0.0));
            Assert.AreEqual(-1, Similarity.DamerauOSA(null, "hi", 0.5));
            Assert.AreEqual(0, Similarity.DamerauOSA("hi", null, 0.0));
            Assert.AreEqual(-1, Similarity.DamerauOSA("hi", null, 0.5));
        }

        [TestMethod]
        [DataRow(-0.1)]
        [DataRow(1.1)]
        [DataRow(double.NaN)]
        public void DamerauOSASimilarityShouldThrowOnInvalidMinSimilarity(double invalidMin) {
            var ed = new DamerauOSA();
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => ed.Similarity("a", "b", invalidMin));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => Similarity.DamerauOSA("a", "b", invalidMin));
        }

        [TestMethod]
        public void DamerauOSASimilarityShouldBeCommutative() {
            var ed = new DamerauOSA();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    Assert.AreEqual(ed.Similarity(s1, s2), ed.Similarity(s2, s1));
                    Assert.AreEqual(ed.Similarity(s1, s2, .5), ed.Similarity(s2, s1, .5));
                }
            }
        }

        [TestMethod]
        public void StaticDamerauOSASimilarityShouldBeCommutative() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    Assert.AreEqual(Similarity.DamerauOSA(s1, s2), Similarity.DamerauOSA(s2, s1));
                    Assert.AreEqual(Similarity.DamerauOSA(s1, s2, .5), Similarity.DamerauOSA(s2, s1, .5));
                }
            }
        }

        [TestMethod]
        public void StaticDamerauOSASimilarityShouldBeThreadsafe() {
            Parallel.For(0, testStrings.Count, i => {
                foreach (var s2 in testStrings) {
                    double actual = Similarity.DamerauOSA(testStrings[i], s2);
                    double expected = EditDistanceReference.RefDamerauOSASimilarity(testStrings[i], s2);
                    Assert.AreEqual(expected, actual, 1e-12);
                }
            });
        }

        [TestMethod]
        public void DamerauOSASpanOverloadsShouldMatchStringOverloads() {
            var ed = new DamerauOSA();
            ReadOnlySpan<char> slice1 = "xx1234yy".AsSpan(2, 4);
            ReadOnlySpan<char> slice2 = "zz1324ww".AsSpan(2, 4);

            Assert.AreEqual(1, ed.Distance(slice1, slice2));
            Assert.AreEqual(1, Distance.DamerauOSA(slice1, slice2));
            Assert.AreEqual(1, slice1.DamerauOSA(slice2));
            Assert.AreEqual(1, slice1.DamerauOSA("1324"));
            Assert.AreEqual(1, ed.Distance(slice1, slice2, 1));
            Assert.AreEqual(1, Distance.DamerauOSA(slice1, slice2, 1));
            Assert.AreEqual(-1, ed.Distance(slice1, slice2, 0.5));
            Assert.AreEqual(-1, ed.Distance("a".AsSpan(), "bc".AsSpan(), 1.5));
            Assert.AreEqual(-1, ed.Distance("a".AsSpan(), "a".AsSpan(), double.NaN));

            Assert.AreEqual(0.75, ed.Similarity(slice1, slice2), 1e-12);
            Assert.AreEqual(0.75, Similarity.DamerauOSA(slice1, slice2), 1e-12);
            Assert.AreEqual(0.75, ed.Similarity(slice1, slice2, 0.75), 1e-12);
            Assert.AreEqual(-1, ed.Similarity(slice1, slice2, 0.8));
            Assert.AreEqual(0.75, Similarity.DamerauOSA(slice1, slice2, 0.75), 1e-12);
            Assert.AreEqual(-1, Similarity.DamerauOSA(slice1, slice2, 0.8));

            Assert.AreEqual(0, ed.Distance(ReadOnlySpan<char>.Empty, ReadOnlySpan<char>.Empty));
            Assert.AreEqual(1.0, ed.Similarity(ReadOnlySpan<char>.Empty, ReadOnlySpan<char>.Empty));
            Assert.AreEqual(0.0, ed.Similarity(ReadOnlySpan<char>.Empty, "abc".AsSpan()));
            Assert.AreEqual(0.5, ed.Similarity("a".AsSpan(), "ab".AsSpan()));

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => ed.Similarity("a".AsSpan(), "b".AsSpan(), 1.1));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => Similarity.DamerauOSA("a".AsSpan(), "b".AsSpan(), double.NaN));
        }

        [TestMethod]
        public void DamerauOSAShouldHandleStringsExceedingMaxCachedBufferLength() {
            string s1 = "x" + new string('a', 598) + "y";
            string s2 = "z" + new string('a', 598) + "w";

            var ed = new DamerauOSA();
            Assert.AreEqual(2, ed.Distance(s1, s2));
            Assert.AreEqual(2, ed.Distance(s1, s2, 2));
            Assert.AreEqual(-1, ed.Distance(s1, s2, 1));
            Assert.AreEqual(1.0 - (2.0 / 600), ed.Similarity(s1, s2), 1e-12);
            // Subsequent short comparisons continue to work cleanly.
            Assert.AreEqual(1, ed.Distance("1234", "1324"));

            var preSized = new DamerauOSA(700);
            Assert.AreEqual(2, preSized.Distance(s1, s2));
            Assert.AreEqual(2, Distance.DamerauOSA(s1, s2));
            Assert.AreEqual(2, Distance.DamerauOSA(s1, s2, 2));
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
