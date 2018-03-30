// Copyright ©2015-2018 SoftWx, Inc.
// Released under the MIT License the text of which appears at the end of this file.
// <authors> Steve Hatchett
using SoftWx.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SoftWx.Match.Benchmark {
    // see https://github.com/softwx/SoftWx.Match for project details.
    class Program {
        static void Main(string[] args) {
#if DEBUG
            Console.WriteLine("** This is a Debug build. Benchmarks should be run with Release builds. **");
#endif
            if (System.Diagnostics.Debugger.IsAttached) {
                Console.WriteLine("** The debugger is attached. Benchmarks should be run without the debugger.");
            }
            var strings = RandomStrings(1000, 1000, 2, 24);
            RunLevDis(strings, 0);
            RunLevSim(strings, 1);
            Console.WriteLine("===============================");
            TimeLevenshtein();
            Console.WriteLine("===============================");
            TimeDamOSA();
            Console.WriteLine("===============================");
            Console.WriteLine("timings complete...hit any key to exit");
            Console.ReadKey();
        }
        static void TimeLevenshtein() {
            var bench = new Bench(20, 1000, false);
            //var words = File.ReadAllLines("wordlist.txt");
            //strings = words.Where(w => w.Length >= 15 && w.Length <= 15).ToList();
            RunLevBenches(bench, 5, 5, 400, 24);
            RunLevBenches(bench, 15, 15, 200, 24);
            RunLevBenches(bench, 25, 25, 200, 24);
            RunLevBenches(bench, 100, 100, 50, 20);
            RunLevBenches(bench, 1000, 1000, 16, 15);
        }
        static void TimeDamOSA() {
            var bench = new Bench(20, 1000, false);
            RunDamOSABenches(bench, 5, 5, 400, 24);
            RunDamOSABenches(bench, 15, 15, 200, 24);
            RunDamOSABenches(bench, 25, 25, 200, 24);
            RunDamOSABenches(bench, 100, 100, 50, 20);
            RunDamOSABenches(bench, 1000, 1000, 16, 15);
        }
        static void Time(Bench bench, string name, Action target, int reps) {
            var res = bench.Time(name, target, reps);
            string nanos = res.NanosecondsPerOperation.ToString("#,##0.0");
            nanos = new string(' ', 12 - nanos.Length) + nanos;
            Console.WriteLine(res.Name + new String('.', 40 - res.Name.Length) + nanos + " nanos/op\r");
        }
        static void RunLevBenches(Bench bench, int minLen, int maxLen, int count, byte alphabetSize) {
            int val; double dval;
            string name = "Lev " + minLen + (minLen == maxLen ? " " : "-"+maxLen+" ");
            var strings = RandomStrings(minLen, maxLen, count, alphabetSize);
            int reps = (count * count)/4;

            Time(bench, name + "dist ", () => val = RunLevDis(strings), reps);
            Time(bench, name + "dist static", () => val = RunLevDisStatic(strings), reps);
            double diff = 1.0;
            int dist = maxLen;
            while(true) {
                Time(bench, name + "dist max " + dist, () => val = RunLevDis(strings, dist), reps);
                Time(bench, name + "dist max " + dist + " static", () => val = RunLevDisStatic(strings, dist), reps);
                if (dist == 1) break;
                diff /= (diff == 1.0) ? 2 : (diff == .5) ? 5 : 10;
                dist = (int)(maxLen * diff);
                if (dist == 0) dist = 1;
            }
            Console.WriteLine();
            Time(bench, name + "sim ", () => dval = RunLevSim(strings), reps);
            Time(bench, name + "sim static", () => dval = RunLevSimStatic(strings), reps);
            diff = 1.0;
            dist = maxLen;
            while (true) {
                double sim = 1 - diff;
                Time(bench, name + "sim max " + sim, () => dval = RunLevSim(strings, sim), reps);
                Time(bench, name + "sim max " + sim + " static", () => dval = RunLevSimStatic(strings, sim), reps);
                if (dist == 1) break;
                diff /= (diff == 1.0) ? 2 : (diff == .5) ? 5 : 10;
                dist = (int)(maxLen * diff);
                if (dist == 0) { diff = 1 / (double)maxLen; dist = (int)(maxLen * diff); }
            }
            Console.WriteLine();
        }
        static void RunDamOSABenches(Bench bench, int minLen, int maxLen, int count, byte alphabetSize) {
            int val; double dval;
            string name = "DamOSA " + minLen + (minLen == maxLen ? " " : "-" + maxLen + " ");
            var strings = RandomStrings(minLen, maxLen, count, alphabetSize);
            int reps = (count * count) / 4;

            Time(bench, name + "dist ", () => val = RunDamOSADis(strings), reps);
            Time(bench, name + "dist static", () => val = RunDamOSADisStatic(strings), reps);
            double diff = 1.0;
            int dist = maxLen;
            while (true) {
                Time(bench, name + "dist max " + dist, () => val = RunDamOSADis(strings, dist), reps);
                Time(bench, name + "dist max " + dist + " static", () => val = RunDamOSADisStatic(strings, dist), reps);
                if (dist == 1) break;
                diff /= (diff == 1.0) ? 2 : (diff == .5) ? 5 : 10;
                dist = (int)(maxLen * diff);
                if (dist == 0) dist = 1;
            }
            Console.WriteLine();
            Time(bench, name + "sim ", () => dval = RunDamOSASim(strings), reps);
            Time(bench, name + "sim static", () => dval = RunDamOSASimStatic(strings), reps);
            diff = 1.0;
            dist = maxLen;
            while (true) {
                double sim = 1 - diff;
                Time(bench, name + "sim max " + sim, () => dval = RunDamOSASim(strings, sim), reps);
                Time(bench, name + "sim max " + sim + " static", () => dval = RunDamOSASimStatic(strings, sim), reps);
                if (dist == 1) break;
                diff /= (diff == 1.0) ? 2 : (diff == .5) ? 5 : 10;
                dist = (int)(maxLen * diff);
                if (dist == 0) { diff = 1 / (double)maxLen; dist = (int)(maxLen * diff); }
            }
            Console.WriteLine();
        }
        static int RunLevDis(List<string> strings) {
            int result = 0;
            var dist = new Levenshtein();
            for (int i = 0; i < strings.Count/2; i++) {
                for (int j = strings.Count/2; j < strings.Count; j++) {
                    result = (int)dist.Distance(strings[i], strings[j]);
                }
            }
            return result;
        }
        static int RunLevDis(List<string> strings, int maxDistance) {
            int result = 0;
            var dist = new Levenshtein();
            for (int i = 0; i < strings.Count / 2; i++) {
                for (int j = strings.Count / 2; j < strings.Count; j++) {
                    result = (int)dist.Distance(strings[i], strings[j], maxDistance);
                }
            }
            return result;
        }
        static double RunLevSim(List<string> strings) {
            double result = 0;
            var dist = new Levenshtein();
            for (int i = 0; i < strings.Count / 2; i++) {
                for (int j = strings.Count / 2; j < strings.Count; j++) {
                    result = (int)dist.Similarity(strings[i], strings[j]);
                }
            }
            return result;
        }
        static double RunLevSim(List<string> strings, double minSimilarity) {
            double result = 0;
            var dist = new Levenshtein();
            for (int i = 0; i < strings.Count / 2; i++) {
                for (int j = strings.Count / 2; j < strings.Count; j++) {
                    result = (int)dist.Similarity(strings[i], strings[j], minSimilarity);
                }
            }
            return result;
        }
        static int RunLevDisStatic(List<string> strings) {
            int result = 0;
            for (int i = 0; i < strings.Count / 2; i++) {
                for (int j = strings.Count / 2; j < strings.Count; j++) {
                    result = Distance.Levenshtein(strings[i], strings[j]);
                }
            }
            return result;
        }
        static int RunLevDisStatic(List<string> strings, int maxDistance) {
            int result = 0;
            for (int i = 0; i < strings.Count / 2; i++) {
                for (int j = strings.Count / 2; j < strings.Count; j++) {
                    result = Distance.Levenshtein(strings[i], strings[j], maxDistance);
                }
            }
            return result;
        }
        static double RunLevSimStatic(List<string> strings) {
            double result = 0;
            for (int i = 0; i < strings.Count / 2; i++) {
                for (int j = strings.Count / 2; j < strings.Count; j++) {
                    result = Similarity.Levenshtein(strings[i], strings[j]);
                }
            }
            return result;
        }
        static double RunLevSimStatic(List<string> strings, double minSimilarity) {
            double result = 0;
            for (int i = 0; i < strings.Count / 2; i++) {
                for (int j = strings.Count / 2; j < strings.Count; j++) {
                    result = Similarity.Levenshtein(strings[i], strings[j], minSimilarity);
                }
            }
            return result;
        }

        static int RunDamOSADis(List<string> strings) {
            int result = 0;
            var dist = new DamerauOSA();
            for (int i = 0; i < strings.Count; i++) {
                for (int j = 0; j < strings.Count; j++) {
                    result = (int)dist.Distance(strings[i], strings[j]);
                }
            }
            return result;
        }
        static int RunDamOSADis(List<string> strings, int maxDistance) {
            int result = 0;
            var dist = new DamerauOSA();
            for (int i = 0; i < strings.Count; i++) {
                for (int j = 0; j < strings.Count; j++) {
                    result = (int)dist.Distance(strings[i], strings[j], maxDistance);
                }
            }
            return result;
        }
        static double RunDamOSASim(List<string> strings) {
            double result = 0;
            var dist = new DamerauOSA();
            for (int i = 0; i < strings.Count; i++) {
                for (int j = 0; j < strings.Count; j++) {
                    result = (int)dist.Similarity(strings[i], strings[j]);
                }
            }
            return result;
        }
        static double RunDamOSASim(List<string> strings, double minSimilarity) {
            double result = 0;
            var dist = new DamerauOSA();
            for (int i = 0; i < strings.Count; i++) {
                for (int j = 0; j < strings.Count; j++) {
                    result = (int)dist.Similarity(strings[i], strings[j], minSimilarity);
                }
            }
            return result;
        }
        static int RunDamOSADisStatic(List<string> strings) {
            int result = 0;
            for (int i = 0; i < strings.Count; i++) {
                for (int j = 0; j < strings.Count; j++) {
                    result = Distance.DamerauOSA(strings[i], strings[j]);
                }
            }
            return result;
        }
        static int RunDamOSADisStatic(List<string> strings, int maxDistance) {
            int result = 0;
            for (int i = 0; i < strings.Count; i++) {
                for (int j = 0; j < strings.Count; j++) {
                    result = Distance.DamerauOSA(strings[i], strings[j], maxDistance);
                }
            }
            return result;
        }
        static double RunDamOSASimStatic(List<string> strings) {
            double result = 0;
            for (int i = 0; i < strings.Count; i++) {
                for (int j = 0; j < strings.Count; j++) {
                    result = Similarity.DamerauOSA(strings[i], strings[j]);
                }
            }
            return result;
        }
        static double RunDamOSASimStatic(List<string> strings, double minSimilarity) {
            double result = 0;
            for (int i = 0; i < strings.Count; i++) {
                for (int j = 0; j < strings.Count; j++) {
                    result = Similarity.DamerauOSA(strings[i], strings[j], minSimilarity);
                }
            }
            return result;
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
        private static List<string> RandomStrings(int minLen, int maxLen, int count, byte alphabetSize) {
            const string alphabet = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            if (alphabetSize > alphabet.Length) alphabetSize = (byte)alphabet.Length;
            Random r = new Random(1);
            var builder = new StringBuilder(maxLen);
            var list = new List<string>(count);
            while(list.Count < count) {
                builder.Clear();
                var len = r.Next(minLen, maxLen + 1);
                while(builder.Length < len) {
                    builder.Append(alphabet[r.Next(alphabetSize)]);
                }
                list.Add(builder.ToString());
            }
            return list;
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
