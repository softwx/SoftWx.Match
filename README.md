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

- [Optimizing Levenshtein Algorithm in C#](http://blog.softwx.net/2014/12/optimizing-levenshtein-algorithm-in-c.html)
- [Optimizing Damerau-Levenshtein in C#](http://blog.softwx.net/2015/01/optimizing-damerau-levenshtein_15.html)

A primary goal of the library is to provide string distance and similarity methods that are optimized for speed and memory efficiency. Functionality is exposed both as instantiable classes (reusing internal buffers for minimal allocations; not thread-safe) and as thread-safe static methods.

## Distance

Classes implementing `IDistance` compute the edit distance between two strings (`0` for equivalent strings, positive values as difference increases, or `-1` when exceeding `maxDistance`).

### Levenshtein

Computes the Levenshtein edit distance between two strings.

```csharp
using SoftWx.Match;

// Instantiated class version (reuses internal buffer; not thread-safe)
var ed = new Levenshtein();
double dist = ed.Distance("flintstone", "hanson");
// Specify a maxDistance for significant speed improvements
dist = ed.Distance("flintstone", "hanson", 2);

// Thread-safe static (and string extension) methods
int staticDist = Distance.Levenshtein("flintstone", "hanson");
staticDist = Distance.Levenshtein("flintstone", "hanson", 2);
staticDist = "flintstone".Levenshtein("hanson");
```

### Damerau-Levenshtein OSA

Computes the Damerau-Levenshtein optimal string alignment (OSA) edit distance between two strings.

```csharp
using SoftWx.Match;

// Instantiated class version (reuses internal buffers; not thread-safe)
var ed = new DamerauOSA();
double dist = ed.Distance("flintstone", "hanson");
// Specify a maxDistance for significant speed improvements
dist = ed.Distance("flintstone", "hanson", 2);

// Thread-safe static (and string / ReadOnlySpan<char> extension) methods
int staticDist = Distance.DamerauOSA("flintstone", "hanson");
staticDist = Distance.DamerauOSA("flintstone", "hanson", 2);
staticDist = "flintstone".DamerauOSA("hanson");
staticDist = "flintstone".AsSpan(0, 5).DamerauOSA("hanson".AsSpan()); // .NET 8+ zero-alloc
```

## Similarity

Classes implementing `ISimilarity` compute a normalized measure of similarity between two strings from `0.0` (no similarity) to `1.0` (equivalent strings), or `-1` when below `minSimilarity`. The similarity formula is `1 - (edit distance / length of longer string)`.

### Levenshtein

```csharp
using SoftWx.Match;

// Instantiated class version
var ed = new Levenshtein();
double sim = ed.Similarity("flintstone", "hanson");
// Specify a minSimilarity for significant speed improvements
sim = ed.Similarity("flintstone", "hanson", 0.75);

// Thread-safe static methods
sim = Similarity.Levenshtein("flintstone", "hanson");
sim = Similarity.Levenshtein("flintstone", "hanson", 0.75);
```

### Damerau-Levenshtein OSA

```csharp
using SoftWx.Match;

// Instantiated class version
var ed = new DamerauOSA();
double sim = ed.Similarity("flintstone", "hanson");
// Specify a minSimilarity for significant speed improvements
sim = ed.Similarity("flintstone", "hanson", 0.75);

// Thread-safe static methods
sim = Similarity.DamerauOSA("flintstone", "hanson");
sim = Similarity.DamerauOSA("flintstone", "hanson", 0.75);
```

## Benchmarks

Benchmarks use [BenchmarkDotNet](https://benchmarkdotnet.org/) and can be run with:

```bash
dotnet run --project SoftWx.Match.Benchmark -c Release
```

## Release Notes

**v3.0.0**
- Multi-targeted `netstandard2.0`, `net8.0`, and `net10.0` (dropped obsolete `netstandard1.0`)
- Enabled nullable reference types, trimming/NativeAOT compatibility, .NET analyzers, and built-in Source Link with `.snupkg` symbols
- Added `ReadOnlySpan<char>` overloads (`.NET 8+`), zero-allocation static methods (`stackalloc`/`ArrayPool`), lazy `maxDistance` initialization, SIMD `CommonPrefixLength`, and `O(1)` fast paths
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
