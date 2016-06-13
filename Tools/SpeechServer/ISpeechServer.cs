using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Text;
using System.Threading.Tasks;

namespace SpeechServer
{
    public interface ISpeechServer
    {
        /// <summary>
        /// Start server but the speech recognition is not active
        /// </summary>
        void Start();
        /// <summary>
        /// End speech
        /// </summary>
        void EndSpeech();
        /// <summary>
        /// Resume speech
        /// </summary>
        void StartSpeech();
        /// <summary>
        /// Prepare server for sound detection
        /// </summary>
        void InitSound();
        /// <summary>
        /// Start sound detection, the event "interrupt" will fire when sound is detected
        /// </summary>
        void StartSound();
        /// <summary>
        /// Stop sound recognition
        /// </summary>
        void EndSound();
        /// <summary>
        /// Close server
        /// </summary>
        void End();
        /// <summary>
        /// Restart sound sensors
        /// </summary>
        void Restart();
        /// <summary>
        /// Set the grammar with dictionary; must do before start detection
        /// </summary>
        /// <param name="dictionary">dictionary to react</param>
        void SetGrammar(Choices dictionary);
        /// <summary>
        /// Set grammar through XML file; must do before start detection
        /// </summary>
        /// <param name="pathToXMLFile">path to XML file</param>
        void SetGrammar(string pathToXMLFile);
        /// <summary>
        /// Subscribe to a keyword.
        /// keywords are defined in the dictionary or path
        /// multiple action can be subscribe
        /// </summary>
        /// <param name="word">Keyword</param>
        /// <param name="handler">action to be done, can be a lambda example: ()=>function(var1,var2)</param>
        void Subscribe(string word, Action<string> handler);
        /// <summary>
        /// Unsubscribe all the events on the word
        /// can be change to only unsubscribe one action only
        /// </summary>
        /// <param name="word">keyword to unsubscribe</param>
        /// <param name="handler"></param>
        void Unsubscribe(string word, Action<string> handler);
        /// <summary>
        /// Delete all the subscribers
        /// </summary>
        void clearSubscribers();
        /// <summary>
        /// Subscribe to a speech detected event.
        /// This will be followed either by a speech recognized or
        /// speech rejected event.
        /// </summary>
        /// <param name="handler"></param>
        void SubscribeSpeechDetected(Action handler);
        /// <summary>
        /// Subscribe to a speech rejected event.
        /// </summary>
        /// <param name="handler"></param>
        void SubscribeSpeechRejected(Action handler);
        /// <summary>
        /// Unsubscribe from a speech detected event.
        /// </summary>
        /// <param name="handler"></param>
        void UnsubscribeSpeechDetected(Action handler);
        /// <summary>
        /// Unsubscribe from a speech rejected event.
        /// </summary>
        /// <param name="handler"></param>
        void UnsubscribeSpeechRejected(Action handler);
    }
}
