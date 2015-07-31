namespace IfSharp.Kernel

open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Reflection
open System.Text
open System.Threading
open System.Security.Cryptography

//open FSharp.Charting

open Newtonsoft.Json
open NetMQ
open NetMQ.Sockets


type IfSharpKernel(connectionInformation : ConnectionInformation) = 

    // startup 0mq stuff
    let context = NetMQContext.Create()

    // heartbeat
    let hbSocket = context.CreateRequestSocket()
    do hbSocket.Bind(String.Format("{0}://{1}:{2}", connectionInformation.transport, connectionInformation.ip, connectionInformation.hb_port))
        
    // shell
    let shellSocket = context.CreateRouterSocket()
    do shellSocket.Bind(String.Format("{0}://{1}:{2}", connectionInformation.transport, connectionInformation.ip, connectionInformation.shell_port))
        
    // control
    let controlSocket = context.CreateRouterSocket()
    do controlSocket.Bind(String.Format("{0}://{1}:{2}", connectionInformation.transport, connectionInformation.ip, connectionInformation.control_port))

    // stdin
    let stdinSocket = context.CreateRouterSocket()
    do stdinSocket.Bind(String.Format("{0}://{1}:{2}", connectionInformation.transport, connectionInformation.ip, connectionInformation.stdin_port))

    // iopub
    let ioSocket = context.CreatePublisherSocket()
    do ioSocket.Bind(String.Format("{0}://{1}:{2}", connectionInformation.transport, connectionInformation.ip, connectionInformation.iopub_port))

    let data = new List<BinaryOutput>()
    let payload = new List<Payload>()
    let compiler = FsCompiler(FileInfo(".").FullName)
    let mutable executionCount = 0
    let mutable lastMessage : Option<KernelMessage> = None

    /// Gets the header code to prepend to all items
    let headerCode = 
        let file = FileInfo(Assembly.GetEntryAssembly().Location)
        let dir = file.Directory.FullName
        let includeFile = Path.Combine(dir, "Include.fsx")
        let code = File.ReadAllText(includeFile)
        String.Format(code, dir.Replace("\\", "\\\\"))

    static let mutable logMutex = new Mutex()
        
    /// Splits the message up into lines and writes the lines to shell.log
    let logMessage (msg : string) =
        let fileName = "shell.log"
        let messages = 
            msg.Split('\r', '\n')
            |> Seq.filter (fun x -> x <> "")
            |> Seq.map (fun x -> String.Format("{0:yyyy-MM-dd HH:mm:ss} - {1}", DateTime.Now, x))
            |> Seq.toArray

        logMutex.WaitOne() |> ignore
        File.AppendAllLines(fileName, messages)
        logMutex.ReleaseMutex()

    /// Logs the exception to the specified file name
    let handleException (ex : exn) = 
        let message = ex.CompleteStackTrace()
        logMessage message

    /// Decodes byte array into a string using UTF8
    let decode (bytes) = Encoding.UTF8.GetString(bytes)

    /// Encodes a string into a byte array using UTF8
    let encode (str : string) = Encoding.UTF8.GetBytes(str)

    /// Deserializes a dictionary from a JSON string
    let deserializeDict (str) = JsonConvert.DeserializeObject<Dictionary<string, string>>(str)

    /// Serializes any object into JSON
    let serialize (obj) =
        let ser = JsonSerializer()
        let sw = new StringWriter()
        ser.Serialize(sw, obj)
        sw.ToString()

    /// Constructs an 'envelope' from the specified socket
    let recvMessage (socket:NetMQSocket) = 
        
        // receive all parts of the message
        let message =
            socket.ReceiveMessages()
            |> Seq.map decode
            |> Seq.toArray

        // find the delimiter between IDS and MSG
        let idx = Array.IndexOf(message, "<IDS|MSG>")
        let idents = message.[0..idx - 1]
        let messageList = message.[idx + 1..message.Length - 1]

        // detect a malformed message
        if messageList.Length < 4 then failwith ("Malformed message")

        // assemble the 'envelope'
        let hmac             = messageList.[0]
        let headerJson       = messageList.[1]
        let parentHeaderJson = messageList.[2]
        let metadata         = messageList.[3]
        let contentJson      = messageList.[4]
        
        let header           = JsonConvert.DeserializeObject<Header>(headerJson)
        let parentHeader     = JsonConvert.DeserializeObject<Header>(parentHeaderJson)
        let metaDataDict     = deserializeDict (metadata)
        let content          = ShellMessages.Deserialize (header.msg_type) (contentJson)

        lastMessage <- Some
            {
                Identifiers = idents |> Seq.toList;
                HmacSignature = hmac;
                Header = header;
                ParentHeader = parentHeader;
                Metadata = metadata;
                Content = content;
            }

        lastMessage.Value

    /// Convenience method for creating a header
    let createHeader (messageType) (sourceEnvelope) =
        {
            msg_type = messageType;
            msg_id = Guid.NewGuid().ToString();
            session = sourceEnvelope.Header.session;
            username = sourceEnvelope.Header.username;
            version = "5.0";
        }

    /// Convenience method for sending a message
    let sendMessage (socket:NetMQSocket) envelope messageType content =
        #if DEBUG
        printfn "send: %A" content
        #endif

        let hmac = new HMACSHA256(encode connectionInformation.key)

        let toHexString (array:byte[]) =
            let hex = new StringBuilder(array.Length * 2)
            for b in array do
                hex.AppendFormat("{0:x2}", b) |> ignore
            hex.ToString()

        let headerJson = serialize <| createHeader messageType envelope
        let ParentHeaderJson = serialize envelope.Header
        let metadataJson = "{}"
        let contentJson = serialize content

        let hmacSignature = 
            [headerJson; ParentHeaderJson; metadataJson; contentJson]
            |> String.Concat
            |> encode 
            |> hmac.ComputeHash
            |> toHexString

        let msg = NetMQMessage()

        for ident in envelope.Identifiers do msg.Append ident

        msg.Append "<IDS|MSG>"
        msg.Append hmacSignature
        msg.Append headerJson
        msg.Append ParentHeaderJson
        msg.Append metadataJson
        msg.Append contentJson
        socket.SendMessage(msg)
        
    /// Convenience method for sending the state of the kernel
    let sendState (envelope) (state) =
        sendMessage ioSocket envelope "status" { execution_state = state } 

    /// Convenience method for sending the state of 'busy' to the kernel
    let sendStateBusy (envelope) =
        sendState envelope "busy"

    /// Convenience method for sending the state of 'idle' to the kernel
    let sendStateIdle (envelope) =
        sendState envelope "idle"

    /// Handles a 'kernel_info_request' message
    let kernelInfoRequest(msg : KernelMessage) (content : KernelRequest) = 
        #if DEBUG
        printfn "** kernel_info_request **"
        #endif

        let content = 
            {
                protocol_version = "5.0"; 
                implementation = "ifsharp_kernel";
                implementation_version = "1.0";
                language_info = 
                    {
                        name = "fsharp";
                        version = "3.1";
                        mimetype = "text/x-fsharp";
                        codemirror_mode = "fsharp";
                        file_extension = ".fsx" 
                    }
            }

        sendMessage shellSocket msg "kernel_info_reply" content

    /// Sends display data information immediately
    let sendDisplayData (contentType) (displayItem) (messageType) =
        data.Add( { ContentType = contentType; Data = displayItem } )
        
        if lastMessage.IsSome then

            let d = Dictionary<string, obj>()
            d.Add(contentType, displayItem)

            let reply = { execution_count = executionCount; data = d; metadata = Dictionary<string, obj>() }
            sendMessage ioSocket lastMessage.Value messageType reply

    /// Sends a message to pyout
    let pyout (message) = sendDisplayData "text/plain" message "execute_result"

    /// Preprocesses the code and evaluates it
    let preprocessAndEval(code) = 

        logMessage code

        // preprocess
        let results = compiler.NuGetManager.Preprocess(code)
        let newCode = String.Join("\n", results.FilteredLines)

        // do nuget stuff
        for package in results.Packages do
            if not (String.IsNullOrWhiteSpace(package.Error)) then
                pyout ("NuGet error: " + package.Error)
            else
                pyout ("NuGet package: " + package.Package.Value.Id)
                for frameworkAssembly in package.FrameworkAssemblies do
                    pyout ("Referenced Framework: " + frameworkAssembly.AssemblyName)
                    let code = String.Format(@"#r @""{0}""", frameworkAssembly.AssemblyName)
                    fsiEval.EvalInteraction(code)

                for assembly in package.Assemblies do
                    let fullAssembly = compiler.NuGetManager.GetFullAssemblyPath(package, assembly)
                    pyout ("Referenced: " + fullAssembly)

                    let code = String.Format(@"#r @""{0}""", fullAssembly)
                    fsiEval.EvalInteraction(code)

        if not <| String.IsNullOrEmpty(newCode) then
            fsiEval.EvalInteraction(newCode)
    
    /// Handles an 'execute_request' message
    let executeRequest(msg : KernelMessage) (content : ExecuteRequest) = 
        
        // clear some state
        sbOut.Clear() |> ignore
        sbErr.Clear() |> ignore
        data.Clear()
        payload.Clear()

        // only increment if we are not silent
        if content.silent = false then executionCount <- executionCount + 1
        
        // send busy
        sendStateBusy msg
        sendMessage ioSocket msg "pyin" { code = content.code; execution_count = executionCount  }

        // evaluate
        let ex = 
            try
                // preprocess
                preprocessAndEval (content.code)
                None
            with
            | exn -> 
                handleException exn
                Some exn

        if sbErr.Length > 0 then
            let err = sbErr.ToString().Trim()
            let executeReply =
                {
                    status = "error";
                    execution_count = executionCount;
                    ename = "generic";
                    evalue = err;
                    traceback = [||]
                }

            sendMessage shellSocket msg "execute_reply" executeReply
            sendMessage ioSocket msg "stream" { name = "stderr"; text = err; }
        else
            let executeReply =
                {
                    status = "ok";
                    execution_count = executionCount;
                    payload = payload |> Seq.toList;
                    user_expressions = Dictionary<string, obj>()
                }

            sendMessage shellSocket msg "execute_reply" executeReply

            // send all the data
            if not <| content.silent then
                if data.Count = 0 then
                    let lastExpression = GetLastExpression()
                    match lastExpression with
                    | Some(it) -> 
                        
                        let printer = Printers.findDisplayPrinter(it.ReflectionType)
                        let (_, callback) = printer
                        let callbackValue = callback(it.ReflectionValue)
                        sendDisplayData callbackValue.ContentType callbackValue.Data "execute_result"

                    | None -> ()

        // we are now idle
        sendStateIdle msg

    /// Handles a 'complete_request' message
    let completeRequest (msg : KernelMessage) (content : CompleteRequest) = 
        let decls, pos, filterString = GetDeclarations(content.code, 0, content.cursor_pos)
        let items = decls |> Array.map (fun x -> x.Value)
        let newContent = 
            {
                matches = items
                cursor_start = pos
                cursor_end = content.cursor_pos
                metadata = Dictionary<string, obj>()
                status = "ok"
            }

        sendMessage (shellSocket) (msg) ("complete_reply") (newContent)

