using System;

namespace SoftWx.Match.Test {
    /// <summary>
    /// Literal implementation of algorithm as described in Wikipedia entry for
    /// Levenshtein edit distance, with trivial addition of max value parameter.
    /// </summary>
    internal static class EditDistanceReference {
        public static int RefLevenshtein(string s, string t, int maxDistance = int.MaxValue) {
            if (maxDistance < 0) maxDistance = 0;
            var d = new int[s.Length + 1, t.Length + 1];
            for (int i = 0; i <= s.Length; i++) d[i, 0] = i;
            for (int i = 0; i <= t.Length; i++) d[0, i] = i;
            for (int j = 1; j <= t.Length; j++)
                for (int i = 1; i <= s.Length; i++)
                    if (s[i - 1] == t[j - 1])
                        d[i, j] = d[i - 1, j - 1];  //no operation
                    else
                        d[i, j] = Math.Min(Math.Min(
                            d[i - 1, j] + 1,    //a deletion
                            d[i, j - 1] + 1),   //an insertion
                            d[i - 1, j - 1] + 1 //a substitution
                            );
            var distance = d[s.Length, t.Length];
            return (distance <= maxDistance) ? distance : -1;
        }
        public static int RefDamerauOSA(string s, string t, int maxDistance = int.MaxValue) {
            if (maxDistance < 0) maxDistance = 0;
            int cost;
            var d = new int[s.Length + 1, t.Length + 1];
            for (int i = 0; i <= s.Length; i++) d[i, 0] = i;
            for (int i = 0; i <= t.Length; i++) d[0, i] = i;
            for (int i = 1; i <= s.Length; i++) {
                for (int j = 1; j <= t.Length; j++) {
                    cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(
                        d[i - 1, j] + 1,    //a deletion
                        d[i, j - 1] + 1),   //an insertion
                        d[i - 1, j - 1] + cost //a substitution
                        );
                    if (i > 1 && j > 1 && s[i - 1] == t[j - 2] && s[i - 2] == t[j - 1]) {
                        d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + cost);
                    }
                }
            }
            var distance = d[s.Length, t.Length];
            return (distance <= maxDistance) ? distance : -1;
        }
    }
}
