# Paket Analyze


## Motivation

I want to know
- what packages have no references in a paket.references file
- what packages are neither directly references in a paket.references nor required transitively

Possible methods
- Find what transitively wants a package even if it's at the top level

It'd be nice to know what packages aren't actually reference in a project, but I'm not sure there's a clear way to determine that.
If nothing else it's a much harder problem

Q: Should this project be renamed to paket-trim?


This augments existing paket functionality. If you're lookin
- `paket find-refs` to find which projects a package is used in
- `paket why` to list packages what packages are causing a transitive reference
