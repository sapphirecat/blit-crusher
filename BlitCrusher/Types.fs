module BlitCrusher.T

open System.Drawing

type Image = {
    Image: Bitmap;
    Filename: string option }

type Channel = Channel of float32
let withChannel f value =
    match value with Channel x -> Channel (f x)
let channelToF32 value =
    match value with Channel x -> x
type Pixel = {
    R: Channel;
    G: Channel;
    B: Channel;
    A: Channel }
let pixelToF32 px =
    Array.map channelToF32 [| px.R; px.G; px.B |]

type CliData = {
    transforms: string[];
    files: string[]
}
type CliResult =
    | CliParse of CliData
    | CliError of string

type Matrix = float32[,]
let matrixInit rows cols (data:float32[]) :Matrix =
    if data.Length = rows*cols then
        Array2D.init rows cols (fun r c -> data.[r*cols + c])
    else
        invalidArg "data" "Length does not match number of cells"
let matrixMult (a:Matrix) (b:Matrix) =
    let rows = Array2D.length1
    let cols = Array2D.length2
    let acr = seq { 0 .. cols a - 1 }
    let brr = seq { 0 .. rows b - 1 }
    let cellMult outr outc =
        Seq.map2 (fun ac br -> a.[outr,ac]*b.[br,outc]) acr brr
        |> Seq.sum

    if cols a <> rows b then
        invalidArg "b" "Row count of b does not match column count of a"
    else
        Array2D.init (rows a) (cols b) cellMult
// FIXME: there is zero protection against using Pixel funcs on non-RGB matrices
let pixelToMatrix px =
    let floats = Array.map channelToF32 [| px.R; px.G; px.B |]
    matrixInit 3 1 floats
let matrixToPixel alpha (m:Matrix) =
    {R = Channel m.[0,0]; G = Channel m.[1,0]; B = Channel m.[2,0]; A = alpha}
let tupleToMatrix (a,b,c) =
    let floats = Array.map channelToF32 [| a; b; c |]
    matrixInit 3 1 floats
let matrixToTuple (m:Matrix) =
    Channel m.[0,0], Channel m.[1,0], Channel m.[2,0]
