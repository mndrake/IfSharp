namespace IfSharp.Charting
open System
open System.Collections.Generic
open Newtonsoft.Json

module ChartTypes =

    /// data point for line and scatter plots
    type Point = { x: float; y: float }

    type Series = Point seq

    /// base class for all chart types
    type ChartBase() =
        let options = new Dictionary<string, obj>()
        let guid = String.Format("N{0}", Guid.NewGuid().ToString("N"))
        let mutable template = """
            <div id="{GUID}">
                <div class="legend small"></div>
                <svg></svg>
            </div>
            <link href="//cdnjs.cloudflare.com/ajax/libs/metrics-graphics/2.1.0/metricsgraphics.min.css" rel="stylesheet" type="text/css">
            <script type="text/javascript">
                require.config({
                    paths: {jquery: "//ajax.googleapis.com/ajax/libs/jquery/1.11.1/jquery.min",
                            d3: "//cdnjs.cloudflare.com/ajax/libs/d3/3.5.3/d3.min",
                            MG: "//cdnjs.cloudflare.com/ajax/libs/metrics-graphics/2.1.0/metricsgraphics.min"}});
                require(["jquery", "d3", "MG"], function(jquery, d3, MG) {        
                    MG.data_graphic({OPTIONS});
                });  
            </script>"""
        do
            options.["width"] <- 600
            options.["height"] <- 400
            options.["y_extended_ticks"] <- true
            options.["x_extended_ticks"] <- false
            options.["target"] <- String.Format("#{0}", guid)

        member this.HasOption key = options.ContainsKey(key)

        member this.SetLegend (keys: seq<string>) =
            this.["legend_target"] <- "#{GUID} .legend"
            this.["legend"] <- keys

        member this.xLabel 
            with get() : string = this.["x_label"] |> unbox 
             and set (value:string) = 
                if not <| this.HasOption("x_label") then this.["bottom"] <- 70                 
                this.["x_label"] <- value

        member this.xExtendedTicks
            with get() : bool = this.["x_extended_ticks"] |> unbox
             and set (value:bool) = this.["x_extended_ticks"] <- value

        member this.yExtendedTicks
            with get() : bool = this.["y_extended_ticks"] |> unbox
             and set (value:bool) = this.["y_extended_ticks"] <- value

        member this.xMax
            with get() : float = this.["max_x"] |> unbox
             and set (value:float) = this.["max_x"] <- value

        member this.xMin
            with get() : float = this.["min_x"] |> unbox
             and set (value:float) = this.["min_x"] <- value

        member this.yLabel 
            with get() : string = this.["y_label"] |> unbox 
             and set (value:string) = 
                 if not <| this.HasOption("y_label") then this.["left"] <- 100
                 this.["y_label"] <- value

        member this.ToHtml() =
            // FUNC can be used to wrap a javascript function passed to the data_graphic method
            template.Replace("{OPTIONS}", JsonConvert.SerializeObject(options))
                    .Replace("{GUID}", guid)
                    .Replace("\"FUNC", "").Replace("FUNC\"", "")

        member this.Item with get(key) = options.[key] and set key (value: obj) = options.[key] <- value


    type LineChart() =
        inherit ChartBase()
        do
            base.["area"] <- false
            base.["x_accessor"] <- "x"
            base.["y_accessor"] <- "y"
            base.["data"] <- Seq.empty<Series>

        member this.addSeries (values: (float*float) seq) = 
            let data : Series seq = this.["data"] |> unbox
            this.["data"] <- Seq.append data (seq [values |> Seq.map (fun p -> {x = fst p; y = snd p})])

    type BarPlotItem = {label:string; value:float; baseline:float; prediction:float}

    type BarChart() =
        inherit ChartBase()
        do
            base.["chart_type"] <- "bar"
            base.["x_accessor"] <- "value"
            base.["y_accessor"] <- "label"
            base.["animate_on_load"] <- true

        member this.Data 
            with get() : seq<BarPlotItem> = this.["data"] |> unbox
             and set v = this.["data"] <- v                     

    type HistogramChart() =
        inherit ChartBase()
        do
            base.["chart_type"] <- "histogram"
            base.["right"] <- 60
            base.["mouseover"] <- @"FUNCfunction(d, i){$('#{GUID} svg .mg-active-datapoint').text('Value: ' + d3.round(d.x,2) + '   Count: ' + d.y);}FUNC"

        member this.Bins with get() : int = this.["bins"] |> unbox and set (value:int) = this.["bins"] <- value
        member this.BarMargin with get() : int = this.["bar_margin"] |> unbox and set (value:int) = this.["bar_margin"] <- value
        member this.Data 
            with get() : seq<float> = this.["data"] |> unbox 
             and set (value:seq<float>) = 
                 this.xMax <- Seq.max value
                 this.["data"] <- value
                                                  
type Line =
    static member create values =
        let chart = new ChartTypes.LineChart()
        chart.addSeries(values)
        chart

    static member addSeries values (chart: ChartTypes.LineChart) =
        chart.addSeries(values)
        chart

    static member xLabel value (chart: ChartTypes.LineChart) =
        chart.xLabel <- value
        chart

    static member yLabel value (chart: ChartTypes.LineChart) =
        chart.yLabel <- value
        chart

    static member xExtendedTicks value (chart: ChartTypes.LineChart) =
        chart.xExtendedTicks <- value
        chart

    static member yExtendedTicks value (chart: ChartTypes.LineChart) =
        chart.yExtendedTicks <- value
        chart

    static member legend value (chart: ChartTypes.LineChart) =
        chart.SetLegend value
        chart

type Histogram =
    static member create values =
        let chart = new ChartTypes.HistogramChart()
        chart.Data <- values
        chart

    static member bins value (chart: ChartTypes.HistogramChart) =
        chart.Bins <- value
        chart    

type Chart =

    static member Line(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
        let chart = ChartTypes.LineChart()
        chart.addSeries(data)
        if XTitle.IsSome then chart.xLabel <- XTitle.Value
        if YTitle.IsSome then chart.yLabel <- YTitle.Value
        chart

    static member Histogram(data, ?Bins) =
        let chart = ChartTypes.HistogramChart()
        chart.Data <- data
        if Bins.IsSome then chart.Bins <- Bins.Value
        chart

    static member Bar(data) =
        let chart = ChartTypes.BarChart()
        chart.Data <- data |> Seq.map (fun (l,v) -> {ChartTypes.BarPlotItem.label=l; ChartTypes.BarPlotItem.value=v; ChartTypes.BarPlotItem.baseline=0.; ChartTypes.BarPlotItem.prediction=0.})
        chart