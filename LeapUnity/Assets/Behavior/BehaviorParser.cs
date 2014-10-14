using System;
using System.IO;
using System.Text;
using System.Xml;

/// <summary>
/// Class for parsing XML messages containing behavior specification.
/// </summary>
public class BehaviorParser
{
	/// <summary>
	/// Constructor. 
	/// </summary>
	public BehaviorParser()
	{
	}
	
	/// <summary>
	/// Reads the behavior data from an XML message.
	/// </summary>
	/// <param name="bhv">
	/// Behavior object for storing the parsed data. <see cref="Behavior"/>
	/// </param>
	/// <param name="bhvXml">
	/// XML message content.
	/// </param>
	/// <returns>
	/// true if XML successfully parsed, false otherwise.
	/// </returns>
	public bool Parse( Behavior bhv, string bhvXml )
	{
		if( bhv == null )
		{
			throw new ArgumentNullException( "bhv",
			                                "Unable to parse behavior XML: behavior object not specified." );
		}
		
		XmlTextReader reader = (XmlTextReader)XmlTextReader.Create( new StringReader(bhvXml) );
		reader.Settings.IgnoreComments = true;
		reader.Settings.IgnoreProcessingInstructions = true;
		reader.Settings.IgnoreWhitespace = true;
		reader.WhitespaceHandling = WhitespaceHandling.None;
		reader.Read();
		if( reader.NodeType == XmlNodeType.Element )
		{
			if( reader.Name == "behaviors" )
			{
				if( !ParseBehaviors( bhv, reader ) )
				{
					reader.Close();
					return false;
				}
			}
			else
			{
				reader.Close();
				return false;
			}
		}
		else
		{
			reader.Close();
			return false;
		}
		
		reader.Close();
		return true;
	}
	
	protected bool ParseBehaviors( Behavior bhv, XmlReader reader )
	{
		while( reader.Read() )
		{
			if( reader.NodeType == XmlNodeType.Element )
			{
				if( reader.Name == "channel" )
				{
					if( !ParseChannel( bhv, reader ) )
						return false;
				}
				else
				{
					return false;
				}
			}
			else if( reader.NodeType == XmlNodeType.EndElement &&
			        reader.Name == "behaviors" )
			{
				return true;
			}
			else
			{
				return false;
			}
		}
		
		return true;
	}
	
	protected bool ParseChannel( Behavior bhv, XmlReader reader )
	{
		string type = reader.GetAttribute("type");
		switch(type)
		{
		
		case "gaze":
			
			if( !ParseGazeChannel( bhv, reader ) )
				return false;
			break;
			
		case "speech":
			
			if( !ParseSpeechChannel( bhv, reader ) )
				return false;
			break;
		}
		
		return true;
	}
	
	protected bool ParseGazeChannel( Behavior bhv, XmlReader reader )
	{
		while( reader.Read() )
		{
			if( reader.NodeType == XmlNodeType.Element )
			{
				if( reader.Name == "action" )
				{
					if( !ParseGazeAction( bhv, reader ) )
						return false;
				}
				else
				{
					return false;
				}
			}
			else if( reader.NodeType == XmlNodeType.EndElement &&
			        reader.Name == "channel" )
			{
				return true;
			}
			else
			{
				return false;
			}
		}
		
		return true;
	}
	
	protected bool ParseGazeAction( Behavior bhv, XmlReader reader )
	{
		string attrv;
		float st, et;
		string target;
		float head_align = 1f;
		
		st = (float)double.Parse( reader.GetAttribute("startTime") );
		et = (float)double.Parse( reader.GetAttribute("endTime") );
		target = reader.GetAttribute("target");
		attrv = reader.GetAttribute("headAlign");
		if( attrv != null && attrv != "" )
			head_align = (float)double.Parse(attrv);
		bhv.AddAction( new GazeAction( st/1000f, et/1000f, target, head_align ) );
		
		return true;
	}
	
	protected bool ParseSpeechChannel( Behavior bhv, XmlReader reader )
	{
		reader.Read();
		if( reader.NodeType != XmlNodeType.Text )
			return false;
		string speech = reader.Value;
		bhv.AddAction( new SpeechAction(speech) );
		
		reader.Read();
		if( reader.NodeType != XmlNodeType.EndElement ||
		   reader.Name != "channel" )
		{
			return false;
		}
		
		return true;
	}
}
