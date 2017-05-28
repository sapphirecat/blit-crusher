module BlitCrusher.Operators

open BlitCrusher.T
open BlitCrusher.Image

// example: `modclamp 0.0f 360.0f hue` will bring hue into
// the half-open interval [0,360) without changing the hue.
let modclamp lo hi value :Channel =
    let range = hi - lo
    let rec step v =
        if v < lo then v + range |> step
        elif v >= hi then v - range |> step
        else v
    step value
// specialize for hue
let hueclamp = modclamp 0.0f 360.0f

// clamp a value to an arbitrary closed interval
let fclamp lo hi value :Channel =
    if value < lo then lo
    elif value > hi then hi
    else value
// clamp to [0,1]
let clamp = fclamp 0.0f 1.0f

// quantize a float to any amount on an arbitrary closed interval
let flevels lo hi count channel :Channel =
    let ct = float32 count
    let range = hi - lo
    let ch = (channel - lo)/range * ct |> round
    (ch/ct) * range + lo
// flevels on [0,1]
let levels count channel :Channel =
    let ct = float32 count
    let c = channel * ct |> round
    c/ct

let bits depth channel =
    let _lv = 2.0f ** float32 depth |> round |> int
    levels _lv channel

let max3 a b c = max a b |> max c
let min3 a b c = min a b |> min c
let toHSV px =
    // find the min/max channel values
    let maxC = max3 px.R px.G px.B
    let minC = min3 px.R px.G px.B
    // range between max/min
    let delta = maxC - minC

    // calculate a basic hue based on delta and which channel is max
    let h = if maxC = minC then 0.0f
            elif maxC = px.R then (px.G - px.B)
            elif maxC = px.G then 2.0f + (px.B - px.R)
            else 4.0f + (px.R - px.G)
    // calculate the final hue and return the HSV tuple
    let h' = h*60.0f |> hueclamp
    h',delta/maxC,maxC
let fromAHSV a (h, s, v) =
    let h' = h / 60.0f
    let i = h' |> floor
    let f = h - i
    let p = v * (1.0f - s)
    let q = v * (1.0f - s * f)
    let t = v * (1.0f - s * (1.0f - f))
    match int i with
    | 0 -> Some({R = v; G = t; B = p; A = a })
    | 1 -> Some({R = q; G = v; B = p; A = a })
    | 2 -> Some({R = p; G = v; B = t; A = a })
    | 3 -> Some({R = p; G = q; B = v; A = a })
    | 4 -> Some({R = t; G = p; B = v; A = a })
    | 5 -> Some({R = v; G = p; B = q; A = a })
    | _ -> None
let fromHSV = fromAHSV 1.0f


let asRGBA redfn greenfn bluefn alphafn px =
    {R = redfn px.R; G = greenfn px.G; B = bluefn px.B; A = alphafn px.A}
let asRGB redfn greenfn bluefn =
    asRGBA redfn greenfn bluefn id

let asHSVA huefn satfn valfn alphafn px =
    let h, s, v = toHSV px
    let a = alphafn px.A
    fromAHSV a (huefn h, satfn s, valfn v)
let asHSV huefn satfn valfn =
    asHSVA huefn satfn valfn id

// ideally, there'd be a generic "get mask" function and foreachPixel
// would just be a 1x1 mask application
let foreachPixel operator source =
    let dest = newImageFrom source
    let pixels, meta = getPixels source
    let pixels' = Array.zeroCreate pixels.Length
    try
        let bpp = 4
        for slot in 0 .. bpp .. pixels.Length-1 do
            pixelFromSlot pixels slot
                |> operator
                |> pixelToSlot pixels' slot
        putPixels dest pixels'
    finally
        freeData source meta

let transformFile operator input output =
    let source = loadFile input
    let dest = foreachPixel operator source
    saveImageAs dest output
