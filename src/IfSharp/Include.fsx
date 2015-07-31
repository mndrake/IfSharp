// include directory, this will be replaced by the kernel
#I "{0}"

// load base dlls
//#r "IfSharp.Kernel.dll"
#r "System.Data.dll"
//#r "System.Windows.Forms.DataVisualization.dll"
#r "IfSharp.exe"
#r "FSharp.Data.TypeProviders.dll"
//#r "FSharp.Charting.Gtk.dll"
//#r "OxyPlot.dll"
//#r "OxyPlot.GtkSharp.dll"
#r "Deedle.dll"
#r "NetMQ.dll"
//#r "fszmq.dll"

// open the global functions and methods
open IfSharp.Kernel
open IfSharp.Kernel.Globals
open IfSharp.Charting
