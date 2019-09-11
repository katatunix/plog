namespace PLog

open Eto.Forms
open Eto.Drawing
open EtoUtils
open System.Threading

type ScreenshotDialog (adb, device, deviceTitle) as this =
    inherit Dialog (Title = "Screenshot for " + deviceTitle, Size = Size (900, 600), Resizable = true, Maximizable = true)

    let PAD = Pad (Padding 8)
    let SPACE = Spacing (Size (8, 8))

    let imageView = new ImageView (BackgroundColor = Colors.Black)
    let captureButton = new Button (Text = "Capture")
    let saveButton = new Button (Text = "Save")
    let closeButton = new Button (Text = "Close")

    let saveFileDialog = new SaveFileDialog (Title = "Save screenshot")
    do saveFileDialog.Filters.Add (new FileFilter ("Portable Network Graphics", [|".png"|]))

    let showError text =
        MessageBox.Show (text, MessageBoxType.Error) |> ignore

    let mutable bitmap : Bitmap = null

    let onCaptured (stream : System.IO.Stream) =
        try
            Ok (new Bitmap (stream))
        with _ ->
            Error "Could not capture screenshot for the current device"

    let capture () =
        let comp = async {
            let backup = captureButton.Text
            captureButton.Text <- "Capturing..."
            captureButton.Enabled <- false
            saveButton.Enabled <- false
            imageView.Image <- null

            let ctx = SynchronizationContext.Current
            do! Async.SwitchToThreadPool ()
            let result = Domain.captureScreenshot adb (Some device) onCaptured
            do! Async.SwitchToContext ctx

            match result with
            | Ok bmp ->
                bitmap <- bmp
                imageView.Image <- bitmap
            | Error text ->
                bitmap <- null
                showError text
            captureButton.Text <- backup
            captureButton.Enabled <- true
            saveButton.Enabled <- true
        }
        let cts = new CancellationTokenSource ()
        this.Closing.Add (fun _ -> cts.Cancel ())
        Async.StartImmediate (comp, cts.Token)

    do
        this.Content <-
            mkLayout <| Tbl [
                StretchedRow [El imageView]
                Row [TableEl <| Tbl [PAD; SPACE; Row [EmptyElement; El captureButton; El saveButton; El closeButton; EmptyElement]]]
            ]

        this.AbortButton <- closeButton

        this.LoadComplete.Add (fun _ -> capture ())

        captureButton.Click.Add (fun _ -> capture ())
        closeButton.Click.Add (fun _ -> this.Close ())

        saveButton.Click.Add (fun _ ->
            if isNull bitmap then
                showError "Nothing to save"
            elif saveFileDialog.ShowDialog (this) = DialogResult.Ok then
                try
                    bitmap.Save (saveFileDialog.FileName, ImageFormat.Png)
                with ex ->
                    showError ex.Message
        )
