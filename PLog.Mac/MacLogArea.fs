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

    static let darkMode = { BackColor = Colors.Black; TextColor = Colors.WhiteSmoke }
    static let lightMode = { BackColor = Colors.White; TextColor = Colors.Black }

    let mutable currentMode = if isDark then darkMode else lightMode

    let SPACE = Spacing (Size (5, 5))

    let rta = new TextArea (Wrap = false, ReadOnly = true, Font = codeFont,
                            BackgroundColor = currentMode.BackColor, TextColor = currentMode.TextColor)

    let ctrl = rta.ControlObject :?> NSTextView


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
            Row [TableEl <| Tbl [Row [El rta]]]
        ]

    let mutable keepEnd = true
    do rta.MouseDown.Add (fun _ -> keepEnd <- false)
    do rta.MouseWheel.Add (fun _ -> keepEnd <- false)

    let findAndSelect key startIdx isForwarding =
        let sc = StringComparison.CurrentCultureIgnoreCase
        let foundIdx =
            if isForwarding then rta.Text.IndexOf (key, startIdx, sc)
            else rta.Text.LastIndexOf (key, startIdx, sc)
        if foundIdx = -1 then
            MessageBox.Show (sprintf "Could not find or no longer find \"%s\"." key, MessageBoxType.Information) |> ignore
        else
            keepEnd <- false
            let a = foundIdx
            let b = foundIdx + key.Length - 1
            rta.Selection <- Range (a, b)
            ctrl.ScrollRangeToVisible (NSRange (int64 a, int64 b))

    let next () =
        if tb.Text.Length > 0 then
            let selection = rta.Selection
            let startIdx = if selection.Length () > 0 then selection.Start + 1 else rta.CaretIndex
            findAndSelect tb.Text startIdx true

    let prev () =
        if tb.Text.Length > 0 then
            let selection = rta.Selection
            let startIdx = if selection.Length () > 0 then selection.End - 1 else rta.CaretIndex
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
        rta.Text <- ""

    member private this._AppendLines lines =
        let sl = rta.Selection
        rta.Append ((lines |> Seq.map fst |> String.concat Environment.NewLine) + Environment.NewLine, keepEnd)
        rta.Selection <- sl

    member this._ChangeMode mode lines =
        currentMode <- mode
        this._Clear ()
        rta.BackgroundColor <- currentMode.BackColor
        rta.TextColor <- currentMode.TextColor
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
            ctrl.ScrollRangeToVisible <| NSRange (int64 rta.Text.Length, 0L)

        member this.SetWrap value =
            rta.Wrap <- value

        member this.ChangeMode isDark lines =
            if isDark && currentMode <> darkMode then
                this._ChangeMode darkMode lines
            elif not isDark && currentMode <> lightMode then
                this._ChangeMode lightMode lines
