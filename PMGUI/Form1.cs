using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PropellerManager;
using System.IO;

namespace PMGUI {
    public partial class Form1 : Form {
        public Form1() {
            InitializeComponent();
            MessageBox.Show("This is a demo DLL GUI", "PMGUI");
        }
        MPK Packget;
        Stream MPKStream;
        private void extractToolStripMenuItem_Click(object sender, EventArgs e) {
            OpenFileDialog File = new OpenFileDialog() {
                Filter = "All MPK Files|*.mpk"
            };
            if (File.ShowDialog() != DialogResult.OK)
                return;
            MPKStream?.Close();
            string BaseDir = File.FileName + "~{0}\\";
            int ind = 0;
            while (Directory.Exists(string.Format(BaseDir, ind)))
                ind++;
            BaseDir = string.Format(BaseDir, ind);
            Directory.CreateDirectory(BaseDir);
            MPKStream = new StreamReader(File.FileName).BaseStream;
            Packget = new MPK(MPKStream);
            MPKEntry[] Files = Packget.Open();
            foreach (MPKEntry Entry in Files) {
                Stream output = new StreamWriter(BaseDir + Entry.FileName).BaseStream;

                CopyStream(Entry.Content, output);
                output.Close();
            }
            MessageBox.Show("Packget Extracted.", "PMGUI", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        public static void CopyStream(Stream input, Stream output) {
            int Readed = 0;
            byte[] Buffer = new byte[1024 * 1024];
            do {
                Readed = input.Read(Buffer, 0, Buffer.Length);
                output.Write(Buffer, 0, Readed);
            } while (Readed > 0);
        }

        private void repackkToolStripMenuItem_Click(object sender, EventArgs e) {
            if (Packget == null) {
                MessageBox.Show("Extract before repack", "PMGUI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            FolderBrowserDialog folder = new FolderBrowserDialog();
            SaveFileDialog save = new SaveFileDialog() {
                Filter = "All MPK Files|*.mpk"
            };

            if (folder.ShowDialog() != DialogResult.OK)
                return;

            if (save.ShowDialog() != DialogResult.OK)
                return;

            string[] Files = Directory.GetFiles(folder.SelectedPath, "*.*", SearchOption.AllDirectories);
            for (int i = 0; i < Files.Length; i++)
                Files[i] = Files[i].Substring(folder.SelectedPath.Length, Files[i].Length - folder.SelectedPath.Length);

            MPKEntry[] Entries = new MPKEntry[Files.Length];
            for (int i = 0; i < Entries.Length; i++) {
                Entries[i] = new MPKEntry() {
                    FileName = Files[i],
                    Content = new StreamReader(folder.SelectedPath + Files[i]).BaseStream
                };
            }

            Stream Output = new StreamWriter(save.FileName).BaseStream;

            Packget.Repack(Output, Entries, true);
            MessageBox.Show("Packget Created.", "PMGUI", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }


        MSCTL Editor;
        private void openToolStripMenuItem_Click(object sender, EventArgs e) {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "All MSC Files|*.msc";

            if (fd.ShowDialog() != DialogResult.OK)
                return;

            byte[] script = File.ReadAllBytes(fd.FileName);
            Editor = new MSCTL(script);

            listBox1.Items.Clear();
            foreach (string str in Editor.Import())
                listBox1.Items.Add(str);
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e) {
            try {
                textBox1.Text = listBox1.Items[listBox1.SelectedIndex].ToString();
            }
            catch { }
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e) {
            if (e.KeyChar == '\n' || e.KeyChar == '\r') {
                try {
                    listBox1.Items[listBox1.SelectedIndex] = textBox1.Text;
                }
                catch {

                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
            SaveFileDialog fd = new SaveFileDialog();
            fd.Filter = "All MSC Files|*.msc";

            if (fd.ShowDialog() != DialogResult.OK)
                return;

            string[] Strings = new string[listBox1.Items.Count];
            for (int i = 0; i < Strings.Length; i++) {
                Strings[i] = listBox1.Items[i].ToString();
            }

            File.WriteAllBytes(fd.FileName, Editor.Export(Strings));
            MessageBox.Show("File Saved.", "PMGUI", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
