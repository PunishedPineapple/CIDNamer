using System;

using Dalamud.Configuration;
using Dalamud.Plugin;

namespace CIDNamer
{
	[Serializable]
	public class Configuration : IPluginConfiguration
	{
		public Configuration()
		{
		}

		//  Our own configuration options and data.

		//	Need a real backing field on the properties for use with ImGui.
		public bool mWriteCHRPrefix = true;
		public bool WriteCHRPrefix
		{
			get { return mWriteCHRPrefix; }
			set { mWriteCHRPrefix = value; }
		}

		public string mDataFilePath = "%appdata%\\PunishedPineapple\\CharacterAliases.cfg";
		public string DataFilePath
		{
			get { return mDataFilePath; }
			set { mDataFilePath = value; }
		}

		//  Plugin framework and related convenience functions below.
		public void Initialize( DalamudPluginInterface pluginInterface )
		{
			mPluginInterface = pluginInterface;
		}

		public void Save()
		{
			mPluginInterface.SavePluginConfig( this );
		}

		[NonSerialized]
		protected DalamudPluginInterface mPluginInterface;

		public int Version { get; set; } = 0;
	}
}
