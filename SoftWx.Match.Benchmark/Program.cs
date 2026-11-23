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

        internal static List<string> RandomStrings(int minLen, int maxLen, int count, byte alphabetSize) {
            const string alphabet = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            if (alphabetSize > alphabet.Length) alphabetSize = (byte)alphabet.Length;
            var r = new Random(1);
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
    }

    [MemoryDiagnoser]
    public class LevenshteinBenchmarks {
        private List<string> strings = null!;
        private Levenshtein levenshtein = null!;
        private int maxDistance;

        [Params(5, 15, 25, 100)]
        public int StringLength { get; set; }

        [GlobalSetup]
        public void Setup() {
            const int count = 40;
            this.strings = Program.RandomStrings(this.StringLength, this.StringLength, count, 24);
            this.levenshtein = new Levenshtein(this.StringLength);
            this.maxDistance = Math.Max(1, this.StringLength / 5);
        }

        [Benchmark]
        public double DistanceInstance() {
            double result = 0;
            int half = this.strings.Count / 2;
            for (int i = 0; i < half; i++) {
                for (int j = half; j < this.strings.Count; j++) {
                    result += this.levenshtein.Distance(this.strings[i], this.strings[j]);
                }
            }
            return result;
        }

        [Benchmark]
        public int DistanceStatic() {
            int result = 0;
            int half = this.strings.Count / 2;
            for (int i = 0; i < half; i++) {
                for (int j = half; j < this.strings.Count; j++) {
                    result += Distance.Levenshtein(this.strings[i], this.strings[j]);
                }
            }
            return result;
        }

        [Benchmark]
        public double DistanceMaxInstance() {
            double result = 0;
            int half = this.strings.Count / 2;
            for (int i = 0; i < half; i++) {
                for (int j = half; j < this.strings.Count; j++) {
                    result += this.levenshtein.Distance(this.strings[i], this.strings[j], this.maxDistance);
                }
            }
            return result;
        }

        [Benchmark]
        public int DistanceMaxStatic() {
            int result = 0;
            int half = this.strings.Count / 2;
            for (int i = 0; i < half; i++) {
                for (int j = half; j < this.strings.Count; j++) {
                    result += Distance.Levenshtein(this.strings[i], this.strings[j], this.maxDistance);
                }
            }
            return result;
        }

        [Benchmark]
        public double SimilarityInstance() {
            double result = 0;
            int half = this.strings.Count / 2;
            for (int i = 0; i < half; i++) {
                for (int j = half; j < this.strings.Count; j++) {
                    result += this.levenshtein.Similarity(this.strings[i], this.strings[j]);
                }
            }
            return result;
        }

        [Benchmark]
        public double SimilarityStatic() {
            double result = 0;
            int half = this.strings.Count / 2;
            for (int i = 0; i < half; i++) {
                for (int j = half; j < this.strings.Count; j++) {
                    result += Similarity.Levenshtein(this.strings[i], this.strings[j]);
                }
            }
            return result;
        }

        [Benchmark]
        public double SimilarityMinInstance() {
            double result = 0;
            int half = this.strings.Count / 2;
            for (int i = 0; i < half; i++) {
                for (int j = half; j < this.strings.Count; j++) {
                    result += this.levenshtein.Similarity(this.strings[i], this.strings[j], 0.8);
                }
            }
            return result;
        }

        [Benchmark]
        public double SimilarityMinStatic() {
            double result = 0;
            int half = this.strings.Count / 2;
            for (int i = 0; i < half; i++) {
                for (int j = half; j < this.strings.Count; j++) {
                    result += Similarity.Levenshtein(this.strings[i], this.strings[j], 0.8);
                }
            }
            return result;
        }
    }

    [MemoryDiagnoser]
    public class DamerauOSABenchmarks {
        private List<string> strings = null!;
        private DamerauOSA damerau = null!;
        private int maxDistance;

        [Params(5, 15, 25, 100)]
        public int StringLength { get; set; }

        [GlobalSetup]
        public void Setup() {
            const int count = 40;
            this.strings = Program.RandomStrings(this.StringLength, this.StringLength, count, 24);
            this.damerau = new DamerauOSA(this.StringLength);
            this.maxDistance = Math.Max(1, this.StringLength / 5);
        }

        [Benchmark]
        public double DistanceInstance() {
            double result = 0;
            int half = this.strings.Count / 2;
            for (int i = 0; i < half; i++) {
                for (int j = half; j < this.strings.Count; j++) {
                    result += this.damerau.Distance(this.strings[i], this.strings[j]);
                }
            }
            return result;
        }

        [Benchmark]
        public int DistanceStatic() {
            int result = 0;
            int half = this.strings.Count / 2;
            for (int i = 0; i < half; i++) {
                for (int j = half; j < this.strings.Count; j++) {
                    result += Distance.DamerauOSA(this.strings[i], this.strings[j]);
                }
            }
            return result;
        }

        [Benchmark]
        public double DistanceMaxInstance() {
            double result = 0;
            int half = this.strings.Count / 2;
            for (int i = 0; i < half; i++) {
                for (int j = half; j < this.strings.Count; j++) {
                    result += this.damerau.Distance(this.strings[i], this.strings[j], this.maxDistance);
                }
            }
            return result;
        }

        [Benchmark]
        public int DistanceMaxStatic() {
            int result = 0;
            int half = this.strings.Count / 2;
            for (int i = 0; i < half; i++) {
                for (int j = half; j < this.strings.Count; j++) {
                    result += Distance.DamerauOSA(this.strings[i], this.strings[j], this.maxDistance);
                }
            }
            return result;
        }

        [Benchmark]
        public double SimilarityInstance() {
            double result = 0;
            int half = this.strings.Count / 2;
            for (int i = 0; i < half; i++) {
                for (int j = half; j < this.strings.Count; j++) {
                    result += this.damerau.Similarity(this.strings[i], this.strings[j]);
                }
            }
            return result;
        }

        [Benchmark]
        public double SimilarityStatic() {
            double result = 0;
            int half = this.strings.Count / 2;
            for (int i = 0; i < half; i++) {
                for (int j = half; j < this.strings.Count; j++) {
                    result += Similarity.DamerauOSA(this.strings[i], this.strings[j]);
                }
            }
            return result;
        }

        [Benchmark]
        public double SimilarityMinInstance() {
            double result = 0;
            int half = this.strings.Count / 2;
            for (int i = 0; i < half; i++) {
                for (int j = half; j < this.strings.Count; j++) {
                    result += this.damerau.Similarity(this.strings[i], this.strings[j], 0.8);
                }
            }
            return result;
        }

        [Benchmark]
        public double SimilarityMinStatic() {
            double result = 0;
            int half = this.strings.Count / 2;
            for (int i = 0; i < half; i++) {
                for (int j = half; j < this.strings.Count; j++) {
                    result += Similarity.DamerauOSA(this.strings[i], this.strings[j], 0.8);
                }
            }
            return result;
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
