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