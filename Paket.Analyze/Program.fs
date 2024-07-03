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


[<EntryPoint>]
let main _ =
    let targetDirectory = "C:/Dev/"
    let targetGroup = GroupName "Current"
    // let targetDirectory = "X:/source/Paket.Analyze"
    // let targetGroup = Paket.Constants.MainDependencyGroup

    let deps = Paket.Dependencies.Locate targetDirectory
    
    

    let rootDependencies = 
        deps.GetDirectDependencies() 
        |> List.map packageNameFromTriple 
        |> Set.ofList
    
    let listPackagesForRefFile refFile =
        deps.GetDirectDependencies(refFile)
        |> List.map packageNameFromTriple

    let collectedDirectReferences =
        Deps.ListReferencesFiles deps
        |> List.collect listPackagesForRefFile
        |> Set.ofList


    let unreferencedPackages = Set.difference rootDependencies collectedDirectReferences

    if unreferencedPackages |> Set.isEmpty then
        printfn "Every package in the paket.dependencies is referenced in a paket.references"
    else
        printfn "paket.dependencies packages not found in any paket.references:"

        unreferencedPackages
        |> Set.iter (printfn "  %s")

    0
