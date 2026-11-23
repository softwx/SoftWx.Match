// Copyright ©2015-2026 SoftWx, Inc.
// Released under the MIT License the text of which appears at the end of this file.
// <authors> Steve Hatchett

using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace SoftWx.Match.Benchmark {
    public static class Program {
        public static void Main(string[] args) {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }

        internal static List<string> RandomStrings(int minLen, int maxLen, int count, byte alphabetSize, int seed = 1) {
            const string alphabet = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            if (alphabetSize > alphabet.Length) alphabetSize = (byte)alphabet.Length;
            var r = new Random(seed);
            var builder = new StringBuilder(maxLen);
            var list = new List<string>(count);
            while (list.Count < count) {
                builder.Clear();
                int len = r.Next(minLen, maxLen + 1);
                while (builder.Length < len) {
                    builder.Append(alphabet[r.Next(alphabetSize)]);
                }
                list.Add(builder.ToString());
            }
            return list;
        }

        /// <summary>Creates a string within edit distance 2 of the input (2 edits: one near start, one near end)
        /// so common prefix/suffix stripping does not trivially collapse the comparison.</summary>
        internal static string MutateWithTwoEdits(string source, int variant) {
            char[] chars = source.ToCharArray();
            int len = chars.Length;
            // First edit near the start (substitution or adjacent transposition)
            if ((variant & 1) == 0 || len < 4) {
                chars[0] = (chars[0] == 'z') ? 'a' : (char)(chars[0] + 1);
            } else {
                // Swap positions 0 and 1 (if identical, substitute position 0)
                if (chars[0] != chars[1]) {
                    (chars[0], chars[1]) = (chars[1], chars[0]);
                } else {
                    chars[0] = (chars[0] == 'z') ? 'a' : (char)(chars[0] + 1);
                }
            }
            // Second edit near the end (substitution)
            int endIdx = len - 1;
            chars[endIdx] = (chars[endIdx] == 'z') ? 'a' : (char)(chars[endIdx] + 1);
            return new string(chars);
        }
    }

    public enum PairKind {
        Similar,
        Dissimilar
    }

    [MemoryDiagnoser]
    public class LevenshteinDistanceBenchmarks {
        private const int PairCount = 16;
        private string[] left = null!;
        private string[] right = null!;
        private Levenshtein levenshtein = null!;

        [Params(6, 25, 1024)]
        public int StringLength { get; set; }

        [Params(PairKind.Similar, PairKind.Dissimilar)]
        public PairKind Kind { get; set; }

        [GlobalSetup]
        public void Setup() {
            var baseStrings = Program.RandomStrings(this.StringLength, this.StringLength, PairCount * 2, 24, seed: 1);
            this.left = new string[PairCount];
            this.right = new string[PairCount];
            for (int i = 0; i < PairCount; i++) {
                this.left[i] = baseStrings[i];
                this.right[i] = this.Kind == PairKind.Similar
                    ? Program.MutateWithTwoEdits(baseStrings[i], i)
                    : baseStrings[PairCount + i];
            }
            this.levenshtein = new Levenshtein(this.StringLength);
        }

        [Benchmark(OperationsPerInvoke = PairCount)]
        public double InstanceUnbounded() {
            double sum = 0;
            for (int i = 0; i < PairCount; i++) {
                sum += this.levenshtein.Distance(this.left[i], this.right[i]);
            }
            return sum;
        }

        [Benchmark(OperationsPerInvoke = PairCount)]
        public int StaticUnbounded() {
            int sum = 0;
            for (int i = 0; i < PairCount; i++) {
                sum += Distance.Levenshtein(this.left[i], this.right[i]);
            }
            return sum;
        }

        [Benchmark(OperationsPerInvoke = PairCount)]
        public double InstanceMax2() {
            double sum = 0;
            for (int i = 0; i < PairCount; i++) {
                sum += this.levenshtein.Distance(this.left[i], this.right[i], 2);
            }
            return sum;
        }

        [Benchmark(OperationsPerInvoke = PairCount)]
        public int StaticMax2() {
            int sum = 0;
            for (int i = 0; i < PairCount; i++) {
                sum += Distance.Levenshtein(this.left[i], this.right[i], 2);
            }
            return sum;
        }
    }

    [MemoryDiagnoser]
    public class DamerauOSADistanceBenchmarks {
        private const int PairCount = 16;
        private string[] left = null!;
        private string[] right = null!;
        private DamerauOSA damerau = null!;

        [Params(6, 25, 1024)]
        public int StringLength { get; set; }

        [Params(PairKind.Similar, PairKind.Dissimilar)]
        public PairKind Kind { get; set; }

        [GlobalSetup]
        public void Setup() {
            var baseStrings = Program.RandomStrings(this.StringLength, this.StringLength, PairCount * 2, 24, seed: 1);
            this.left = new string[PairCount];
            this.right = new string[PairCount];
            for (int i = 0; i < PairCount; i++) {
                this.left[i] = baseStrings[i];
                this.right[i] = this.Kind == PairKind.Similar
                    ? Program.MutateWithTwoEdits(baseStrings[i], i)
                    : baseStrings[PairCount + i];
            }
            this.damerau = new DamerauOSA(this.StringLength);
        }

        [Benchmark(OperationsPerInvoke = PairCount)]
        public double InstanceUnbounded() {
            double sum = 0;
            for (int i = 0; i < PairCount; i++) {
                sum += this.damerau.Distance(this.left[i], this.right[i]);
            }
            return sum;
        }

        [Benchmark(OperationsPerInvoke = PairCount)]
        public int StaticUnbounded() {
            int sum = 0;
            for (int i = 0; i < PairCount; i++) {
                sum += Distance.DamerauOSA(this.left[i], this.right[i]);
            }
            return sum;
        }

        [Benchmark(OperationsPerInvoke = PairCount)]
        public double InstanceMax2() {
            double sum = 0;
            for (int i = 0; i < PairCount; i++) {
                sum += this.damerau.Distance(this.left[i], this.right[i], 2);
            }
            return sum;
        }

        [Benchmark(OperationsPerInvoke = PairCount)]
        public int StaticMax2() {
            int sum = 0;
            for (int i = 0; i < PairCount; i++) {
                sum += Distance.DamerauOSA(this.left[i], this.right[i], 2);
            }
            return sum;
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
