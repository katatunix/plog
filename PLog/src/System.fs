module System

open System
open System.Threading

let isWindows = Environment.OSVersion.Platform <> PlatformID.Unix

let splitLines (str: string) =
    str.Replace("\r\n", "\n").Split([|"\n"|], StringSplitOptions.RemoveEmptyEntries)

let startThread f =
    (Thread (ThreadStart f)).Start ()

let parseInt (str: string) =
    match Int32.TryParse str with
    | true, x -> Some x
    | _ -> None
