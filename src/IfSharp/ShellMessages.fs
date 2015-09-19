namespace IfSharp.Kernel

open System
open System.Collections.Generic
open Newtonsoft.Json

type ExecuteRequest =
    {
        // # Source code to be executed by the kernel, one or more lines.
        code: string;

        // # A boolean flag which, if True, signals the kernel to execute
        // # this code as quietly as possible.  This means that the kernel
        // # will compile the code with 'exec' instead of 'single' (so
        // # sys.displayhook will not fire), forces store_history to be False,
        // # and will *not*:
        // #   - broadcast exceptions on the PUB socket
        // #   - do any logging
        // #
        // # The default is False.
        silent: bool;

        // # A boolean flag which, if True, signals the kernel to populate history
        // # The default is True if silent is False.  If silent is True, store_history
        // # is forced to be False.
        store_history: bool;

        // # A list of variable names from the user's namespace to be retrieved.
        // # What returns is a rich representation of each variable (dict keyed by name).
        // # See the display_data content for the structure of the representation data.
        //user_variables: array<string>;

        // # Similarly, a dict mapping names to expressions to be evaluated in the
        // # user's dict.
        user_expressions: Dictionary<string, obj>;

        // # Some frontends (e.g. the Notebook) do not support stdin requests. If
        // # raw_input is called from code executed from such a frontend, a
        // # StdinNotImplementedError will be raised.
        allow_stdin: bool;
    }

type Payload = 
    {
        source: string;
        start: int;
        data: Dictionary<string,obj>;
    }

type ExecuteReplyOk =
    {
        status: string;
        execution_count: int;

        // # 'payload' will be a list of payload dicts.
        // # Each execution payload is a dict with string keys that may have been
        // # produced by the code being executed.  It is retrieved by the kernel at
        // # the end of the execution and sent back to the front end, which can take
        // # action on it as needed.  See main text for further details.
        payload: list<Payload>;

        // # Results for the user_variables and user_expressions.
        //user_variables: dict;
        user_expressions: Dictionary<string, obj>;
    }

type ExecuteReplyError =
    {
        status: string;
        execution_count: int;

        ename: string;  // # Exception name, as a string
        evalue: string; // # Exception value, as a string

        // # The traceback will contain a list of frames, represented each as a
        // # string.  For now we'll stick to the existing design of ultraTB, which
        // # controls exception level of detail statefully.  But eventually we'll
        // # want to grow into a model where more information is collected and
        // # packed into the traceback object, with clients deciding how little or
        // # how much of it to unpack.  But for now, let's start with a simple list
        // # of strings, since that requires only minimal changes to ultratb as
        // # written.
        traceback: array<string>;
    }

type InspectRequest =
    {
        // # The code context in which introspection is requested
        // # this may be up to an entire multiline cell.
        code : string;

        // # The cursor position within 'code' (in unicode characters) where inspection is requested
        cursor_pos : int;

        // # The level of detail desired.  In IPython, the default (0) is equivalent to typing
        // # 'x?' at the prompt, 1 is equivalent to 'x??'.
        // # The difference is up to kernels, but in IPython level 1 includes the source code
        // # if available.
        detail_level : int;
    }

type ArgsSpec =
    {
        // # The names of all the arguments
        args: array<string>;
        
        // # The name of the varargs (*args), if any
        varargs: string;
            
        // # The name of the varkw (**kw), if any
        varkw : string;
            
        // # The values (as strings) of all default arguments.  Note
        // # that these must be matched *in reverse* with the 'args'
        // # list above, since the first positional args have no default
        // # value at all.
        defaults : array<string>;
    }

type InspectReply =
    {
        // # 'ok' if the request succeeded or 'error', with error information as in all other replies.
        status : string;
        found: bool;

        // # data can be empty if nothing is found
        data : Dictionary<string,obj>;
        metadata : Dictionary<string,obj>;
    }

type CompleteRequest =
    {
        // # The code context in which completion is requested
        // # this may be up to an entire multiline cell, such as
        // # 'foo = a.isal'
        code: string;

        // # The cursor position within 'code' (in unicode characters) where completion is requested
        cursor_pos: int
    }

type BlockType =
    {
        selectedIndex: int;
        ch: int;
        line: int;
    }

type CompleteReplyStatus = 
    | Ok
    | Error

