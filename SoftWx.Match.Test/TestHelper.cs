// Copyright ©2015-2026 SoftWx, Inc.
// Released under the MIT License the text of which appears at the end of this file.
// <authors> Steve Hatchett

using System;
using System.Collections.Generic;
using System.Text;

namespace SoftWx.Match.Test {
    internal static class TestHelper {
        public static List<string> BuildTestStrings(int minLength, int maxLength) {
            var strings = new List<string>(500);
            if (minLength == 0) strings.Add(string.Empty);
            BuildStrings(string.Empty, minLength, maxLength, strings);
            return strings;
        }

        /// <summary>
        /// Builds a deterministic set of longer string pairs (lengths 5..32 plus buffer-tier boundary pairs
        /// around 265 and 525 chars) exercising multi-row diagonal-band sliding, band-edge transpositions,
        /// SIMD common-prefix stripping, and ArrayPool/maxCachedLength transitions.
        /// </summary>
        public static List<(string S1, string S2)> BuildLongerTestPairs() {
            const string alphabet = "abcd";
            var rng = new Random(42);
            var pairs = new List<(string, string)>(220);

            // 1. Mutated pairs (lengths 6..32) with 1..6 edits (sub, ins, del, adjacent transposition)
            for (int i = 0; i < 120; i++) {
                int len = rng.Next(6, 33);
                string s1 = RandomString(rng, alphabet, len);
                int edits = rng.Next(1, 7);
                string s2 = ApplyRandomEdits(rng, alphabet, s1, edits);
                pairs.Add((s1, s2));
            }

            // 2. Pairs with long common prefixes (8..32 chars for SIMD CommonPrefixLength) and/or suffixes
            for (int i = 0; i < 60; i++) {
                string prefix = RandomString(rng, alphabet, rng.Next(8, 33));
                string suffix = (i % 2 == 0) ? RandomString(rng, alphabet, rng.Next(4, 17)) : string.Empty;
                string mid1 = RandomString(rng, alphabet, rng.Next(4, 18));
                string mid2 = ApplyRandomEdits(rng, alphabet, mid1, rng.Next(1, 6));
                pairs.Add((prefix + mid1 + suffix, prefix + mid2 + suffix));
            }

            // 3. Independent random pairs (lengths 5..32)
            for (int i = 0; i < 40; i++) {
                string s1 = RandomString(rng, alphabet, rng.Next(5, 33));
                string s2 = RandomString(rng, alphabet, rng.Next(5, 33));
                pairs.Add((s1, s2));
            }

            // 4. Buffer-tier boundary pairs (> 256 StackAlloc/TLS threshold and > 512 DefaultMaxCachedLength)
            int[] longLengths = [265, 270, 520, 530];
            foreach (int len in longLengths) {
                string baseStr = RandomString(rng, alphabet, len);
                pairs.Add((baseStr, ApplyRandomEdits(rng, alphabet, baseStr, 3)));
                string prefix = RandomString(rng, alphabet, 24);
                string core = RandomString(rng, alphabet, len);
                pairs.Add((prefix + core, prefix + ApplyRandomEdits(rng, alphabet, core, 4)));
            }

            return pairs;
        }

        private static string RandomString(Random rng, string alphabet, int length) {
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++) {
                sb.Append(alphabet[rng.Next(alphabet.Length)]);
            }
            return sb.ToString();
        }

        private static string ApplyRandomEdits(Random rng, string alphabet, string source, int editCount) {
            var chars = new List<char>(source);
            for (int e = 0; e < editCount; e++) {
                int op = rng.Next(4);
                if (op == 0 && chars.Count > 0) {
                    // Substitution
                    int idx = rng.Next(chars.Count);
                    chars[idx] = alphabet[rng.Next(alphabet.Length)];
                } else if (op == 1) {
                    // Insertion
                    int idx = rng.Next(chars.Count + 1);
                    chars.Insert(idx, alphabet[rng.Next(alphabet.Length)]);
                } else if (op == 2 && chars.Count > 1) {
                    // Deletion
                    int idx = rng.Next(chars.Count);
                    chars.RemoveAt(idx);
                } else if (chars.Count >= 2) {
                    // Adjacent transposition
                    int idx = rng.Next(chars.Count - 1);
                    (chars[idx], chars[idx + 1]) = (chars[idx + 1], chars[idx]);
                }
            }
            return new string(chars.ToArray());
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
