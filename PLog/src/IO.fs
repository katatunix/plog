module IO

open System.IO
open FSharp.Json

let writeValueToFile value (path: string) =
    use stream = new StreamWriter (path)
    Json.serialize value |> stream.Write

let readValueFromFile (path: string) =
    use stream = new StreamReader (path)
    stream.ReadToEnd () |> Json.deserialize
