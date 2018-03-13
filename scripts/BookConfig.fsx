#r "../libs/SharpYaml.dll"

open System.IO
open System.Collections.Generic
open SharpYaml.Serialization

type BookConfig =
    struct
        val mutable Isbn : string
        val mutable Date : string
        val mutable Generated : bool
        new(isbn, date, generated) =
            { Isbn = isbn
              Date = date
              Generated = generated }
    end

let private getYamlSerializer =
    let serializerSettings = new SerializerSettings()
    serializerSettings.NamingConvention <- new FlatNamingConvention()
    serializerSettings.EmitTags <- false
    serializerSettings.EmitAlias <- false
    new Serializer(serializerSettings)

let Load configFileName =
    let serializer = getYamlSerializer
    use configFile = new FileStream(configFileName, FileMode.Open)
    let books = serializer.Deserialize<List<BookConfig>>(configFile)
    books

let ToYaml booksConfig =
    let serializer = getYamlSerializer
    use stringWriter = new StringWriter()
    serializer.Serialize(stringWriter, (new List<BookConfig>(Seq.toArray booksConfig)))
    stringWriter.ToString()
