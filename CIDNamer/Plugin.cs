using System;
using System.IO;
using System.Threading.Tasks;

using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Logging;
using Dalamud.Plugin;

namespace CIDNamer
{
	public sealed class Plugin : IDalamudPlugin
	{
		//	Initialization
		public Plugin(
			DalamudPluginInterface pluginInterface,
			ClientState clientState,
			CommandManager commandManager )
		{
			//	API Access
			mPluginInterface	= pluginInterface;
			mClientState		= clientState;
			mCommandManager		= commandManager;

			//	Configuration
			mConfiguration = mPluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
			mConfiguration.Initialize( mPluginInterface );

			//	Localization and Command Initialization
			mCommandManager.AddHandler( mTextCommandName, new CommandInfo( ProcessTextCommand )
			{
				HelpMessage = "Text command.  Supported options are \"config\" and \"debug\"."
			} );

			//	UI Initialization
			mUI = new PluginUI( this, mConfiguration );
			mPluginInterface.UiBuilder.Draw += DrawUI;
			mPluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
			mUI.Initialize();


			//	Event Subscription
			mClientState.Login += OnLogin;
		}

		//	Cleanup
		public void Dispose()
		{
			mClientState.Login -= OnLogin;
			mPluginInterface.UiBuilder.Draw -= DrawUI;
			mPluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
			mCommandManager.RemoveHandler( mTextCommandName );

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

		private void OnLogin( object sender, EventArgs e )
		{
			WriteCurrentCharacterData( 5000 );
		}

		internal void WriteCurrentCharacterData( int delay_mSec )
		{
			Task.Run( async () =>
			{
				await Task.Delay( delay_mSec );

				var fullPath =  Environment.ExpandEnvironmentVariables( mConfiguration.DataFilePath );
				if( mClientState.LocalPlayer != null )
				{
					CIDMapFile mapFile;
					if( !File.Exists( fullPath ) )
					{
						PluginLog.LogInformation( $"Specified file \"{fullPath}\" doesn't exist; creating new file." );
						mapFile = new();
					}
					else
					{
						mapFile = CIDMapFile.LoadFile( fullPath );
						if( mapFile == null )
						{
							PluginLog.LogError( $"Unable to open file \"{fullPath}\".  Aborting attempts to save this character's data, since this could delete data from the specified file that already exists." );
							return;
						}
					}

					string playerServerString = $"{mClientState.LocalPlayer.Name} ({mClientState.LocalPlayer.HomeWorld.GameData.Name})";
					if( mapFile.CIDMap.ContainsKey( mClientState.LocalContentId ) )
					{
						mapFile.CIDMap[mClientState.LocalContentId] = playerServerString;
					}
					else
					{
						mapFile.CIDMap.TryAdd( mClientState.LocalContentId, playerServerString );
					}

					LastSeenMapFile = mapFile;
					mapFile.WriteFile( fullPath, mConfiguration.WriteCHRPrefix );
				}
				else
				{
					PluginLog.LogError( "LocalPlayer was null!  Unable to save this character's data to the specified file." );
				}
			} );
		}

		public string Name => "CIDNamer";
		private const string mTextCommandName = "/cidnamer";

		internal CIDMapFile LastSeenMapFile { get; private set; }

		private readonly PluginUI mUI;
		private readonly Configuration mConfiguration;

		private readonly DalamudPluginInterface mPluginInterface;
		private readonly ClientState mClientState;
		private readonly CommandManager mCommandManager;
	}
}
