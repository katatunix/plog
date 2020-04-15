namespace PLog

open System.Collections.Generic
open Domain
    
type MainView =
    abstract ShowError : string -> unit
    abstract UpdateDevices : string [] -> unit
    abstract SelectDevice : deviceIdx:int -> unit
    abstract UpdateConnectButton : string -> unit
    abstract AppendLogItems : pageIdx:int -> seq<string * Severity> -> unit
    abstract Clear : pageIdx:int -> unit
    abstract GoEnd : pageIdx:int -> unit
    abstract AppendFilter : name:string -> unit
    abstract CreateLogArea : isDark:bool -> unit
    abstract ShowLogArea : idx:int -> unit
    abstract SelectFilter : idx:int -> unit
    abstract UpdateFilterLabel : idx:int -> label:string -> unit
    abstract RemoveFilter : idx:int -> unit
    abstract RemoveLogArea : idx:int -> unit
    abstract UpdateDsymFile : dsymFile:string -> unit
    abstract OpenStacktrace : dsymFile:string -> LogSource -> unit
    abstract OpenConfig : string -> seq<FilterInfo> -> unit
    abstract OpenScreenshot : adb:string -> Device -> deviceTitle:string -> unit
    abstract ChangeMode : isDark:bool -> idx:int -> seq<string * Severity> -> unit
    abstract SetModeCheckBox : bool -> unit
    abstract SetWrap : value:bool -> idx:int -> seq<string * Severity> -> unit

type StacktraceOption =
    | LiveLog
    | LogFile of string

type LogPage (filter, logItems) =
    member val Filter : Filter = filter
    member val LogItems : List<LogItem> = logItems
    member val UnreadCount = 0 with get, set
    new (filter) = LogPage (filter, List ())

