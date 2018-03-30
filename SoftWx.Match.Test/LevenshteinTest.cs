// Copyright ©2015-2018 SoftWx, Inc.
// Released under the MIT License the text of which appears at the end of this file.
// <authors> Steve Hatchett

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoftWx.Match;
using System.Collections.Generic;

namespace SoftWx.Match.Test {
    [TestClass]
    public class LevenshteinTest {
        private static List<string> testStrings;

        static LevenshteinTest() {
            testStrings = TestHelper.BuildTestStrings(0, 4);
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
        public void LevDistanceShouldMatchReferenceImplementationMax0() {
            var ed = new Levenshtein();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = (int)ed.Distance(s1, s2, 0);
                    int d2 = EditDistanceReference.RefLevenshtein(s1, s2, 0);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void StaticLevDistanceShouldMatchReferenceImplementationMax0() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = Distance.Levenshtein(s1, s2, 0);
                    int d2 = EditDistanceReference.RefLevenshtein(s1, s2, 0);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void LevDistanceShouldMatchReferenceImplementationMax1() {
            var ed = new Levenshtein();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = (int)ed.Distance(s1, s2, 1);
                    int d2 = EditDistanceReference.RefLevenshtein(s1, s2, 1);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void StaticLevDistanceShouldMatchReferenceImplementationMax1() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = Distance.Levenshtein(s1, s2, 1);
                    int d2 = EditDistanceReference.RefLevenshtein(s1, s2, 1);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void LevDistanceShouldMatchReferenceImplementationMax3() {
            var ed = new Levenshtein();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = (int)ed.Distance(s1, s2, 3);
                    int d2 = EditDistanceReference.RefLevenshtein(s1, s2, 3);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void StaticLevDistanceShouldMatchReferenceImplementationMax3() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = Distance.Levenshtein(s1, s2, 3);
                    int d2 = EditDistanceReference.RefLevenshtein(s1, s2, 3);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void LevDistanceShouldMatchReferenceImplementationMaxHuge() {
            var ed = new Levenshtein();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = (int)ed.Distance(s1, s2, int.MaxValue);
                    int d2 = EditDistanceReference.RefLevenshtein(s1, s2, int.MaxValue);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void StaticLevDistanceShouldMatchReferenceImplementationMaxHuge() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = Distance.Levenshtein(s1, s2, int.MaxValue);
                    int d2 = EditDistanceReference.RefLevenshtein(s1, s2, int.MaxValue);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void LevDistanceShouldHandleNulls() {
            var ed = new Levenshtein();
            var actual = ed.Distance(null, null);
            Assert.AreEqual(0, actual);
            actual = ed.Distance(null, "hi");
            Assert.AreEqual(2, actual);
            actual = ed.Distance("hi", null);
            Assert.AreEqual(2, actual);
        }

        [TestMethod]
        public void StaticLevDistanceShouldHandleNulls() {
            var actual = Distance.Levenshtein(null, null);
            Assert.AreEqual(0, actual);
            actual = Distance.Levenshtein(null, "hi");
            Assert.AreEqual(2, actual);
            actual = Distance.Levenshtein("hi", null);
            Assert.AreEqual(2, actual);
        }

        [TestMethod]
        public void LevDistanceShouldBeCommutative() {
            var ed = new Levenshtein();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    double d1 = ed.Distance(s1, s2);
                    double d2 = ed.Distance(s2, s1);
                    Assert.AreEqual(d1, d2);
                    d1 = ed.Distance(s1, s2, 2);
                    d2 = ed.Distance(s2, s1, 2);
                    Assert.AreEqual(d1, d2);
                }
            }
        }

        [TestMethod]
        public void StaticLevDistanceShouldBeCommutative() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = Distance.Levenshtein(s1, s2);
                    int d2 = Distance.Levenshtein(s2, s1);
                    Assert.AreEqual(d1, d2);
                    d1 = Distance.Levenshtein(s1, s2, 2);
                    d2 = Distance.Levenshtein(s2, s1, 2);
                    Assert.AreEqual(d1, d2);
                }
            }
        }

