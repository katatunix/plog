namespace PLog

open Eto.Forms
open Eto.Drawing
open EtoUtils

type AddFilterDialog (callback) as this =
    inherit Dialog (Title = "Add filter", Size = Size (400, -1))

    let DIST = 8
    let SPACE = Spacing (Size (DIST, DIST))
    let PAD = Pad (Padding DIST)

    let nameTextBox = new TextBox ()
    let tagTextBox = new TextBox ()
    let pidTextBox = new TextBox ()
    let okButton = new Button (Text = "OK")
    let cancelButton = new Button (Text = "Cancel")

    let label text = new Label (Text = text, VerticalAlignment = VerticalAlignment.Center)

    let layout =
        mkLayout <| Tbl [
            PAD
            Row [TableEl <| Tbl [SPACE
                                 Row [El (label "Filter name"); StretchedEl nameTextBox]
                                 Row [El (label "Tag"); StretchedEl tagTextBox]
                                 Row [El (label "PID"); StretchedEl pidTextBox]
                                 ]]
            Row [TableEl <| Tbl [SPACE
                                 Pad (Padding (0, DIST, 0, 0))
                                 Row [EmptyElement; El okButton; El cancelButton; EmptyElement]
                                 ]]
        ]

    do
        this.DefaultButton <- okButton
        this.AbortButton <- cancelButton
        this.Content <- layout
        cancelButton.Click.Add (fun _ -> this.Close ())
        okButton.Click.Add (fun _ ->
            match Domain.createFilter nameTextBox.Text tagTextBox.Text pidTextBox.Text with
            | Error str -> MessageBox.Show (str, MessageBoxType.Error) |> ignore
            | Ok filter -> this.Close (); callback filter
        )
        nameTextBox.Focus ()
