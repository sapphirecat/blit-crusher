using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinUI
{
    public partial class AppWin : Form
    {
        private Image LoadedImage { get; set; } = null; // full size
        private Image ThumbImage { get; set; } = null; // reduced size
        private Image DisplayImage { get; set; } = null; // reduced and transformed
        private int[] CurrentLevels { get; set; } = null;
        private string CurrentSpace { get; set; } = "rgb";
        private string ImageBasename { get; set; } = null;
        private bool TaskRunning { get; set; } = false;
        private bool TaskNeeded { get; set; } = false;

        public AppWin()
        {
            InitializeComponent();
            ParseLevelsFromToolStrip();
            SetColorspace(CurrentSpace);
            ClearDisplayImage(); // initial "no file loaded" message
        }

        private void SetColorspace (string space)
        {
            var items = colorSpaceButton.DropDownItems;
            ToolStripItem item;
            ToolStripMenuItem menu;
            bool locked = false;
            int i;

            for (i = 0; i < items.Count; i++)
            {
                item = items[i];
                if (item is ToolStripMenuItem)
                {
                    menu = item as ToolStripMenuItem;
                    menu.Checked = locked ? false : (menu.Tag.ToString() == space);
                    if (menu.Checked)
                    {
                        locked = true;
                        CurrentSpace = space;
                    }
                }
            }
        }

        private bool ParseLevelsFromText (ToolStripTextBox[] controls, out int[] levels)
        {
            int tmp;
            int pos = 0;
            levels = new int[controls.Length];
            
            foreach (var control in controls)
            {
                if (! Int32.TryParse(control.Text, out tmp))
                {
                    return false; // at least one failure
                }

                levels[pos++] = tmp;
            }

            return true; // all parses successful
        }

        private int[] ParseLevelsFromToolStrip()
        {
            var controls = new ToolStripTextBox[] { levels1, levels2, levels3 };
            if (ParseLevelsFromText(controls, out int[] levels))
            {
                CurrentLevels = levels;
            }
            return CurrentLevels;
        }

        private Bitmap ApplyTransform (Image image, string colorspace, int[] levels)
        {
            if (! (image is Bitmap))
            {
                throw new ArgumentException("Image to transform should be a Bitmap", "image");
            }
            if (levels.Length != 3)
            {
                throw new ArgumentException($"Received {levels.Length} levels, expected 3", "levels");
            }

            return Bitcore.Interop.Apply3(image as Bitmap, colorspace,
                    Bitcore.Interop.UseLevels(levels[0]),
                    Bitcore.Interop.UseLevels(levels[1]),
                    Bitcore.Interop.UseLevels(levels[2]));
        }

        private void ClearDisplayImage()
        {
            mainStatusLabel.Text = "No file loaded";
            imagePreview.Image = null;
            ImageBasename = null;
            LoadedImage = null;
            ThumbImage = null;
            DisplayImage = null;
            TaskNeeded = false;
        }

        private Image CreateDisplayImage()
        {
            if (CurrentLevels != null && ThumbImage is Bitmap)
            {
                return ApplyTransform(ThumbImage, CurrentSpace, CurrentLevels);
            }
            else
            {
                return ThumbImage;
            }
        }

        private async Task<Image> UpdateDisplayImage()
        {
            var box = imagePreview;
            bool reenter;

            // if we're already processing, don't try again (image bits may already be locked)
            if (TaskRunning)
            {
                TaskNeeded = true; // signal need for reprocessing
                return await Task.FromResult(DisplayImage);
            }
            else
            {
                TaskRunning = true; // block later processors
            }

            if (LoadedImage == null)
            {
                ClearDisplayImage();
                return await Task.FromResult(LoadedImage);
            }

            mainStatusLabel.Text = "Processing...";

            // reset size if needed
            if (ThumbImage == null || (box.Width != ThumbImage.Width && box.Height != ThumbImage.Height))
            {
                ThumbImage = Bitcore.Image.resizeBitmap(box.Width, box.Height, LoadedImage);
            }

            // apply transformation on a background thread
            DisplayImage = await Task.Run((Func<Image>)CreateDisplayImage);

            // set transformation results
            mainStatusLabel.Text = ImageBasename;
            box.Image = DisplayImage;

            // check whether a reprocessing request was signaled
            reenter = TaskNeeded;
            TaskNeeded = false;
            TaskRunning = false;
            // reenter if needed, or just return what we have
            return reenter ? await UpdateDisplayImage() : DisplayImage;
        }

        private async Task ShowFile(string name)
        {
            ImageBasename = System.IO.Path.GetFileName(name);
            LoadedImage = Image.FromFile(name);
            await UpdateDisplayImage();
        }

        private async Task<string> OpenNewFile()
        {
            string opened = null;
            try
            {
                var dlg = new OpenFileDialog();

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    opened = dlg.FileName;
                    await ShowFile(opened);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load file: " + ex.Message);
            }

            return await Task.FromResult(opened);
        }

        private void CloseFile ()
        {
            ClearDisplayImage();
        }

        private async void RefreshEvent (object sender, EventArgs e)
        {
            ParseLevelsFromToolStrip();
            await UpdateDisplayImage();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private async void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await OpenNewFile();
        }

        private async void openImage_Click(object sender, EventArgs e)
        {
            await OpenNewFile();
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseFile();
        }

        private void levels1_Enter(object sender, EventArgs e)
        {
            levels1.SelectAll();
        }

        private void levels2_Enter(object sender, EventArgs e)
        {
            levels2.SelectAll();
        }

        private void levels3_Enter(object sender, EventArgs e)
        {
            levels3.SelectAll();
        }

        private async void colorspaceMenuItem_Click(object sender, EventArgs e)
        {
            var item = sender as ToolStripMenuItem;
            SetColorspace(item.Tag.ToString());
            await UpdateDisplayImage();
        }

    }
}
