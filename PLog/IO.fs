module IO

open System.IO
open System.Runtime.Serialization.Formatters.Binary

let private writeValue stream x =
    let formatter = BinaryFormatter ()
    formatter.Serialize (stream, box x)

let private readValue stream =
    let formatter = BinaryFormatter ()
    let res = formatter.Deserialize stream
    unbox res

let writeValueToFile x path =
    use stream = new FileStream (path, FileMode.Create)
    writeValue stream x

let readValueFromFile path =
    use stream = new FileStream (path, FileMode.Open)
    readValue stream
    