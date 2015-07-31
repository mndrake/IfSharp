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

    let getImageBytes(name) =
        use memstream = new MemoryStream()
        let asm = Assembly.GetExecutingAssembly()
        asm.GetManifestResourceStream(name).CopyTo(memstream)
        memstream.ToArray()

    let kernel_json() = getString("kernel.json")

    let image_bytes() = getImageBytes("logo-64x64.png")