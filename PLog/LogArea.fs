namespace PLog

type LogArea =
    abstract GetEtoControl : unit -> Eto.Forms.Control
    abstract Clear : unit -> unit
    abstract AppendLines : seq<string * Domain.Severity> -> unit
    abstract GoEnd : unit -> unit
    abstract SetWrap : bool -> unit
    abstract ChangeMode : bool -> seq<string * Domain.Severity> -> unit
