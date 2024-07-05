# Paket Analyze


This tool answers questions about your paket dependency graph.

Specifically
- What packages are referenced in my paket.dependencies but not in any of my paket.references?
- What packages are not referenced by any of my projects (directly or transitively)

## How to Install

Paket.Analyze is a dotnet tool. So you can install it via the dotnet cli

```pwsh
dotnet nuget add source "https://nuget.pkg.github.com/WesternCapital/index.json" --name WesternCapital --username "%WCRI_GITHUB_PACKAGES_USER%" --password "%WCRI_GITHUB_PACKAGES_PASSWORD%" --store-password-in-clear-text

dotnet tool install WesternCapital.paket-analyze --global 
```

Note that despite the `--store-password-in-clear-text` argument, the password will not be stored in the nuget file. 
WCRI_GITHUB_PACKAGES_USER and WCRI_GITHUB_PACKAGES_PASSWORD are environment variables. 
Passing them as plain text is requied for [nuget.config environment variable support](https://learn.microsoft.com/en-us/nuget/reference/nuget-config-file#nugetconfig-environment-variable-support)

These environment variables should contain
- WCRI_GITHUB_PACKAGES_USER: Your company Github username
- WCRI_GITHUB_PACKAGES_PASSWORD: A github personal access token with package access permissions


## Commands

### list-unreferenced

List all packages in the paket.dependencies that don't show up in any paket.references. Only counts packages directly listed a paket.references by default
```pwsh
paket-analyze list-unreferenced
```

But, you can also specify `--include-transitive` to count transitive dependencies when deciding if a package is used by a paket.references. 
This is useful to find packages that are completely unused.

```pwsh
paket-analyze list-unreferenced --include-transitive
```

By default, the command with find the packet hierarchy that your current working directory belongs to.
If you want to analyze a paket dependency tree somewhere else, specify `--paket-root`.

```pwsh
paket-analyze list-unreferenced --paket-root path/to/other/paket-directory
```

You'll probably point `--paket-root` to the directory where the paket.dependencies file is, but you can point it to any folder in the paket hierarchy.
