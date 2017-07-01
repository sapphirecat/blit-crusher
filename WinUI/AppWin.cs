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
        private int[] LastValidLevels { get; set; }

        public AppWin()
        {
            InitializeComponent();
            CloseFile();
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
                LastValidLevels = levels;
            }
            return LastValidLevels;
        }

        private void UpdateDisplayImage()
        {
            // TODO: pull transform from menus, don't hardcode RGB332
            // TODO: figure out how to make PictureBox track the parent control size
            var box = imagePreview;
            int[] levels = ParseLevelsFromToolStrip();
            string colorspace = "yiq";

            if (LoadedImage == null)
            {
                // TODO: have a default image built in
                box.Image = null;
                return;
            }

            // reset size
            var thumb = Bitcore.Image.resizeBitmap(box.Width, box.Height, LoadedImage);

            // apply transformation
            if (levels != null && thumb is Bitmap)
            {
                thumb = Bitcore.Interop.Apply3(thumb as Bitmap, colorspace,
                    Bitcore.Interop.UseLevels(levels[0]),
                    Bitcore.Interop.UseLevels(levels[1]),
                    Bitcore.Interop.UseLevels(levels[2]));
            }

            // set transformation results
            box.Image = thumb;
        }

        private void ShowFile(string name)
        {
            mainStatusLabel.Text = System.IO.Path.GetFileName(name);
            LoadedImage = System.Drawing.Image.FromFile(name);
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
    }
}
