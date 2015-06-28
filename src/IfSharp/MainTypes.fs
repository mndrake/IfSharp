﻿namespace IfSharp.Kernel

open System.Collections.Generic
//open FSharp.Charting

type ConnectionInformation = 
    {
        stdin_port: int;
        ip: string;
        control_port: int;
        hb_port: int;
        signature_scheme: string;
        key: string;
        shell_port: int;
        transport: string;
        iopub_port: int;
    }