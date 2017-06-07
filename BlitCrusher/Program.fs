﻿open BlitCrusher.Types
open BlitCrusher.Image
open BlitCrusher.Operators

let averageChan a b =
    Channel.transform (fun x -> (x+b) / 2.0) a

// 1-channel functions for making sure the channel mapping is correct
let redder p =
    {R = averageChan p.R 1.0; G = p.G; B = p.B; A = p.A}
let greener p =
    {G = averageChan p.G 1.0; R = p.R; B = p.B; A = p.A}
let bluer p =
    {B = averageChan p.B 1.0; G = p.G; R = p.R; A = p.A}
let transer p =
    {A = averageChan p.A 0.2; G = p.G; B = p.B; R = p.R}
   
// basic bit-crushing primitives
let bit2 = bits 2
let bit3 = bits 3
let bit4 = bits 4
let bit5 = bits 5
let bit6 = bits 6
let bit7 = bits 7
let bit8 = bits 8
let near2 = nearbits 2
let near3 = nearbits 3
let near4 = nearbits 4
let near5 = nearbits 5
let near6 = nearbits 6
let near7 = nearbits 7
let near8 = nearbits 8

// RGB bit-crushing transformation functions
let rgba4444 p =
    {R = bit4 p.R; G = bit4 p.G; B = bit4 p.B; A = bit4 p.A}
let rgb332 p =
    {R = bit3 p.R; G = bit3 p.G; B = bit2 p.B; A = Opaque}
let rgba2222 p =
    {R = bit2 p.R; G = bit2 p.G; B = bit2 p.B; A = bit2 p.A}

let yiq332 = asYIQ bit3 bit3 bit2
let yiq844 = asYIQ bit8 near4 near4
let yiq853 = asYIQ bit8 near5 near3
let yiq655 = asYIQ bit6 near5 near5

let yuv332 = asYUV bit3 bit3 bit2
let yuv844 = asYUV bit8 near4 near4
let yuv853 = asYUV bit8 near5 near3
let yuv655 = asYUV bit6 near5 near5

let y8 = asY bit8
let y6 = asY bit6
let y4 = asY bit4
let y3 = asY bit3
let y2 = asY bit2

let hsv422_12 = asHSV  (levels 12) bit2 bit2
let hsv422_15 = asHSV  (levels 15) bit2 bit2
let hsv633    = asHSV  (levels 60) bit3 bit3
let hsva5443  = asHSVA (levels 30) bit4 bit4 bit3

// take an input filename and add a tag to it before the last dot
// e.g. `tagname input "red"` for use with an operator that makes it red
// also forces png; e.g. file.jpg -> file.tag.png
let tagname (basename:string) tag =
    let splice = basename.LastIndexOf('.')
    match splice with
    | -1 -> basename + "." + tag + ".png"
    | _  -> basename.Substring(0, splice) + "." + tag + ".png"

let transformations = 
    dict [|
        "red", redder;
        "grn", greener;
        "blu", bluer;
        "lfa", transer;
        "hsv422_12", hsv422_12;
        "hsv422_15", hsv422_15;
        "hsv633", hsv633;
        "hsva5443", hsva5443;
        "yiq332", yiq332;
        "yiq844", yiq844;
        "yiq853", yiq853;
        "yiq655", yiq655;
        "yuv332", yuv332;
        "yuv844", yuv844;
        "yuv853", yuv853;
        "yuv655", yuv655;
        "y8", y8;
        "y6", y6;
        "y4", y4;
        "y3", y3;
        "y2", y2;
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
        | 0 -> alltransforms |> Set.toArray
        | _ -> transforms
    match files.Length with
        | 0 -> CliError "No input files were given"
        | _ -> CliParse { transforms = transforms'; files = files }


let fileDoOne reportfn input tag =
    let operator = transformations.[tag]
    let out = tagname input tag |> transformFile operator input
    reportfn input out

let fileDoAll reportfn input transforms =
    Array.map (fileDoOne reportfn input) transforms


let reportCliInline input (out:Result<Image,exn>) =
    // let us pipe printfn somewhere, making
    // "print and set exit code" a one-liner
    let setCode c () = c
    match out with
    | Ok img ->
        match img.Filename with
        | Some name -> printfn "%s -> %s" input name |> setCode 0
        | None -> printfn "UNSAVED %s" input |> setCode 1
    | Error e -> printfn "ERROR %s: %s" input e.Message |> setCode 2

let runCli r =
    Array.collect (fun f -> fileDoAll reportCliInline f r.transforms) r.files

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
    | CliParse r -> runCli r |> Array.max
    | CliError s -> showUsageError s
