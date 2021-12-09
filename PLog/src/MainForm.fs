namespace PLog

open System.Collections.Generic
open Eto.Forms
open Eto.Drawing
open EtoUtils

type MainForm (mkLogArea : bool -> LogArea) as this =
    inherit Form (Title = "PLog 9.9 | nghia.buivan@hotmail.com", Size = Size (1200, 800))

    let pre = MainPresenter (this, Application.Instance.Invoke)

    do this.Menu <- new MenuBar ()

    let SPACE = Spacing (Size (5, 5))
    let PAD = Pad (Padding 5)

    let refreshButton = new Button (Text = "Refresh")
    let deviceListDropDown = new DropDown ()
    let connectButton = new Button ()
    let screenshotButton = new Button (Text = "Screenshot")
    let configButton = new Button (Text = "Config")

    let filterPanel = new Panel ()
    let filterListBox = new ListBox ()
    let addFilterButton = new Button (Text = "+", ToolTip = "Add a new filter", MinimumSize = Size (40, -1))
    let removeFilterButton = new Button (Text = "–", ToolTip = "Remove current filter", MinimumSize = Size (40, -1))
    do filterPanel.Content <-
        mkLayout <| Tbl [
            SPACE
            StretchedRow [El filterListBox]
            Row [TableEl (Tbl [SPACE
                               Row [EmptyElement; El addFilterButton; El removeFilterButton; EmptyElement]])]
        ]

    let logPanel = new Panel ()
    let logAreas = List<LogArea> ()

    let modeCheckBox = new CheckBox (Text = "Dark mode")
    let clearButton = new Button (Text = "Clear", ToolTip = "Clear log in all filters")
    let clearInDeviceButton = new Button (Text = "Clear in device", ToolTip = "Clear log in all filters and in device")
    let wrapCheckBox = new CheckBox (Text = "Wrap")
    let goEndButton = new Button (Text = "Go end")
    let exportButton = new Button (Text = "Export")
    let importButton = new Button (Text = "Import")

    let dsymLabel = new Label (Text = "Dsym file", VerticalAlignment = VerticalAlignment.Center)
    let dsymTextBox = new TextBox (PlaceholderText = "Path to your dsym file")
    let browseDsymButton = new Button (Text = "Browse", ToolTip = "Browse dsym file")
    let getStacktraceButton = new Button (Text = "Get stacktrace", ToolTip = "Get stacktrace from live log")

    let openDsymDialog = new OpenFileDialog (Title = "Select dsym file", MultiSelect = false)
    do browseDsymButton.Click.Add (fun _ ->
        if openDsymDialog.ShowDialog this = DialogResult.Ok then
            dsymTextBox.Text <- openDsymDialog.FileName
    )

    let getStacktraceFromFileButton = new Button (Text = "From log file", ToolTip = "Get stacktrace from external log file")
    let openLogFileDialog = new OpenFileDialog (Title = "Select log file to get stacktrace", MultiSelect = false)

    let exportFileDialog = new SaveFileDialog (Title = "Export log to text file")
    do exportFileDialog.Filters.Add (FileFilter ("Text", [|".txt"|]))

    let importFileDialog = new OpenFileDialog (Title = "Import log from text file", MultiSelect = false)

    do this.Content <-
        mkLayout <| Tbl [
            PAD
            SPACE
            Row [TableEl (Tbl [SPACE
                               Row [El refreshButton; StretchedEl deviceListDropDown
                                    El connectButton; El screenshotButton; El configButton
                                    ]])]
            StretchedRow [
                El (new Splitter (Panel1 = filterPanel, Panel2 = logPanel, Panel1MinimumSize = 150, Panel2MinimumSize = 300))
            ]
            Row [TableEl (Tbl [SPACE
                               Row [El modeCheckBox
                                    El clearButton
                                    El clearInDeviceButton
                                    El wrapCheckBox
                                    El goEndButton
                                    El exportButton
                                    El importButton
                                    El dsymLabel
                                    StretchedEl dsymTextBox
                                    El browseDsymButton
                                    El getStacktraceButton
                                    El getStacktraceFromFileButton
                                    ]])]
        ]

    do
        this.LoadComplete.Add (fun _ -> pre.Init ())

        refreshButton.Click.Add (fun _ -> pre.RefreshDevices ())
        connectButton.Click.Add (fun _ -> pre.ToggleConnect deviceListDropDown.SelectedIndex)
        screenshotButton.Click.Add (fun _ -> pre.OpenScreenshot deviceListDropDown.SelectedIndex)
        configButton.Click.Add (fun _ -> pre.OpenConfig ())

        addFilterButton.Click.Add (fun _ -> (new AddFilterDialog (pre.AddFilter)).ShowModal ())
        removeFilterButton.Click.Add (fun _ -> pre.RemoveFilter ())
        filterListBox.SelectedIndexChanged.Add (fun _ -> pre.SelectFilter filterListBox.SelectedIndex)

        modeCheckBox.CheckedChanged.Add (fun _ -> pre.ChangeMode modeCheckBox.Checked.Value)
        clearButton.Click.Add (fun _ -> pre.Clear ())
        clearInDeviceButton.Click.Add (fun _ -> pre.ClearInDevice deviceListDropDown.SelectedIndex)
        goEndButton.Click.Add (fun _ -> pre.GoEnd ())

        exportButton.Click.Add (fun _ ->
            if exportFileDialog.ShowDialog this = DialogResult.Ok then
                pre.Export exportFileDialog.FileName
        )

        importButton.Click.Add (fun _ ->
            if importFileDialog.ShowDialog this = DialogResult.Ok then
                pre.Import importFileDialog.FileName
        )

        wrapCheckBox.CheckedChanged.Add (fun _ -> pre.SetWrap wrapCheckBox.Checked.Value)

        getStacktraceButton.Click.Add (fun _ -> pre.GetStacktrace dsymTextBox.Text LiveLog)
        getStacktraceFromFileButton.Click.Add (fun _ ->
            if openLogFileDialog.ShowDialog this = DialogResult.Ok then
                pre.GetStacktrace dsymTextBox.Text (LogFile openLogFileDialog.FileName)
        )

        this.Closing.Add (fun _ -> pre.Exit dsymTextBox.Text)

    interface MainView with

        member this.ShowError text =
            MessageBox.Show (text, MessageBoxType.Error) |> ignore

        member this.UpdateDevices ss =
            deviceListDropDown.Items.Clear ()
            ss |> Array.iter deviceListDropDown.Items.Add

        member this.SelectDevice idx =
            deviceListDropDown.SelectedIndex <- idx

        member this.UpdateConnectButton text =
            connectButton.Text <- text

        member this.AppendLogItems idx items =
            logAreas.[idx].AppendLines items

        member this.Clear idx =
            logAreas.[idx].Clear ()

        member this.GoEnd idx =
            logAreas.[idx].GoEnd ()

        member this.AppendFilter name =
            filterListBox.Items.Add name

        member this.CreateLogArea isDark =
            logAreas.Add (mkLogArea isDark)

        member this.ShowLogArea idx =
            logPanel.Content <- logAreas.[idx].GetEtoControl ()

        member this.SelectFilter idx =
            filterListBox.SelectedIndex <- idx

        member this.UpdateFilterLabel idx text =
            let tmp = filterListBox.SelectedIndex
            filterListBox.Items.[idx] <- ListItem (Text = text)
            filterListBox.SelectedIndex <- tmp

        member this.RemoveFilter idx =
            filterListBox.Items.RemoveAt idx

        member this.RemoveLogArea idx =
            logAreas.RemoveAt idx

        member this.UpdateDsymFile str =
            dsymTextBox.Text <- str

        member this.OpenStacktrace dsymFile logSource =
            (new StacktraceDialog (dsymFile, logSource)).ShowModal ()

        member this.OpenConfig adb maxLogLines negative =
            (new ConfigDialog (adb, maxLogLines, negative, pre.UpdateConfig)).ShowModal ()

        member this.OpenScreenshot adb device deviceTitle =
            (new ScreenshotDialog (adb, device, deviceTitle)).ShowModal ()

        member this.ChangeMode isDark idx lines =
            logAreas.[idx].Clear ()
            logAreas.[idx].ChangeMode isDark
            logAreas.[idx].AppendLines lines

        member this.SetModeCheckBox value =
            modeCheckBox.Checked <- System.Nullable(value)

        member this.SetWrap value idx lines =
            logAreas.[idx].Clear ()
            logAreas.[idx].SetWrap value
            logAreas.[idx].AppendLines lines
