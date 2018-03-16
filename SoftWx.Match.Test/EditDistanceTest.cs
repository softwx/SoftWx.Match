// Copyright ©2015-2018 SoftWx, Inc.
// Released under the MIT License the text of which appears at the end of this file.
// <authors> Steve Hatchett

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoftWx.Match;
using System.Collections.Generic;

namespace SoftWx.TextMatch.Tests {
    [TestClass]
    public class EditDistanceTest {
        private static List<string> testStrings;

        static EditDistanceTest() {
            testStrings = BuildTestStrings(0, 4);
        }

        [TestMethod]
        public void LevenshteinShouldMatchReferenceImplementationNoMax() {
            var ed = new EditDistance();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = ed.Levenshtein(s1, s2);
                    int d2 = EditDistanceReference.RefLevenshtein(s1, s2);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void StaticLevenshteinShouldMatchReferenceImplementationNoMax() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = s1.Levenshtein(s2);
                    int d2 = EditDistanceReference.RefLevenshtein(s1, s2);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void LevenshteinShouldMatchReferenceImplementationMax0() {
            var ed = new EditDistance();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = ed.Levenshtein(s1, s2, 0);
                    int d2 = EditDistanceReference.RefLevenshtein(s1, s2, 0);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void StaticLevenshteinShouldMatchReferenceImplementationMax0() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = s1.Levenshtein(s2, 0);
                    int d2 = EditDistanceReference.RefLevenshtein(s1, s2, 0);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void LevenshteinShouldMatchReferenceImplementationMax1() {
            var ed = new EditDistance();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = ed.Levenshtein(s1, s2, 1);
                    int d2 = EditDistanceReference.RefLevenshtein(s1, s2, 1);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void StaticLevenshteinShouldMatchReferenceImplementationMax1() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = s1.Levenshtein(s2, 1);
                    int d2 = EditDistanceReference.RefLevenshtein(s1, s2, 1);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void LevenshteinShouldMatchReferenceImplementationMax3() {
            var ed = new EditDistance();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = ed.Levenshtein(s1, s2, 3);
                    int d2 = EditDistanceReference.RefLevenshtein(s1, s2, 3);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void StaticLevenshteinShouldMatchReferenceImplementationMax3() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = s1.Levenshtein(s2, 3);
                    int d2 = EditDistanceReference.RefLevenshtein(s1, s2, 3);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void LevenshteinShouldMatchReferenceImplementationMaxHuge() {
            var ed = new EditDistance();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = ed.Levenshtein(s1, s2, int.MaxValue);
                    int d2 = EditDistanceReference.RefLevenshtein(s1, s2, int.MaxValue);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void StaticLevenshteinShouldMatchReferenceImplementationMaxHuge() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = s1.Levenshtein(s2, int.MaxValue);
                    int d2 = EditDistanceReference.RefLevenshtein(s1, s2, int.MaxValue);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void LevenshteinShouldHandleNulls() {
            var ed = new EditDistance();
            var actual = ed.Levenshtein(null, null);
            Assert.AreEqual(0, actual);
            actual = ed.Levenshtein(null, "hi");
            Assert.AreEqual(2, actual);
            actual = ed.Levenshtein("hi", null);
            Assert.AreEqual(2, actual);
        }

        [TestMethod]
        public void StaticLevenshteinShouldHandleNulls() {
            var actual = ((string)null).Levenshtein(null);
            Assert.AreEqual(0, actual);
            actual = ((string)null).Levenshtein("hi");
            Assert.AreEqual(2, actual);
            actual = "hi".Levenshtein(null);
            Assert.AreEqual(2, actual);
        }

        [TestMethod]
        public void LevenshteinShouldBeCommutative() {
            var ed = new EditDistance();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = ed.Levenshtein(s1, s2);
                    int d2 = ed.Levenshtein(s2, s1);
                    Assert.AreEqual(d1, d2);
                    d1 = ed.Levenshtein(s1, s2, 2);
                    d2 = ed.Levenshtein(s2, s1, 2);
                    Assert.AreEqual(d1, d2);
                }
            }
        }

        [TestMethod]
        public void StaticLevenshteinShouldBeCommutative() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = s1.Levenshtein(s2);
                    int d2 = s2.Levenshtein(s1);
                    Assert.AreEqual(d1, d2);
                    d1 = s1.Levenshtein(s2, 2);
                    d2 = s2.Levenshtein(s1, 2);
                    Assert.AreEqual(d1, d2);
                }
            }
        }

