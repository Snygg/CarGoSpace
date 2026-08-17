using Godot;
using System;
using System.Collections.Generic;
using CargoSpace.Server;
using CargoSpace.Client;

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
		ServerManager serverManager = new ServerManager();
		AddChild(serverManager);
	}

	private void StartClient()
	{
		ClientManager clientManager = new ClientManager();
		AddChild(clientManager);
	}
}
