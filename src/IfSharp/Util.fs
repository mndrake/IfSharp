namespace IfSharp.Kernel

open System
open System.IO
open System.Net
open System.Text
open System.Web

type BinaryOutput =
    { 
        ContentType: string;
        Data: obj
    }

type TableOutput = 
    {
        Columns: array<string>;
        Rows: array<array<string>>;
    }

type LatexOutput =
    {
        Latex: string;
    }

type HtmlOutput =
    {
        Html: string;
    }

[<AutoOpen>]
module ExtensionMethods =

    type Exception with
        
        /// Convenience method for getting the full stack trace by going down the inner exceptions
        member self.CompleteStackTrace() = 
            
            let mutable ex = self
            let sb = StringBuilder()
            while ex <> null do
                sb.Append(ex.GetType().Name)
                  .AppendLine(ex.Message)
                  .AppendLine(ex.StackTrace) |> ignore

                ex <- ex.InnerException

            sb.ToString()

type Util = 

    static member IsRunningOnMono() = 
        not <| obj.ReferenceEquals(Type.GetType("Mono.Runtime"), null) 

    /// Wraps a LatexOutput around a string in order to send to the UI.
    static member Latex (str) =
        { Latex = str}

    /// Wraps a LatexOutput around a string in order to send to the UI.
    static member Math (str) =
        { Latex = "$$" + str + "$$" }

    /// Wraps a HtmlOutput around a string in order to send to the UI.
    static member Html (str) =
        { Html = str }

    ///  Creates an array of strings with the specified properties and the item to get the values out of.
    static member Row (columns:seq<Reflection.PropertyInfo>) (item:'A) =
        columns
        |> Seq.map (fun p -> p.GetValue(item))
        |> Seq.map (fun x -> Convert.ToString(x))
        |> Seq.toArray

    /// Creates a TableOutput out of a sequence of items and a list of property names.
    static member Table (items:seq<'A>, ?propertyNames:seq<string>) =

        // get the properties
        let properties =
            if propertyNames.IsSome then
                typeof<'A>.GetProperties()
                |> Seq.filter (fun x -> (propertyNames.Value |> Seq.exists (fun y -> x.Name = y)))
                |> Seq.toArray
            else
                typeof<'A>.GetProperties()

        {
            Columns = properties |> Array.map (fun x -> x.Name);
            Rows = items |> Seq.map (Util.Row properties) |> Seq.toArray;
        }

    /// Downloads the specified url and wraps a BinaryOutput around the results.
    static member Url (url:string) =
        let req = WebRequest.Create(url)
        let res = req.GetResponse()
        use stream = res.GetResponseStream()
        use mstream = new MemoryStream()
        stream.CopyTo(mstream)
        { ContentType = res.ContentType; Data =  mstream.ToArray() }


    /// Wraps a BinaryOutput around image bytes with the specified content-type
    static member Image (bytes:seq<byte>, ?contentType:string) =
        {
            ContentType = if contentType.IsSome then contentType.Value else "image/jpeg";
            Data = bytes;
        }

    /// Loads a local image from disk and wraps a BinaryOutput around the image data.
    static member Image (fileName:string) =
        Util.Image (File.ReadAllBytes(fileName))


module zmq =
  open NetMQ

     /// Operator equivalent to `Socket.send`
  let (<<|) (socket: IOutgoingSocket) data = socket.Send(data)

  /// Operator equivalent to `Socket.sendMore`
  let (<~|) (socket: IOutgoingSocket) (data:byte[]) = socket.SendMore(data)