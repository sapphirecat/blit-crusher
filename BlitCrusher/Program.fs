open BlitCrusher.T
open BlitCrusher.Image
open BlitCrusher.Operators

let averagef32 a b :Channel =
    (a+b) / 2.0f

// 1-channel functions for making sure the channel mapping is correct
let redder p =
    {R = averagef32 p.R 1.0f; G = p.G; B = p.B; A = p.A}
let greener p =
    {G = averagef32 p.G 1.0f; R = p.R; B = p.B; A = p.A}
let bluer p =
    {B = averagef32 p.B 1.0f; G = p.G; R = p.R; A = p.A}
let transer p =
    {A = averagef32 p.A 0.2f; G = p.G; B = p.B; R = p.R}
   
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
    {R = bit3 p.R; G = bit3 p.G; B = bit2 p.B; A = 1.0f}
let rgba2222 p =
    {R = bit2 p.R; G = bit2 p.G; B = bit2 p.B; A = bit2 p.A}

// take an input filename and add a tag to it before the last dot
// e.g. `tagname input "red"` for use with an operator that makes it red
// also forces png; e.g. file.jpg -> file.tag.png
let tagname (basename:string) tag =
    let splice = basename.LastIndexOf('.')
    basename.Substring(0, splice) + "." + tag + ".png"

let transformations = 
    [|  "rgba4444", rgba4444;
        "rgba2222", rgba2222;
        "rgb332", rgb332 |]

let transformOne input tag operator =
    tagname input tag |> transformFile operator input
let transformAll input =
    [|for tag,operator in transformations do
        yield transformOne input tag operator |]

[<EntryPoint>]
let main argv = 
    let input = match argv.Length with
                | 0 -> [|"file.png"|]
                | _ -> argv
    Array.iter (fun x -> transformAll x |> ignore) input
    0 // return an integer exit code