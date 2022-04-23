using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

using Dalamud.Logging;

namespace CIDNamer
{
	internal class CIDMapFile
	{
		internal static CIDMapFile LoadFile( string filePath )
		{
			if( !File.Exists( filePath ) )
			{
				PluginLog.LogWarning( $"Unable to open file {filePath}." );
				return null;
			}

			var fileLines = File.ReadAllLines( filePath );
			var mapFile = new CIDMapFile();
			foreach( var line in fileLines )
			{
				var entries = line.Split( '=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries );
				if( entries.Length == 2 && UInt64.TryParse( entries[0].Replace( "FFXIV_CHR", "" ), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out UInt64 CID ) )
				{
					if( !mapFile.CIDMap.TryAdd( CID, entries[1] ) )
					{
						PluginLog.LogWarning( $"Unable to add entry to dictionary." );
					}
				}
				else
				{
					PluginLog.LogWarning( $"Invalid entry ({entries.Length} parts) in file {filePath}." );
					return null;
				}
			}

			return mapFile;
		}

		internal void WriteFile( string filePath, bool useCHRPrefix )
		{
			List<string> lines = new();
			foreach( var entry in CIDMap )
			{
				lines.Add( $"{(useCHRPrefix ? "FFXIV_CHR" : "")}{entry.Key:X16} = {entry.Value}" );
			}

			File.WriteAllLines( filePath, lines );
		}

		internal readonly Dictionary<UInt64, string> CIDMap = new();
	}
}