type CompleteReply =
    {
        // # The list of all matches to the completion request, such as
        // # ['a.isalnum', 'a.isalpha'] for the above example.
        matches: array<string>;

        // # The range of text that should be replaced by the above matches when a completion is accepted.
        // # typically cursor_end is the same as cursor_pos in the request.
        cursor_start : int;
        cursor_end : int;

        // # Information that frontend plugins might use for extra display information about completions.
        metadata : Dictionary<string, obj>;

        // # status should be 'ok' unless an exception was raised during the request,
        // # in which case it should be 'error', along with the usual error message content
        // # in other messages.
        status: string;
    }

type HistoryRequest =
    {
        // # If True, also return output history in the resulting dict.
        output: bool;

        // # If True, return the raw input history, else the transformed input.
        raw: bool;

        // # So far, this can be 'range', 'tail' or 'search'.
        hist_access_type: string;

        // # If hist_access_type is 'range', get a range of input cells. session can
        // # be a positive session number, or a negative number to count back from
        // # the current session.
        session: int;
        
        // # start and stop are line numbers within that session.
        start: int;
        stop: int;

        // # If hist_access_type is 'tail' or 'search', get the last n cells.
        n: int;

        // # If hist_access_type is 'search', get cells matching the specified glob
        // # pattern (with * and ? as wildcards).
        pattern: string;

        // # If hist_access_type is 'search' and unique is true, do not
        // # include duplicated history.  Default is false.
        unique: bool;
    }

// TODO: fix this
type HistoryReply =
    {
        // # A list of 3 tuples, either:
        // # (session, line_number, input) or
        // # (session, line_number, (input, output)),
        // # depending on whether output was False or True, respectively.
        history: list<string>;
    }

type ConnectRequest = obj

type ConnectReply = 
    {
        shell_port: int;   // # The port the shell ROUTER socket is listening on.
        iopub_port: int;   // # The port the PUB socket is listening on.
        stdin_port: int;   // # The port the stdin ROUTER socket is listening on.
        hb_port: int;      // # The port the heartbeat socket is listening on.
    }

type KernelRequest = obj

type LanguageInfo = 
    {
        // # Name of the programming language in which kernel is implemented.
        // # Kernel included in IPython returns 'python'.
        name: string;

        // # Language version number.
        // # It is Python version number (e.g., '2.7.3') for the kernel
        // # included in IPython.
        version: string;

        // # mimetype for script files in this language
        mimetype: string;

        // # Extension without the dot, e.g. 'py'
        file_extension: string;

        // # Pygments lexer, for highlighting
        // # Only needed if it differs from the top level 'language' field.
        //pygments_lexer: string;

        // # Codemirror mode, for for highlighting in the notebook.
        // # Only needed if it differs from the top level 'language' field.
        codemirror_mode: string;

        // # Nbconvert exporter, if notebooks written with this kernel should
        // # be exported with something other than the general 'script'
        // # exporter.
        //nbconvert_exporter: string;
    }

type KernelReply =
    {
        // # Version of messaging protocol (mandatory).
        // # The first integer indicates major version.  It is incremented when
        // # there is any backward incompatible change.
        // # The second integer indicates minor version.  It is incremented when
        // # there is any backward compatible change.
        //protocol_version: array<int>;
        protocol_version: string;

        // # The kernel implementation name
        // # (e.g. 'ipython' for the IPython kernel)
        implementation: string;

        // # Implementation version number.
        // # The versino number of the kernel's implementation
        // # (e.g. IPython.__version__ for the IPython kernel)
        implementation_version: string;

        // # Information about the language of the code for the kernel
        language_info: LanguageInfo;

        // # IPython version number (optional).
        // # Non-python kernel backend may not have this version number.
        // # The last component is an extra field, which may be 'dev' or
        // # 'rc1' in development version.  It is an empty string for
        // # released version.
        //ipython_version: Option<array<obj>>;

        // # Language version number (mandatory).
        // # It is Python version number (e.g., [2, 7, 3]) for the kernel
        // # included in IPython.
        //language_version: array<int>;

        // # Programming language in which kernel is implemented (mandatory).
        // # Kernel included in IPython returns 'python'.
        //language: string
    }

