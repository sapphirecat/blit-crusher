module BlitCrusher.Types

open System.Drawing

type Image = {
    Image: Bitmap;
    Filename: string option }

let roundHalfUp (value:float) =
    System.Math.Round(value, System.MidpointRounding.AwayFromZero)

type RangedFloat = float * float * float
type Channel = Channel of RangedFloat with
    // Data access
    static member raw channel =
        match channel with Channel (x,_,_) -> x
    static member normalize channel =
        match channel with Channel (x,lo,hi) -> (x-lo)/(hi-lo)
    static member denormalize channel normal =
        match channel with
        Channel (_,lo,hi) ->
            let x = (hi - lo) * normal + lo
            Channel (x, lo, hi)
    static member transform operator value =
        Channel.normalize value
        |> operator
        |> Channel.denormalize value

    // Quantizers
    static member levels nLevels channel =
        let v = Channel.normalize channel
        let n = nLevels - 1 |> float
        roundHalfUp (v*n) / n |> Channel.denormalize channel

    // Range clamping
    static member private _modclamp lo hi value =
        let range = hi - lo
        let rec step v =
            if v < lo then v + range |> step
            elif v >= hi then v - range |> step
            else v
        step value

    // Constructors
    static member Std x = Channel (x,0.0,1.0)   // Standard (0 to 1)
    static member Hue x =                         // Hue (0 to 360)
        let lo = 0.0
        let hi = 360.0
        Channel (Channel._modclamp lo hi x, lo, hi)


type Pixel = {
    R: Channel;
    G: Channel;
    B: Channel;
    A: Channel }
type Pixel with
    static member RGBA r g b a =
        {R = Channel.Std r; G = Channel.Std g; B = Channel.Std b; A = Channel.Std a}
    static member asTuple3 px =
        (Channel.raw px.R, Channel.raw px.G, Channel.raw px.B)
let Opaque = Channel.Std 1.0

type CliData = {
    transforms: string[];
    files: string[]
}
type CliResult =
    | CliParse of CliData
    | CliError of string

type Matrix = float[,]
let matrixInit rows cols (data:float[]) :Matrix =
    if data.Length = rows*cols then
        Array2D.init rows cols (fun r c -> data.[r*cols + c])
    else
        invalidArg "data" "Length does not match number of cells"
let matrixMult (a:Matrix) (b:Matrix) :Matrix =
    let rows = Array2D.length1
    let cols = Array2D.length2
    let mr = seq { 0 .. cols a - 1 } // "mid" range (or NxM * MxP -> M range)
    let cellMult r c =
        mr
        |> Seq.map (fun k -> a.[r,k]*b.[k,c])
        |> Seq.sum

    if cols a = rows b then
        Array2D.init (rows a) (cols b) cellMult
    else
        sprintf "Row count of b (%d) does not match column count of a (%d)" (rows b) (cols a)
        |> invalidArg "b"


let tupleToMatrix (a,b,c) =
    matrixInit 3 1 [| Channel.raw a; Channel.raw b; Channel.raw c |]
let matrixToRangeTuple (low:float[]) (high:float[]) (m:Matrix) =
    let slot s = Channel (m.[s,0],low.[s],high.[s])
    slot 0, slot 1, slot 2

let pixelToMatrix px =
    tupleToMatrix (px.R, px.G, px.B)
let matrixToPixel alpha (m:Matrix) =
    {R = Channel.Std m.[0,0]; G = Channel.Std m.[1,0]; B = Channel.Std m.[2,0]; A = alpha}
