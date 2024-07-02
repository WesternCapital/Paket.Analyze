# Paket Analyze


## Motivation

I want to know
- what packages have no references in a paket.references file
- what packages are neither directly references in a paket.references nor required transitively

Possible methods
- Find what transitively wants a package even if that package is also specified at the top level (find-refs-with-transitive?). This helps us identify when a package might have been specified to pin a transitive version versus it's now just unused

It'd be nice to know what packages aren't actually reference in a project, but I'm not sure there's a clear way to determine that.
If nothing else it's a much harder problem

Q: Should this project be renamed to paket-trim?


This augments existing paket functionality. If you're lookin
- `paket find-refs` to find which projects a package is used in
- `paket why` to list packages what packages are causing a transitive reference


## Exploration

Looks like there's a couple different approaches to finding references
- `Paket.FindReferences.FindReferencesForPackage` i'd guess backs the `find-refs` command
  - this only returns a project list. I can't really customize it much 
  - ALT: the Dependencies type has `FindProjectsFor` and `FindReferencesFor` 
- `PaketEnv.locatePaketRootDirectory` plus `PaketEnv.fromRootDirectory` allows me to get a PaketEnv, which seems to include object models for operating on all the core paket files
  - ALT: `Paket.Dependencies.Locate` seems to bundle this process. It returns a Dependencies type, but you can then call `.GetLockFile`
- The lock file has convenient methods for creating dependency lookup tables
  - this includes flattened or direct (`GetAllNormalizedDependenciesOf`, `GetDirectDependenciesOfSafe`)
	- 
	
Q: How do I list packages in the dependencies file?
- `Dependencies.GetDirectDependencies` appears to be the play. It also works per reference file
- There's also a `GetDirectDependenciesForPackage`
- Many of these methods have a `Show` variant

Paket.DependencyModel `CalcDependenciesForDirectPackages`, `CalcDependenciesForReferencesFile`

There is a `Simplify` command that returns the simplest dependency graph. I'm guessing that powers `paket simplify`. I wonder if it trims unneeded refs
- I also notice there's a `paket install --only-referenced` that doesn't install any packages not references in a paket.references file, but I don't think it cleans the dependencies file

At this point I definitely have the tools to detect unreferenced packages. I could get a list of packages with `GetDirectDependencies` then find files with `FindReferencesFor` or `FindProjectsFor`. 
Alternatively, I could just iterate over the reference files and build a hashset of packages then diff it with the top-level package list (this would potentially translate better for transitive extensions)

Q: Can I get all ReferencesFiles without constructing the PaketEnv?
- Q: How does PaketEnv get the reference files? 
  - A: RestoreProcess.findAllReferencesFiles
- Q: How would Dependencies.GetReferencesFor do it?
  - Q: Where is the Dependencies type?
    - A: src > PublicAPI.fs'
    - Oh. I should probably work through the Dependencies type. It seems to be the official public API
  - A: It uses FindReferences.FindReferencesForPackage, 
- Q: How does FindReferencesForPackage get the list of projects?
  - A: `this.Process`
    - Dependencies.Process just takes a function, ensures the PaketEnv can be created, and then passes the env to that function 
    - It then iterates over reference files and does a set lookup against `lockFile.GetPackageHullSafe(referencesFile,groupName)`
- PROBLEN: `Process` is a private method...
  - it's pretty simple. I should be able to just recreate it 

PROBLEM: GetPackageHull returns all dependencies, but I just want direct for now

Q: How does Dependencies.GetDirectDependencies do it?
- [It's more complicated than I expected](https://github.com/fsprojects/Paket/blob/d6fee2407c91a84ef16c39a92cdcfc758e9f25f7/src/Paket.Core/PublicAPI.fs#L564)

## Transitive reference support 

It's still not clear how I would approach finding projects where a file is referenced transitively.
If there's a method that creates a flattened list of all depdendencies for a project or reference file, then I could use the dictionary approach.
This dictionary could be used both to ask "What package have no references at all?" and "What projects is the package used in directly or transitively?"

Q: is there a method that returns all dependencies of a project?
- `ProjectFile.GetPackageReferences` might work, attainable via paketEnv.Projects. Need to see what it contains
- I could chain listing the deps in a reference file with `dependencies.GetDirectDependencies refFile` then iterate over the packages and call `lockFile.GetAllDependenciesOf`
- Looks like `lockFile.GetPackageHull` goes directly from a ref file to a list of packages

I don't think the install model is what i'm looking for. Seems more like methods for installing an individual package than a tree of packages to install


## Testing

Q: how do I test this project?
- The most representative signal is probably to set up an example project in the repository and point paket at it
  - testing errors will be a bit hard. I'll need to set up malformed projects. The problem is that to interpret the errors, I'll need to return them instead of relying on Paket's exceptions to print errors. So, I'll need to find out if any of their error formatters are available separately 

Q: Is there an a composable way to turn domain errors into strings?
- A: [yes, DomainMessage has an overloaded .ToString](https://github.com/fsprojects/Paket/blob/d6fee2407c91a84ef16c39a92cdcfc758e9f25f7/src/Paket.Core/Common/Domain.fs#L151)
