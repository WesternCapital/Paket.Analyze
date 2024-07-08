---
date: 2024-07-08
---

## Motivation

Paket.Analyze is currently prints full error stack traces if anything goes wrong with 
parsing paket files.

I'd like this to be more aligned with the way paket prints its error messages.


## Exploration

Q: How does paket manage their error logging?
- They use the same Dependencies.Locate as I'm using
- A: The main method wraps everything in a try-catch that formats error messages


This try catch basically just parses arguments, decides how much logging the user wants,
then runs the command.

The error printing is reusable in Paket.Logging
```fsharp
with
| exn when not (exn :? System.NullReferenceException) ->
    Environment.ExitCode <- 1
    traceErrorfn "Paket failed with"
    if Environment.GetEnvironmentVariable "PAKET_DETAILED_ERRORS" = "true" then
        printErrorExt true true true exn
    else printError exn
```

PROBLEM: Nothing is being logged, why?
- It looks like they're using events under the hood to allow multiple subscribers (and probably to manage parallelism?)
- A: Console logging is not subscribed by default, I have to call `use consoleTrace = Logging.event.Publish |> Observable.subscribe Logging.traceToConsole`



## Future

It'd be easy to support silent, verbose, and log to file.
Their reusable methods already handle it, I just would need to define the parameters for the user to specify

The relevant code from the paket main
```fsharp
let silent = results.Contains Silent
tracePaketVersion silent

if results.Contains Verbose then
    Logging.verbose <- true
    Logging.verboseWarnings <- true

let version = results.Contains Version
if not version then

    use fileTrace =
        match results.TryGetResult Log_File with
        | Some lf -> setLogFile lf
        | None -> null

    handleCommand silent (results.GetSubCommand())
```