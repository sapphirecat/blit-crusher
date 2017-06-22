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

        public AppWin()
        {
            InitializeComponent();
            mainStatusLabel.Text = "Open an image file to begin";
        }

        private void UpdateDisplayImage()
        {
            /*
            // TODO: restructure F# to take dynamic things (colorspace, levels, levels, levels) and call foreachPixel itself
            // TODO: pull transform from menus, don't hardcode RGB332
            // TODO: rescale base image to current size here
            string colorspace = "rgb";
            int depthA = 8;
            int depthB = 8;
            int depthC = 4;
            DoubleCrush fA = v => Bitcore.Operators.levels(depthA, v);
            DoubleCrush fB = v => Bitcore.Operators.levels(depthB, v);
            DoubleCrush fC = v => Bitcore.Operators.levels(depthC, v);
            var mapFn = Bitcore.Operators.asRGB;
            */

            imagePreview.Image = LoadedImage;
        }

        private void ShowFile(string name)
        {
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
    }
}
