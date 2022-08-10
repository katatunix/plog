module PLog.Domain

open System
open System.IO
open System.Text.RegularExpressions
open System.Collections.Generic
open System.Threading
open FsToolkit.ErrorHandling

type Device =
    { Model: string option
      Serial: string }

type Severity = Info | Debug | Warning | Err

type LogItem =
    { Content: string
      Severity: Severity
      Tag: string option
      Pid: int option }

type FilterInfo =
    { Tag: string option
      Pid: int option }

type Filter =
    { Name: string
      Info: FilterInfo }

type Config =
    { Adb: string
      DsymFile: string
      Filters: Filter list
      Negative: FilterInfo []
      IsDark: bool
      MaxLogLines: int }

type CrashLogSource =
    | Live of seq<string>
    | File of string

let private parseDeviceSerial (line: string) =
    match line.IndexOf "\t" with
    | -1 -> None
    | i -> line.Substring(0, i).Trim() |> Some

let private parseDeviceModel (line: string) =
    if line.StartsWith "[ro.product.model]" then
        match line.IndexOf ":" with
        | -1 -> None
        | i -> line.Substring(i + 1).Trim() |> Some
    else
        None

let fetchDevices adb = result {
    let! lines = Process.run adb "devices" { ShowWindow = true; RedirectStdErr = false }
    return
        lines
        |> Seq.choose parseDeviceSerial
        |> Seq.map (fun serial ->
            let model =
                Process.run
                    adb
                    (sprintf "-s %s shell getprop" serial)
                    { ShowWindow = false; RedirectStdErr = false }
                |> Option.ofResult
                |> Option.bind (Seq.tryPick parseDeviceModel)
            { Model = model; Serial = serial }
        )
        |> Array.ofSeq
}

let private parseLogItem1 (str: string) =
    let severity =
        if str.StartsWith "E/" then Err
        elif str.StartsWith "W/" then Warning
        elif str.StartsWith "D/" then Debug
        else Info
    let mutable tag = None
    let mutable pid = None
    let i = str.IndexOf '/'
    if i > -1 then
        let j = str.IndexOf '('
        if j > -1 then
            tag <- str.Substring(i + 1, j - i - 1).Trim() |> Some
            let k = str.IndexOf (')', j)
            if k > -1 then
                pid <- str.Substring(j + 1, k - j - 1) |> Int.parse
    { Content = str
      Severity = severity
      Tag = tag
      Pid = pid }

let private parseLogItem2 (str: string) =
    let mutable severity = Info
    let mutable tag = None
    let mutable pid = None
    if str.Length > 20 then
        let i = str.IndexOf (':', 20)
        if i > -1 then
            let p = str.Substring(0, i).Split([| ' '; '\t' |], StringSplitOptions.RemoveEmptyEntries)
            if p.Length >= 5 then
                pid <- Int.parse p[2]
                severity <- match p[4] with
                            | "E" -> Err
                            | "W" -> Warning
                            | "D" -> Debug
                            | _ -> Info
            if p.Length >= 6 then
                tag <- Some p[5]
    { Content = str
      Severity = severity
      Tag = tag
      Pid = pid }

let parseLogItem (str : string) =
    if str.Length < 2 || str[1] = '/'
    then parseLogItem1 str
    else parseLogItem2 str

let mainFilter =
    { Name = "MAIN"
      Info = { Tag = None; Pid = None } }

