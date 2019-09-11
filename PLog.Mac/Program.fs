module PLog.Main

open System
open PLog

[<EntryPoint; STAThread>]
let main argv =
    let mkLogArea isDark = new MacLogArea (isDark) :> LogArea
    (new Eto.Forms.Application ()).Run (new MainForm (mkLogArea))
    0
