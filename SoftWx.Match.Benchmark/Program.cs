// Copyright ©2015-2018 SoftWx, Inc.
// Released under the MIT License the text of which appears at the end of this file.
// <authors> Steve Hatchett
using SoftWx.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SoftWx.Match.Benchmark {
    // see https://github.com/softwx/SoftWx.Match for project details.
    class Program {
        static void Main(string[] args) {
            var dist = new EditDistance();
            TimeLevenshtein();
            Console.WriteLine();
            TimeDamLev();
            Console.WriteLine();
            Console.WriteLine("timings complete...hit any key to exit");
            Console.ReadKey();
        }
        static void TimeLevenshtein() {
            var bench = new Bench(10, 500, true);
            int val;
            var strings = BuildTestStrings(0, 5);
            bench.Time("Lev 0-5", () => val = RunLevenshtein(strings), strings.Count * strings.Count);
            bench.Time("Lev 0-5 static", () => val = RunLevenshteinStatic(strings), strings.Count * strings.Count);
            bench.Time("Lev 0-5 max huge", () => val = RunLevenshtein(strings, 9999), strings.Count * strings.Count);
            bench.Time("Lev 0-5 max huge static", () => val = RunLevenshteinStatic(strings, 9999), strings.Count * strings.Count);
            bench.Time("Lev 0-5 max 2", () => val = RunLevenshtein(strings, 2), strings.Count * strings.Count);
            Console.WriteLine();
            var words = File.ReadAllLines("wordlist.txt");
            strings = words.Where(w => w.Length >= 15 && w.Length <= 15).ToList();
            bench.Time("Lev 15", () => val = RunLevenshtein(strings), strings.Count * strings.Count);
            bench.Time("Lev 15 static", () => val = RunLevenshteinStatic(strings), strings.Count * strings.Count);
            bench.Time("Lev 15 max huge", () => val = RunLevenshtein(strings, 9999), strings.Count * strings.Count);
            bench.Time("Lev 15 max huge static", () => val = RunLevenshteinStatic(strings, 9999), strings.Count * strings.Count);
            bench.Time("Lev 15 max 14", () => val = RunLevenshtein(strings, 14), strings.Count * strings.Count);
            bench.Time("Lev 15 max 3", () => val = RunLevenshtein(strings, 3), strings.Count * strings.Count);
            bench.Time("Lev 15 static max 3", () => val = RunLevenshteinStatic(strings, 3), strings.Count * strings.Count);
            Console.WriteLine();
            strings = words.Where(w => w.Length >= 20 && w.Length <= 22).ToList();
            bench.Time("Lev 20-22", () => val = RunLevenshtein(strings), strings.Count * strings.Count);
            bench.Time("Lev 20-22 max huge", () => val = RunLevenshtein(strings, int.MaxValue), strings.Count * strings.Count);
            bench.Time("Lev 20-22 max 3", () => val = RunLevenshtein(strings, 3), strings.Count * strings.Count);
            Console.WriteLine();
            strings = words.Where(w => w.Length >= 24).ToList();
            bench.Time("Lev >=24", () => val = RunLevenshtein(strings), strings.Count * strings.Count);
            bench.Time("Lev >=24 max huge", () => val = RunLevenshtein(strings, 9999), strings.Count * strings.Count);
            bench.Time("Lev >=24 max 3", () => val = RunLevenshtein(strings, 3), strings.Count * strings.Count);
            Console.WriteLine();
            strings = words.Where(w => w.Length == 8).Take(500).Union(words.Where(w=>w.Length==20)).ToList();
            bench.Time("Lev 8 & 20", () => val = RunLevenshtein(strings), strings.Count * strings.Count);
            bench.Time("Lev 8 & 20 max 14", () => val = RunLevenshtein(strings, 14), strings.Count * strings.Count);
        }
        static void TimeDamLev() {
            var bench = new Bench(10, 500, true);
            int val;
            var strings = BuildTestStrings(0, 5);
            bench.Time("DamLev 0-5", () => val = RunDamLev(strings), strings.Count * strings.Count);
            bench.Time("DamLev 0-5 static", () => val = RunDamLevStatic(strings), strings.Count * strings.Count);
            bench.Time("DamLev 0-5 max 2", () => val = RunDamLev(strings, 2), strings.Count * strings.Count);
            Console.WriteLine();
            var words = File.ReadAllLines("wordlist.txt");
            strings = words.Where(w => w.Length >= 15 && w.Length <= 15).ToList();
            bench.Time("DamLev 15", () => val = RunDamLev(strings), strings.Count * strings.Count);
            bench.Time("DamLev 15 max 3", () => val = RunDamLev(strings, 3), strings.Count * strings.Count);
            Console.WriteLine();
            strings = words.Where(w => w.Length >= 20 && w.Length <= 22).ToList();
            bench.Time("DamLev 20-22", () => val = RunDamLev(strings), strings.Count * strings.Count);
            Console.WriteLine();
            strings = words.Where(w => w.Length >= 24).ToList();
            bench.Time("DamLev >=24", () => val = RunDamLev(strings), strings.Count * strings.Count);
            bench.Time("DamLev >=24 max 3", () => val = RunDamLev(strings, 3), strings.Count * strings.Count);
            Console.WriteLine();
            strings = words.Where(w => w.Length == 8).Take(500).Union(words.Where(w => w.Length == 20)).ToList();
            bench.Time("DamLev 8 & 20", () => val = RunDamLev(strings), strings.Count * strings.Count);
        }
        static int RunLevenshtein(List<string> strings) {
            int result = 0;
            var dist = new EditDistance();
            for (int i = 0; i < strings.Count; i++) {
                for (int j = 0; j < strings.Count; j++) {
                    result = dist.Levenshtein(strings[i], strings[j]);
                }
            }
            return result;
        }
        static int RunDamLev(List<string> strings) {
            int result = 0;
            var dist = new EditDistance();
            for (int i = 0; i < strings.Count; i++) {
                for (int j = 0; j < strings.Count; j++) {
                    result = dist.DamLev(strings[i], strings[j]);
                }
            }
            return result;
        }
        static int RunLevenshteinStatic(List<string> strings) {
            int result = 0;
            for (int i = 0; i < strings.Count; i++) {
                for (int j = 0; j < strings.Count; j++) {
                    result = EditDistance.StaticLevenshtein(strings[i], strings[j]);
                }
            }
            return result;
        }
        static int RunDamLevStatic(List<string> strings) {
            int result = 0;
            for (int i = 0; i < strings.Count; i++) {
                for (int j = 0; j < strings.Count; j++) {
                    result = EditDistance.StaticDamLev(strings[i], strings[j]);
                }
            }
            return result;
        }
        static int RunLevenshteinStatic(List<string> strings, int maxDistance) {
            int result = 0;
            for (int i = 0; i < strings.Count; i++) {
                for (int j = 0; j < strings.Count; j++) {
                    result = EditDistance.StaticLevenshtein(strings[i], strings[j], maxDistance);
                }
            }
            return result;
        }
        static int RunLevenshtein(List<string> strings, int maxDistance) {
            int result = 0;
            var dist = new EditDistance();
            for (int i = 0; i < strings.Count; i++) {
                for (int j = 0; j < strings.Count; j++) {
                    result = dist.Levenshtein(strings[i], strings[j], maxDistance);
                }
            }
            return result;
        }
        static int RunDamLev(List<string> strings, int maxDistance) {
            int result = 0;
            var dist = new EditDistance();
            for (int i = 0; i < strings.Count; i++) {
                for (int j = 0; j < strings.Count; j++) {
                    result = dist.DamLev(strings[i], strings[j], maxDistance);
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