let connectDevice adb device onItemsReceived onExited (key: 'key) =
    let args =
        match device with
        | None -> "logcat"
        | Some dev -> sprintf "-s %s logcat" dev.Serial

    let cache = List ()

    let addToCache (line: string) =
        if line.Length > 0 then
            lock cache (fun _ -> cache.Add line)

    let agent = MailboxProcessor.Start <| fun inbox ->
        let rec loop () = async {
            let! msg = inbox.TryReceive 100
            match msg with
            | Some () -> ()
            | None ->
                lock cache <| fun _ ->
                    if cache.Count > 0 then
                        onItemsReceived key (cache |> Seq.map parseLogItem)
                        cache.Clear ()
                return! loop ()
        }
        loop ()

    Process.start adb args { ShowWindow = false; RedirectStdErr = true } addToCache (agent.Post >> onExited)
    |> Result.map (fun action -> { action with Kill = agent.Post >> action.Kill})

let logcatClear adb device =
    let args =
        match device with
        | None -> "logcat -c"
        | Some dev -> sprintf "-s %s logcat -c" dev.Serial

    let mre0 = new ManualResetEvent (false)
    let mre1 = new ManualResetEvent (false)

    Process.start
        adb
        args
        { ShowWindow = false; RedirectStdErr = true }
        (fun line -> if line = "- waiting for device -" then mre0.Set () |> ignore)
        (mre1.Set >> ignore)

    |> Result.bind (fun action ->
        let handles: WaitHandle [] = [| mre0; mre1 |]
        let idx = WaitHandle.WaitAny (handles, 2000)

        if idx <> 1 then
            action.Kill ()
            Error "Could not clear log for the current device. \
                   Make sure the device is available and its log is not being consumed by any adb client (even PLog)."
        else
            Ok ()
    )

let matchesFilter (info: FilterInfo) (logItem: LogItem) =
    let tagOk =
        match info.Tag with
        | None -> true
        | _ -> logItem.Tag = info.Tag
    let pidOk =
        match info.Pid with
        | None -> true
        | _ -> logItem.Pid = info.Pid
    tagOk && pidOk

let private toOption str =
    if String.IsNullOrWhiteSpace str
    then None
    else Some str

let createFilter (name: string) (tag: string) (pid: string) = result {
    let! name =  name |> toOption |> Result.requireSome "Filter name cannot be empty."
    let tag = tag |> toOption
    let pid = pid |> toOption
    match tag, pid with
    | None, None ->
        return! Error "At least Tag or PID must be provided."
    | _, None ->
        return { Name = name; Info = { Tag = tag; Pid = None } }
    | _, Some pid ->
        let! pid = pid |> Int.parse |> Result.requireSome "PID must be an integer number."
        return { Name = name; Info = { Tag = tag; Pid = Some pid } }
}

let private (+/) p1 p2 = Path.Combine (p1, p2)

let private addr2line =
    if OS.isWindows then
        AppDomain.CurrentDomain.BaseDirectory +/ "addr2line.exe"
    else
        AppDomain.CurrentDomain.BaseDirectory +/ "../Resources" +/ "addr2line"

let private parseAddresses lines = [
    for line in lines do
        let m = Regex.Match (line, @"#\d+\s+pc\s+(\w+)\s")
        if m.Success then
            yield m.Groups[1].Value
]

let private getStacktraceFromLive dsymFile lines =
    let addresses = parseAddresses lines |> String.concat " "
    if addresses.Length = 0 then
        Ok ""
    else
        Process.runFull addr2line (sprintf "-pCafe \"%s\" %s" dsymFile addresses)
        <| { ShowWindow = false; RedirectStdErr = false }

let readLogFile path =
    try
        let tenMB_in_bytes = 10485760L
        if FileInfo(path).Length <= tenMB_in_bytes then
            IO.File.ReadAllLines path |> Ok
        else
            Error "Log file size cannot be bigger than 10 MB."
    with ex ->
        Error ex.Message

let private getStacktraceFromFile dsymFile logFile =
    readLogFile logFile
    |> Result.bind (getStacktraceFromLive dsymFile)

let getStacktrace dsymFile source =
    match source with
    | Live lines -> getStacktraceFromLive dsymFile lines
    | File file -> getStacktraceFromFile dsymFile file

let captureScreenshot adb device =
    let devicePrefix =
        match device with
        | None -> ""
        | Some dev -> sprintf "-s %s " dev.Serial
    Process.getOutputStream adb (sprintf "%sexec-out screencap -p" devicePrefix)

let private dataFolder = (Environment.GetFolderPath Environment.SpecialFolder.UserProfile) +/ ".plog"
let private configFile = dataFolder +/ "config.dat"

let saveConfig (config: Config) =
    Directory.CreateDirectory dataFolder |> ignore
    IO.writeValueToFile config configFile

let loadConfig () =
    let defaultMaxLogLines = 40000
    try
        let config = IO.readValueFromFile configFile
        if config.MaxLogLines = 0
        then { config with MaxLogLines = defaultMaxLogLines }
        else config
    with _ ->
        let defaultConfig = { Adb = "adb"
                              DsymFile = ""
                              Filters = []
                              Negative = [||]
                              IsDark = true
                              MaxLogLines = defaultMaxLogLines }
        defaultConfig
