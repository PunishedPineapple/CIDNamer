using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using CIDNamer.Services;

using Dalamud.Game.Command;
using Dalamud.Plugin;

namespace CIDNamer;

public sealed class Plugin : IDalamudPlugin
{
	//	Initialization
	public Plugin( DalamudPluginInterface pluginInterface )
	{
		//	API Access
		pluginInterface.Create<Service>();
		mPluginInterface = pluginInterface;

		//	Configuration
		mConfiguration = mPluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
		mConfiguration.Initialize( mPluginInterface );

		//	Localization and Command Initialization
		Service.CommandManager.AddHandler( mTextCommandName, new CommandInfo( ProcessTextCommand )
		{
			HelpMessage = "Text command.  Supported options are \"config\" and \"debug\"."
		} );

		//	UI Initialization
		mUI = new PluginUI( this, mConfiguration );
		mPluginInterface.UiBuilder.Draw += DrawUI;
		mPluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
		mUI.Initialize();


		//	Event Subscription
		Service.ClientState.Login += OnLogin;
	}

	//	Cleanup
	public void Dispose()
	{
		Service.ClientState.Login -= OnLogin;
		mPluginInterface.UiBuilder.Draw -= DrawUI;
		mPluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
		Service.CommandManager.RemoveHandler( mTextCommandName );

		mUI.Dispose();
	}

	//	Text Commands
	private void ProcessTextCommand( string command, string args )
	{
		//*****TODO: Don't split, just substring off of the first space so that other stuff is preserved verbatim.
		//	Seperate into sub-command and paramters.
		string subCommand = "";
		string subCommandArgs = "";
		string[] argsArray = args.Split( ' ' );
		if( argsArray.Length > 0 )
		{
			subCommand = argsArray[0];
		}
		if( argsArray.Length > 1 )
		{
			//	Recombine because there might be spaces in JSON or something, that would make splitting it bad.
			for( int i = 1; i < argsArray.Length; ++i )
			{
				subCommandArgs += argsArray[i] + ' ';
			}
			subCommandArgs = subCommandArgs.Trim();
		}

		//	Process the commands.
		if( subCommand.Length == 0 )
		{
			//	For now just have no subcommands act like the config subcommand
			mUI.SettingsWindowVisible = !mUI.SettingsWindowVisible;
		}
		else if( subCommand.ToLower() == "config" )
		{
			mUI.SettingsWindowVisible = !mUI.SettingsWindowVisible;
		}
		else if( subCommand.ToLower() == "debug" )
		{
			mUI.DebugWindowVisible = !mUI.DebugWindowVisible;
		}
	}

	private void DrawUI()
	{
		mUI.Draw();
	}

	private void DrawConfigUI()
	{
		mUI.SettingsWindowVisible = true;
	}

	private void OnLogin()
	{
		WriteCurrentCharacterData( 15000 );
	}

	internal void WriteCurrentCharacterData( int maxDelay_mSec = 500 )
	{
		Task.Run( () =>
		{
			var startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			while( Service.ClientState.LocalPlayer == null )
			{
				if( DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > startTime + maxDelay_mSec )
				{
					Service.PluginLog.Error( "LocalPlayer never became valid within the allotted time!  Unable to save this character's data to the specified file." );
					return;
				}
				else
				{
					Thread.Sleep( 1000 );
				}
			}

			var fullPath =  Environment.ExpandEnvironmentVariables( mConfiguration.DataFilePath );
			CIDMapFile mapFile;
			if( !File.Exists( fullPath ) )
			{
				Service.PluginLog.Information( $"Specified file \"{fullPath}\" doesn't exist; creating new file." );
				mapFile = new();
			}
			else
			{
				mapFile = CIDMapFile.LoadFile( fullPath );
				if( mapFile == null )
				{
					Service.PluginLog.Error( $"Unable to open file \"{fullPath}\".  Aborting attempts to save this character's data, since this could delete data from the specified file that already exists." );
					return;
				}
			}

			string playerServerString = $"{Service.ClientState.LocalPlayer.Name} ({Service.ClientState.LocalPlayer.HomeWorld.GameData.Name})";
			if( mapFile.CIDMap.ContainsKey( Service.ClientState.LocalContentId ) )
			{
				mapFile.CIDMap[Service.ClientState.LocalContentId] = playerServerString;
			}
			else
			{
				mapFile.CIDMap.TryAdd( Service.ClientState.LocalContentId, playerServerString );
			}

			LastSeenMapFile = mapFile;
			mapFile.WriteFile( fullPath, mConfiguration.WriteCHRPrefix );
		} );
	}

	public string Name => "CIDNamer";
	private const string mTextCommandName = "/cidnamer";

	internal CIDMapFile LastSeenMapFile { get; private set; }

	private readonly PluginUI mUI;
	private readonly Configuration mConfiguration;

	private readonly DalamudPluginInterface mPluginInterface;
}
