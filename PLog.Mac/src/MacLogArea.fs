namespace PLog

open System
open System.Text
open System.Runtime.InteropServices
open Eto.Forms
open Eto.Drawing
open EtoUtils
open Foundation
open AppKit

type Mode =
    { BackColor: Color
      ErrorColor: NSColor
      WarningColor: NSColor
      DebugColor: NSColor
      InfoColor: NSColor
      SelectionColor: NSColor }

type MacLogArea (isDark) as this =
    inherit Panel ()

    static let darkMode =
        { BackColor = Color.FromRgb 0x1e1e1e
          ErrorColor = NSColor.SystemRed
          WarningColor = NSColor.SystemYellow
          DebugColor = NSColor.SystemGreen
          InfoColor = NSColor.FromRgb (NFloat(0.9), NFloat(0.9), NFloat(0.9))
          SelectionColor = NSColor.FromRgb (21, 79, 142) }

    static let lightMode =
        { BackColor = Colors.White
          ErrorColor = NSColor.FromRgb (186, 26, 30)
          WarningColor = NSColor.Brown
          DebugColor = NSColor.Blue
          InfoColor = NSColor.Black
          SelectionColor = NSColor.FromRgb (163, 215, 255) }

    let mutable currentMode = if isDark then darkMode else lightMode

    let DIST = 5

    let SPACE = Spacing (Size (DIST, DIST))

    let font = NSFont.FromFontName ("Menlo", NFloat(11.5))

    let textArea = new TextArea (Wrap = false, ReadOnly = true, BackgroundColor = currentMode.BackColor)

    let textAreaControl = textArea.ControlObject :?> NSTextView

    let label = new Label (Text = "Find text", VerticalAlignment = VerticalAlignment.Center)
    let findTextBox = new SearchBox (PlaceholderText = "Type the text you want to find")
    let previousButton = new Button (Text = "◀︎", MinimumSize = Size (40, -1))
    let nextButton = new Button (Text = "▶︎", MinimumSize = Size (40, -1))
    let rewindButton = new Button (Text = "Rewind")

    do this.Content <-
        mkLayout <| Tbl [
            SPACE
            Row [TableEl <| Tbl [SPACE
                                 Pad (Padding (DIST, 0, 0, 0))
                                 Row [El label
                                      StretchedEl findTextBox
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
            MessageBox.Show ("Text not found.", MessageBoxType.Information) |> ignore
        else
            keepEnd <- false
            let range = NSRange (foundIdx, key.Length)
            textAreaControl.SelectedRange <- range
            textAreaControl.ScrollRangeToVisible range
            textArea.Focus()

    let next () =
        if findTextBox.Text.Length > 0 then
            let selection = textArea.Selection
            let startIdx = if selection.Length () > 0 then selection.Start + 1 else textArea.CaretIndex
            findAndSelect findTextBox.Text startIdx true

    let prev () =
        if findTextBox.Text.Length > 0 then
            let selection = textArea.Selection
            let startIdx = if selection.Length () > 0 then selection.End - 1 else textArea.CaretIndex
            findAndSelect findTextBox.Text startIdx false

    let rewind () =
        if findTextBox.Text.Length > 0 then
            let startIdx = 0
            findAndSelect findTextBox.Text startIdx true

    let applyCurrentMode () =
        textArea.BackgroundColor <- currentMode.BackColor
        use attrs = new NSMutableDictionary ()
        attrs.Add (NSStringAttributeKey.BackgroundColor, currentMode.SelectionColor)
        textAreaControl.SelectedTextAttributes <- attrs

    do
        nextButton.Click.Add (fun _ -> next ())
        previousButton.Click.Add (fun _ -> prev ())
        rewindButton.Click.Add (fun _ -> rewind ())

        let mutable enterDown = false
        findTextBox.KeyDown.Add (fun e -> if e.Key = Keys.Enter then enterDown <- true)
        findTextBox.KeyUp.Add (fun e ->
            if e.Key = Keys.Enter && enterDown then
                enterDown <- false; rewind ()
        )

        textArea.KeyDown.Add (fun e ->
            if e.Key = Keys.Enter then
                e.Handled <- true
                next ()
            elif e.Application && e.Key = Keys.G then
                e.Handled <- true
                if e.Shift then prev () else next ()
            elif e.Application && e.Key = Keys.F then
                e.Handled <- true
                findTextBox.Focus ()
            elif e.Application && e.Key = Keys.Down then
                keepEnd <- true
            elif e.Key = Keys.Up || e.Key = Keys.Down then
                keepEnd <- false
        )

        applyCurrentMode ()

    let clear () = textArea.Text <- ""

    let append (text: string) severity =
        let color =
            match severity with
            | Domain.Err     -> currentMode.ErrorColor
            | Domain.Warning -> currentMode.WarningColor
            | Domain.Debug   -> currentMode.DebugColor
            | Domain.Info    -> currentMode.InfoColor
        use str = new NSMutableAttributedString (text)
        use attrs = new NSMutableDictionary ()
        attrs.Add (NSStringAttributeKey.ForegroundColor, color)
        attrs.Add (NSStringAttributeKey.Font, font)
        str.AddAttributes (attrs, NSRange (0, str.Length))
        textAreaControl.TextStorage.Append str

    let appendLines lines =
        let mutable isFirst = true
        let mutable currentSeverity = Domain.Err
        let collectedString = StringBuilder ()

        for text, severity in lines do
            if isFirst then
                isFirst <- false
                currentSeverity <- severity
                collectedString.AppendLine (text: string) |> ignore
            elif severity = currentSeverity then
                collectedString.AppendLine text |> ignore
            else
                append (collectedString.ToString()) currentSeverity
                currentSeverity <- severity
                collectedString.Clear().AppendLine(text) |> ignore

        if collectedString.Length > 0 then
            append (collectedString.ToString()) currentSeverity

        if keepEnd then
            textAreaControl.ScrollRangeToVisible <| NSRange (textAreaControl.TextStorage.Length, 0)

    let changeMode mode =
        currentMode <- mode
        applyCurrentMode ()

    interface LogArea with

        member this.GetEtoControl () =
            this :> Control

        member this.Clear () =
            clear ()

        member this.AppendLines lines =
            appendLines lines

        member this.GoEnd () =
            keepEnd <- true
            textAreaControl.ScrollRangeToVisible <| NSRange (textArea.Text.Length, 0)

        member this.SetWrap value =
            textArea.Wrap <- value

        member this.ChangeMode isDark =
            if isDark && currentMode <> darkMode then
                changeMode darkMode
            elif not isDark && currentMode <> lightMode then
                changeMode lightMode
