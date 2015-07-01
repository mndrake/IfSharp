#I "../../bin"
#r "NetMQ.dll"
#r "IfSharp.exe"
#r "FSharp.Compiler.Service.dll"
#r "Newtonsoft.Json.dll"

open System
open System.IO
open System.Reflection
open Microsoft.FSharp.Compiler.SourceCodeServices
open Newtonsoft.Json

open IfSharp.Kernel

let compiler = FsCompiler(FileInfo(".").FullName)

/// Gets the header code to prepend to all items
let headerCode = 
    let file = FileInfo(typeof<IfSharpKernel>.Assembly.Location)
    let dir = file.Directory.FullName
    let includeFile = Path.Combine(dir, "Include.fsx")
    let code = File.ReadAllText(includeFile)
    String.Format(code, dir.Replace("\\", "\\\\"))

let content : InspectRequest = 
    {
        code = "['let f(x) = x+2']";
        cursor_pos = 5;
        detail_level = 0;
    }


let cells = JsonConvert.DeserializeObject<array<string>>(content.code)
let codes = cells |> Seq.append [headerCode]

let lineOffset = 
    codes
    |> Seq.take (content.cursor_pos + 1)
    |> Seq.map (fun x -> x.Split('\n').Length)
    |> Seq.sum

let realLineNumber = lineOffset + 1
let codeString = String.Join("\n", codes)


open Microsoft.FSharp.Compiler
open System
open Microsoft.FSharp.Compiler.SourceCodeServices

let checker = FSharpChecker.Create()


let identToken = Parser.tagOfToken(Parser.token.IDENT("")) 

// Sample input as a multi-line string
let input = 
  """
  open System

  let foo() = 
    let msg = String.Concat("Hello"," ","world")
    if true then 
      printfn "%s" msg.
  """
// Split the input & define file name
let inputLines = input.Split('\n')
let file = "/home/user/Test.fsx"

let projOptions = 
    checker.GetProjectOptionsFromScript(file, input)
    |> Async.RunSynchronously

// Perform parsing  
let parseFileResults = 
    checker.ParseFileInProject(file, input, projOptions) 
    |> Async.RunSynchronously

// Perform type checking
let checkFileAnswer = 
    checker.CheckFileInProject(parseFileResults, file, 0, input, projOptions) 
    |> Async.RunSynchronously

let checkFileResults = 
    match checkFileAnswer with
    | FSharpCheckFileAnswer.Succeeded(res) -> res
    | res -> failwithf "Parsing did not finish... (%A)" res

// Get tool tip at the specified location

let tip = checkFileResults.GetToolTipTextAlternate(4, 7, inputLines.[1], ["foo"], identToken) |> Async.RunSynchronously
printfn "%A" tip

match tip with
| FSharpToolTipText([FSharpToolTipElement.Single(x,_)]) -> x
| _ -> ""