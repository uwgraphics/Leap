using UnityEngine;
using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

/// <summary>
/// Script for receiving messages from clients (e.g., Gaze Tracker, Webcam, Kinect Speech Recognition)
/// </summary>
public class AsynchronousSocketListener : MonoBehaviour {
    // Thread signal.
    public static ManualResetEvent allDone = new ManualResetEvent(false);
	public int ipAddressIndex = 1;
	public int portNumber = 4242;
	
	private Socket listener;
	private Thread acceptConnectionsThread;
	private bool isListening = true;
	
    public void StartListening() {
        // Data buffer for incoming data.
        byte[] bytes = new Byte[1024];

        // Establish the local endpoint for the socket.
        // The DNS name of the computer
        // running the listener is "host.contoso.com".
		IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
		IPAddress[] ipAddresses = ipHostInfo.AddressList;
        IPAddress ipAddress = ipAddresses[ipAddressIndex];
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, portNumber);

        // Create a TCP/IP socket.
        listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );

        // Bind the socket to the local endpoint and listen for incoming connections.
        try {
            listener.Bind(localEndPoint);
            listener.Listen(100);
			
            while (isListening) {
                // Set the event to nonsignaled state.
                allDone.Reset();

                // Start an asynchronous socket to listen for connections.
				string s = string.Format ("Waiting for a connection on IP {0}, port {1}...",ipAddress.ToString(), portNumber);
                UnityEngine.Debug.Log(s);
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener );

                // Wait until a connection is made before continuing.
                allDone.WaitOne();
            }

        } catch (Exception e) {
            UnityEngine.Debug.Log(e.ToString());
        }
    }

    public void AcceptCallback(IAsyncResult ar) {
		if (!isListening)
			return;
        // Signal the main thread to continue.
        allDone.Set();

        // Get the socket that handles the client request.
        Socket listener = (Socket) ar.AsyncState;
        Socket handler = listener.EndAccept(ar);

        // Create the state object.
        StateObject state = new StateObject();
        state.workSocket = handler;
        handler.BeginReceive( state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
    }

    public void ReadCallback(IAsyncResult ar) {
		if (!isListening)
			return;
		
        String content = String.Empty;
        
        // Retrieve the state object and the handler socket
        // from the asynchronous state object.
        StateObject state = (StateObject) ar.AsyncState;
        Socket handler = state.workSocket;

        // Read data from the client socket. 
        int bytesRead = handler.EndReceive(ar);

        if (bytesRead > 0) {
            // There  might be more data, so store the data received so far.
            state.sb.Append(Encoding.ASCII.GetString(state.buffer,0,bytesRead));

            // Check for end-of-file tag. If it is not there, read 
            // more data.
            content = state.sb.ToString();
			//UnityEngine.Debug.Log (content);
			//Send (handler, content);
            if (content.IndexOf('\n') > -1) {
                // All the data has been read from the 
                // client. Display it on the console.
				//Replace the following two lines with code that parses the message and calls the Move functions
				string s = string.Format("Read {0} bytes from socket. \n Data : {1}", content.Length, content);
                UnityEngine.Debug.Log(s);
                // Echo the data back to the client.
                //Send(handler, content);
				state.sb = new StringBuilder();
            }
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
        }
    }
    
    private void Send(Socket handler, String data) {
        // Convert the string data to byte data using ASCII encoding.
        byte[] byteData = Encoding.ASCII.GetBytes(data);

        // Begin sending the data to the remote device.
        handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
    }

    private void SendCallback(IAsyncResult ar) {
        try {
            // Retrieve the socket from the state object.
            Socket handler = (Socket) ar.AsyncState;

            // Complete sending the data to the remote device.
            int bytesSent = handler.EndSend(ar);
            Console.WriteLine("Sent {0} bytes to client.", bytesSent);

            handler.Shutdown(SocketShutdown.Both);
            handler.Close();

        } catch (Exception e) {
            UnityEngine.Debug.Log(e.ToString());
        }
    }

    public void Start() {
		acceptConnectionsThread = new Thread(new ThreadStart(StartListening));
		acceptConnectionsThread.Start();
    }
	
	public void OnApplicationQuit() {
		isListening = false;
		listener.Close ();
		acceptConnectionsThread.Abort();
	}
}

// State object for reading client data asynchronously
public class StateObject {
    // Client  socket.
    public Socket workSocket = null;
    // Size of receive buffer.
    public const int BufferSize = 1024;
    // Receive buffer.
    public byte[] buffer = new byte[BufferSize];
// Received data string.
    public StringBuilder sb = new StringBuilder();  
}