        [TestMethod]
        public void StaticLevDistanceShouldBeThreadsafe() {
            System.Threading.Tasks.Parallel.For(0, testStrings.Count, i => {
                foreach (var s2 in testStrings) {
                    int d1 = Distance.Levenshtein(testStrings[i], s2);
                    int d2 = EditDistanceReference.RefLevenshtein(testStrings[i], s2);
                    Assert.AreEqual(d2, d1);
                }
            });
        }

        [TestMethod]
        public void LevSimilarityShouldHandleRoundUpDownMin() {
            var ed = new Levenshtein();
            var actual = ed.Similarity("123456789", "a2345aaaa", .5);
            Assert.AreEqual(-1, actual);
            actual = ed.Similarity("123456789", "a23456aaa", .5);
            Assert.AreEqual(5/9.0, actual);
        }

        [TestMethod]
        public void StaticLevSimilarityShouldHandleRoundUpDownMin() {
            var actual = Similarity.Levenshtein("123456789", "a2345aaaa", .5);
            Assert.AreEqual(-1, actual);
            actual = Similarity.Levenshtein("123456789", "a23456aaa", .5);
            Assert.AreEqual(5 / 9.0, actual);
        }

        [TestMethod]
        public void LevSimilarityShouldGiveRange0To1() {
            var ed = new Levenshtein();
            var actual = ed.Similarity("1234", "aaaa");
            Assert.AreEqual(0, actual);
            actual = ed.Similarity("1234", "a2aa");
            Assert.AreEqual(.25, actual);
            actual = ed.Similarity("1234", "a23a");
            Assert.AreEqual(.5, actual);
            actual = ed.Similarity("1234", "1a34");
            Assert.AreEqual(.75, actual);
            actual = ed.Similarity("1234", "1234");
            Assert.AreEqual(1, actual);
        }

        [TestMethod]
        public void StaticLevSimilarityShouldGiveRange0To1() {
            var actual = Similarity.Levenshtein("1234", "aaaa");
            Assert.AreEqual(0, actual);
            actual = Similarity.Levenshtein("1234", "a2aa");
            Assert.AreEqual(.25, actual);
            actual = Similarity.Levenshtein("1234", "a23a");
            Assert.AreEqual(.5, actual);
            actual = Similarity.Levenshtein("1234", "1a34");
            Assert.AreEqual(.75, actual);
            actual = Similarity.Levenshtein("1234", "1234");
            Assert.AreEqual(1, actual);
        }

        [TestMethod]
        public void LevSimilarityShouldHandleNulls() {
            var ed = new Levenshtein();
            var actual = ed.Similarity(null, null);
            Assert.AreEqual(1.0, actual);
            actual = ed.Similarity(null, "hi");
            Assert.AreEqual(0, actual);
            actual = ed.Similarity("hi", null);
            Assert.AreEqual(0, actual);
        }

        [TestMethod]
        public void StaticLevSimilarityShouldHandleNulls() {
            var actual = Similarity.Levenshtein(null, null);
            Assert.AreEqual(1.0, actual);
            actual = Similarity.Levenshtein(null, "hi");
            Assert.AreEqual(0, actual);
            actual = Similarity.Levenshtein("hi", null);
            Assert.AreEqual(0, actual);
        }

        [TestMethod]
        public void LevSimilarityShouldBeCommutative() {
            var ed = new Levenshtein();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    double d1 = ed.Similarity(s1, s2);
                    double d2 = ed.Similarity(s2, s1);
                    Assert.AreEqual(d1, d2);
                    d1 = ed.Similarity(s1, s2, .5);
                    d2 = ed.Similarity(s2, s1, .5);
                    Assert.AreEqual(d1, d2);
                }
            }
        }

        [TestMethod]
        public void StaticLevSimilarityShouldBeCommutative() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    double d1 = Similarity.Levenshtein(s1, s2);
                    double d2 = Similarity.Levenshtein(s2, s1);
                    Assert.AreEqual(d1, d2);
                    d1 = Similarity.Levenshtein(s1, s2, .5);
                    d2 = Similarity.Levenshtein(s2, s1, .5);
                    Assert.AreEqual(d1, d2);
                }
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
