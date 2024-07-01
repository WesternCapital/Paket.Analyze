open Paket
open Paket.Domain
open Chessie.ErrorHandling

module Trial =
    let defaultWith errorHandler (result: Chessie.ErrorHandling.Result<'TSuccess, 'TError>) = 
        match result with
        | Ok (a,_) -> a
        | Bad errorList -> errorHandler errorList


[<EntryPoint>]
let main _ =
    let paketEnv = 
        PaketEnv.locatePaketRootDirectory (System.IO.DirectoryInfo "C:/Dev/")
        |> Option.defaultWith (fun _ -> invalidOp "Cannot locate paket root")
        |> PaketEnv.fromRootDirectory
        |> Trial.defaultWith (fun _ -> invalidOp "Couldn't resolve paket env")


    let references = FindReferences.FindReferencesForPackage (GroupName "Current") (PackageName "Figgle") paketEnv
    
    let printReferences (projectList : ProjectFile list) =
        printfn "ref list: "
        for p in projectList do
            printfn "%s" p.FileName
            

    // TODO: I need to poke around for code that maps domain messages to console errors
    references
    |> Trial.defaultWith (fun domainErrors -> failwithf "Stuff went wrong during reference discovery: %A" domainErrors)
    |> printReferences
    printfn "Done"
    0
// For more information see https://aka.ms/fsharp-console-apps
