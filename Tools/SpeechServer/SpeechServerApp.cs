using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Speech.Recognition;

namespace SpeechServer
{
    class SpeechServerApp
    {
        public ISpeechServer SpeechRecognizer
        {
            get
            {
                return speechRecognizer;
            }
        }

        public List<string> Phrases
        {
            get
            {
                return phrases;
            }
        }

        public SpeechServerApp()
        {
            speechRecognizer = new SpeechServer();
        }

        public void Start()
        {
            speechFlags.Release();

            // Create port listener
            TcpListener listener = new TcpListener(1408);
            listener.Start();

            bool shutdown = false;
            while (!shutdown)
            {
                Socket soc = listener.AcceptSocket();
                try
                {
                    NetworkStream s = new NetworkStream(soc);
                    shutdown = ProcessRequest(s);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SpeechServer: Error communicating with client: " + ex.Message);
                }
                finally
                {
                    soc.Close();
                }
            }

            // Stop speech recognition
            SpeechRecognizer.EndSpeech();
            SpeechRecognizer.End();
        }

        public void SpeechDetected()
        {
            speechFlags.WaitOne();
            if (listenToSpeech)
            {
                phraseOut.WriteLine("speechDetected");
                Console.WriteLine("SpeechServer: speechDetected");
            }
            speechFlags.Release();
        }

        public void SpeechRecognized(string phrase)
        {
            speechFlags.WaitOne();
            if (listenToSpeech)
            {
                phraseOut.WriteLine("speechRecognized " + phrase);
                Console.WriteLine("SpeechServer: speechRecognized " + phrase);
            }
            speechFlags.Release();
        }

        public void SpeechRejected()
        {
            speechFlags.WaitOne();
            if (listenToSpeech)
            {
                phraseOut.WriteLine("speechRejected");
                Console.WriteLine("SpeechServer: speechRejected");
            }
            speechFlags.Release();
        }

        private void StartSpeechRecognizer()
        {
            InitGrammar();
            SpeechRecognizer.SubscribeSpeechDetected(delegate() { SpeechDetected(); });
            SpeechRecognizer.SubscribeSpeechRejected(delegate() { SpeechRejected(); });
            SpeechRecognizer.Start();
            SpeechRecognizer.StartSpeech();
        }

        private void StopSpeechRecognizer()
        {
            SpeechRecognizer.End();
        }

        private void InitGrammar()
        {
            SpeechRecognizer.SetGrammar(new Choices(phrases.ToArray()));
            foreach (string phrase in Phrases)
            {
                SpeechRecognizer.Subscribe(phrase, delegate(string phr) { SpeechRecognized(phr); });
            }
        }

        private bool ProcessRequest(NetworkStream s)
        {
            bool shutdown = false;
            StreamReader sr = new StreamReader(s);
            phraseOut = new StreamWriter(s);
            phraseOut.AutoFlush = true;
            while (true)
            {
                string cmd = sr.ReadLine();
                if (cmd == "shutdown")
                {
                    Console.WriteLine("SpeechServer: " + cmd);

                    shutdown = true;
                    break;
                }
                else if (cmd == "disconnect")
                {
                    Console.WriteLine("SpeechServer: " + cmd);

                    break;
                }
                else if (cmd.StartsWith("addPhrase"))
                {
                    Console.WriteLine("SpeechServer: " + cmd);

                    string phrase = cmd.Substring("addPhrase".Length + 1);
                    Phrases.Add(phrase);
                }
                else if (cmd.StartsWith("initSpeechRecognizer"))
                {
                    Console.WriteLine("SpeechServer: " + cmd);

                    StartSpeechRecognizer();
                }
                else if (cmd == "listenToSpeech")
                {
                    Console.WriteLine("SpeechServer: " + cmd);

                    speechFlags.WaitOne();
                    listenToSpeech = true;
                    speechFlags.Release();
                }
                else if (cmd == "stopListenToSpeech")
                {
                    Console.WriteLine("SpeechServer: " + cmd);

                    speechFlags.WaitOne();
                    listenToSpeech = false;
                    speechFlags.Release();
                }
                System.Threading.Thread.Sleep(0);
            }

            phraseOut = null;
            return shutdown;
        }

        private ISpeechServer speechRecognizer = null;
        private List<string> phrases = new List<string>();
        StreamWriter phraseOut = null;
        private Semaphore speechFlags = new Semaphore(0, 1);
        bool listenToSpeech = false;

        static void Main(string[] args)
        {
            SpeechServerApp app = new SpeechServerApp();
            app.Start();

            return;
        }
    }
}
