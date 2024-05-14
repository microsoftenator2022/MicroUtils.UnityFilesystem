#nowarn "3391"

open System.Diagnostics
open System.IO

open Newtonsoft.Json.Linq

type MicroOption<'a> = MicroUtils.Functional.Option<'a>

open UnityDataTools
open UnityDataTools.FileSystem

open MicroUtils.UnityFilesystem
open MicroUtils.UnityFilesystem.Parsers
open MicroUtils.UnityFilesystem.Converters

let mountPoint = @"archive:/"

let toMicroOption<'a> (value : ValueOption<'a>) : MicroOption<'a> = value

let toValueOption<'a> (microOption : MicroOption<'a>) : 'a voption =
    if microOption.IsSome then
        ValueSome microOption.Value
    else ValueNone

//let toMicroOption<'a> (valueOption : ValueOption<'a>) : MicroOption<'a> =
//    match valueOption with
//    | ValueSome some -> MicroOption.Some(some)
//    | ValueNone -> MicroOption<'a>.None

let tryGetObject (tto : ITypeTreeValue) : ITypeTreeObject voption =
    tto.TryGetObject()
    |> toValueOption

let tryGetField<'a> fieldName (tto : TypeTreeObject) : 'a voption =
    tto.TryGetField<'a>(fieldName)
    |> toValueOption
    |> ValueOption.map (fun f -> f.Invoke())

let rec dumpTypeTree (ttn : TypeTreeNode) = seq {
    yield sprintf "%s : %s" ttn.Name ttn.Type
        
    for n in ttn.Children do
        yield!
            dumpTypeTree n
            |> Seq.map (sprintf "  %s")
}

let rec getPPtrs (tto : ITypeTreeValue) = seq {
    match tto with
    | :? TypeTreeValue<PPtr> as v ->
        yield v.Value
    | :? TypeTreeValue<ITypeTreeValue[]> as arr ->
        yield! arr.Value |> Seq.collect getPPtrs
    | :? ITypeTreeObject as o ->
        yield! o.ToDictionary().Values |> Seq.collect getPPtrs
    | _ -> ()
}

let invalidFileChars = Path.GetInvalidFileNameChars()

let getName (tto : ITypeTreeValue) =
    match tto with
    | :? ITypeTreeObject as tto ->
        let name =
            match tto.ToDictionary().TryGetValue("m_Name") with
            | true, (:? TypeTreeValue<string> as name) -> name.Value
            | _ -> ""

        name
        |> Seq.map (fun c -> if invalidFileChars |> Array.contains c then '_' else c)
        |> Seq.toArray
        |> System.String
    | _ -> tto.Node.Name
    
let formatAsFileSize (size : int64) : string =
    if size > 10L * pown 2L 40 then
        size / (pown 2L 30)
        |> sprintf "%i TiB"
    elif size > 10L * pown 2L 30 then
        size / (pown 2L 30)
        |> sprintf "%i GiB"
    elif size > 10L * pown 2L 20 then
        size / (pown 2L 20)
        |> sprintf "%i MiB"
    elif size > 10L * pown 2L 10 then
        size / (pown 2L 10)
        |> sprintf "%i KiB"
    else
        size |> sprintf "%i B"

let getDependencyFiles dependencylist =
    if System.IO.File.Exists dependencylist then
        let json =
            File.ReadAllText(dependencylist)
            |> JObject.Parse
    
        json.Property("BundleToDependencies")
        |> Option.ofObj
        |> Option.bind (fun p -> 
            match p.Value with
            | :? JObject as o -> Some o
            | _ -> None)
        |> function
        | Some bundles ->
            bundles.Properties()
            |> Seq.map (fun p -> p.Name, p.Value)
            |> Seq.choose (function
            | _, :? JArray as (name, dependencies) ->
                let deps = 
                    dependencies
                    |> Seq.map (fun d -> d.ToObject<string>())
                    |> Seq.toArray
                Some (name, deps)
            | _ -> None)
        | None -> Seq.empty
        |> Map.ofSeq
    else Map.empty

let mountDependencies (bundlePath : string) =
    let mutable mounted : string list = []

    let rec mountDependenciesInner (bundlePath : string) =
        let dir = Path.GetDirectoryName(bundlePath)
        let name = Path.GetFileName(bundlePath)
        
        printfn "Mounting dependencies of %s" name

        Path.Join(dir, "dependencylist.json")
        |> getDependencyFiles
        |> Map.tryFind name
        |> Option.map (fun deps ->
            deps
            |> Seq.collect (fun d ->
                seq {
                    if mounted |> Seq.contains d |> not then
                        let path = Path.Join(dir, d)

                        printfn "Mounting %s" path
                        yield UnityFileSystem.MountArchive(path, mountPoint)

                        mounted <- d :: mounted

                        yield! mountDependenciesInner path
                })
            |> Seq.toList)
        |> function
        | Some list -> list
        | None -> List.empty

    mountDependenciesInner bundlePath

