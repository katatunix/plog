namespace PLog

open System
open Eto.Forms
open Eto.Drawing
open EtoUtils
open MonoMac.Foundation
open MonoMac.AppKit

type Mode = {
    BackColor : Color
    TextColor : Color
}

type MacLogArea (isDark) as this =
    inherit Panel ()

    static let darkMode = { BackColor = Color.FromRgb 0x1e1e1e; TextColor = Color.FromRgb 0xfcfcfc }
    static let lightMode = { BackColor = Colors.White; TextColor = Colors.Black }

    let mutable currentMode = if isDark then darkMode else lightMode

    let SPACE = Spacing (Size (5, 5))

    let textArea = new TextArea (Wrap = false, ReadOnly = true, Font = codeFont,
                                 BackgroundColor = currentMode.BackColor, TextColor = currentMode.TextColor)

    let textAreaControl = textArea.ControlObject :?> NSTextView

    let lb = new Label (Text = "Find text", VerticalAlignment = VerticalAlignment.Center)
    let tb = new TextBox (PlaceholderText = "Pressing Enter is the same as clicking the Next button")
    let nextButton = new Button (Text = "Next")
    let previousButton = new Button (Text = "Prev")
    let rewindButton = new Button (Text = "Rewind")

    do this.Content <-
        mkLayout <| Tbl [
            SPACE
            Row [TableEl <| Tbl [SPACE
                                 Row [
                                      El lb
                                      StretchedEl tb
                                      El nextButton
                                      El previousButton
                                      El rewindButton
                                      ]]]
            Row [TableEl <| Tbl [Row [El textArea]]]
        ]

    let mutable keepEnd = true
    do textArea.MouseDown.Add (fun _ -> keepEnd <- false)
    do textArea.MouseWheel.Add (fun _ -> keepEnd <- false)

    let findAndSelect key startIdx isForwarding =
        let sc = StringComparison.CurrentCultureIgnoreCase
        let foundIdx =
            if isForwarding then textArea.Text.IndexOf (key, startIdx, sc)
            else textArea.Text.LastIndexOf (key, startIdx, sc)
        if foundIdx = -1 then
            MessageBox.Show (sprintf "Could not find or no longer find \"%s\"" key, MessageBoxType.Information) |> ignore
        else
            keepEnd <- false
            let a = foundIdx
            let b = foundIdx + key.Length - 1
            textArea.Selection <- Range (a, b)
            textAreaControl.ScrollRangeToVisible (NSRange (int64 a, int64 b))

    let next () =
        if tb.Text.Length > 0 then
            let selection = textArea.Selection
            let startIdx = if selection.Length () > 0 then selection.Start + 1 else textArea.CaretIndex
            findAndSelect tb.Text startIdx true

    let prev () =
        if tb.Text.Length > 0 then
            let selection = textArea.Selection
            let startIdx = if selection.Length () > 0 then selection.End - 1 else textArea.CaretIndex
            findAndSelect tb.Text startIdx false

    let rewind () =
        if tb.Text.Length > 0 then
            let startIdx = 0
            findAndSelect tb.Text startIdx true

    do
        let mutable enterDown = false
        tb.KeyDown.Add (fun e -> if e.Key = Keys.Enter then enterDown <- true)
        tb.KeyUp.Add (fun e -> if e.Key = Keys.Enter then
                                   if enterDown then enterDown <- false; next ())

        nextButton.Click.Add (fun _ -> next ())
        previousButton.Click.Add (fun _ -> prev ())
        rewindButton.Click.Add (fun _ -> rewind ())

    member private this._Clear () =
        textArea.Text <- ""

    member private this._AppendLines lines =
        let sl = textArea.Selection
        textArea.Append ((lines |> Seq.map fst |> String.concat Environment.NewLine) + Environment.NewLine, keepEnd)
        textArea.Selection <- sl

    member this._ChangeMode mode lines =
        currentMode <- mode
        this._Clear ()
        textArea.BackgroundColor <- currentMode.BackColor
        textArea.TextColor <- currentMode.TextColor
        this._AppendLines lines

    interface LogArea with

        member this.GetEtoControl () =
            this :> Control

        member this.Clear () =
            this._Clear ()

        member this.AppendLines lines =
            this._AppendLines lines

        member this.GoEnd () =
            keepEnd <- true
            textAreaControl.ScrollRangeToVisible <| NSRange (int64 textArea.Text.Length, 0L)

        member this.SetWrap value =
            textArea.Wrap <- value

        member this.ChangeMode isDark lines =
            if isDark && currentMode <> darkMode then
                this._ChangeMode darkMode lines
            elif not isDark && currentMode <> lightMode then
                this._ChangeMode lightMode lines