type MainPresenter (view : MainView, invoke : (unit -> unit) -> unit) =
    static let MAX_LOG_LINES = 40000
    static let CONNECT = "Connect"
    static let DISCONNECT = "Disconnect"

    let mutable adb = ""
    let mutable devices : Device [] = Array.empty
    let mutable connectionId = 0
    let mutable killFun : (unit -> unit) option = None
    let logPages = List<LogPage> ()
    let mutable curPageIdx = 0
    let mutable dsymFile = ""
    let mutable negativeFilterInfos : FilterInfo [] = Array.empty
    let mutable isDark = false

    let formatDevice (device : Device) =
        match device.Model with
        | Some model -> sprintf "%s %s" model device.Serial
        | None -> device.Serial

    let disconnect () =
        match killFun with
        | Some f -> f (); killFun <- None; view.UpdateConnectButton CONNECT
        | None -> ()

    let resetLog () =
        for i = 0 to logPages.Count - 1 do
            let page = logPages.[i]
            page.LogItems.Clear ()
            page.UnreadCount <- 0
            view.Clear i
            view.GoEnd i
            view.UpdateFilterLabel i page.Filter.Name

    let formatFilterLabel name unreadCount =
        sprintf "%s (%d)" name unreadCount

    member this.Init () =
        let config = Domain.loadConfig ()

        adb <- config.Adb
        dsymFile <- config.DsymFile
        negativeFilterInfos <- config.Negative
        isDark <- config.IsDark

        logPages.Add (LogPage (mainFilter))
        for filter in config.Filters do
            logPages.Add (LogPage filter)

        for page in logPages do
            view.AppendFilter page.Filter.Name
            view.CreateLogArea isDark

        view.SelectFilter 0
        view.ShowLogArea 0
        view.UpdateDsymFile dsymFile
        view.UpdateConnectButton CONNECT
        view.SetModeCheckBox isDark

        this.RefreshDevices ()

    member this.RefreshDevices () =
        disconnect ()
        resetLog ()
        match Domain.fetchDevices adb with
        | Error text ->
            view.ShowError text
        | Ok _devices ->
            devices <- _devices
            view.UpdateDevices (devices |> Array.map formatDevice)
            if devices.Length > 0 then view.SelectDevice 0

    member this.ToggleConnect deviceIdx =
        match killFun with
        | None ->
            if devices.Length = 0 then
                view.ShowError "No device selected."
            else
                resetLog ()
                connectionId <- connectionId + 1
                Domain.connectDevice
                    adb
                    (Some devices.[deviceIdx])
                    (fun connId items -> invoke (fun _ -> this.LogItemsReceived connId items))
                    (fun _ -> invoke (fun _ -> this.Disconnected ()))
                    connectionId
                |> function
                | Error text ->
                    view.ShowError text
                | Ok action ->
                    killFun <- Some action.Kill
                    view.UpdateConnectButton DISCONNECT
        | Some _ ->
            disconnect ()

    member this.Import file =
        System.IO.File.ReadAllLines file
        |> Seq.map Domain.parseLogItem
        |> this.AddLogItems

    member private this.AddLogItems items =
        let isNegative item = negativeFilterInfos |> Array.exists (fun info -> Domain.matchesFilter info item)

        let matchedItemss =
            logPages
            |> Seq.mapi (fun i page ->
                items
                |> Seq.filter (fun item -> item |> isNegative |> not && item |> Domain.matchesFilter page.Filter.Info)
                |> Array.ofSeq
            )
            |> List.ofSeq

        let numLines = logPages.[0].LogItems.Count + matchedItemss.[0].Length
        if numLines > MAX_LOG_LINES then
            disconnect ()
            view.ShowError <| sprintf "Stopped due to too much log (%d lines). It is recommented to clear log in the device before continuing."
                                      numLines
        else
            (logPages, matchedItemss)
            ||> Seq.iteri2 (fun i page matchedItems ->
                page.LogItems.AddRange matchedItems
                if i <> curPageIdx then
                    page.UnreadCount <- page.UnreadCount + matchedItems.Length
                if page.UnreadCount > 0 then
                    view.UpdateFilterLabel i (formatFilterLabel page.Filter.Name page.UnreadCount)
                if matchedItems.Length > 0 then
                    view.AppendLogItems i (matchedItems |> Seq.map (fun item -> item.Content, item.Severity))
            )

    member private this.LogItemsReceived connId items =
        if killFun.IsSome && connectionId = connId then
            this.AddLogItems items

    member private this.Disconnected () =
        killFun <- None
        view.UpdateConnectButton CONNECT
        view.ShowError "Device disconnected."

    member this.AddFilter filter =
        let page = LogPage (filter,
                            logPages.[0].LogItems.FindAll (fun item -> Domain.matchesFilter filter.Info item))
        logPages.Add page

        view.AppendFilter filter.Name
        view.CreateLogArea isDark
        let items = page.LogItems
                    |> Seq.map (fun item -> item.Content, item.Severity)
        let lastIdx = logPages.Count - 1
        view.AppendLogItems lastIdx items
        view.SelectFilter lastIdx

    member this.RemoveFilter () =
        if curPageIdx = 0 then
            view.ShowError "Could not remove MAIN filter."
        else
            let i = curPageIdx
            logPages.RemoveAt i
            curPageIdx <- 0
            view.RemoveFilter i
            view.RemoveLogArea i
            view.ShowLogArea 0
            view.SelectFilter 0

    member this.SelectFilter idx =
        if idx <> curPageIdx then
            if idx >= 0 then
                curPageIdx <- idx
                logPages.[idx].UnreadCount <- 0
                view.UpdateFilterLabel idx logPages.[idx].Filter.Name
                view.ShowLogArea idx
            elif not System.isWindows then
                curPageIdx <- 0
                logPages.[0].UnreadCount <- 0
                view.UpdateFilterLabel 0 logPages.[0].Filter.Name
                view.ShowLogArea 0
                view.SelectFilter 0

    member this.Clear () =
        resetLog ()

    member this.DeepClear deviceIdx =
        if devices.Length = 0 then
            view.ShowError "No device selected."
        else
            resetLog ()
            match Domain.logcatClear adb (Some devices.[deviceIdx]) with
            | Error text -> view.ShowError text
            | Ok _ -> ()

    member this.GoEnd () =
        view.GoEnd curPageIdx

    member this.Export (path: string) =
        use stream = new System.IO.StreamWriter (path)
        for item in logPages.[curPageIdx].LogItems do
            stream.WriteLine item.Content

    member this.GetStacktrace _dsymFile stOption =
        dsymFile <- _dsymFile
        match stOption with
        | LiveLog -> Live (logPages.[0].LogItems |> Seq.map (fun item -> item.Content) |> List.ofSeq)
        | LogFile file -> File file
        |> view.OpenStacktrace dsymFile

    member this.OpenConfig () =
        view.OpenConfig adb negativeFilterInfos

    member this.UpdateConfig _adb _negative =
        adb <- _adb
        negativeFilterInfos <- _negative

    member this.OpenScreenshot deviceIdx =
        if devices.Length = 0 then
            view.ShowError "No device selected."
        else
            let device = devices.[deviceIdx]
            view.OpenScreenshot adb device (formatDevice device)

    member this.ChangeMode _isDark =
        isDark <- _isDark
        logPages
        |> Seq.iteri (fun i page ->
            page.LogItems
            |> Seq.map (fun item -> item.Content, item.Severity)
            |> view.ChangeMode isDark i
        )

    member this.SetWrap value =
        logPages
        |> Seq.iteri (fun i page ->
            page.LogItems
            |> Seq.map (fun item -> item.Content, item.Severity)
            |> view.SetWrap value i
        )

    member this.Exit _dsymFile =
        dsymFile <- _dsymFile
        disconnect ()
        let filters = logPages |> Seq.tail |> Seq.map (fun page -> page.Filter) |> List.ofSeq
        Domain.saveConfig { Adb = adb; DsymFile = dsymFile; Filters = filters; Negative = negativeFilterInfos; IsDark = isDark }