//    /// Handles a 'connect_request' message
//    let connectRequest (msg : KernelMessage) (content : ConnectRequest) = 
//
//        let reply =
//            {
//                hb_port = connectionInformation.hb_port;
//                iopub_port = connectionInformation.iopub_port;
//                shell_port = connectionInformation.shell_port;
//                stdin_port = connectionInformation.stdin_port; 
//            }
//
//        logMessage "connectRequest()"
//        sendMessage shellSocket msg "connect_reply" reply

//    /// Handles a 'shutdown_request' message
//    let shutdownRequest (msg : KernelMessage) (content : ShutdownRequest) =
//
//        // TODO: actually shutdown        
//        let reply = { restart = true; }
//
//        sendMessage shellSocket msg "shutdown_reply" reply
//
//    /// Handles a 'history_request' message
//    let historyRequest (msg : KernelMessage) (content : HistoryRequest) =
//
//        // TODO: actually handle this
//        sendMessage shellSocket msg "history_reply" { history = [] }

    /// Handles a 'inspect_request' message
    let inspectRequest (msg : KernelMessage) (content : InspectRequest) =
        // TODO: actually handle this
        #if DEBUG
        //content.code

        // in our custom UI we put all cells in content.text and more information in content.block
        // the position is contains the selected index and the relative character and line number
