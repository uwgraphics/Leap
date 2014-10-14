using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;

/// <summary>
/// Class representing a behavior client, which fetches
/// agent behaviors from a task server, and dispatches them
/// to the behavior schedules.
/// </summary>
public class BehaviorClient : MonoBehaviour
{
	/// <summary>
	/// Task server name.
	/// </summary>
	public string server = "localhost";
	
	/// <summary>
	/// Task server port.
	/// </summary>
	public int port = 7777;
	
	/// <summary>
	/// Virtual agent.
	/// </summary>
	public GameObject[] agents = new GameObject[0];
	
	/// <summary>
	/// If true, the client will automatically request
	/// new behaviors.
	/// </summary>
	public bool autoReq = true;
	
	private TcpClient client;
	private NetworkStream stream;
	private float time = 0;
	
	public bool GetNextBehavior( GameObject agent )
	{
		return GetNextBehavior( agent, "" );
	}
	
	public bool GetNextBehavior( GameObject agent, string userAction )
	{
		if( agent == null )
		{
			Debug.LogError( "Unable to request behavior for null agent." );
			return false;
		}
		
		// Request new behavior
		string msg;
		if( userAction == "" )
			msg = string.Format( "<requestBehaviors agent=\"{0}\" task=\"{1}\"/>",
			                    agent.name, Application.loadedLevelName );
		else
			msg = string.Format( "<requestBehaviors agent=\"{0}\" task=\"{1}\" userAction=\"{2}\"/>",
			                    agent.name, Application.loadedLevelName, userAction );
		if( !SendRequest(msg) )
		{
			Debug.LogError( "Behavior request failed for agent " + agent.name );
			return false;
		}
		
		// Get new behavior
		string resp;
		if( !GetResponse( out resp ) )
		{
			Debug.LogError( "Failed to get behavior request response for agent " + agent.name );
			return false;
		}
		
		// Has there been an error?
		string err;
		if( IsError( resp, out err ) )
		{
			Debug.LogError( "Task server says behavior request was invalid: " + err );
			return false;
		}
		
		// Schedule new behavior
		BehaviorScheduler bhvs = agent.GetComponent<BehaviorScheduler>();
		Behavior bhv = bhvs.CreateBehavior();
		if( !bhv.Parse(resp) )
		{
			Debug.LogWarning( "Behavior received for agent " + agent.name + " is invalid." );
		}
		
		return true;
	}

	private void Awake()
	{
		// Connect to task server
		try
		{
			client = new TcpClient( server, port );
			stream = client.GetStream();
		}
		catch( Exception )
		{
			Debug.LogError( string.Format( "Error connecting to task server {0}:{1}", server, port ) );
			enabled = false;
			client = null;
			
			return;
		}
		
		// Begin task
		if( !SendRequest( string.Format( "<beginTask task=\"{0}\"/>",
		                                Application.loadedLevelName ) ) )
		{
			Debug.LogError( "Unable to begin current task." );
		}
	}
	
	private void Update()
	{
		if(!autoReq)
			return;
		
		time += Time.deltaTime;
		
		foreach( GameObject agent in agents )
		{
			BehaviorScheduler bhvs = agent.GetComponent<BehaviorScheduler>();
			if( bhvs.Schedule.Count <= 0 )
				// Need to schedule more behaviors
				GetNextBehavior(agent);
		}
	}
	
	private void OnDestroy()
	{
		if( client == null )
			return;
		
		stream.Close();
		client.Close();
	}
	
	private bool IsError( string msg, out string errMsg )
	{
		XmlTextReader reader = (XmlTextReader)XmlTextReader.Create( new StringReader(msg) );
		reader.Settings.IgnoreComments = true;
		reader.Settings.IgnoreProcessingInstructions = true;
		reader.Settings.IgnoreWhitespace = true;
		reader.WhitespaceHandling = WhitespaceHandling.None;
		reader.Read();
		if( reader.NodeType == XmlNodeType.Element &&
		   reader.Name == "error" )
		{
			errMsg = reader.GetAttribute("message");
			
			reader.Close();
			return true;
		}
		
		errMsg = "";
		reader.Close();
		
		return false;
	}
	
	private bool SendRequest( string msg )
	{
		Byte[] data = Encoding.ASCII.GetBytes(msg);
		try
		{
			stream.Write( data, 0, data.Length );
		}
		catch( Exception )
		{
			return false;
		}
		
		return true;
	}
	
	private bool GetResponse( out string resp )
	{
		Byte[] data = new Byte[4096];
		int data_len = 0;
		try
		{
			data_len = stream.Read( data, 0, data.Length );
		}
		catch( Exception )
		{
			resp = "";
			return false;
		}
		resp = Encoding.ASCII.GetString( data, 0, data_len );
		
		return true;
	}	
}
