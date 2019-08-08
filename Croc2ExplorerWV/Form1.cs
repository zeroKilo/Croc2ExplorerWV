using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Be.Windows.Forms;

namespace Croc2ExplorerWV
{
    public partial class Form1 : Form
    {
        WADFile wad;
        string basefolder;
        public Form1()
        {
            InitializeComponent();
            Log.box = rtb1;
        }

        private void openWADFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog d = new FolderBrowserDialog();
            if (d.ShowDialog() == DialogResult.OK)
            {
                basefolder = d.SelectedPath;
                if (!basefolder.EndsWith("\\"))
                    basefolder += "\\";
                string[] files = Directory.GetFiles(basefolder, "*.wad", SearchOption.AllDirectories);
                listBox1.Items.Clear();
                foreach (string file in files)
                    listBox1.Items.Add(file.Substring(basefolder.Length));
            }
        }

        private void RefreshTexture()
        {
            int n = listBox3.SelectedIndex;
            if (n == -1)
                return;
            foreach (WADFile.WADSection sec in wad.sections)
                if (sec.type == "TEXT")
                {
                    WADFile.WADTexture tex = sec.textures[n];
                    if (tex.flags == 0)
                    {
                        comboBox1.Visible = true;
                        pb1.Image = tex.GetBitmap(sec.palettes[comboBox1.SelectedIndex]);
                    }
                    else
                    {
                        comboBox1.Visible = false;
                        pb1.Image = tex.GetBitmap(null);
                    }
                }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshTexture();
        }

        private void LoadWAD()
        {
            int n = listBox1.SelectedIndex;
            if (n == -1)
                return;
            pb1.Image = null;
            wad = new WADFile(basefolder + listBox1.SelectedItem);
            listBox2.Items.Clear();
            listBox3.Items.Clear();
            listBox4.Items.Clear();
            comboBox1.Items.Clear();
            foreach (WADFile.WADSection sec in wad.sections)
            {
                listBox2.Items.Add(sec.type);
                switch (sec.type)
                {
                    case "TEXT":
                        for (int i = 0; i < sec.palettes.Count; i++)
                            comboBox1.Items.Add("Palette " + i);
                        if (sec.palettes.Count > 0)
                            comboBox1.SelectedIndex = 0;
                        for (int i = 0; i < sec.textures.Count; i++)
                            listBox3.Items.Add("Texture " + i + "(" + sec.textures[i].flags.ToString("X2") + ")");
                        break;
                    case "SMPC":
                        for (int i = 0; i < sec.sounds.Count; i++)
                            listBox4.Items.Add("Sound " + i);
                        break;
                }
            }
        }

        private void listBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            rtb1.Clear();
            LoadWAD();
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            int n = listBox2.SelectedIndex;
            if (n == -1)
                return;
            hb1.ByteProvider = new DynamicByteProvider(wad.sections[n].raw);
        }

        private void listBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshTexture();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (pb1.Image == null)
                return;
            Bitmap bmp = (Bitmap)pb1.Image;
            SaveFileDialog d = new SaveFileDialog();
            d.Filter = "*.png|*.png";
            if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                bmp.Save(d.FileName);
                Log.WriteLine("Saved to " + d.FileName);
            }
        }

        private void listBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            int n = listBox4.SelectedIndex;
            if (n == -1)
                return;
            foreach (WADFile.WADSection sec in wad.sections)
                if (sec.type == "SMPC")
                    hb2.ByteProvider = new DynamicByteProvider(sec.sounds[n].data);
        }

        private void saveToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SaveFileDialog d = new SaveFileDialog();
            d.Filter = "*.bin|*.bin";
            if(d.ShowDialog() == DialogResult.OK)
            {
                MemoryStream m = new MemoryStream();
                for (int i = 0; i < hb2.ByteProvider.Length; i++)
                    m.WriteByte(hb2.ByteProvider.ReadByte(i));
                File.WriteAllBytes(d.FileName, m.ToArray());
                Log.WriteLine("Saved to " + d.FileName);
            }
        }

        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int n = listBox3.SelectedIndex;
            if (n == -1)
                return;
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "*.png|*.png";
            if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                foreach(WADFile.WADSection sec in wad.sections)
                    if(sec.type == "TEXT")
                    {
                        WADFile.WADTexture tex = sec.textures[n];
                        tex.ImportData(new Bitmap(d.FileName));
                    }
                rtb1.Clear();
                wad.Resave();
                LoadWAD();
                listBox3.SelectedIndex = n;
            }
        }

        private void exportAsBinToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog d = new SaveFileDialog();
            d.Filter = "*.bin|*.bin";
            if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                MemoryStream m = new MemoryStream();
                for (int i = 0; i < hb1.ByteProvider.Length; i++)
                    m.WriteByte(hb1.ByteProvider.ReadByte(i));
                File.WriteAllBytes(d.FileName, m.ToArray());
                Log.WriteLine("Saved to " + d.FileName);
            }
        }

        private void importFromBinToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int n = listBox2.SelectedIndex;
            if (n == -1)
                return;
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "*.bin|*.bin";
            if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                wad.sections[n].raw = File.ReadAllBytes(d.FileName);
                rtb1.Clear();
                wad.Resave();
                LoadWAD();
                listBox2.SelectedIndex = n;
            }
        }
    }
}
