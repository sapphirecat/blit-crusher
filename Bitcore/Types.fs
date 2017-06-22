module Bitcore.Types

open System.Drawing

// Image types
type Image = {
    Image: Bitmap;
    Filename: string option }


// math utilities (roundHalfUp also used by Operators.levels)
let inline roundHalfUp (value:float) =
    System.Math.Round(value, System.MidpointRounding.AwayFromZero)
let inline private saturate lo hi x :float =
    // using min/max: +3% overall run time.
    // using pattern match (nop branch first): +9%.
    // since this is called for every channel, let's keep it fast.
    if x < lo then lo
    elif x > hi then hi
    else x
let inline private fmod x m =
    match x with
    | x when x <  0.0 -> x + (m * (-x/m |> ceil))
    | x when x >= m   -> x - (m * (x/m |> floor))
    | _               -> x


// Components of an individual pixel
type Channel =
    abstract member raw: unit -> float
    abstract member normalize: unit -> float
    abstract member denormalize: float -> Channel
    abstract member apply: (float -> float) -> Channel
type MatrixPixel =
    abstract member asTuple3: unit -> float*float*float


let inline private transform f (c:Channel) = c.normalize() |> f |> c.denormalize

type Channel1 = private Channel1 of float with
    static member Create value = saturate 0.0 1.0 value |> Channel1
    member private self.toChannel value = Channel1.Create value :> Channel
    interface Channel with
        member self.raw () = match self with Channel1 value -> value
        member self.normalize () = (self :> Channel).raw()
        member self.denormalize value = self.toChannel value
        member self.apply f =
            match self with Channel1 value -> f value |> self.toChannel

type ChannelAxis = private {value:float; limit:float} with
    static member Create limit value =
        {value = saturate -limit limit value; limit = limit}
    interface Channel with
        member self.raw () = self.value
        member self.normalize () = (self.value+self.limit)/(2.0*self.limit)
        member self.denormalize value =
            let v = 2.0 * self.limit * value - self.limit
            ChannelAxis.Create self.limit v :> Channel
        member self.apply f = transform f self

type ChannelMod = private {value:float; modulus:float} with
    static member Create modulus value =
        {value = fmod value modulus; modulus = modulus}
    interface Channel with
        member self.raw () = self.value
        member self.normalize () = (self.value / self.modulus)
        member self.denormalize value =
            let v = value * self.modulus
            ChannelMod.Create self.modulus v :> Channel
        member self.apply f = transform f self


// Pixel types
type PxRGB = private {R: Channel; G: Channel; B: Channel; A: Channel} with
    member self.r = self.R.normalize()
    member self.g = self.G.normalize()
    member self.b = self.B.normalize()
    member self.a = self.A.normalize()
    member self.applyA redfn greenfn bluefn alphafn =
        {R = self.R.apply redfn; G = self.G.apply greenfn; B = self.B.apply bluefn;
        A = self.A.apply alphafn}
    member self.apply redfn greenfn bluefn =
        // optimization: copy alpha, to skip .raw/id/saturate/Create calls
        {R = self.R.apply redfn; G = self.G.apply greenfn; B = self.B.apply bluefn;
        A = self.A}
    interface MatrixPixel with
        member self.asTuple3 () = (self.R.raw(), self.G.raw(), self.B.raw())

    // special extra features for Image to read/write RGBA pixels
    member self.setByteSlice (a:byte[]) pos =
        let inline asByte v = v*255.0 |> roundHalfUp |> byte
        a.[pos]   <- self.B.raw() |> asByte
        a.[pos+1] <- self.G.raw() |> asByte
        a.[pos+2] <- self.R.raw() |> asByte
        a.[pos+3] <- self.A.raw() |> asByte
    static member fromByteSlice (a:byte[]) pos =
        let inline asFloat x = float x / 255.0
        {B = Channel1 (asFloat a.[pos]);
         G = Channel1 (asFloat a.[pos+1]);
         R = Channel1 (asFloat a.[pos+2]);
         A = Channel1 (asFloat a.[pos+3]) }