let printArchiveFiles bundlePath =

    UnityFileSystem.Init()

    use archive = UnityFileSystem.MountArchive(bundlePath, mountPoint)

    let dependencies = mountDependencies bundlePath

    let dependencyNodes =
        dependencies
        |> Seq.collect (fun archive -> archive.Nodes)
        |> Seq.cache

    for n in archive.Nodes do
        printfn "%s" n.Path
        n.Size |> formatAsFileSize |> printfn "  Size: %s"
        printfn "  Flags %A" n.Flags

    for n in dependencyNodes do
        printfn "%s" n.Path
        n.Size |> formatAsFileSize |> printfn "  Size: %s"
        printfn "  Flags %A" n.Flags


    UnityFileSystem.Cleanup()

let dumpStreamData bundlePath dumpPath =

    UnityFileSystem.Init()

    use archive = UnityFileSystem.MountArchive(bundlePath, mountPoint)

    for node in archive.Nodes |> Seq.where (fun n -> n.Flags.HasFlag(ArchiveNodeFlags.SerializedFile)) do
        let path = $"{mountPoint}{node.Path}"
        printfn "%s" path

        use sf = UnityFileSystem.OpenSerializedFile(path)
        use sfReader = new UnityBinaryFileReader(path)

        let sw = Stopwatch.StartNew()
        let mutable i = 0

        let mutable fileReader : (string * UnityBinaryFileReader) voption = ValueNone

        //let getReader rPath (bufferSize : uint) =
        //    match fileReader with
        //    | ValueSome (readerPath, fileReader) when readerPath = rPath ->
        //        ValueSome fileReader
        //    | maybeReader ->
        //        match maybeReader with
        //        | ValueSome (_, reader) ->
        //            reader.Dispose()
        //        | _ -> ()
                
        //        fileReader <- ValueSome (rPath, new UnityBinaryFileReader(rPath, bufferSize |> int))
        //        sfReader |> ValueSome
        try
            for objectInfo in sf.Objects do
                let tto = TypeTreeValue.Get(sf, sfReader, objectInfo)

                let sis =
                    tto.Find(fun tto -> tto :? TypeTreeValue<StreamingInfo>)
                    |> Seq.map (fun si -> (si :?> TypeTreeValue<StreamingInfo>).Value)
                    |> Seq.cache
                    |> Seq.mapi (fun i si ->
                        let name = tto |> getName
                        let filePath = Path.Join($"{name}.{objectInfo.Id}", $"{i}.{tto.Node.Type}")

                        filePath, si)

                for (filePath, si) in sis do
                    let data =
                        si.TryGetData()
                        |> toValueOption

                    match data with
                    | ValueSome arr ->
                        let filePath = Path.Join(dumpPath, "StreamData", filePath)
                        let dirPath = Path.GetDirectoryName(filePath)

                        if Directory.Exists(dirPath) |> not then
                            Directory.CreateDirectory(dirPath) |> ignore

                        printfn $"Dumping {si.Size} bytes to {filePath}"

                        File.WriteAllBytes(filePath, arr)
                        i <- i + 1
                    | _ -> ()

        finally
            match fileReader with
            | ValueSome (path, reader) ->
                reader.Dispose()
                fileReader <- ValueNone
            | _ -> ()

        sw.Stop()

        printfn "Dumped stream data x %i in %ims" i sw.ElapsedMilliseconds

    UnityFileSystem.Cleanup()

