namespace IfSharp.Kernel

open System
open System.IO
open System.Text
open System.Reflection

module IfSharpResources = 

    let getString(name) =
        let asm = Assembly.GetExecutingAssembly()
        use reader = new StreamReader(asm.GetManifestResourceStream(name), Encoding.UTF8)
        reader.ReadToEnd()

    let kernel_json() = getString("Resources.kernel.json")