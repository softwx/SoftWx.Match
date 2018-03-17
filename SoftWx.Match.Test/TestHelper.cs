// Copyright ©2015-2018 SoftWx, Inc.
// Released under the MIT License the text of which appears at the end of this file.
// <authors> Steve Hatchett

using System.Collections.Generic;

namespace SoftWx.Match.Test {
    internal class TestHelper {
        public static List<string> BuildTestStrings(int minLength, int maxLength) {
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