let dump bundlePath outputDir =
    UnityFileSystem.Init()

    use archive = UnityFileSystem.MountArchive(bundlePath, mountPoint)

    let dependencies = mountDependencies bundlePath

    let dependencyNodes =
        dependencies
        |> Seq.collect (fun archive -> archive.Nodes)
        |> Seq.cache

    let mutable serializedFiles : SerializedFile list = []

    let getSerializedFile path =
        serializedFiles
        |> Seq.tryFind (fun sf -> sf.Path = path)
        |> function
        | Some sf -> sf |> ValueSome
        | None ->
            if
                dependencyNodes
                |> Seq.exists (fun n ->
                    n.Flags.HasFlag(ArchiveNodeFlags.SerializedFile)
                    && $"{mountPoint}{n.Path}" = path)
            then
                let sf = UnityFileSystem.OpenSerializedFile(path)
                serializedFiles <- sf :: serializedFiles
                sf |> ValueSome
            else ValueNone

    let mutable readers : Map<string, UnityBinaryFileReader> = Map.empty

    let getReader path =
        readers |> Map.tryFind path
        |> function
        | Some reader -> reader
        | _ ->
            let reader = new UnityBinaryFileReader(path)
            readers <- readers |> Map.add path reader
            reader
        |> ValueSome

    for node in archive.Nodes |> Seq.where (fun n -> n.Flags.HasFlag(ArchiveNodeFlags.SerializedFile)) do
        let path = $"{mountPoint}{node.Path}"
        printfn "open %s" path

        use sf = UnityFileSystem.OpenSerializedFile(path)

        serializedFiles <- sf :: serializedFiles

        let newReader() = new UnityBinaryFileReader(path)

        let reader = newReader()

        //let getReader readerPath =
        //    if readerPath = path then reader |> ValueSome
        //    else getReader path
        
        let sw = Stopwatch.StartNew()

        let trees =
            sf.Objects
            |> Seq.map (fun o -> sf.GetTypeTreeRoot o.Id)
            |> Seq.distinctBy (fun t -> t.Handle)
            |> Seq.groupBy (fun t -> t.Type)
            |> Seq.collect (fun (t, tts) ->
                if tts |> Seq.length = 1 then
                    [t, tts |> Seq.head] |> Seq.ofList
                else
                    tts
                    |> Seq.mapi (fun i tt -> $"{t}{i}", tt))

        let typeTreesDumpPath = Path.Join(outputDir, "TypeTrees")

        if Directory.Exists typeTreesDumpPath |> not then
            Directory.CreateDirectory typeTreesDumpPath |> ignore

        for (n, tree) in trees do
            File.WriteAllLines(Path.Join(typeTreesDumpPath, $"{n}.txt"), dumpTypeTree tree)

        sw.Stop()

        printfn "Dumped type trees in %ims" sw.ElapsedMilliseconds

        reader.Dispose()
        let reader = newReader()

        sw.Restart()

        //let mutable i = 0

        let ttObjects =
            sf.Objects
            |> Seq.map (fun o -> o.Id, TypeTreeValue.Get(sf, reader, o))
            |> Seq.toArray

        sw.Stop()

        printfn "Got %i objects in %ims" (ttObjects.Length) sw.ElapsedMilliseconds

        printfn "Dump start"

        sw.Restart()

        let dir =  Path.Join(outputDir, node.Path)

        if Directory.Exists(dir) |> not then
            Directory.CreateDirectory(dir) |> ignore

        ttObjects
        |> Seq.map (fun (oid, tto) ->

        //for oid, tto in ttObjects do
            async {
                match tto with
                | :? ITypeTreeObject as tto ->
                    let name =
                        match tto.ToDictionary().TryGetValue("m_Name") with
                        | true, (:? TypeTreeValue<string> as name) -> name.Value
                        | _ -> ""

                    let name =
                        name
                        |> Seq.map (fun c -> if invalidFileChars |> Array.contains c then '_' else c)
                        |> Seq.toArray
                        |> System.String

                    let filename = Path.Join(dir, $"{name}.{oid}.{tto.Node.Type}.txt")
                    
                    return!
                        File.WriteAllTextAsync(filename, tto.ToString())
                        |> Async.AwaitTask
                    //File.WriteAllText(filename, tto.ToString())

                | _ -> ()

                //i <- i + 1
            }
        )
        |> Async.Parallel
        |> Async.RunSynchronously
        |> ignore

        sw.Stop()

        printfn "Dump completed in %ims" sw.ElapsedMilliseconds

        reader.Dispose()
        let reader = newReader()

        sw.Restart()

        let pptrs =
            ttObjects
            |> Seq.mapi (fun i (_, o) -> o)
            |> Seq.collect getPPtrs
            |> Seq.cache
            |> Seq.distinct

        pptrs
        |> Seq.length
        |> printfn "Found %i unique PPtrs"

        reader.Dispose()
        let reader = newReader()

        let pptrs =
            pptrs
            |> Seq.map (fun pptr ->
                let tto =
                    pptr.TryDereference(
                        getSerializedFile >> toMicroOption,
                        //(fun readerPath -> (if readerPath = path then MicroOption.Some(reader) else MicroOption<UnityBinaryFileReader>.None))
                        getReader >> toMicroOption
                    )
                    |> toValueOption
                    |> ValueOption.bind (fun tto -> tto.TryGetObject() |> toValueOption)

                let ttoName =
                    tto
                    |> ValueOption.bind (fun tto -> tto.TryGetField("m_Name") |> toValueOption)
                    |> ValueOption.map(fun f -> f.Invoke().ToString())

                pptr, tto, ttoName)
            |> Seq.cache
    
        seq {
            for (pptr, tto, ttoName) in pptrs do
                match tto with
                | ValueSome tto ->
                    let name = match ttoName with ValueSome name -> name | _ -> "<anonymous>"

                    yield sprintf "%A -> %s : %s (%s)" pptr name tto.Node.Type (tto.Node.CSharpType.ToString())
                | _ -> ()
        }
        |> fun lines -> File.WriteAllLines(Path.Join(outputDir, "pptrs.txt"), lines)

        sw.Stop()

        (pptrs
        |> Seq.length,
        sw.ElapsedMilliseconds)
        ||> printfn "Dumped %i PPtrs in %ims" 

    printfn "Done"

    for r in readers.Values do
        r.Dispose()

    for sf in serializedFiles do
        sf.Dispose()

    for archive in dependencies do
        archive.Dispose()

    UnityFileSystem.Cleanup()

