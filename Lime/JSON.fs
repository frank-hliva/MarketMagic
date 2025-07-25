namespace Lime

open Newtonsoft.Json
open Newtonsoft.Json.Converters
open System.IO

type JSON() =
    let settings = new JsonSerializerSettings()
    static member stringify (o : obj) = o |> JsonConvert.SerializeObject
    static member parse<'t> (json : string) = JsonConvert.DeserializeObject<'t>(json)
    static member parse (json : string) = JsonConvert.DeserializeObject(json)
    static member prettify (json : string) =
        use stringReader = new StringReader(json)
        use stringWriter = new StringWriter()
        let jsonReader = new JsonTextReader(stringReader)
        let jsonWriter = new JsonTextWriter(stringWriter, Formatting = Formatting.Indented)
        jsonWriter.WriteToken(jsonReader)
        stringWriter.ToString()