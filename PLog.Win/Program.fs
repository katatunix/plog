module PLog.Main

open System
open PLog

[<EntryPoint; STAThread>]
let main argv =
    let app = new Eto.Forms.Application ()
    let mkLogArea isDark = new WinLogArea (isDark) :> LogArea
    let form = new MainForm (mkLogArea)
    let winform = form.ControlObject :?> System.Windows.Forms.Form
    winform.Icon <- System.Drawing.Icon.ExtractAssociatedIcon (System.Windows.Forms.Application.ExecutablePath)
    app.Run (form)
    0
