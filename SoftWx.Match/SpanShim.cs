// Copyright © 2015-2026 Steve Hatchett
// SPDX-License-Identifier: MIT
// Released under the MIT License the text of which appears at the end of this file.
// <authors> Steve Hatchett

#if !NET8_0_OR_GREATER
using System;
using System.Runtime.CompilerServices;

namespace SoftWx.Match {
    // Lightweight netstandard2.0 stand-in for System.ReadOnlySpan<char> backed by a string slice.
    // On .NET 8+, CharSpan is a per-file using alias for System.ReadOnlySpan<char> (zero overhead).
    internal readonly struct CharSpan {
        private readonly string str;
        private readonly int start;

        public int Length {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        public static CharSpan Empty {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new CharSpan(string.Empty, 0, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CharSpan(string str) {
            this.str = str;
            this.start = 0;
            this.Length = str.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private CharSpan(string str, int start, int length) {
            this.str = str;
            this.start = start;
            this.Length = length;
        }

        public char this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.str[this.start + index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CharSpan Slice(int start) {
#if DEBUG
            if ((uint)start > (uint)this.Length) throw new ArgumentOutOfRangeException(nameof(start));
#endif
            return new CharSpan(this.str, this.start + start, this.Length - start);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CharSpan Slice(int start, int length) {
#if DEBUG
            if ((uint)start > (uint)this.Length || (uint)length > (uint)(this.Length - start)) {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
#endif
            return new CharSpan(this.str, this.start + start, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SequenceEqual(CharSpan other) {
            if (this.Length != other.Length) return false;
            if (this.Length == 0) return true;
            if (ReferenceEquals(this.str, other.str) && this.start == other.start) return true;
            if (this.start == 0 && other.start == 0
                && this.Length == this.str.Length && other.Length == other.str.Length) {
                return string.Equals(this.str, other.str, StringComparison.Ordinal);
            }
            return string.CompareOrdinal(this.str, this.start, other.str, other.start, this.Length) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(char value) {
            if (this.Length == 0) return -1;
            int idx = this.str.IndexOf(value, this.start, this.Length);
            return (idx < 0) ? -1 : idx - this.start;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CommonPrefixLength(CharSpan other) {
            int max = (this.Length < other.Length) ? this.Length : other.Length;
            string s1 = this.str;
            string s2 = other.str;
            int o1 = this.start;
            int o2 = other.start;
            int i = 0;
            if (o1 == 0 && o2 == 0) {
                while (i < max && s1[i] == s2[i]) i++;
            } else {
                while (i < max && s1[o1 + i] == s2[o2 + i]) i++;
            }
            return i;
        }
    }

    // Lightweight netstandard2.0 stand-in for System.Span<int> backed by an int[] starting at index 0.
    // Single 8-byte field so it passes in a CPU register on x64 with zero overhead vs raw int[].
    // On .NET 8+, IntSpan is a per-file using alias for System.Span<int> (zero overhead).
    internal readonly struct IntSpan {
        private readonly int[] array;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IntSpan(int[] array) {
            this.array = array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator IntSpan(int[] array) => new IntSpan(array);

        public int this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.array[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => this.array[index] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntSpan Slice(int start, int length) {
            if (start != 0) ThrowInvalidSliceStart(start);
#if DEBUG
            if ((uint)length > (uint)this.array.Length) throw new ArgumentOutOfRangeException(nameof(length));
#endif
            return this;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidSliceStart(int start) {
            throw new ArgumentOutOfRangeException(nameof(start), start, "IntSpan shim only supports slicing from index 0.");
        }
    }

    internal static class SpanShimExtensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CharSpan AsSpan(this string value) => new CharSpan(value);
    }
}
#endif
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

