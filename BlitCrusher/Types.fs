module BlitCrusher.T

open System.Drawing

type Image = {
    Image: Bitmap;
    Filename: string option }

type Channel = float32
type Pixel = {
    R: Channel;
    G: Channel;
    B: Channel;
    A: Channel }
