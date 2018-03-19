# SoftWx.Match
#### C# library to support text and person name fuzzy matching
Available as a nuget package [SoftWx.Match](https://www.nuget.org/packages/SoftWx.Match/)

This project started as a place to put code related to a series of blog posts I made on optimizing the Levenshtein and Damerau-Levenshtein algorithms:

[Optimizing Levenshtein Algorithm in C#](http://blog.softwx.net/2014/12/optimizing-levenshtein-algorithm-in-c.html)

[Optimizing Damerau-Levenshtein in C#](http://blog.softwx.net/2015/01/optimizing-damerau-levenshtein_15.html)

Beginning with version 2, I'll be fleshing out the library with additional functionality. Most of this comes out of work I've done over the past several years working on high confidence matching of person data. A primary goal of the library is to provide methods that are optimized for speed and memory efficiency. To the best of my knowledge, the Levenshtein and Damerau-Levenshtein function implementations in this libary are among the faster and more memory efficient ones available, and for non-trivial strings, possibly the fastest.

In many cases, functionality is exposed as methods in instantiable classes, and also as static class methods.

Immediate future plans are first to add similarity functions for Levenshtein and Damerau-Levenshtein. After that, I'll be adding some functionality specifically for person name matching, including a novel phonetic algorithm.

## Distance
This includes classes that implement an IDistance interface by exposing methods for computing the distance between two strings.
### Levenshtein
This class provides methods that compute the Levenshtein edit distance.
```
// instantiated class version
var ed = new Levenshtein();
var dist = ed.Distance("flintstone", "hanson");
// you can also specify the max distance you care about, which can result in significant speed improvement
var dist = ed.Distance("flintstone", "hanson", 2);

// static class version
var dist = Distance.Levenshtein("flintstone", "hanson");
dist = Distance.Levenshtein("flintstone", "hanson", 2);
```
### Damerau-Levenshtein OSA
This class provides methods that compute the Damerau-Levenshtein optimal string alignment (OSA) edit distance.
```
// instantiated class version
var ed = new DamerauOSA();
var dist = ed.Distance("flintstone", "hanson");
// you can also specify the max distance you care about, which can result in significant speed improvement
var dist = ed.Distance("flintstone", "hanson", 2);

// static class version
var dist = Distance.DamerauOSA("flintstone", "hanson");
dist = Distance.DamerauOSA("flintstone", "hanson", 2);
```
## Similarity
This includes classes that implement an ISimilarity interface by exposing methods for computing a nromalized measure of the similarity of two strings. Similarity results are a double value from 0 to 1.0, where 0 is no similarity at all, and 1.0 is equivalent strings.
### Levenshtein
This class provides methods that compute a normalized similarity value based on the Levenshtein edit distance between two strings. The formula is similarity = 1 - (edit distance / length of longer string).
```
// instantiated class version
var ed = new Levenshtein();
var sim = ed.Similarity("flintstone", "hanson");
// you can also specify the min similarity you care about, which can result in significant speed improvement
var sim = ed.Similarity("flintstone", "hanson", .75);

// static class version
var sim = Similarity.Levenshtein("flintstone", "hanson");
sim = Similarity.Levenshtein("flintstone", "hanson", .75);
```
#### Release Notes
**v2.0.2** Corrected some comments, added Levenshtein similarity functions, added xml docs
 
**v2.0.1** Fixed error in README, Fixed bug in DEBUG conditional code, added detail to nuspec file

**v2.0.0** Restructured classes to better accomodate future additions

**v1.1.0** Added maxDistance version of Levenshtein, added instantiable class versions, added unit tests, added benchmark project, moved to .Net Standard 1.0 libary project, added nuget package creation

**v1.0.0** initial version containing basic Levenshtein and Damerau-Levenshtein functions from my blog posts	
