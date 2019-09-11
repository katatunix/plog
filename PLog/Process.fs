module Process

open System
open System.Text
open System.Collections.Generic

type StartInfo = {
    ShowWindow : bool
    RedirectStdErr : bool
}

type Action = {
    Kill : unit -> unit
    Wait : unit -> unit
}

let mkProcess program args info =
    let p = new Diagnostics.Process ()
    p.StartInfo.FileName <- program
    p.StartInfo.Arguments <- args
    p.StartInfo.UseShellExecute <- false
    p.StartInfo.CreateNoWindow <- not info.ShowWindow
    p.StartInfo.RedirectStandardOutput <- true
    p.StartInfo.RedirectStandardError <- info.RedirectStdErr
    p.StartInfo.RedirectStandardInput <- true
    p.StartInfo.WorkingDirectory <- AppDomain.CurrentDomain.BaseDirectory
    p.StartInfo.StandardOutputEncoding <- System.Text.Encoding.UTF8
    if info.RedirectStdErr then p.StartInfo.StandardErrorEncoding <- System.Text.Encoding.UTF8
    p

let private startProcess (p : Diagnostics.Process) =
    if p.Start () |> not then raise <| Exception ()

let private error program = Error <| sprintf "Could not start %s" program

let start program args info onLineReceived onExited =
    try
        let p = mkProcess program args info

        p.OutputDataReceived.Add (fun x ->
            if isNull x.Data then onExited () else onLineReceived x.Data
        )

        if info.RedirectStdErr then
            p.ErrorDataReceived.Add (fun x ->
                if not (isNull x.Data) then onLineReceived x.Data
            )

        startProcess p

        p.StandardInput.Close ()
        p.BeginOutputReadLine ()
        if info.RedirectStdErr then
            p.BeginErrorReadLine ()

        Ok {
            Kill = fun _ -> p.CancelOutputRead ()
                            if info.RedirectStdErr then
                                p.CancelErrorRead ()
                            try p.Kill () with _ -> ()
            Wait = p.WaitForExit
        }
    with _ ->
        error program

let run program args info =
    let lines = List<string> ()
    start program args info lines.Add ignore
    |> Result.map (fun action ->
        action.Wait ()
        lines :> seq<string>
    )

let runFull program args info =
    let sb = StringBuilder ()
    start program args info (sb.AppendLine >> ignore) ignore
    |> Result.map (fun action ->
        action.Wait ()
        sb.ToString ()
    )

let getOutputStream program args consume =
    try
        let p = mkProcess program args { ShowWindow = false; RedirectStdErr = false }
        startProcess p
        consume p.StandardOutput.BaseStream
    with _ ->
        error program
