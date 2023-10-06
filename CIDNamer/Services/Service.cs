using Dalamud.IoC;
using Dalamud.Plugin.Services;

namespace CIDNamer.Services;

internal class Service
{
	[PluginService] internal static IClientState ClientState { get; private set; } = null!;
	[PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
	[PluginService] internal static IPluginLog PluginLog { get; private set; } = null!;
}