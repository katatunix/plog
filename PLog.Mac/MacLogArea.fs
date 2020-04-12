namespace PLog

open System
open Eto.Forms
open Eto.Drawing
open EtoUtils
open MonoMac.Foundation
open MonoMac.AppKit

type Mode =
    { BackColor: Color
      ErrorColor: NSColor
      WarningColor: NSColor
      DebugColor: NSColor
      InfoColor: NSColor }

type MacLogArea (isDark) as this =
    inherit Panel ()

    static let darkMode =
        { BackColor = Color.FromRgb 0x1e1e1e
          ErrorColor = NSColor.SystemRedColor
          WarningColor = NSColor.SystemYellowColor
          DebugColor = NSColor.SystemGreenColor
          InfoColor = NSColor.FromRgba (0.9, 0.9, 0.9, 1.0) }

    static let lightMode =
        { BackColor = Colors.WhiteSmoke
          ErrorColor = NSColor.Red
          WarningColor = NSColor.Brown
          DebugColor = NSColor.Blue
          InfoColor = NSColor.Black }

    let mutable currentMode = if isDark then darkMode else lightMode

    let DIST = 5

    let SPACE = Spacing (Size (DIST, DIST))

    let font = NSFont.FromFontName ("Menlo", 11.5)

    let textArea = new TextArea (Wrap = false, ReadOnly = true, BackgroundColor = currentMode.BackColor)

    let textAreaControl = textArea.ControlObject :?> NSTextView

    do
        use attrs = new NSMutableDictionary ()
        attrs.Add (NSAttributedString.BackgroundColorAttributeName, NSColor.SystemBlueColor)
        textAreaControl.SelectedTextAttributes <- attrs

    let label = new Label (Text = "Find text", VerticalAlignment = VerticalAlignment.Center)
    let textBox = new TextBox ()
    let previousButton = new Button (Text = "◀︎")
    let nextButton = new Button (Text = "▶︎")
    let rewindButton = new Button (Text = "Rewind")

    do this.Content <-
        mkLayout <| Tbl [
            SPACE
            Row [TableEl <| Tbl [SPACE
                                 Pad (Padding (DIST, 0, 0, 0))
                                 Row [El label
                                      StretchedEl textBox
                                      El previousButton
                                      El nextButton
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
            MessageBox.Show ("Text not found", MessageBoxType.Information) |> ignore
        else
            keepEnd <- false
            let range = NSRange (int64 foundIdx, int64 key.Length)
            textAreaControl.SelectedRange <- range
            textAreaControl.ScrollRangeToVisible range
            textArea.Focus()

    let next () =
        if textBox.Text.Length > 0 then
            let selection = textArea.Selection
            let startIdx = if selection.Length () > 0 then selection.Start + 1 else textArea.CaretIndex
            findAndSelect textBox.Text startIdx true

    let prev () =
        if textBox.Text.Length > 0 then
            let selection = textArea.Selection
            let startIdx = if selection.Length () > 0 then selection.End - 1 else textArea.CaretIndex
            findAndSelect textBox.Text startIdx false

    let rewind () =
        if textBox.Text.Length > 0 then
            let startIdx = 0
            findAndSelect textBox.Text startIdx true

    do
        nextButton.Click.Add (fun _ -> next ())
        previousButton.Click.Add (fun _ -> prev ())
        rewindButton.Click.Add (fun _ -> rewind ())

        let mutable enterDown = false
        textBox.KeyDown.Add (fun e -> if e.Key = Keys.Enter then enterDown <- true)
        textBox.KeyUp.Add (fun e -> if e.Key = Keys.Enter then
                                        if enterDown then enterDown <- false; rewind ())

        textArea.KeyDown.Add (fun e -> if e.Application && e.Key = Keys.G then
                                           e.Handled <- true
                                           if e.Shift then prev () else next ()
                                       elif e.Application && e.Key = Keys.Down then
                                           keepEnd <- true
                                       elif e.Key = Keys.Up || e.Key = Keys.Down then
                                           keepEnd <- false)

    member private this._Clear () =
        textArea.Text <- ""

    member private this._AppendLines lines =
        let mutable len = uint64 textAreaControl.TextStorage.Length
        for (line, severity) in lines do
            let color =
                match severity with
                | Domain.Err     -> currentMode.ErrorColor
                | Domain.Warning -> currentMode.WarningColor
                | Domain.Debug   -> currentMode.DebugColor
                | Domain.Info    -> currentMode.InfoColor

            use str = new NSMutableAttributedString (line + Environment.NewLine)
            use attrs = new NSMutableDictionary ()
            attrs.Add (NSAttributedString.ForegroundColorAttributeName, color)
            attrs.Add (NSAttributedString.FontAttributeName, font)
            str.AddAttributes (attrs, NSRange (0L, str.Length))

            textAreaControl.TextStorage.Insert (str, len)
            len <- len + uint64 str.Length

        if keepEnd then
            textAreaControl.ScrollRangeToVisible <| NSRange (int64 len, 0L)

    member this._ChangeMode mode lines =
        currentMode <- mode
        this._Clear ()
        textArea.BackgroundColor <- currentMode.BackColor
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
