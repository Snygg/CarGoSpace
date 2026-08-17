using Godot;
using System;
using System.Collections.Generic;

public partial class Main : Node
{
	public override void _Ready()
	{
		string[] args = OS.GetCmdlineArgs();
		
		// "dedicated_server" is a built-in feature tag when exporting a headless server
		if (OS.HasFeature("dedicated_server")|| ((ICollection<string>)args).Contains("--server"))
		{
			GD.Print("Booting Headless Linux Server...");
			StartServer();
		}
		else
		{
			GD.Print("Booting Windows Client...");
			StartClient();
		}
	}

	private void StartServer()
	{
		// Load your server-side grid logic, open ports, wait for connections
	}

	private void StartClient()
	{
		// Load the Main Menu UI, connect to IP
	}
}
