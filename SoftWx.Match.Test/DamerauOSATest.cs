// Copyright ©2015-2018 SoftWx, Inc.
// Released under the MIT License the text of which appears at the end of this file.
// <authors> Steve Hatchett

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoftWx.Match;
using System.Collections.Generic;

namespace SoftWx.Match.Test {
    [TestClass]
    public class DamerauOSATest {
        private static List<string> testStrings;

        static DamerauOSATest() {
            testStrings = TestHelper.BuildTestStrings(0, 4);
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
        public void DamerauOSAShouldMatchReferenceImplementationMax0() {
            var ed = new DamerauOSA();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = (int)ed.Distance(s1, s2, 0);
                    int d2 = EditDistanceReference.RefDamerauOSA(s1, s2, 0);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void StaticDamerauOSAShouldMatchReferenceImplementationMax0() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = Distance.DamerauOSA(s1, s2, 0);
                    int d2 = EditDistanceReference.RefDamerauOSA(s1, s2, 0);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void DamerauOSAShouldMatchReferenceImplementationMax1() {
            var ed = new DamerauOSA();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = (int)ed.Distance(s1, s2, 1);
                    int d2 = EditDistanceReference.RefDamerauOSA(s1, s2, 1);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void StaticDamerauOSAShouldMatchReferenceImplementationMax1() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = Distance.DamerauOSA(s1, s2, 1);
                    int d2 = EditDistanceReference.RefDamerauOSA(s1, s2, 1);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void DamerauOSAShouldMatchReferenceImplementationMax3() {
            var ed = new DamerauOSA();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = (int)ed.Distance(s1, s2, 3);
                    int d2 = EditDistanceReference.RefDamerauOSA(s1, s2, 3);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void StaticDamerauOSAShouldMatchReferenceImplementationMax3() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = Distance.DamerauOSA(s1, s2, 3);
                    int d2 = EditDistanceReference.RefDamerauOSA(s1, s2, 3);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void DamerauOSAShouldMatchReferenceImplementationMaxHuge() {
            var ed = new DamerauOSA();
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = (int)ed.Distance(s1, s2, int.MaxValue);
                    int d2 = EditDistanceReference.RefDamerauOSA(s1, s2, int.MaxValue);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void StaticDamerauOSAShouldMatchReferenceImplementationMaxHuge() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = Distance.DamerauOSA(s1, s2, int.MaxValue);
                    int d2 = EditDistanceReference.RefDamerauOSA(s1, s2, int.MaxValue);
                    Assert.AreEqual(d2, d1);
                }
            }
        }

        [TestMethod]
        public void DamerauOSAShouldHandleNulls() {
            var ed = new DamerauOSA();
            var actual = ed.Distance(null, null);
            Assert.AreEqual(0, actual);
            actual = ed.Distance(null, "hi");
            Assert.AreEqual(2, actual);
            actual = ed.Distance("hi", null);
            Assert.AreEqual(2, actual);
        }

        [TestMethod]
        public void StaticDamerauOSAShouldHandleNulls() {
            var actual = Distance.DamerauOSA(null, null);
            Assert.AreEqual(0, actual);
            actual = Distance.DamerauOSA(null, "hi");
            Assert.AreEqual(2, actual);
            actual = Distance.DamerauOSA("hi", null);
            Assert.AreEqual(2, actual);
        }

        [TestMethod]
        public void DamerauOSAShouldBeCommutative() {
            var ed = new DamerauOSA();
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
        public void StaticDamerauOSAShouldBeCommutative() {
            foreach (var s1 in testStrings) {
                foreach (var s2 in testStrings) {
                    int d1 = Distance.DamerauOSA(s1, s2);
                    int d2 = Distance.DamerauOSA(s2, s1);
                    Assert.AreEqual(d1, d2);
                    d1 = Distance.DamerauOSA(s1, s2, 2);
                    d2 = Distance.DamerauOSA(s2, s1, 2);
                    Assert.AreEqual(d1, d2);
                }
            }
        }

        [TestMethod]
        public void StaticDamerauOSAShouldBeThreadsafe() {
            System.Threading.Tasks.Parallel.For(0, testStrings.Count, i => {
                foreach (var s2 in testStrings) {
                    int d1 = Distance.DamerauOSA(testStrings[i], s2);
                    int d2 = EditDistanceReference.RefDamerauOSA(testStrings[i], s2);
                    Assert.AreEqual(d2, d1);
                }
            });
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
