// C# image interop.
// See: loadFile, newImageFrom, foreachPixel.

module BlitCrusher.Image

open System.Drawing
open System.Drawing.Imaging
open BlitCrusher.T


let normalize (x:byte) =
    Channel.Std (float32 x / 255.0f)
let denormalize x =
    255.0f * (Channel.normalize x) |> byte

let pixelFromSlot (v:byte[]) (o:int) :Pixel =
    {   R = normalize v.[o+2];
        G = normalize v.[o+1];
        B = normalize v.[o];
        A = normalize v.[o+3] }
let pixelToSlot (v:byte[]) (o:int) p =
    v.[o+3] <- denormalize p.A
    v.[o+2] <- denormalize p.R
    v.[o+1] <- denormalize p.G
    v.[o]   <- denormalize p.B


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