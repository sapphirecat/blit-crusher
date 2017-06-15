// Learn more about F# at http://fsharp.org. See the 'F# Tutorial' project
// for more guidance on F# programming.

#load "Types.fs"
#load "Image.fs"
#load "Operators.fs"
open Bitcore.Types
open Bitcore.Operators

// Define your library scripting code here
let as255 f = f * 255.0 |> float
let print255 value =
    printf "%A " (as255 value)
    value
let printUV value =
    printf "%A " value
    value
let print255n value =
    printfn "%A" (as255 value)
    value
let printUVn value =
    printfn "%A" value
    value
let printRGB item (px:PxRGB) =
    printf "RGB %s: " item
    px.apply3 print255 print255 print255n |> ignore
    px
let printYUV item (px:PxYUV) =
    printf "YUV %s: " item
    px.apply3 print255 printUV printUVn |> ignore
    px

let RGBbytes (r,g,b) =
    RGB (float r/255.0, float g/255.0, float b/255.0)
let traceYUV r g b =
    let rgb_in = RGBbytes (r,g,b) |> printRGB "in"
    let yuv = toYUV rgb_in |> printYUV "in"
    let yuv' = yuv.apply3 (bits 5) (bits 3) (bits 3) |> printYUV "out"
    fromYUV yuv' |> printRGB "out" |> ignore


traceYUV 60 219 32
traceYUV 255 254 55
printfn "Tune in next time!"
