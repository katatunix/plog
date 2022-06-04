module EtoUtils

open Eto.Forms
open Eto.Drawing

type TCell =
    | El of Control
    | StretchedEl of Control
    | EmptyElement
    | TableEl of Table

and TRow =
    | Row of TCell list
    | StretchedRow of TCell list
    | Spacing of Size
    | Pad of Padding

and Table = Tbl of TRow list

let tblWithOneRow row = Tbl [Row row]

let rec mkLayout (Tbl t) =
    let ret = new TableLayout ()
    for r in t  do
        let makeTd (tds : TCell list) =
            let row = TableRow()
            for td in tds do
                match td with
                | El c -> row.Cells.Add (TableCell(c, false))
                | StretchedEl c -> row.Cells.Add (TableCell(c, true))
                | EmptyElement -> row.Cells.Add (TableCell(null, true))
                | TableEl t -> row.Cells.Add (TableCell(mkLayout t, true))
            row
        match r with
        | Row tds -> let r = makeTd tds in ret.Rows.Add r
        | StretchedRow tds -> let r = makeTd tds in r.ScaleHeight <- true; ret.Rows.Add r
        | Spacing sz -> ret.Spacing <- sz
        | Pad pad -> ret.Padding <- pad
    ret

let codeFont =
    if OS.isWindows
    then new Font ("Consolas", 9.0f)
    else new Font ("Menlo", 11.0f)
