using System;
using System.Numerics;
using System.IO;

using ImGuiNET;

namespace CIDNamer
{
	// It is good to have this be disposable in general, in case you ever need it
	// to do any cleanup
	public class PluginUI : IDisposable
	{
		//	Construction
		public PluginUI( Plugin plugin, Configuration configuration )
		{
			mPlugin = plugin;
			mConfiguration = configuration;
		}

		//	Destruction
		public void Dispose()
		{
		}

		public void Initialize()
		{
		}

		public void Draw()
		{
			//	Draw the sub-windows.
			DrawSettingsWindow();
			DrawDebugWindow();
		}

		protected void DrawSettingsWindow()
		{
			if( !SettingsWindowVisible )
			{
				mSettingsWindowPathString = mConfiguration.DataFilePath;
				return;
			}

			if( ImGui.Begin( "CIDNamer Settings",
				ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse ) )
			{
				//***** TODO: Input box to set file path.
				ImGui.Text( "Data file path:" );
				ImGui.InputText( "###Data file path input", ref mSettingsWindowPathString, 512 );
				ImGui.Checkbox( "Save content IDs with the \"FFXIV_CHR\" prefix.", ref mConfiguration.mWriteCHRPrefix );

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				if( ImGui.Button( "Save and Close" ) )
				{
					string newPath = Environment.ExpandEnvironmentVariables( mSettingsWindowPathString );
					var newFile = mPlugin.LastSeenMapFile ?? new CIDMapFile();
					newFile.WriteFile( newPath, mConfiguration.WriteCHRPrefix );

					//	If the new file couldn't be saved for some reason and didn't already throw, make sure that we throw before we save the plugin settings.
					if( !File.Exists( newPath ) )
					{
						throw new FileNotFoundException( $"The specified file {newPath} could not be accessed." );
					}

					mConfiguration.DataFilePath = newPath;
					mConfiguration.Save();
					SettingsWindowVisible = false;
				}
			}

			ImGui.End();
		}

		protected void DrawDebugWindow()
		{
			if( !DebugWindowVisible )
			{
				return;
			}

			//	Draw the window.
			ImGui.SetNextWindowSize( new Vector2( 300, 300 ) * ImGui.GetIO().FontGlobalScale, ImGuiCond.FirstUseEver );
			ImGui.SetNextWindowSizeConstraints( new Vector2( 300, 300 ) * ImGui.GetIO().FontGlobalScale, new Vector2( float.MaxValue, float.MaxValue ) );
			if( ImGui.Begin( "CIDNamer Debug Data", ref mDebugWindowVisible ) )
			{
				if( ImGui.Button( "Write Current Character to File" ) )
				{
					mPlugin.WriteCurrentCharacterData();
				}

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Spacing();

				ImGui.Text( "Known Mappings:" );
				ImGui.Indent();
				if( mPlugin.LastSeenMapFile != null )
				{
					foreach( var entry in mPlugin.LastSeenMapFile.CIDMap )
					{
						ImGui.Text( $"{entry.Key:X16} = {entry.Value}" );
					}
				}
				else
				{
					ImGui.Text( "No file yet processed." );
				}
				ImGui.Unindent();
			}

			//	We're done.
			ImGui.End();
		}

		protected readonly Plugin mPlugin;
		protected readonly Configuration mConfiguration;
		protected string mSettingsWindowPathString;

		//	Need a real backing field on the following properties for use with ImGui.
		protected bool mSettingsWindowVisible = false;
		public bool SettingsWindowVisible
		{
			get { return mSettingsWindowVisible; }
			set { mSettingsWindowVisible = value; }
		}

		protected bool mDebugWindowVisible = false;
		public bool DebugWindowVisible
		{
			get { return mDebugWindowVisible; }
			set { mDebugWindowVisible = value; }
		}
	}
}