        [TestMethod]
        public void DamLevShouldMatchReferenceImplementationNoMax() {
            var ed = new EditDistance();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = ed.DamLev(s1, s2);
                    int d2 = EditDistanceReference.RefDamLev(s1, s2);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void StaticDamLevShouldMatchReferenceImplementationNoMax() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = s1.DamLev(s2);
                    int d2 = EditDistanceReference.RefDamLev(s1, s2);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void DamLevShouldMatchReferenceImplementationMax0() {
            var ed = new EditDistance();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = ed.DamLev(s1, s2, 0);
                    int d2 = EditDistanceReference.RefDamLev(s1, s2, 0);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void StaticDamLevShouldMatchReferenceImplementationMax0() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = s1.DamLev(s2, 0);
                    int d2 = EditDistanceReference.RefDamLev(s1, s2, 0);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void DamLevShouldMatchReferenceImplementationMax1() {
            var ed = new EditDistance();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = ed.DamLev(s1, s2, 1);
                    int d2 = EditDistanceReference.RefDamLev(s1, s2, 1);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void StaticDamLevShouldMatchReferenceImplementationMax1() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = s1.DamLev(s2, 1);
                    int d2 = EditDistanceReference.RefDamLev(s1, s2, 1);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void DamLevShouldMatchReferenceImplementationMax3() {
            var ed = new EditDistance();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = ed.DamLev(s1, s2, 3);
                    int d2 = EditDistanceReference.RefDamLev(s1, s2, 3);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void StaticDamLevShouldMatchReferenceImplementationMax3() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = s1.DamLev(s2, 3);
                    int d2 = EditDistanceReference.RefDamLev(s1, s2, 3);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void DamLevShouldMatchReferenceImplementationMaxHuge() {
            var ed = new EditDistance();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = ed.DamLev(s1, s2, int.MaxValue);
                    int d2 = EditDistanceReference.RefDamLev(s1, s2, int.MaxValue);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void StaticDamLevShouldMatchReferenceImplementationMaxHuge() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = s1.DamLev(s2, int.MaxValue);
                    int d2 = EditDistanceReference.RefDamLev(s1, s2, int.MaxValue);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void DamLevShouldHandleNulls() {
            var ed = new EditDistance();
            var actual = ed.DamLev(null, null);
            Assert.AreEqual(0, actual);
            actual = ed.DamLev(null, "hi");
            Assert.AreEqual(2, actual);
            actual = ed.DamLev("hi", null);
            Assert.AreEqual(2, actual);
        }

        [TestMethod]
        public void StaticDamLevShouldHandleNulls() {
            var actual = ((string)null).DamLev(null);
            Assert.AreEqual(0, actual);
            actual = ((string)null).DamLev("hi");
            Assert.AreEqual(2, actual);
            actual = "hi".DamLev(null);
            Assert.AreEqual(2, actual);
        }

        [TestMethod]
        public void DamLevShouldBeCommutative() {
            var ed = new EditDistance();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = ed.DamLev(s1, s2);
                    int d2 = ed.DamLev(s2, s1);
                    Assert.AreEqual(d1, d2);
                    d1 = ed.DamLev(s1, s2, 2);
                    d2 = ed.DamLev(s2, s1, 2);
                    Assert.AreEqual(d1, d2);
                }
            }
        }

        [TestMethod]
        public void StaticDamLevShouldBeCommutative() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = s1.DamLev(s2);
                    int d2 = s2.DamLev(s1);
                    Assert.AreEqual(d1, d2);
                    d1 = s1.DamLev(s2, 2);
                    d2 = s2.DamLev(s1, 2);
                    Assert.AreEqual(d1, d2);
                }
            }
        }

        private static List<string> BuildTestStrings(int minLength, int maxLength) {
            var strings = new List<string>(500);
            if (minLength == 0) strings.Add("");
            BuildStrings("", minLength, maxLength, strings);
            return strings;
        }
        private static void BuildStrings(string s, int minLength, int maxLength, List<string> strings) {
            const string alphabet = "abcd";
            foreach (var c in alphabet) {
                var s2 = s + c;
                if (s2.Length >= minLength) strings.Add(s2);
                if (s2.Length < maxLength) BuildStrings(s2, minLength, maxLength, strings);
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
