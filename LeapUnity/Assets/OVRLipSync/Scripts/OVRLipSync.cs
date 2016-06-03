/************************************************************************************
Filename    :   OVRLipSync.cs
Content     :   Interface to Oculus Lip-Sync engine
Created     :   August 4th, 2015
Copyright   :   Copyright 2015 Oculus VR, Inc. All Rights reserved.

Licensed under the Oculus VR Rift SDK License Version 3.1 (the "License"); 
you may not use the Oculus VR Rift SDK except in compliance with the License, 
which is provided at the time of installation or download, or which 
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

http://www.oculusvr.com/licenses/LICENSE-3.1 

Unless required by applicable law or agreed to in writing, the Oculus VR SDK 
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
************************************************************************************/
using UnityEngine;
using System;
using System.Runtime.InteropServices;

//-------------------------------------------------------------------------------------
// ***** OVRPhoneme
//
/// <summary>
/// OVRLipSync interfaces into the Oculus lip-sync engine. This component should be added
/// into the scene once. 
///
/// </summary>
public class OVRLipSync : MonoBehaviour 
{
    public const int ovrLipSyncSuccess = 0;

    // Error codes that may return from Lip-Sync engine
    public enum ovrLipSyncError 
    {
        Unknown = 				-2200,	//< An unknown error has occurred
        CannotCreateContext = 	-2201, 	//< Unable to create a context
        InvalidParam = 			-2202,	//< An invalid parameter, e.g. NULL pointer or out of range
        BadSampleRate = 		-2203,	//< An unsupported sample rate was declared
        MissingDLL = 			-2204,	//< The DLL or shared library could not be found
        BadVersion = 			-2205,	//< Mismatched versions between header and libs
        UndefinedFunction = 	-2206	//< An undefined function 
    };

    // Various visemes
    public enum ovrLipSyncViseme 
    {
        sil,				
        PP,
        FF,
        TH,
        DD,
        kk,
        CH,
        SS,
        nn,
        RR,
        aa,
        E,
        ih,
        oh,
        ou,
        Count
    };

    /// Flags
    public enum ovrLipSyncFlag
    {
        None = 0x0000,
        DelayCompensateAudio = 0x0001
        
    };
    
    // Enum for sending lip-sync engine specific signals
    public enum ovrLipSyncSignals 
    {
        VisemeOn, 
        VisemeOff,
        VisemeAmount,
        VisemeSmoothing,
        Count
    };

    // Enum for provider context to create
    public enum ovrLipSyncContextProvider
    {
        Main,
        Other
    };

    /// NOTE: Opaque typedef for lip-sync context is an unsigned int (uint)

    /// Current phoneme frame results
    public struct ovrLipSyncFrame
    {
        public ovrLipSyncFrame(int init)
        {
            frameNumber = 0;
            frameDelay  = 0;
            Visemes = new float[(int)ovrLipSyncViseme.Count];
        }

        public void CopyInput(ref ovrLipSyncFrame input)
        {
            frameNumber = input.frameNumber;
            frameDelay  = input.frameDelay;
            input.Visemes.CopyTo(Visemes, 0);
        }

        public int   	frameNumber; 	// count from start of recognition
        public int   	frameDelay;  	// in ms
        public float[] 	Visemes;		// Array of floats for viseme frame. Size of Viseme Count, above
    };
    
    // * * * * * * * * * * * * *
    // Import functions
    public const string strOVRLS = "OVRLipSync";
    [DllImport(strOVRLS)]
    private static extern int ovrLipSyncDll_Initialize(int samplerate, int buffersize);
    [DllImport(strOVRLS)]
    private static extern void ovrLipSyncDll_Shutdown();
    [DllImport(strOVRLS)]
    private static extern IntPtr ovrLipSyncDll_GetVersion(ref int Major, 
                                                          ref int Minor,
                                                          ref int Patch);
    [DllImport(strOVRLS)]
    private static extern int ovrLipSyncDll_CreateContext(ref uint context, 
                                                           ovrLipSyncContextProvider provider);
    [DllImport(strOVRLS)]
    private static extern int ovrLipSyncDll_DestroyContext(uint context);


    [DllImport(strOVRLS)]
    private static extern int ovrLipSyncDll_ResetContext(uint context);
    [DllImport(strOVRLS)]
    private static extern int ovrLipSyncDll_SendSignal(uint context,
                                                       ovrLipSyncSignals signal,
                                                       int arg1, int arg2);
    [DllImport(strOVRLS)]
    private static extern int ovrLipSyncDll_ProcessFrame(uint context,
                                                         float [] audioBuffer, ovrLipSyncFlag flags,
                                                         ref int frameNumber, ref int frameDelay, 
                                                         float [] visemes, int visemeCount);
    [DllImport(strOVRLS)]
    private static extern int ovrLipSyncDll_ProcessFrameInterleaved(uint context,
                                                         float [] audioBuffer, ovrLipSyncFlag flags,
                                                         ref int frameNumber, ref int frameDelay, 
                                                         float [] visemes, int visemeCount);

    // * * * * * * * * * * * * *
    // Public members
    
    // * * * * * * * * * * * * *
    // Static members
    private static int sOVRLipSyncInit = (int)ovrLipSyncError.Unknown;

    // interface through this static member.
    public static OVRLipSync sInstance = null;


    // * * * * * * * * * * * * *
    // MonoBehaviour overrides

