using Godot;
using System;
using System.Collections.Generic;
using CargoSpace.Server;
using CargoSpace.Client;
using CargoSpace.Core;
using CargoSpace.Shared;

public partial class Main : Node
{
	private ClientManager _clientManager;
	private ServerManager _serverManager;
	private NetworkBridge _networkBridge;

	public override void _Ready()
	{
		// Load tile data registry first - this is data required by both server and client
		TileRegistry.LoadFromFile("res://Data/tiles.json");
		
		string[] args = OS.GetCmdlineArgs();
		
		// Create NetworkBridge first - this ensures consistent NodePath for RPCs
		_networkBridge = new NetworkBridge();
		_networkBridge.Name = "NetworkBridge";
		AddChild(_networkBridge);
		GameLogger.Debug("NetworkBridge created and added to scene tree");
		
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
		_serverManager = new ServerManager(_networkBridge);
		AddChild(_serverManager);
		_networkBridge.SetServerManager(_serverManager);
		GameLogger.Debug("Server mode - ServerManager created and linked to NetworkBridge");
	}

	private void StartClient()
	{
		_clientManager = new ClientManager(_networkBridge);
		AddChild(_clientManager);
		_networkBridge.SetClientManager(_clientManager);
		GameLogger.Debug("Client mode - ClientManager created and linked to NetworkBridge");
	}
}
