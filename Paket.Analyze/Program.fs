open Paket
open Paket.Domain
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
    let Process (dependencies: Dependencies) f =
            dependencies |> rootDirectory |> PaketEnv.fromRootDirectory
            >>= f
            |> returnOrFail

[<EntryPoint>]
let main _ =
    // let targetDirectory = "C:/Dev/"
    // let targetGroup = GroupName "Current"
    let targetDirectory = "X:/source/Paket.Analyze"
    let targetGroup = Paket.Constants.MainDependencyGroup

    let deps = Paket.Dependencies.Locate targetDirectory
    
    let ListAllDependenciesForReferencesFiles groupName environment = trial {
        let! lockFile = environment |> PaketEnv.ensureLockFileExists
        
        let listAllDepsForReferencesFile groupName (refFile: ReferencesFile) = 
            lockFile.GetPackageHullSafe (refFile, groupName)

        return! 
            environment.Projects
            |> List.map (snd >> listAllDepsForReferencesFile groupName)
            |> Trial.collect
            |> Trial.lift (List.collect Set.toList)
    }

    let ListDirectDependenciesForReferencesFiles groupName environment = trial {
        let! lockFile = environment |> PaketEnv.ensureLockFileExists

        let listDepsForReferencesFile refFile =
            deps.GetDirectDependencies(refFile) 
        
        return
            environment.Projects
            |> List.collect (snd >> listDepsForReferencesFile)
            |> List.map packageNameFromTriple
    }

    let rootDependencies = deps.GetDirectDependencies()
    
    let collectedDirectReferences =
        ListDirectDependenciesForReferencesFiles targetGroup |> Deps.Process deps

    printfn "Root deps: "

    // rootDependencies
    collectedDirectReferences
    |> List.map (printfn "%A")
    |> ignore

    printfn "Done"
    0
