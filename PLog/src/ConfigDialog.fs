namespace PLog

open System
open Eto.Forms
open Eto.Drawing
open EtoUtils
open Domain

type ConfigDialog (adb, maxLogLines, negative: seq<FilterInfo>, callback) as this =
    inherit Dialog (Title = "Config", Size = Size (500, 350))

    let adbTextBox = new TextBox (Text = adb)
    let browseAdbButton = new Button (Text = "Browse")
    let maxLogStepper = new NumericStepper (MinValue = 1000.0, MaxValue = 1000000.0, Value = float maxLogLines)
    let negativeTextArea = new TextArea (Font = codeFont)
    let okButton = new Button (Text = "OK")
    let cancelButton = new Button (Text = "Cancel")

    let SPACE = Spacing (Size (8, 8))
    let PAD = Pad (Padding 8)

    do this.Content <-
        mkLayout <| Tbl [
            PAD; SPACE
            Row [El (new Label (Text = "Path to adb – just type adb to use the one in system path"))]
            Row [TableEl <| Tbl [SPACE
                                 Row [StretchedEl adbTextBox; El browseAdbButton]
                                 ]]
            Row [El (new Label (Text = "Max log lines"))]
            Row [El maxLogStepper]
            Row [El (new Label (Text = "Negative filters – log matched any of these will be totally ignored"))]
            StretchedRow [El negativeTextArea]
            Row [TableEl <| Tbl [SPACE
                                 Row [EmptyElement; El okButton; El cancelButton; EmptyElement]
                                 ]]
        ]

    let setupNegative () =
        let str =
            negative
            |> Seq.map (fun info ->
                match info.Tag, info.Pid with
                | Some tag, Some pid -> sprintf "%s, %d" tag pid
                | Some tag, None     -> tag
                | None, Some pid     -> sprintf ", %d" pid
                | None, None         -> failwith "Should not go here"
            )
            |> String.concat Environment.NewLine

        negativeTextArea.Text <-
            if str.Length > 0 then str
            else [ "//Syntax: Tag, PID"
                   "//DummyTag, 1234"
                   "//JustDummyTag"
                   "//, 1234" ]
                 |> String.concat Environment.NewLine

        if System.isWindows then
            negativeTextArea.KeyDown.Add (fun e ->
                if e.Control && e.Key = Keys.V then
                    let str = Clipboard.Instance.Text
                    Clipboard.Instance.Text <- str
                elif e.Key = Keys.Escape then
                    this.Close ()
            )

    do setupNegative ()

    let openFileDialog = new OpenFileDialog (Title = "Select adb file", MultiSelect = false)
    do browseAdbButton.Click.Add (fun _ ->
        if openFileDialog.ShowDialog this = DialogResult.Ok then
            adbTextBox.Text <- openFileDialog.FileName
    )

    do okButton.Click.Add (fun _ ->
        this.Close ()
        let adb = adbTextBox.Text
        let maxLogLines = maxLogStepper.Value |> int
        let negative =
            negativeTextArea.Text
            |> System.splitLines
            |> Array.choose (fun _line ->
                let line = _line.Trim ()
                if line.Length = 0 || line.StartsWith "//" then
                    None
                else
                    let i = line.IndexOf ','
                    let tag, pid =
                        if i = -1 then Some line, None
                        else
                            let tag = line.Substring(0, i).Trim()
                            let tag = if tag.Length > 0 then Some tag else None
                            match parseInt (line.Substring(i + 1).Trim()) with
                            | None -> tag, None
                            | Some pid -> tag, Some pid
                    if tag = None && pid = None then None
                    else Some { Tag = tag; Pid = pid }
            )
        callback adb maxLogLines negative
    )

    do cancelButton.Click.Add (fun _ -> this.Close ())
    do this.AbortButton <- cancelButton
