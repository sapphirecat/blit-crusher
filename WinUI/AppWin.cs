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
        private Image LoadedImage { get; set; }
        private int[] CurrentLevels { get; set; } = null;
        private string CurrentSpace { get; set; }

        public AppWin()
        {
            InitializeComponent();
            ParseLevelsFromToolStrip();
            SetColorspace("rgb");
            CloseFile();
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

        private void UpdateDisplayImage()
        {
            // TODO: figure out how to make PictureBox track the parent control size
            var box = imagePreview;

            if (LoadedImage == null)
            {
                // TODO: have a default image built in
                box.Image = null;
                return;
            }

            // reset size
            var thumb = Bitcore.Image.resizeBitmap(box.Width, box.Height, LoadedImage);

            // apply transformation
            if (CurrentLevels != null && thumb is Bitmap)
            {
                thumb = ApplyTransform(thumb, CurrentSpace, CurrentLevels);
            }

            // set transformation results
            box.Image = thumb;
        }

        private void ShowFile(string name)
        {
            mainStatusLabel.Text = System.IO.Path.GetFileName(name);
            LoadedImage = Image.FromFile(name);
            UpdateDisplayImage();
        }

        private void OpenNewFile()
        {
            try
            {
                var dlg = new OpenFileDialog();

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    this.ShowFile(dlg.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load file: " + ex.Message);
            }
        }

        private void CloseFile ()
        {
            LoadedImage = null;
            imagePreview.Image = null;
            mainStatusLabel.Text = "No file loaded";
        }

        private void RefreshEvent (object sender, EventArgs e)
        {
            ParseLevelsFromToolStrip();
            UpdateDisplayImage();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenNewFile();
        }

        private void openImage_Click(object sender, EventArgs e)
        {
            OpenNewFile();
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

        private void colorspaceMenuItem_Click(object sender, EventArgs e)
        {
            var item = sender as ToolStripMenuItem;
            SetColorspace(item.Tag.ToString());
            UpdateDisplayImage();
        }
    }
}