type PxYUV = private {Y: Channel; U: Channel; V: Channel; A: Channel} with
    member self.y = self.Y.normalize()
    member self.u = self.U.normalize()
    member self.v = self.V.normalize()
    member self.a = self.A.normalize()
    member self.applyA yfn ufn vfn alphafn =
        {Y = self.Y.apply yfn; U = self.U.apply ufn; V = self.V.apply vfn;
        A = self.A.apply alphafn}
    member self.apply yfn ufn vfn =
        {Y = self.Y.apply yfn; U = self.U.apply ufn; V = self.V.apply vfn;
        A = self.A}
    interface MatrixPixel with
        member self.asTuple3 () = (self.Y.raw(), self.U.raw(), self.V.raw())

type PxYIQ = private {Y: Channel; I: Channel; Q: Channel; A: Channel} with
    member self.y = self.Y.normalize()
    member self.i = self.I.normalize()
    member self.q = self.Q.normalize()
    member self.a = self.A.normalize()
    member self.applyA yfn ifn qfn alphafn =
        {Y = self.Y.apply yfn; I = self.I.apply ifn; Q = self.Q.apply qfn;
         A = self.A.apply alphafn}
    member self.apply yfn ifn qfn =
        {Y = self.Y.apply yfn; I = self.I.apply ifn; Q = self.Q.apply qfn;
         A = self.A}
    interface MatrixPixel with
        member self.asTuple3 () = (self.Y.raw(), self.I.raw(), self.Q.raw())

type PxHSV = private {H: Channel; S: Channel; V: Channel; A: Channel} with
    member self.h6 = self.H.raw()
    member self.h = self.H.normalize()
    member self.s = self.S.normalize()
    member self.v = self.V.normalize()
    member self.a = self.A.normalize()
    member self.applyA huefn satfn valfn alphafn =
        {H = self.H.apply huefn; S = self.S.apply satfn; V = self.V.apply valfn;
         A = self.A.apply alphafn}
    member self.apply huefn satfn valfn =
        {H = self.H.apply huefn; S = self.S.apply satfn; V = self.V.apply valfn;
         A = self.A}

type PxGray = private {Y: Channel; A: Channel} with
    member self.y = self.Y.normalize()
    member self.a = self.A.normalize()
    member self.applyA yfn alphafn =
        {Y = self.Y.apply yfn; A = self.A.apply alphafn}
    member self.apply yfn =
        {Y = self.Y.apply yfn; A = self.A}


// aliases for the pixel constructors
let private createStd = Channel1.Create
let private createAlpha = Channel1.Create

let private createY = Channel1.Create
let private createU = ChannelAxis.Create 0.436
let private createV = ChannelAxis.Create 0.615
let private createI = ChannelAxis.Create 0.5957
let private createQ = ChannelAxis.Create 0.5226

// Hue on 0.0..6.0 shows up a lot in the calculations. Instead of *60 and /60
// going to/from the space, let's just store it as 0.0..6.0.  Outside code
// only sees it normalized anyway.
let private createHue = ChannelMod.Create 6.0
let private createSat = Channel1.Create
let private createVal = Channel1.Create

let private Opaque = createAlpha 1.0

// Pixel constructors
let ARGB alpha (r, g, b) =
    {R = createStd r; G = createStd g; B = createStd b; A = createAlpha alpha}
let RGB (r, g, b) =
    {R = createStd r; G = createStd g; B = createStd b; A = Opaque}

let AYUV alpha (y, u, v) =
    {Y = createY y; U = createU u; V = createV v; A = createAlpha alpha}
let YUV (y, u, v) =
    {Y = createY y; U = createU u; V = createV v; A = Opaque}

let AYIQ alpha (y, i, q) =
    {Y = createY y; I = createI i; Q = createQ q; A = createAlpha alpha}
let YIQ (y, i, q) =
    {Y = createY y; I = createI i; Q = createQ q; A = Opaque}

let AHSV alpha (h, s, v) =
    {H = createHue h; S = createSat s; V = createVal v; A = createAlpha alpha}
let HSV (h, s, v) =
    {H = createHue h; S = createSat s; V = createVal v; A = Opaque}

let AGray alpha y =
    {Y = createY y; A = createAlpha alpha}
let Gray y =
    {Y = createY y; A = Opaque}
