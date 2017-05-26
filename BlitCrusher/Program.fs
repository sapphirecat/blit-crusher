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

// should this be part of SysImage.fs?
let transform operator input output =
    let source = loadFile input
    let dest = foreachPixel operator source
    saveImageAs dest output

// take an input filename and add a tag to it before the last dot
// e.g. `tagname input "red"` for use with an operator that makes it red
// also forces png; e.g. file.jpg -> file.tag.png
let tagname (basename:string) tag =
    let splice = basename.LastIndexOf('.')
    basename.Substring(0, splice) + "." + tag + ".png"

[<EntryPoint>]
let main argv = 
    let input = "file.png"
    tagname input "red" |> transform redder input |> ignore
    tagname input "grn" |> transform greener input |> ignore
    tagname input "blu" |> transform bluer input |> ignore
    tagname input "lfa" |> transform transer input |> ignore

    //printfn "%A" argv
    0 // return an integer exit code