let extRefs bundlePath =
    UnityFileSystem.Init()

    use archive = UnityFileSystem.MountArchive(bundlePath, mountPoint)

    for node in archive.Nodes |> Seq.where (fun n -> n.Flags.HasFlag(ArchiveNodeFlags.SerializedFile)) do
        let nodePath = $"{mountPoint}{node.Path}"
        node.Path |> printfn "%s"
        printfn "External references:"

        use sf = UnityFileSystem.OpenSerializedFile(nodePath)

        for er in sf.ExternalReferences |> Seq.toArray do
            let pathAsBytes = er.Path.ToCharArray() |> Array.map(fun c -> c |> byte)
            printfn $"  Path: {er.Path}"
            printfn $"    Guid: {er.Guid}"
            printfn $"    Type: {er.Type}"

    UnityFileSystem.Cleanup()

let decodeTextures bundlePath outputDir =
    UnityFileSystem.Init()

    use archive = UnityFileSystem.MountArchive(bundlePath, mountPoint)

    for node in archive.Nodes |> Seq.where (fun n -> n.Flags.HasFlag(ArchiveNodeFlags.SerializedFile)) do
        let path = $"{mountPoint}{node.Path}"
        printfn "%s" path

        use sf = UnityFileSystem.OpenSerializedFile(path)
        use sfReader = new UnityBinaryFileReader(path)

        let mutable readers : Map<string, UnityBinaryFileReader> = Map.empty

        let dir = Path.Join(outputDir, "Texture2D")

        if not <| Directory.Exists(dir) then
            Directory.CreateDirectory(dir)
            |> ignore

        sf.Objects
        |> Seq.map (fun o -> o, TypeTreeValue.Get(sf, sfReader, o))
        |> Seq.choose(fun (o, t) -> match t with :? Texture2D as t -> Some (o, t) | _ -> None)
        |> Seq.iter(fun (o, t) ->
            getName t
            |> printfn "Dump texture \"%s\""
            
            let getReader path =
                readers |> Map.tryFind path
                |> function
                | Some reader -> reader
                | _ ->
                    let reader = new UnityBinaryFileReader(path)
                    readers <- readers |> Map.add path reader
                    reader
                |> ValueSome

            let fileName = Path.Join(dir, $"{getName t}.{o.Id}.png")
            
            if File.Exists(fileName) |> not then
                let buf = Array.zeroCreate (t.Width * t.Height * 4)

                let success = Texture2DConverter.DecodeTexture2D(t, System.Span(buf), fun path -> (getReader path))

                if success then
                    let format = System.Drawing.Imaging.PixelFormat.Format32bppArgb

                    use image = new System.Drawing.Bitmap(t.Width, t.Height, format)

                    let rect = new System.Drawing.Rectangle(0, 0, t.Width, t.Height)
                
                    let data = image.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, format)
                    let ptr = data.Scan0

                    System.Runtime.InteropServices.Marshal.Copy(buf, 0, ptr, data.Stride * data.Height)
                    image.UnlockBits(data)
                    image.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipY)

                    printfn $"Saving texture to {fileName}"

                    image.Save(fileName, System.Drawing.Imaging.ImageFormat.Png)
        )

        for reader in readers |> Map.values do
            reader.Dispose()

    UnityFileSystem.Cleanup()

[<EntryPoint>]
let main args =

    if args.Length > 0 && args[0] <> "" then
        let bundlePath = args[0]

        printArchiveFiles bundlePath

        let outputDir =
            if args.Length > 1 && args[1] <> "" then args[1] else $"{Path.GetFileName(bundlePath)}_dump"

        dump bundlePath outputDir
        //Debugger.Break()
        dumpStreamData bundlePath outputDir
        //Debugger.Break()
        decodeTextures bundlePath outputDir
        //Debugger.Break()
    0