type KernelStatus = 
    {
        // # When the kernel starts to execute code, it will enter the 'busy'
        // # state and when it finishes, it will enter the 'idle' state.
        // # The kernel will publish state 'starting' exactly once at process startup.
        // # ('busy', 'idle', 'starting')
        execution_state: string;
    }

type ShutdownRequest =
    {
        restart: bool; // # whether the shutdown is final, or precedes a restart
    }

type ShutdownReply = ShutdownRequest

type DisplayData = 
    {
        // # Who create the data
        source: string;

        // # The data dict contains key/value pairs, where the kids are MIME
        // # types and the values are the raw data of the representation in that
        // # format.
        data: Dictionary<string, obj>;

        // # Any metadata that describes the data
        metadata: Dictionary<string, obj>;
    }

type Pyin = 
    {
        code: string;  // # Source code to be executed, one or more lines

        // # The counter for this execution is also provided so that clients can
        // # display it, since IPython automatically creates variables called _iN
        // # (for input prompt In[N]).
        execution_count: int;
    }

type Pyout = 
    {
        // # The counter for this execution is also provided so that clients can
        // # display it, since IPython automatically creates variables called _N
        // # (for prompt N).
        execution_count: int;

        // # data and metadata are identical to a display_data message.
        // # the object being displayed is that passed to the display hook,
        // # i.e. the *result* of the execution.
        data: Dictionary<string, obj>;
        metadata: Dictionary<string, obj>;
    }

type Stream = 
    {
        // # The name of the stream is one of 'stdout', 'stderr'
        name: string;
        //  # The data is an arbitrary string to be written to that stream
        text: string;
    }

type ClearOutput = 
    {
        // # Wait to clear the output until new output is available.  Clears the
        // # existing output immediately before the new output is displayed.
        // # Useful for creating simple animations with minimal flickering.
        wait: bool;

        // this is undocumented!?
        // TODO: figure out if this is right
        stdout: bool;
        stderr: bool;
        other: bool;
    } 

type CommOpen =
    {
        comm_id : string;
        data : obj
    }

type ShellMessage = 
    // execute
    | ExecuteRequest of ExecuteRequest
    | ExecuteReplyOk of ExecuteReplyOk
    | ExecuteReplyError of ExecuteReplyError

    // intellisense
    | InspectRequest of InspectRequest
    | CompleteRequest of CompleteRequest
    | CompleteReply of CompleteReply

    // history
    | HistoryRequest of HistoryRequest
    | HistoryReply of HistoryReply

    // connect
    | ConnectRequest of ConnectRequest
    | ConnectReply of ConnectReply

    // kernel info
    | KernelRequest of KernelRequest
    | KernelReply of KernelReply

    // shutdown
    | ShutdownRequest of ShutdownRequest
    | ShutdownReply of ShutdownReply
    
    // input / output
    | Pyout of Pyout
    | DisplayData of DisplayData

    // comm
    | CommOpen of CommOpen

type Header = 
    {
        msg_id: string;
        username: string;
        session: string;
        msg_type: string;
        version: string;
    }

type KernelMessage = 
    {
        Identifiers: list<string>;
        HmacSignature: string;
        Header: Header;
        ParentHeader: Header;
        Metadata: string;
        Content: ShellMessage;
    }


module ShellMessages =

    let Deserialize (messageType:string) (messageJson:string) =
        
        match messageType with
        | "execute_request"      -> ExecuteRequest (JsonConvert.DeserializeObject<ExecuteRequest>(messageJson))
        | "inspect_request"      -> InspectRequest (JsonConvert.DeserializeObject<InspectRequest>(messageJson))
        | "complete_request"     -> CompleteRequest (JsonConvert.DeserializeObject<CompleteRequest>(messageJson))
        //| "history_request"      -> HistoryRequest (JsonConvert.DeserializeObject<HistoryRequest>(messageJson))
        //| "connect_request"      -> ConnectRequest (JsonConvert.DeserializeObject<ConnectRequest>(messageJson))
        | "kernel_info_request"  -> KernelRequest (JsonConvert.DeserializeObject<KernelRequest>(messageJson))
        //| "shutdown_request"     -> ShutdownRequest (JsonConvert.DeserializeObject<ShutdownRequest>(messageJson))
        | "comm_open"            -> CommOpen (JsonConvert.DeserializeObject<CommOpen>(messageJson))
        | _                      -> failwith ("Unsupported messageType: " + messageType)