    /// <summary>
    /// Awake this instance.
    /// </summary>
    void Awake () 
    {	
        // We can only have one instance of OVRLipSync in a scene (use this for local property query)
        if(sInstance == null)
        {
            sInstance = this;
        }
        else
        {
            Debug.LogWarning (System.String.Format ("OVRLipSync Awake: Only one instance of OVRPLipSync can exist in the scene."));
            return;
        }

        int samplerate;
        int bufsize;
        int numbuf;

        // Get the current sample rate
        samplerate = AudioSettings.outputSampleRate;
        // Get the current buffer size and number of buffers
        AudioSettings.GetDSPBufferSize (out bufsize, out numbuf);

        String str = System.String.Format 
        ("OvrLipSync Awake: Queried SampleRate: {0:F0} BufferSize: {1:F0}", samplerate, bufsize);
        Debug.LogWarning (str);

        sOVRLipSyncInit = ovrLipSyncDll_Initialize(samplerate, bufsize);

        if(sOVRLipSyncInit != ovrLipSyncSuccess)
        {
            Debug.LogWarning (System.String.Format
            ("OvrLipSync Awake: Failed to init Speech Rec library"));
        }

        // Important: Use the touchpad mechanism for input, call Create on the OVRTouchpad helper class
        OVRTouchpad.Create();

    }
   
    /// <summary>
    /// Start this instance.
    /// Note: make sure to always have a Start function for classes that have editor scripts.
    /// </summary>
    void Start()
    {
    }
    
    /// <summary>
    /// Run processes that need to be updated in our game thread
    /// </summary>
    void Update()
    {
    }

    /// <summary>
    /// Raises the destroy event.
    /// </summary>
    void OnDestroy()
    {
        if(sInstance != this)
        {
            Debug.LogWarning ( 
            "OVRLipSync OnDestroy: This is not the correct OVRLipSync instance.");
            return;
        }

        // Do not shut down at this time
//		ovrLipSyncDll_Shutdown();
//		sOVRLipSyncInit = (int)ovrLipSyncError.Unknown;
    }


    // * * * * * * * * * * * * *
    // Public Functions
    
    /// <summary>
    /// Determines if is initialized.
    /// </summary>
    /// <returns><c>true</c> if is initialized; otherwise, <c>false</c>.</returns>
    public static int IsInitialized()
    {
        return sOVRLipSyncInit;
    }

    /// <summary>
    /// Creates a lip-sync context.
    /// </summary>
    /// <returns>error code</returns>
    /// <param name="context">Context.</param>
    /// <param name="provider">Provider.</param>
    public static int CreateContext(ref uint context, ovrLipSyncContextProvider provider)
    {
        if(IsInitialized() != ovrLipSyncSuccess)
            return (int)ovrLipSyncError.CannotCreateContext;

        return ovrLipSyncDll_CreateContext(ref context, provider);
    }

    /// <summary>
    /// Destroy a lip-sync context.
    /// </summary>
    /// <returns>The context.</returns>
    /// <param name="context">Context.</param>
    public static int DestroyContext (uint context)
    {
        if(IsInitialized() != ovrLipSyncSuccess)
            return (int)ovrLipSyncError.Unknown;
        
        return ovrLipSyncDll_DestroyContext(context);
    }

    /// <summary>
    /// Resets the context.
    /// </summary>
    /// <returns>error code</returns>
    /// <param name="context">Context.</param>
    public static int ResetContext(uint context)
    {
        if(IsInitialized() != ovrLipSyncSuccess)
            return (int)ovrLipSyncError.Unknown;

        return ovrLipSyncDll_ResetContext(context);
    }

    /// <summary>
    /// Sends a signal to the lip-sync engine.
    /// </summary>
    /// <returns>error code</returns>
    /// <param name="context">Context.</param>
    /// <param name="signal">Signal.</param>
    /// <param name="arg1">Arg1.</param>
    /// <param name="arg2">Arg2.</param>
    public static int SendSignal(uint context, ovrLipSyncSignals signal, int arg1, int arg2)
    {
        if(IsInitialized() != ovrLipSyncSuccess)
            return (int)ovrLipSyncError.Unknown;

        return ovrLipSyncDll_SendSignal(context, signal, arg1, arg2);
    }

    /// <summary>
    /// Processes the frame.
    /// </summary>
    /// <returns>error code</returns>
    /// <param name="context">Context.</param>
    /// <param name="monoBuffer">Mono buffer.</param>
    /// <param name="delayCompensate">If set to <c>true</c> delay compensate.</param>
    /// <param name="frame">Frame.</param>
    public static int ProcessFrame(uint context, float [] audioBuffer, ovrLipSyncFlag flags, ref ovrLipSyncFrame frame)
    {
        if(IsInitialized() != ovrLipSyncSuccess)
            return (int)ovrLipSyncError.Unknown;

        // We need to pass the array of Visemes directly into the C call (no pointers of structs allowed, sadly)
        return ovrLipSyncDll_ProcessFrame(context, audioBuffer, flags, 
                                          ref frame.frameNumber, ref frame.frameDelay,
                                          frame.Visemes, frame.Visemes.Length);
    }

    /// <summary>
    /// Processes the frame interleaved.
    /// </summary>
    /// <returns>The frame interleaved.</returns>
    /// <param name="context">Context.</param>
    /// <param name="audioBuffer">Audio buffer.</param>
    /// <param name="delayCompensate">If set to <c>true</c> delay compensate.</param>
    /// <param name="frame">Frame.</param>
    public static int ProcessFrameInterleaved(uint context, float [] audioBuffer, ovrLipSyncFlag flags, ref ovrLipSyncFrame frame)
    {
        if(IsInitialized() != ovrLipSyncSuccess)
            return (int)ovrLipSyncError.Unknown;
        
        // We need to pass the array of Visemes directly into the C call (no pointers of structs allowed, sadly)
        return ovrLipSyncDll_ProcessFrameInterleaved(context, audioBuffer, flags, 
                                          ref frame.frameNumber, ref frame.frameDelay,
                                          frame.Visemes, frame.Visemes.Length);
    }
}