//
//        let codes = cells |> Seq.append [headerCode]
////        let position = JsonConvert.DeserializeObject<BlockType>(content.block)
//        // calculate absolute line number
//        let lineOffset = 
//            codes
//            |> Seq.take (content.cursor_pos + 1)
//            |> Seq.map (fun x -> x.Split('\n').Length)
//            |> Seq.sum
//        let realLineNumber = lineOffset + 1
//        let codeString = String.Join("\n", codes)



        //compiler.GetToolTipText(codeString,realLineNumber,1)

        let dataDict = Dictionary<string,obj>()
        dataDict.["text/plain"] <- box "hello world"

        let reply = 
            {
                status = "ok";
                found = true;
                data = dataDict;
                metadata = Dictionary<string,obj>()
            }

        sendMessage shellSocket msg "inspect_reply" reply
        #else
        ()
        #endif

    /// Loops forever receiving messages from the client and processing them
    let doShell() =

        try
            preprocessAndEval headerCode
        with
        | exn -> handleException exn

        logMessage (sbErr.ToString())
        logMessage (sbOut.ToString())

        while true do

            let msg = recvMessage (shellSocket)

            #if DEBUG
            printfn "request: %A" msg.Content
            #endif

            try
                match msg.Content with
                | KernelRequest(r)       -> kernelInfoRequest msg r
                | ExecuteRequest(r)      -> executeRequest msg r
                | CompleteRequest(r)     -> completeRequest msg r
                //| ConnectRequest(r)      -> connectRequest msg r
                //| ShutdownRequest(r)     -> shutdownRequest msg r
                //| HistoryRequest(r)      -> historyRequest msg r
                | InspectRequest(r)   -> inspectRequest msg r
                | _                      -> logMessage (String.Format("Unknown content type. msg_type is `{0}`", msg.Header.msg_type))
            with 
            | ex -> handleException ex
   
    /// Loops repeating message from the client
    let doHeartbeat() =
        try
            while true do
                let bytes = hbSocket.Receive()
                hbSocket.Send(bytes)
        with
        | ex -> handleException ex

    /// Clears the display
    member __.ClearDisplay () =
        if lastMessage.IsSome then
            sendMessage (ioSocket) (lastMessage.Value) ("clear_output") { wait = false; stderr = true; stdout = true; other = true; }

    /// Sends auto complete information to the client
    member __.AddPayload (text) =
        let dataDict = Dictionary<string,obj>()
        dataDict.["text/plain"] <- text
        payload.Add( { source = "page"; start = 1; data = dataDict })

    /// Adds display data to the list of display data to send to the client
    member __.SendDisplayData (contentType, displayItem) =
        sendDisplayData contentType displayItem "display_data"

    /// Starts the kernel asynchronously
    member __.StartAsync() = 
        
        Async.Start (async { doHeartbeat() } )
        Async.Start (async { doShell() } )
