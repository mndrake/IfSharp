namespace IfSharp.Kernel
// based on https://github.com/tpetricek/Deedle/blob/mainline/docs/tools/formatters.fsx
open System
open Deedle
open Deedle.Internal

module DeedleFormat =

    // --------------------------------------------------------------------------------------
    // How many columns and rows from frame should be rendered
    let startColumnCount = 3
    let endColumnCount = 3
    let startRowCount = 8
    let endRowCount = 4
    // How many items from a series should be rendered
    let startItemCount = 5
    let endItemCount = 3

    // --------------------------------------------------------------------------------------
    // Helper functions etc.
    // --------------------------------------------------------------------------------------
    /// Extract values from any series using reflection
    let (|SeriesValues|_|) (value : obj) = 
        let iser = value.GetType().GetInterface("ISeries`1")
        if iser <> null then 
            let keys = 
                value.GetType().GetProperty("Keys").GetValue(value) :?> System.Collections.IEnumerable
            let vector = value.GetType().GetProperty("Vector").GetValue(value) :?> IVector
            Some(Seq.zip (Seq.cast<obj> keys) vector.ObjectSequence)
        else None
        
    /// Format value as a single-literal paragraph
    let formatValue def value = 
        match value with 
        | Some v -> v.ToString()
        | None -> def
        |> sprintf "<p>%s</p>"
        
    /// Format body of a single table cell
    let td v = sprintf "<p>%s</p>" v

    /// Use 'f' to transform all values, then call 'g' with Some for 
    /// values to show and None for "..." in the middle
    let mapSteps (startCount, endCount) f g input = 
        input
        |> Seq.map f
        |> Seq.startAndEnd startCount endCount
        |> Seq.map (function 
               | Choice1Of3 v | Choice3Of3 v -> g (Some v)
               | _ -> g None)
        |> List.ofSeq

        
    // Tuples with the counts, for easy use later on
    let fcols = startColumnCount, endColumnCount
    let frows = startRowCount, endRowCount
    let sitms = startItemCount, endItemCount


    let getHtml (value : obj) = 
            match value with
            | SeriesValues s ->
                // Pretty print series!
                let head = 
                    s |> mapSteps sitms fst (function 
                             | Some k -> td (k.ToString())
                             | _ -> td " ... ")
                      |> List.fold (fun a b -> a + sprintf "<th>%s</th>\n" b) "<th><p>Keys</p></th>\n"
                      |> sprintf "<thead>\n<tr class='header'>\n%s</tr>\n</thead>\n"
                let body =
                    s |> mapSteps sitms snd (function 
                             | Some v -> formatValue "N/A" (OptionalValue.asOption v)
                             | _ -> td " ... ")
                      |> List.fold (fun a b -> a + sprintf "<td>%s</td>\n" b) "<td><p>Values</p></td>\n"
                      |> sprintf "<tbody>\n<tr class='odd'>\n%s</tr>\n</tbody>\n"
                sprintf "<div class='deedleseries'>\n<table>\n%s%s</table>\n</div>\n" head body            
            | :? IFrame as f -> 
                { // Pretty print frame!
                  new IFrameOperation<_> with
                      member x.Invoke(f) = 
                          let head = 
                              f.ColumnKeys 
                              |> mapSteps fcols id (function 
                                                  | Some k -> td (k.ToString())
                                                  | _ -> td " ... ")
                              |> List.fold (fun a b -> a + sprintf "<th>%s</th>\n" b) "<th></th>\n"
                              |> sprintf "<thead>\n<tr class='header'>\n%s</tr>\n</thead>\n"
                      
                          let body =
                              f.Rows
                              |> Series.observationsAll
                              |> mapSteps frows id (fun item -> 
                                     let def, k, data = 
                                         match item with
                                         | Some(k, Some d) -> 
                                             "N/A", k.ToString(), Series.observationsAll d |> Seq.map snd
                                         | Some(k, _) -> 
                                             "N/A", k.ToString(), f.ColumnKeys |> Seq.map (fun _ -> None)
                                         | None -> " ... ", " ... ", f.ColumnKeys |> Seq.map (fun _ -> None)
                                     
                                     let row = 
                                         data |> mapSteps fcols id (function 
                                                     | Some v -> formatValue def v
                                                     | _ -> td " ... ")
                                     
                                     (td k) :: row)
                              |> List.mapi (fun i row -> row |> List.fold (fun a b -> a + sprintf "<td>%s</td>\n" b) ""
                                                             |> match i%2=0 with
                                                                |true -> sprintf "<tr class='odd'>\n%s</tr>\n" 
                                                                |false -> sprintf "<tr class='even'>\n%s</tr>\n")
                              |> List.fold (+) ""
                              |> sprintf "<tbody>\n%s</tbody>\n"

                          sprintf "<div class='deedleframe'>\n<table>\n%s%s</table>\n</div>\n" head body }
                |> f.Apply
            | _ -> ""