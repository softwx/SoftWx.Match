# SoftWx.Match

[![NuGet](https://img.shields.io/nuget/v/SoftWx.Match.svg)](https://www.nuget.org/packages/SoftWx.Match/)
[![CI](https://github.com/softwx/SoftWx.Match/actions/workflows/ci.yml/badge.svg)](https://github.com/softwx/SoftWx.Match/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

C# library to support text and person name fuzzy matching, targeting `.NET Standard 2.0`, `.NET 8`, and `.NET 10` (with nullable annotations, Source Link, and NativeAOT/trimming compatibility).

## Installation

```bash
dotnet add package SoftWx.Match
```

## Overview

This project started as a place to publish code from a series of blog posts on optimizing the Levenshtein and Damerau-Levenshtein algorithms:

- [Optimizing Levenshtein Algorithm in C#](https://blog.softwx.net/2014/12/optimizing-levenshtein-algorithm-in-c.html)
- [Optimizing Damerau-Levenshtein in C#](https://blog.softwx.net/2015/01/optimizing-damerau-levenshtein_15.html)

A primary goal of the library is to provide string distance and similarity methods optimized for speed and memory efficiency. Functionality is exposed both as instantiable classes (reusing internal buffers up to 512 chars by default, or a caller-specified `expectedMaxStringLength`; not thread-safe) and as thread-safe static methods.

SoftWx.Match is a 100% safe managed C# implementation of Wagner-Fischer Levenshtein and Damerau-Levenshtein OSA — ideal for short strings (person names, words, identifiers) and bounded `maxDistance` / `minSimilarity` filtering, with zero steady-state allocations on `.NET 8+`. For unbounded edit distance on long strings (or bulk 1-to-many ASCII searches against a fixed query), bit-parallel algorithms such as Myers/Hyyrö are asymptotically faster.

## Distance

Classes implementing `IDistance` compute the edit distance between two strings (`0` for equivalent strings, positive values as difference increases, or `-1` when exceeding `maxDistance`). Comparisons are case-sensitive (e.g. `"Fred"` vs. `"fred"` has distance `1`).

### Levenshtein

Computes the Levenshtein edit distance (insertions, deletions, and substitutions) between two strings or character spans.

```csharp
using SoftWx.Match;

// Instantiated class version (reuses internal buffer; not thread-safe)
// Optional: pass expectedMaxStringLength to pre-size and raise the retained buffer cap
var ed = new Levenshtein();
double dist = ed.Distance("flintstone", "hanson");        // 6
// Specify a maxDistance for significant speed improvements
dist = ed.Distance("flintstone", "hanson", 2);            // -1 (exceeds maxDistance)
// ReadOnlySpan<char> overloads (.NET 8+) avoid substring allocations
dist = ed.Distance("fred flintstone".AsSpan(5), "hanson".AsSpan()); // 6

// Thread-safe static (and string / ReadOnlySpan<char> extension) methods
int staticDist = Distance.Levenshtein("flintstone", "hanson");      // 6
staticDist = Distance.Levenshtein("flintstone", "hanson", 2);       // -1
staticDist = "flintstone".Levenshtein("hanson");                    // 6
staticDist = "fred flintstone".AsSpan(5).Levenshtein("hanson".AsSpan(), 2); // -1 (.NET 8+)
```

### Damerau-Levenshtein OSA

Computes the Damerau-Levenshtein optimal string alignment (OSA) edit distance, which also counts an adjacent character transposition (such as `"to"` → `"ot"`) as a single edit.

```csharp
using SoftWx.Match;

// Instantiated class version (reuses internal buffers; not thread-safe)
var ed = new DamerauOSA();
double dist = ed.Distance("flintstone", "hanson");        // 6
dist = ed.Distance("flintstone", "flintsotne");           // 1 (adjacent transposition)
// Specify a maxDistance for significant speed improvements
dist = ed.Distance("flintstone", "hanson", 2);            // -1
// ReadOnlySpan<char> overloads (.NET 8+) avoid substring allocations
dist = ed.Distance("fred flintstone".AsSpan(5), "flintsotne".AsSpan(), 2); // 1

// Thread-safe static (and string / ReadOnlySpan<char> extension) methods
int staticDist = Distance.DamerauOSA("flintstone", "hanson");       // 6
staticDist = Distance.DamerauOSA("flintstone", "flintsotne", 2);    // 1
staticDist = "flintstone".DamerauOSA("flintsotne");                 // 1
staticDist = "fred flintstone".AsSpan(5).DamerauOSA("flintsotne".AsSpan()); // 1 (.NET 8+)
```

## Similarity

Classes implementing `ISimilarity` compute a normalized measure of similarity between two strings from `0.0` (no similarity) to `1.0` (equivalent strings), or `-1` when below `minSimilarity`. The similarity formula is `1 - (edit distance / length of longer string)`.

### Levenshtein

```csharp
using SoftWx.Match;

// Instantiated class version (not thread-safe)
var ed = new Levenshtein();
double sim = ed.Similarity("flintstone", "hanson");       // 0.4
// Specify a minSimilarity for significant speed improvements
sim = ed.Similarity("flintstone", "hanson", 0.75);        // -1 (below minSimilarity)
sim = ed.Similarity("fred flintstone".AsSpan(5), "hanson".AsSpan()); // 0.4 (.NET 8+)

// Thread-safe static methods
sim = Similarity.Levenshtein("flintstone", "hanson");     // 0.4
sim = Similarity.Levenshtein("flintstone", "hanson", 0.75); // -1
sim = Similarity.Levenshtein("fred flintstone".AsSpan(5), "hanson".AsSpan(), 0.4); // 0.4 (.NET 8+)
```

### Damerau-Levenshtein OSA

```csharp
using SoftWx.Match;

// Instantiated class version (not thread-safe)
var ed = new DamerauOSA();
double sim = ed.Similarity("flintstone", "flintsotne");   // 0.9
// Specify a minSimilarity for significant speed improvements
sim = ed.Similarity("flintstone", "flintsotne", 0.75);    // 0.9
sim = ed.Similarity("fred flintstone".AsSpan(5), "flintsotne".AsSpan()); // 0.9 (.NET 8+)

// Thread-safe static methods
sim = Similarity.DamerauOSA("flintstone", "flintsotne");  // 0.9
sim = Similarity.DamerauOSA("flintstone", "hanson", 0.75); // -1
sim = Similarity.DamerauOSA("fred flintstone".AsSpan(5), "flintsotne".AsSpan(), 0.75); // 0.9 (.NET 8+)
```

## Benchmarks

Benchmarks use [BenchmarkDotNet](https://benchmarkdotnet.org/) and can be run with:

```bash
dotnet run --project SoftWx.Match.Benchmark -c Release
```

## Release Notes

**v3.0.2**
- Added an internal `netstandard2.0` span shim (`CharSpan`/`IntSpan`, zero-overhead aliases for `ReadOnlySpan<char>`/`Span<int>` on `.NET 8+`) to unify core DP algorithms and helper paths across target frameworks
- Consolidated duplicated fast-path, `Similarity`, and static facade logic, cutting library code size by ~28% and eliminating mid-method `#if` directives while preserving `.NET 8+` performance
- Minor internal code cleanup and benchmark setup deduplication

**v3.0.1**
- Updated copyright attribution to Steve Hatchett, removed inactive company metadata, and added `SPDX-License-Identifier: MIT` headers
- Updated blog links to HTTPS
- Added consumer-level NativeAOT publish and execution smoke test in CI/Release

**v3.0.0**
- Multi-targeted `netstandard2.0`, `net8.0`, and `net10.0` (dropped obsolete `netstandard1.0`)
- Enabled nullable reference types, trimming/NativeAOT compatibility, .NET analyzers, and built-in Source Link with `.snupkg` symbols
- Added `ReadOnlySpan<char>` overloads (`.NET 8+`), zero-allocation static methods (`stackalloc`/`ArrayPool` on `.NET 8+`, capped thread-local cache on `netstandard2.0`), capped instance buffer retention, lazy `maxDistance` initialization, SIMD `CommonPrefixLength`, and `O(1)` fast paths
- Fixed `Similarity` returning `1.0` when one string was a prefix or suffix of the other (e.g. `""` vs `"abc"`, `"a"` vs `"ab"`)
- Fixed inverted `null` handling in `Similarity.DamerauOSA` and `Helpers.NullSimilarityResults`
- Fixed fractional and `NaN` `maxDistance` handling in `Levenshtein.Distance` and `DamerauOSA.Distance`
- Added missing `ISimilarity` interface declaration to `DamerauOSA`
- Migrated unit tests to SDK-style MSTest and benchmarks to BenchmarkDotNet; added GitHub Actions CI/Release workflows

**v2.0.3** Added DamerauOSA similarity functions, minor speed improvements to Levenshtein distance functions

**v2.0.2** Corrected some comments, added Levenshtein similarity functions, added XML docs

**v2.0.1** Fixed error in README, fixed bug in DEBUG conditional code, added detail to nuspec file

**v2.0.0** Restructured classes to better accommodate future additions

**v1.1.0** Added maxDistance version of Levenshtein, added instantiable class versions, added unit tests, added benchmark project, moved to .NET Standard 1.0 library project, added NuGet package creation

**v1.0.0** Initial version containing basic Levenshtein and Damerau-Levenshtein functions from blog posts
