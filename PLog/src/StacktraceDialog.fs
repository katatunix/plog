namespace PLog

open Eto.Forms
open Eto.Drawing
open EtoUtils
open System.Threading

type StacktraceDialog (dsymFile, logSource) as this =
    inherit Dialog (Title = "Stacktrace", Size = Size (900, 480), Resizable = true, Maximizable = true)

    let area = new TextArea (Text = "Parsing...", ReadOnly = true, Wrap = false, Font = codeFont)
    let closeButton = new Button (Text = "Close")

    let PAD = Pad (Padding 8)

    do
        this.Content <-
            mkLayout <| Tbl [
                StretchedRow [El area]
                Row [TableEl <| Tbl [PAD; Row [EmptyElement; El closeButton; EmptyElement]]]
            ]

        this.AbortButton <- closeButton

        closeButton.Click.Add (fun _ -> this.Close ())

        if OS.isWindows then
            this.KeyUp.Add (fun e -> if e.Key = Keys.Escape then this.Close ())

        this.LoadComplete.Add (fun _ ->
            let comp = async {
                let ctx = SynchronizationContext.Current
                do! Async.SwitchToThreadPool ()
                let stacktrace = Domain.getStacktrace dsymFile logSource
                do! Async.SwitchToContext ctx
                match stacktrace with
                | Ok str -> area.Text <- str
                | Error str -> area.Text <- str
            }
            let cts = new CancellationTokenSource ()
            this.Closing.Add (fun _ -> cts.Cancel ())
            Async.StartImmediate (comp, cts.Token)
        )
