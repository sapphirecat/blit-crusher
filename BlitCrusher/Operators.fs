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
let levels count (channel:Channel) :Channel =
    let ct = count - 1 |> float32
    let c = channel * ct |> round
    c/ct

let bits depth channel =
    let _lv = 2.0f ** float32 depth |> round |> int
    levels _lv channel

let toHSV px =
    // find the min/max channel values
    let maxC = Array.reduce max [| px.R; px.G; px.B |]
    let minC = Array.reduce min [| px.R; px.G; px.B |]
    // range between max/min
    let delta = maxC - minC

    // calculate a basic hue based on delta and which channel is max
    let h = if maxC <= minC then 0.0f
            elif maxC <= px.R then (px.G - px.B)
            elif maxC <= px.G then 2.0f + (px.B - px.R)
            else 4.0f + (px.R - px.G)
    // calculate final hue
    let h' = h*60.0f |> hueclamp
    // safely calculate saturation
    let s = if maxC > 0.0f then delta/maxC else 0.0f
    h',s,maxC
let fromAHSV a (h:Channel, s:Channel, v:Channel) =
    let h' = h / 60.0f
    let i = h' |> floor
    let f = h' - i
    let p = v * (1.0f - s)
    let q = v * (1.0f - s * f)
    let t = v * (1.0f - s * (1.0f - f))
    // FIXME: Pythonic/imperative approach
    let red,green,blue =
        if s <= 0.0f then v,v,v
        elif i < 1.0f then v,t,p
        elif i < 2.0f then q,v,p
        elif i < 3.0f then p,v,t
        elif i < 4.0f then p,q,v
        elif i < 5.0f then t,p,v
        else v,p,q
    {R = red; G = green; B = blue; A = a }
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
