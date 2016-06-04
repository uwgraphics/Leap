using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Speech.Synthesis;

namespace SpeechSynthesis
{
    public partial class Form1 : Form
    {
        private StreamWriter visemeFile = null;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SpeechSynthesizer synth = new SpeechSynthesizer();

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = @".";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.Filter = "txt files (*.txt)|*.txt";
            openFileDialog.Multiselect = true;
            openFileDialog.Title = "Convert .txt files to speech (.wav) with viseme info";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string[] filenames = openFileDialog.FileNames;
                foreach (string file in filenames)
                {
                    synth.VisemeReached += new EventHandler<VisemeReachedEventArgs>(synth_VisemeReached);
                    StreamReader sr = new StreamReader(file);
                    string contents = sr.ReadToEnd();
                    visemeFile = File.CreateText(file.Replace(".txt", ".csv"));
                    synth.SetOutputToDefaultAudioDevice();
                    synth.Speak(contents);
                    visemeFile.Close();
                    synth.VisemeReached -= synth_VisemeReached;

                    synth.SetOutputToWaveFile(file.Replace(".txt", ".wav"));
                    synth.Speak(contents);
                }
            }
            Application.Exit();
        }

        private void synth_VisemeReached(object sender, VisemeReachedEventArgs e)
        {
            visemeFile.WriteLine("{0},{1}", e.AudioPosition.TotalSeconds, e.Viseme);
            visemeFile.Flush();
        }
    }
}
