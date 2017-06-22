// C# interop module
module Bitcore.Interop

open Bitcore.Image
open Bitcore.Operators


// expose partial application
let UseLevels i = levels i
// Use[Near]Bits provided for convenience.  Callers don't need to also use
// Bitcore.Operators to get bit2, near2, etc.
let UseBits i = bits i
let UseNearBits i = nearbits i
// These exist, so let's expose them, too.
let UseMin = set0
let UseCenter = setCenter
let UseMax = set1


// expose known color space names
let ColorSpaceNames = spaces.Keys


// foreachPixel with a System.Drawing.Bitmap and return the result Bitmap
let Apply4 bitmap spaceName fn1 fn2 fn3 fnA =
    let image = newImageFromBitmap bitmap
    let operator = spaces.[spaceName] fnA fn1 fn2 fn3
    let output = foreachPixel operator image
    output.Image
let Apply3 bitmap spaceName fn1 fn2 fn3 =
    Apply4 bitmap spaceName fn1 fn2 fn3 id
let Apply1 bitmap spaceName fn1 =
    Apply4 bitmap spaceName fn1 id id id
