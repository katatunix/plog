[<AutoOpen>]
module Common

open System

module OS =
    let isWindows = Environment.OSVersion.Platform <> PlatformID.Unix

module String =
    let splitLines (str: string) =
        str.Replace("\r\n", "\n").Split([|"\n"|], StringSplitOptions.RemoveEmptyEntries)

module Int =
    let parse (str: string) =
        match Int32.TryParse str with
        | true, x -> Some x
        | _ -> None
