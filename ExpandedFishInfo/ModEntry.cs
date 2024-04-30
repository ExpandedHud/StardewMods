using HarmonyLib;

namespace ExpandedFishInfo;

/// <summary> Entry Class for the mod. </summary>
[UsedImplicitly]
public class ModEntry : Mod
{
  private static ModEntry _instance = null!;

  private Harmony _harmony = null!;

  /// <inheritdoc cref="IMonitor.Log" />
  public static void Log(string message, LogLevel level = LogLevel.Trace)
  {
    _instance.Monitor.Log(message, level);
  }

  /// <summary>
  ///   Mod Entry
  /// </summary>
  /// <param name="helper">Mod Helper</param>
  public override void Entry(IModHelper helper)
  {
    _instance = this;
    _harmony = new Harmony(ModManifest.UniqueID);
  }
}
