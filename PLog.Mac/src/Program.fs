module PLog.Main

open System
open PLog

[<EntryPoint; STAThread>]
let main _ =
    let mkLogArea isDark = new MacLogArea (isDark) :> LogArea
    let app = new Eto.Forms.Application (Eto.Platforms.Mac64)
    app.Run (new MainForm (mkLogArea))
    0
