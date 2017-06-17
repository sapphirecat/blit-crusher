module Bitcore.Operators

open Bitcore.Types


let levels nLevels v =
    let n = nLevels - 1 |> float
    roundHalfUp (v*n) / n
let private bits_ depth = 2.0 ** (float depth) |> int
let bits depth = bits_ depth |> levels
let nearbits depth = bits_ depth - 1 |> levels

// basic bit-crushing primitives
let bit1 = bits 1
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


// If we only multiply [3x3] by [3x1] and the [3x1] is always based on some channels,
// then let's hardcode that.  Stop creating a float[] to create a float[3,1] to send
// to an any-dimension matrix multiplication routine.
// Any pixel that shouldn't participate (e.g. HSV) doesn't implement MatrixPixel.
let linearMult co (px:MatrixPixel) =
    let (a,b,c) = px.asTuple3()
    if Array.length co <> 9 then invalidArg "coefficients" "Not a 3x3 matrix"
    else (co.[0]*a + co.[1]*b + co.[2]*c,
          co.[3]*a + co.[4]*b + co.[5]*c,
          co.[6]*a + co.[7]*b + co.[8]*c)


let private rgb2yiq =
    [| 0.299;  0.587;  0.114;
       0.596; -0.275; -0.321;
       0.212; -0.523;  0.311 |]
let private yiq2rgb =
    [| 1.0;  0.956;  0.621;
       1.0; -0.272; -0.647;
       1.0; -1.105;  1.702 |]
let toYIQ (px:PxRGB) = linearMult rgb2yiq px |> AYIQ px.a
let fromYIQ (px:PxYIQ) = linearMult yiq2rgb px |> ARGB px.a

let private rgb2yuv =
    [| 0.299;    0.587;    0.114;
      -0.14713; -0.28886;  0.436;
       0.615;   -0.51499; -0.10001 |]
let private yuv2rgb =
    [| 1.0;  0.0;      1.13983;
       1.0; -0.39465; -0.58060;
       1.0;  2.03211;  0.0 |]
let toYUV (px:PxRGB) = linearMult rgb2yuv px |> AYUV px.a
let fromYUV (px:PxYUV) = linearMult yuv2rgb px |> ARGB px.a

let toY (px:PxRGB) =
    let m = rgb2yiq
    let y = m.[0]*px.r + m.[1]*px.g + m.[2]*px.b
    AGray px.a y
let fromY (px:PxGray) =
    let v = px.y
    ARGB px.a (v,v,v)


let toHSV (px:PxRGB) =
    // find the min/max channel values
    let mpx = px :> MatrixPixel
    let r, g, b = mpx.asTuple3()
    let px' = [| r; g; b |]
    let maxC = Array.reduce max px'
    let minC = Array.reduce min px'
    // range between max/min
    let delta = maxC - minC

    // calculate a basic hue based on delta and which channel is max
    let h = if maxC <= minC then 0.0
            elif maxC <= r then (g - b)
            elif maxC <= g then 2.0 + (b - r)
            else 4.0 + (r - g)
    // safely calculate saturation
    let s = if maxC > 0.0 then delta/maxC else 0.0
    AHSV px.a (h, s, maxC)
let fromHSV (px:PxHSV) =
    let h = px.h6
    let s = px.s
    let v = px.v
    // integer and fractional parts of hue
    let i = h |> floor
    let f = h - i
    // magic values (I never understood these, nor their names)
    let p = v * (1.0 - s)
    let q = v * (1.0 - s * f)
    let t = v * (1.0 - s * (1.0 - f))
    // FIXME: Pythonic/imperative approach
    let rgbTuple =
        if s <= 0.0 then v,v,v
        elif i < 1.0 then v,t,p
        elif i < 2.0 then q,v,p
        elif i < 3.0 then p,v,t
        elif i < 4.0 then p,q,v
        elif i < 5.0 then t,p,v
        else v,p,q
    ARGB px.a rgbTuple


let asRGBA redfn greenfn bluefn alphafn (px:PxRGB) =
    ARGB (alphafn px.a) (redfn px.r, greenfn px.g, bluefn px.b)
let asRGB redfn greenfn bluefn (px:PxRGB) =
    RGB (redfn px.r, greenfn px.g, bluefn px.b)

let asHSVA huefn satfn valfn alphafn px =
    (toHSV px).apply4 alphafn huefn satfn valfn |> fromHSV
let asHSV huefn satfn valfn px =
    (toHSV px).apply3 huefn satfn valfn |> fromHSV

let asYIQA yfn ifn qfn alphafn px =
    (toYIQ px).apply4 alphafn yfn ifn qfn |> fromYIQ
let asYIQ yfn ifn qfn px =
    (toYIQ px).apply3 yfn ifn qfn |> fromYIQ

let asYUVA yfn ufn vfn alphafn px =
    (toYUV px).apply4 alphafn yfn ufn vfn |> fromYUV
let asYUV yfn ufn vfn px =
    (toYUV px).apply3 yfn ufn vfn |> fromYUV

let asY fn (px:PxRGB) =
    (toY px).apply1 fn |> fromY
