module Bitcore.Types

open System.Drawing

type Image = {
    Image: Bitmap;
    Filename: string option }


// math utilities (roundHalfUp also used by Operators.levels)
let inline roundHalfUp (value:float) =
    System.Math.Round(value, System.MidpointRounding.AwayFromZero)
let private saturate lo hi x :float =
    if x < lo then lo
    elif x > hi then hi
    else x
let private fmod x m =
    let rec step v =
        if v < 0.0 then v + m |> step
        elif v >= m then v - m |> step
        else v
    step x


type MatrixPixel =
    abstract member asTuple3: unit -> float*float*float
type Channel =
    abstract member raw: unit -> float
    abstract member normalize: unit -> float
    abstract member denormalize: float -> Channel
    abstract member apply: (float -> float) -> Channel

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

type PxRGB = private {R: Channel; G: Channel; B: Channel; A: Channel} with
    member self.r = self.R.normalize()
    member self.g = self.G.normalize()
    member self.b = self.B.normalize()
    member self.a = self.A.normalize()
    member self.apply4 alphafn redfn greenfn bluefn =
        {R = self.R.apply redfn; G = self.G.apply greenfn; B = self.B.apply bluefn;
         A = self.A.apply alphafn}
    member self.apply3 redfn greenfn bluefn =
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
let ARGB alpha (r, g, b) =
    {R = createStd r; G = createStd g; B = createStd b; A = createAlpha alpha}
let RGB (r, g, b) =
    {R = createStd r; G = createStd g; B = createStd b; A = Opaque}

type PxYUV = private {Y: Channel; U: Channel; V: Channel; A: Channel} with
    member self.y = self.Y.normalize()
    member self.u = self.U.normalize()
    member self.v = self.V.normalize()
    member self.a = self.A.normalize()
    interface MatrixPixel with
        member self.asTuple3 () = (self.Y.raw(), self.U.raw(), self.V.raw())
    member self.apply4 alphafn yfn ufn vfn =
        {Y = self.Y.apply yfn; U = self.U.apply ufn; V = self.V.apply vfn;
         A = self.A.apply alphafn}
    member self.apply3 yfn ufn vfn =
        {Y = self.Y.apply yfn; U = self.U.apply ufn; V = self.V.apply vfn;
         A = self.A}
let AYUV alpha (y, u, v) =
    {Y = createY y; U = createU u; V = createV v; A = createAlpha alpha}
let YUV (y, u, v) =
    {Y = createY y; U = createU u; V = createV v; A = Opaque}

type PxYIQ = private {Y: Channel; I: Channel; Q: Channel; A: Channel} with
    member self.y = self.Y.normalize()
    member self.i = self.I.normalize()
    member self.q = self.Q.normalize()
    member self.a = self.A.normalize()
    interface MatrixPixel with
        member self.asTuple3 () = (self.Y.raw(), self.I.raw(), self.Q.raw())
    member self.apply4 alphafn yfn ifn qfn =
        {Y = self.Y.apply yfn; I = self.I.apply ifn; Q = self.Q.apply qfn;
         A = self.A.apply alphafn}
    member self.apply3 yfn ifn qfn =
        {Y = self.Y.apply yfn; I = self.I.apply ifn; Q = self.Q.apply qfn;
         A = self.A}
let AYIQ alpha (y, i, q) =
    {Y = createY y; I = createI i; Q = createQ q; A = createAlpha alpha}
let YIQ (y, i, q) =
    {Y = createY y; I = createI i; Q = createQ q; A = Opaque}

type PxGray = private {Y: Channel; A: Channel} with
    member self.y = self.Y.normalize()
    member self.a = self.A.normalize()
    member self.apply2 alphafn yfn =
        {Y = self.Y.apply yfn; A = self.A.apply alphafn}
    member self.apply1 yfn =
        {Y = self.Y.apply yfn; A = self.A}
let AGray alpha y =
    {Y = createY y; A = createAlpha alpha}
let Gray y =
    {Y = createY y; A = Opaque}


type PxHSV = private {H: Channel; S: Channel; V: Channel; A: Channel} with
    member self.h6 = self.H.raw()
    member self.h = self.H.normalize()
    member self.s = self.S.normalize()
    member self.v = self.V.normalize()
    member self.a = self.A.normalize()
    member self.apply4 alphafn huefn satfn valfn =
        {H = self.H.apply huefn; S = self.S.apply satfn; V = self.V.apply valfn;
         A = self.A.apply alphafn}
    member self.apply3 huefn satfn valfn =
        {H = self.H.apply huefn; S = self.S.apply satfn; V = self.V.apply valfn;
         A = self.A}
let AHSV alpha (h, s, v) =
    {H = createHue h; S = createSat s; V = createVal v; A = createAlpha alpha}
let HSV (h, s, v) =
    {H = createHue h; S = createSat s; V = createVal v; A = Opaque}


type CliData = {
    transforms: string[];
    files: string[]
}
type CliResult =
    | CliParse of CliData
    | CliError of string
