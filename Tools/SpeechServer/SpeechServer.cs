using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Speech.AudioFormat;
using System.Speech.Recognition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpeechServer
{
    public class SpeechServer : ISpeechServer
    {

        public SpeechServer(Choices dic) : this()
        {
            dictionary = dic;
        }

        public SpeechServer()
        {
            status = true;
            speechRecgonizedThread = new List<Thread>();
        }

        public SpeechServer(String XML)
            : this()
        {
            this.pathToXML = XML;
        }

        #region Variables

        protected Stream audioStream;
        public bool status;
        protected Choices dictionary;
        protected string pathToXML;
        public SpeechRecognitionEngine speechEngine;
        protected Thread audioThread;
        protected bool audioDetect;
        protected bool soundDetect;
        protected Dictionary<string, List<Object>> _Subscribers = new Dictionary<string, List<Object>>();
        List<Object> handlersSpeechDetected = new List<object>();
        List<Object> handlersSpeechRejected = new List<object>();

        #endregion

        #region Speech recognition setup

        /// <summary>
        /// Gets the metadata for the speech recognizer (acoustic model).
        /// </summary>
        /// <returns>
        /// RecognizerInfo if found, <code>null</code> otherwise.
        /// </returns>
        protected static RecognizerInfo GetRecognizer()
        {
            foreach (RecognizerInfo recognizer in SpeechRecognitionEngine.InstalledRecognizers())
            {
                if ("en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                    return recognizer;
            }

            return null;
        }

        /// <summary>
        /// Initialize speech recognizer.
        /// </summary>
        protected void Init()
        {
            RecognizerInfo ri = GetRecognizer();

            if (ri != null)
            {
                //Populate the speech engine with keywords we are interested in.
                this.speechEngine = new SpeechRecognitionEngine(ri.Id);

                var gb = new GrammarBuilder { Culture = ri.Culture };

                if (pathToXML != null)
                {
                    gb.AppendRuleReference(pathToXML);
                }
                else if (dictionary != null)
                    gb.Append(dictionary);
                else
                    throw new NullReferenceException();

                var g = new Grammar(gb);

                speechEngine.LoadGrammar(g);

                // For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model. 
                // This will prevent recognition accuracy from degrading over time.
                speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);
                speechEngine.SetInputToDefaultAudioDevice();
                status = true;
            }
            else
            {
                throw new Exception("Unable to start speech recognition, no suitable recognized found!");
            }
        }

        /// <summary>
        /// Individual Handler for recognized speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        protected virtual void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            //Defensive code in case when threads stack up
            if (speechEngine == null)
                return;

            speechEngine.SpeechRecognized -= SpeechRecognizedThread;
            speechEngine.SpeechRecognitionRejected -= SpeechRejected;
            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            const double ConfidenceThreshold = 0.75f;

            if (e.Result.Confidence >= ConfidenceThreshold)
            {
                Console.Out.Write(string.Format("Speech recognized: {0}, {1}", e.Result.Confidence, e.Result.Text));
                //var word = e.Result.Semantics.Value.ToString();
                var word = e.Result.Text;
                if (_Subscribers.ContainsKey(word))
                {
                    var handlers = _Subscribers[word];
                    foreach (Action<string> handler in handlers)
                    {
                        handler.Invoke(word);
                    }
                }
            }
            else
            {
                Console.Out.Write(string.Format("Speech recognized, but below confidence threshold: {0}, {1}", e.Result.Confidence, e.Result.Text));
            }
            if (speechEngine != null)
            {
                speechEngine.SpeechDetected += SpeechDetected;
                speechEngine.SpeechRecognized += SpeechRecognizedThread;
                speechEngine.SpeechRecognitionRejected += SpeechRejected;
            }
        }

        List<Thread> speechRecgonizedThread;
        protected virtual void SpeechRecognizedThread(object sender, SpeechRecognizedEventArgs e)
        {
            Thread thread = new Thread(() => SpeechRecognized(sender, e));
            thread.Start();
            speechRecgonizedThread.Add(thread);
            updateThread();
        }

        protected void updateThread()
        {
            //To remove previous finished tread, prevent the threads from stacking up
            for (int i = speechRecgonizedThread.Count - 1; i >= 0; i--)
            {
                if (!speechRecgonizedThread[i].IsAlive)
                {
                    speechRecgonizedThread.Remove(speechRecgonizedThread[i]);
                }
            }
        }

        protected virtual void SpeechDetected(object sender, SpeechDetectedEventArgs e)
        {
            Console.WriteLine("Speech detected");

            foreach (Action handler in handlersSpeechDetected)
                handler.Invoke();
        }


        /// <summary>
        /// Handler for rejected speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        protected void SpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            Console.WriteLine("Speech rejected");

            foreach (Action handler in handlersSpeechRejected)
                handler.Invoke();
        }

        protected void Shutdown()
        {
            if (null != this.speechEngine)
            {
                this.speechEngine.SpeechRecognized -= SpeechRecognized;
                this.speechEngine.SpeechRecognitionRejected -= SpeechRejected;
                this.speechEngine.RecognizeAsyncStop();
                this.speechEngine = null;
            }
        }
        #endregion

        #region Implementations

        public void Start()
        {
            Init();
            status = true;
        }

        public virtual void EndSpeech()
        {
            speechEngine.SpeechDetected -= SpeechDetected;
            speechEngine.SpeechRecognized -= SpeechRecognizedThread;
            speechEngine.SpeechRecognitionRejected -= SpeechRejected;
            speechEngine.RecognizeAsyncCancel();
        }

        public virtual void StartSpeech()
        {
            speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            speechEngine.SpeechDetected += SpeechDetected;
            speechEngine.SpeechRecognized += SpeechRecognizedThread;
            speechEngine.SpeechRecognitionRejected += SpeechRejected;
        }

        public void End()
        {
            Shutdown();
            _Subscribers = new Dictionary<string,List<object>>();
            status = false;
        }

        public void SetGrammar(Choices dictionary)
        {
            Shutdown();
            this.dictionary = dictionary;
            this.Init();
        }

        public void SetGrammar(String pathtoXMLFile)
        {
            Shutdown();
            this.pathToXML = pathtoXMLFile;
            this.Init();

        }

        public void clearSubscribers()
        {
            _Subscribers = new Dictionary<string, List<object>>();
        }

        public void Subscribe(string words, Action<string> handler)
        {
            if (_Subscribers.ContainsKey(words))
            {
                var handlers = _Subscribers[words];
                if (!handlers.Contains(handler))
                    handlers.Add(handler);
                else
                {
                    Console.WriteLine("Already have this function");
                }
            }
            else
            {
                var handlers = new List<Object>();
                handlers.Add(handler);
                _Subscribers[words] = handlers;
            }
        }

        public void Unsubscribe(string words, Action<string> handler)
        {
            if (_Subscribers.ContainsKey(words))
            {
                _Subscribers.Remove(words);
            }
        }

        public void SubscribeSpeechDetected(Action handler)
        {
            if (!handlersSpeechDetected.Contains(handler))
                handlersSpeechDetected.Add(handler);
        }

        public void SubscribeSpeechRejected(Action handler)
        {
            if (!handlersSpeechRejected.Contains(handler))
                handlersSpeechRejected.Add(handler);
        }

        public void UnsubscribeSpeechDetected(Action handler)
        {
            if (handlersSpeechDetected.Contains(handler))
                handlersSpeechDetected.Remove(handler);
        }

        public void UnsubscribeSpeechRejected(Action handler)
        {
            if (handlersSpeechRejected.Contains(handler))
                handlersSpeechRejected.Remove(handler);
        }

        public void Restart()
        {
        }


        #endregion

        #region Audio Implementation

        public void InitSound()
        {
            audioDetect = false;
            soundDetect = true;
            audioThread = new Thread(SoundInitialize);
            audioThread.Start();
        }

        protected void SoundInitialize()
        {
         /// <summary>
            /// Number of milliseconds between each read of audio data from the stream.
            /// </summary>
            const int AudioPollingInterval = 50;

            /// <summary>
            /// Number of samples captured from the audio stream each millisecond.
            /// </summary>
            const int SamplesPerMillisecond = 16;

            /// <summary>
            /// Number of bytes in each audio stream sample.
            /// </summary>
            const int BytesPerSample = 2;

            /// <summary>
            /// Number of audio samples represented by each column of pixels in wave bitmap.
            /// </summary>
            const int SamplesPerColumn = 40;

            /// <summary>
            /// Buffer used to hold audio data read from audio stream.
            /// </summary>
            byte[] audioBuffer = new byte[AudioPollingInterval * SamplesPerMillisecond * BytesPerSample];

            // Bottom portion of computed energy signal that will be discarded as noise.
            // Only portion of signal above noise floor will be displayed.
            const double EnergyNoiseFloor = 0.2;

            /// <summary>
            /// Sum of squares of audio samples being accumulated to compute the next energy value.
            /// </summary>
            double accumulatedSquareSum = 0;

            /// <summary>
            /// Number of audio samples accumulated so far to compute the next energy value.
            /// </summary>
            int accumulatedSampleCount = 0;

            double refValue = 0.1;

            bool startAudioSpike = false;
            double threshold = 0.25;

            while (soundDetect)
            {
                int readCount = audioStream.Read(audioBuffer, 0, audioBuffer.Length);

                // Calculate energy corresponding to captured audio in the dispatcher
                // (UI Thread) context, so that rendering code doesn't need to
                // perform additional synchronization.
               /* Dispatcher.BeginInvoke(
                new Action(
                    () =>
                    {*/
                        for (int i = 0; i < readCount; i += 2)
                        {
                            // compute the sum of squares of audio samples that will get accumulated
                            // into a single energy value.
                            short audioSample = BitConverter.ToInt16(audioBuffer, 0);
                            accumulatedSquareSum += audioSample * audioSample;
                            ++accumulatedSampleCount;

                            if (accumulatedSampleCount < SamplesPerColumn)
                            {
                                continue;
                            }

                            // Each energy value will represent the logarithm of the mean of the
                            // sum of squares of a group of audio samples.
                            double meanSquare = accumulatedSquareSum / SamplesPerColumn;
                            double rms = Math.Sqrt(meanSquare);
                            double decibels = 20.0 * Math.Log10(rms / refValue);
                            double amplitude = Math.Log(meanSquare) / Math.Log(int.MaxValue);

                            // Renormalize signal above noise floor to [0,1] range.
                            double energy = Math.Max(0, amplitude - EnergyNoiseFloor) / (1 - EnergyNoiseFloor);
                            if (energy > threshold && !startAudioSpike)
                            {
                                if (_Subscribers.ContainsKey("Interrupt") && audioDetect)
                                {
                                    var handlers = _Subscribers["Interrupt"];
                                    foreach (Action<string> handler in handlers)
                                    {
                                        handler.Invoke("Interrupt");
                                    }
                                }
                                startAudioSpike = true;
                                
                            }
                            else if (energy <= threshold)
                            {
                                startAudioSpike = false;
                            }
                            //this.energyIndex = (this.energyIndex + 1) % this.energy.Length;
                            accumulatedSquareSum = 0;
                            accumulatedSampleCount = 0;
                         }
                   // }));
            }
        }

        public void StartSound()
        {
            audioDetect = true;
        }

        public void EndSound()
        {
            soundDetect = false;
            audioThread.Join();
        }

        #endregion
    }
}
