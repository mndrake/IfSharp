// include directory, this will be replaced by the kernel
#I "{0}"

// load base dlls
#r "IfSharp.Kernel.dll"
#r "System.Data.dll"
#r "System.Windows.Forms.DataVisualization.dll"
#r "FSharp.Data.TypeProviders.dll"
#r "FSharp.Charting.Gtk.dll"
#r "OxyPlot.dll"
#r "OxyPlot.GtkSharp.dll"
#r "fszmq.dll"

// open the global functions and methods
open FSharp.Charting
open IfSharp.Kernel
open IfSharp.Kernel.Globals
