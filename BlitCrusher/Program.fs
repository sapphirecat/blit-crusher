﻿open BlitCrusher.T
open BlitCrusher.Image
open BlitCrusher.Operators

let averageChan a b =
    withChannel (fun x -> (x+b) / 2.0f) a

// 1-channel functions for making sure the channel mapping is correct
let redder p =
    {R = averageChan p.R 1.0f; G = p.G; B = p.B; A = p.A}
let greener p =
    {G = averageChan p.G 1.0f; R = p.R; B = p.B; A = p.A}
let bluer p =
    {B = averageChan p.B 1.0f; G = p.G; R = p.R; A = p.A}
let transer p =
    {A = averageChan p.A 0.2f; G = p.G; B = p.B; R = p.R}
   
// basic bit-crushing primitives
let bit2 = bits 2
let bit3 = bits 3
let bit4 = bits 4
let bit5 = bits 5
let bit6 = bits 6

// RGB bit-crushing transformation functions
let rgba4444 p =
    {R = bit4 p.R; G = bit4 p.G; B = bit4 p.B; A = bit4 p.A}
let rgb332 p =
    {R = bit3 p.R; G = bit3 p.G; B = bit2 p.B; A = Channel 1.0f}
let rgba2222 p =
    {R = bit2 p.R; G = bit2 p.G; B = bit2 p.B; A = bit2 p.A}

let hsv422 =
    asHSV (levels 15) bit2 bit2
let hsv633 =
    asHSV (levels 60) bit3 bit3
let hsva5443 =
    asHSVA (levels 30) bit4 bit4 bit3

// take an input filename and add a tag to it before the last dot
// e.g. `tagname input "red"` for use with an operator that makes it red
// also forces png; e.g. file.jpg -> file.tag.png
let tagname (basename:string) tag =
    let splice = basename.LastIndexOf('.')
    basename.Substring(0, splice) + "." + tag + ".png"

let transformations = 
    dict [|
        "red", redder;
        "grn", greener;
        "blu", bluer;
        "lfa", transer;
        "hsv422", hsv422;
        "hsv633", hsv633;
        "hsva5443", hsva5443;
        "rgba4444", rgba4444;
        "rgba2222", rgba2222;
        "rgb332", rgb332 |]

let parseCmdLine argv =
    let alltransforms = set transformations.Keys
    let isTransform x = alltransforms.Contains(x)
    let transforms = Array.filter isTransform argv
    let files = Array.filter (fun s -> not (isTransform s)) argv

    let transforms' =
        match transforms.Length with
        | 0 -> [| for t in alltransforms -> t |]
        | _ -> transforms
    match files.Length with
        | 0 -> CliError "No input files were given"
        | _ -> CliParse { transforms = transforms'; files = files }


let fileDoOne input tag =
    let operator = transformations.[tag]
    tagname input tag |> transformFile operator input

let fileDoAll input transforms =
    Array.map (fileDoOne input) transforms


let reportCli images =
    let failFilter v =
        match v with
        | Ok _ -> false
        | Error _ -> true
    let failures = Array.filter failFilter images
    Array.iter (printfn "Failed: %A") failures
    match failures.Length with
    | 0 -> 0
    | _ -> 1

let runCli r =
    Array.collect (fun i -> fileDoAll i r.transforms) r.files

let showUsage rv =
    printfn "Usage: BlitCrusher [transformations] FILE [FILE2 ...]"
    printfn ""
    printfn "Runs transformations (all possible ones, by default) on each input file."
    match rv with
    | Some i -> i
    | None -> 0

let showUsageError s =
    printfn "Argument error: %s" s
    showUsage (Some 2) // "CLI argument problem" exit code

[<EntryPoint>]
let main argv = 
    let parsed = parseCmdLine argv
    match parsed with
    | CliParse r -> runCli r |> reportCli
    | CliError s -> showUsageError s
