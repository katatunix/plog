module IO

open System.IO
open System.Text.Json
open System.Text.Json.Serialization

let options = JsonFSharpOptions.Default().ToJsonSerializerOptions()

let writeValueToFile (value: 'a) (path: string) =
    use stream = new StreamWriter (path)
    JsonSerializer.Serialize(value, options)
    |> stream.Write

let readValueFromFile (path: string) =
    use stream = new StreamReader (path)
    let str = stream.ReadToEnd()
    JsonSerializer.Deserialize(str, options)
