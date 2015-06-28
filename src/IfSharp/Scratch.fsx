#I "../../bin"
#r "NetMQ.dll"
#r "IfSharp.exe"

open System.Resources
open System.Reflection
open System
open System.IO
open System.Text

open IfSharp.Kernel

let asm = Assembly.GetAssembly(typeof<IfSharpKernel>)

asm.GetManifestResourceNames()
    
let getString(name) =
    use reader = new StreamReader(asm.GetManifestResourceStream(name), Encoding.UTF8)
    reader.ReadToEnd()

String.Format("\"mono\",\"{0}\"","hello")

getString("kernel.json")

let ms = Assembly.GetExecutingAssembly().GetManifestResourceStream( "IfSharp.Resources.kernel.json" )
ms.ToString()


getString("IfSharp.Resources.kernel.json")