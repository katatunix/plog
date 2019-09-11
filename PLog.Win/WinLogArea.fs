namespace PLog

open Eto.Forms
open System.Drawing
open FastColoredTextBoxNS
open System

type Mode = {
    BackColor : Color
    SelColor : Color
    ErrorStyle : TextStyle
    WarningStyle : TextStyle
    DebugStyle : TextStyle
    InfoStyle : TextStyle
}

type WinLogArea (isDark) =

    static let darkMode = {
        BackColor = Color.Black
        SelColor = Color.FromArgb(150, 255, 255, 255)
        ErrorStyle = new TextStyle(Brushes.Red, null, FontStyle.Regular)
        WarningStyle = new TextStyle(Brushes.Yellow, null, FontStyle.Regular)
        DebugStyle = new TextStyle(Brushes.LightGreen, null, FontStyle.Regular)
        InfoStyle = new TextStyle(Brushes.White, null, FontStyle.Regular)
    }
    static let lightMode = {
        BackColor = Color.White
        SelColor = Color.FromArgb(150, 0, 0, 255)
        ErrorStyle = new TextStyle(Brushes.Red, null, FontStyle.Regular)
        WarningStyle = new TextStyle(Brushes.Brown, null, FontStyle.Regular)
        DebugStyle = new TextStyle(Brushes.Blue, null, FontStyle.Regular)
        InfoStyle = new TextStyle(Brushes.Black, null, FontStyle.Regular)
    }

    let mutable currentMode = if isDark then darkMode else lightMode

    let fctb = new FastColoredTextBox (ReadOnly = true, BackColor = currentMode.BackColor, ForeColor = Color.White,
                                        SelectionColor = currentMode.SelColor,
                                        Font = new Font("Consolas", 9.75f))
                                        
    member private this._AppendLines lines =
        fctb.BeginUpdate ()
        fctb.Selection.BeginUpdate ()

        let userSelection = fctb.Selection.Clone ()
        let shouldNotGoEnd = (not userSelection.IsEmpty) || (userSelection.Start.iLine < fctb.LinesCount - 1)
        
        fctb.TextSource.CurrentTB <- fctb
        
        for (text, severity) in lines do
            let style =
                match severity with
                | Domain.Err     -> currentMode.ErrorStyle
                | Domain.Warning -> currentMode.WarningStyle
                | Domain.Debug   -> currentMode.DebugStyle
                | Domain.Info    -> currentMode.InfoStyle
            fctb.AppendText (text + Environment.NewLine, style)

        if shouldNotGoEnd then
            fctb.Selection.Start <- userSelection.Start
            fctb.Selection.End <- userSelection.End
        else
            fctb.GoEnd ()
            
        fctb.Selection.EndUpdate ()
        fctb.EndUpdate ()

    member private this._ChangeMode mode lines =
        currentMode <- mode
        fctb.Clear ()
        fctb.BackColor <- currentMode.BackColor
        fctb.SelectionColor <- currentMode.SelColor
        this._AppendLines lines
    
    interface LogArea with

        member this.GetEtoControl () =
            fctb.ToEto ()

        member this.Clear () =
            fctb.Clear ()

        member this.GoEnd () =
            fctb.GoEnd ()

        member this.SetWrap value =
            fctb.WordWrap <- value

        member this.AppendLines lines =
            this._AppendLines lines

        member this.ChangeMode isDark lines =
            if isDark && currentMode <> darkMode then
                this._ChangeMode darkMode lines
            elif not isDark && currentMode <> lightMode then
                this._ChangeMode lightMode lines
