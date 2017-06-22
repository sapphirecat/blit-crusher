// C# image interop.
// See: loadFile, newImageFrom, foreachPixel.

module Bitcore.Image

open System.Drawing
open System.Drawing.Imaging
open Bitcore.Types


let getData lockmode source =
    let image = source.Image
    let rect = new Rectangle(0, 0, image.Width, image.Height)
    let format = PixelFormat.Format32bppArgb
    image.LockBits(rect, lockmode, format)

let getDataIn = getData ImageLockMode.ReadOnly
let getDataOut = getData ImageLockMode.WriteOnly

let freeData source data =
    source.Image.UnlockBits(data)

let loadFile filename =
    try
        Ok {Image = new Bitmap(Image.FromFile(filename)); Filename = Some filename}
    with ex ->
        Error ex

let _newImage (width:int) (height:int) filename =
    {Image = new Bitmap(width, height); Filename = filename}

let newImage width height filename =
    _newImage width height (Some filename)

let newImageFrom source =
    let image = source.Image
    _newImage image.Width image.Height None

let newImageFromBitmap bitmap =
    {Image = bitmap; Filename = None}


let saveImage image =
    match image.Filename with
    | Some name -> image.Image.Save(name)
    | None -> invalidOp "Image does not have a filename, use saveImageAs"
    image

let saveImageAs image filename =
    image.Image.Save(filename)
    {image with Filename = Some filename}

let getPixels image =
    let meta = getDataIn image
    let start = meta.Scan0
    let size = meta.Height * abs meta.Stride
    let pixels:array<byte> = Array.zeroCreate size
    System.Runtime.InteropServices.Marshal.Copy(start, pixels, 0, size)
    pixels, meta

let putPixels image (pixels:array<byte>) =
    let meta = getDataOut image
    try
        let start = meta.Scan0
        let size = meta.Height * abs meta.Stride
        System.Runtime.InteropServices.Marshal.Copy(pixels, 0, start, size)
        image
    finally
        freeData image meta


// ideally, there'd be a generic "get mask" function and foreachPixel
// would just be a 1x1 mask application
let foreachPixel (operator:PxRGB -> PxRGB) source =
    let dest = newImageFrom source
    let pixels, meta = getPixels source
    let pixels' = Array.zeroCreate pixels.Length

    let fn slot =
        let out = PxRGB.fromByteSlice pixels slot |> operator
        out.setByteSlice pixels' slot

    try
        Array.Parallel.iter fn [| 0 .. 4 .. pixels.Length-1 |]
        putPixels dest pixels'
    finally
        freeData source meta

let transformSource operator image output =
    let dest = foreachPixel operator image
    saveImageAs dest output

let transformFile operator input output =
    let source = loadFile input
    match source with
    | Ok image -> Ok (transformSource operator image output)
    | _ -> source
