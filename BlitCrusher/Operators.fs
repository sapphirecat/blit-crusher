module BlitCrusher.Operators

open BlitCrusher.Types


let levels = Channel.levels
let private bits_ depth = 2.0 ** (float depth) |> int
let bits depth = bits_ depth |> Channel.levels
let nearbits depth = bits_ depth - 1 |> Channel.levels


let toLinearSpace matrix loRanges hiRanges px =
    pixelToMatrix px
    |> matrixMult matrix
    |> matrixToRangeTuple loRanges hiRanges
// FIXME: alpha is a pain. it doesn't participate in the color transforms,
// but I want to carry it through.  (but toLinearSpace doesn't...)
let fromLinearSpace matrix tuple3 alpha =
    tupleToMatrix tuple3
    |> matrixMult matrix
    |> matrixToPixel alpha


let toYIQ px =
    let t =
        [| 0.299;  0.587;  0.114;
           0.596; -0.275; -0.321;
           0.212; -0.523;  0.311 |]
    let t' = matrixInit 3 3 t
    let lo = [| 0.0; -0.5957; -0.5226 |]
    let hi = [| 1.0; -lo.[1]; -lo.[2] |]
    toLinearSpace t' lo hi px
let fromAYIQ a yiq =
    let t =
        [| 1.0;  0.956;  0.621;
           1.0; -0.272; -0.647;
           1.0; -1.105;  1.702 |]
    let t' = matrixInit 3 3 t
    fromLinearSpace t' yiq a
let fromYIQ = fromAYIQ Opaque


let toYUV px =
    let uMax, vMax = 0.436, 0.615
    let t =
        [| 0.299;    0.587;    0.114;
          -0.14713; -0.28886;  uMax;
           vMax;    -0.51499; -0.10001 |]
    let t' = matrixInit 3 3 t
    let lo = [| 0.0; -uMax; -vMax |]
    let hi = [| 1.0;  uMax;  vMax |]
    toLinearSpace t' lo hi px
let fromAYUV a yuv =
    let t =
        [| 1.0;  0.0;      1.13983;
           1.0; -0.39465; -0.58060;
           1.0;  2.03211;  0.0 |]
    let t' = matrixInit 3 3 t
    fromLinearSpace t' yuv a
let fromYUV = fromAYUV Opaque


let toHSV px =
    // find the min/max channel values
    let r, g, b = Pixel.asTuple3 px
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
    // calculate final hue
    let h' = h*60.0 |> Channel.Hue
    // safely calculate saturation
    let s = if maxC > 0.0 then delta/maxC else 0.0
    h', Channel.Std s, Channel.Std maxC
let fromAHSV a (hC, sC, vC) =
    let h = Channel.raw hC / 60.0
    let s = Channel.raw sC
    let v = Channel.raw vC
    // integer and fractional parts of hue
    let i = h |> floor
    let f = h - i
    // magic values (I never understood these, nor their names)
    let p = v * (1.0 - s)
    let q = v * (1.0 - s * f)
    let t = v * (1.0 - s * (1.0 - f))
    // FIXME: Pythonic/imperative approach
    let red,green,blue =
        if s <= 0.0 then v,v,v
        elif i < 1.0 then v,t,p
        elif i < 2.0 then q,v,p
        elif i < 3.0 then p,v,t
        elif i < 4.0 then p,q,v
        elif i < 5.0 then t,p,v
        else v,p,q
    Pixel.RGBA red green blue (Channel.normalize a)
let fromHSV = fromAHSV Opaque


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

let asYIQA yfn ifn qfn alphafn px =
    let y, i, q = toYIQ px
    let a = alphafn px.A
    fromAYIQ a (yfn y, ifn i, qfn q)
let asYIQ yfn ifn qfn =
    asYIQA yfn ifn qfn id

let asYUVA yfn ufn vfn alphafn px =
    let y, u, v = toYUV px
    let a = alphafn px.A
    fromAYUV a (yfn y, ufn u, vfn v)
let asYUV yfn ufn vfn =
    asYUVA yfn ufn vfn id

let toGray value px =
    {R = value; G = value; B = value; A = px.A}
let asY fn px =
    let y, _, _ = toYIQ px
    toGray (fn y) px
let asU fn px =
    let _, u, _ = toYUV px
    toGray (fn u) px
let asV fn px =
    let _, _, v = toYUV px
    toGray (fn v) px
