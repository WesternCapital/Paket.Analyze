open Paket
open Chessie.ErrorHandling

module Trial =
    let defaultWith errorHandler (result: Chessie.ErrorHandling.Result<'TSuccess, 'TError>) = 
        match result with
        | Ok (a,_) -> a
        | Bad errorList -> errorHandler errorList

    let map = Trial.lift


let packageNameFromTriple (_group, packageName, _version) = packageName

module Deps =
    let rootDirectory (deps:Dependencies) = DirectoryInfo(deps.RootPath)
    let Process f (dependencies: Dependencies) =
            dependencies |> rootDirectory |> PaketEnv.fromRootDirectory
            >>= f
            |> returnOrFail

    let ListProjects (deps: Dependencies) = 
        let getProjects (paketEnv:PaketEnv) = trial{
            return paketEnv.Projects
        }
        
        deps |> Process getProjects

    let ListReferencesFiles (deps: Dependencies) =
        deps |> ListProjects|> List.map snd
            
module ReferencesFile =

    let listDirectReferences (deps : Dependencies) (refFile: ReferencesFile) =
        deps.GetDirectDependencies(refFile)
        |> List.map packageNameFromTriple


module LockFile =
    let listAllDepsForGroup (group: LockFileGroup) = 
        group.Resolution.Keys
        |> List.ofSeq
        |> List.map _.Name

// let listAllDependenciesForReferencesFiles (deps:Dependencies) = 
//     let lockFile = deps.GetLockFile()
    
//     let listAllPackages groupName (refFile : ReferencesFile)=
//         lockFile.GetPackageHull (groupName, refFile) 
//         |> _.Keys 
//         |> List.ofSeq
//         |> List.map (snd >> _.Name)

//     let listAllDepsForGroup (group: LockFileGroup) = 
//         group.Resolution.Keys
//         |> List.ofSeq
//         |> List.map _.Name
        

//     return! 
//         environment.Projects
//         |> List.map (snd >> listAllDepsForReferencesFile groupName)
//         |> Trial.collect
//         |> Trial.lift (List.collect Set.toList)



let listAllDependenciesForReferencesFilesSafe environment = trial {
    // this emulates what is used for find-refs
    let! lockFile = environment |> PaketEnv.ensureLockFileExists
    
    let listAllDepsForReferencesFile (refFile: ReferencesFile) = 
        let listAllPackages groupName=
            lockFile.GetPackageHullSafe (refFile, groupName) 

        lockFile.Groups.Keys 
        |> List.ofSeq
        |> List.map listAllPackages
        |> Trial.collect
        |> Trial.lift (Set.unionMany)

    return! 
        environment.Projects
        |> List.map (snd >> listAllDepsForReferencesFile)
        |> Trial.collect
        |> Trial.lift (Set.unionMany)
}


[<EntryPoint>]
let main _ =
    // let targetDirectory = "C:/Dev/"
    let targetDirectory = "X:/source/Paket.Analyze"
    let includeTransitive = false

    let deps = Paket.Dependencies.Locate targetDirectory
    
    let rootDependencies = 
        deps.GetDirectDependencies() 
        |> List.map packageNameFromTriple 
        |> Set.ofList
    

    let getReferencedPackages_DirectOnly (deps: Dependencies) =
        Deps.ListReferencesFiles deps
        |> List.collect (ReferencesFile.listDirectReferences deps)
        |> Set.ofList

    let getReferencedPackages_IncludeTransitive (deps: Dependencies) =
        deps 
        |> Deps.Process listAllDependenciesForReferencesFilesSafe
        |> Set.map _.Name

    let allReferencedPackages =
        match includeTransitive with
        | true -> getReferencedPackages_IncludeTransitive deps
        | false -> getReferencedPackages_DirectOnly deps

    let unreferencedPackages = Set.difference rootDependencies allReferencedPackages

    if unreferencedPackages |> Set.isEmpty then
        printfn "Every package in the paket.dependencies is referenced in a paket.references"
    else
        printfn "paket.dependencies packages not found in any paket.references:"

        unreferencedPackages
        |> Set.iter (printfn "  %s")

    0
