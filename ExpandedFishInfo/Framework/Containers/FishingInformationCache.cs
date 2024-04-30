using ExpandedHudShared.Extensions;
using ExpandedHudShared.MathUtil;
using StardewValley.GameData.Locations;

namespace ExpandedFishInfo.Framework.Containers;

public class FishingInformationCache
{
  /******************/
  /*     Caches     */
  /******************/

  private readonly RollingStatistic _fishChanceVariance = new();
  private readonly Dictionary<string, FishSpawnInfo> _fishInfo = new();
  private readonly HashSet<WaterTileCacheData> _waterTileData = new();
  private int _catchChanceActionsQueued;
  private int _lastCatchableFishCount;

  /******************************/
  /*     Enumerable Helpers     */
  /******************************/
  /// <summary> Water Tile Data. </summary>
  public IEnumerable<WaterTileCacheData> WaterTiles => _waterTileData.AsEnumerable();

  /// <summary> Fish Spawn Cache Data. </summary>
  public IEnumerable<FishSpawnInfo> FishInfo => _fishInfo.Values.AsEnumerable();

  /// <summary> Fish Data Where <see cref="FishSpawnInfo.CouldSpawn" />. </summary>
  public IEnumerable<FishSpawnInfo> CatchableFishUnordered => FishInfo.Where(item => item.CouldSpawn);

  /// <summary> Catchable Fish ordered by Precedence, then DisplayName. </summary>
  public IOrderedEnumerable<FishSpawnInfo> CatchableFishDisplayOrder
  {
    get { return CatchableFishUnordered.OrderBy(fish => fish.Precedence).ThenBy(fish => fish.DisplayName); }
  }

  /// <summary> Catchable fish ordered by precedence, then randomly. </summary>
  public IOrderedEnumerable<FishSpawnInfo> CatchableFishRandomOrder
  {
    get { return CatchableFishUnordered.OrderBy(fish => fish.Precedence).ThenBy(_ => Game1.random.Next()); }
  }

  /**************************************/
  /*     Internal Getters / Setters     */
  /**************************************/

  /// <summary> The number of explicit catch chance simulations to perform. </summary>
  public int CatchChanceActionsQueued
  {
    get => _catchChanceActionsQueued;
    set => _catchChanceActionsQueued = Math.Max(value, 0);
  }

  /// <summary> If any explicit catch simulations are queued. </summary>
  public bool HasCatchChanceActionsQueued => CatchChanceActionsQueued > 0;

  /// <summary> If the water cache has been initialized for this location. </summary>
  public bool WaterDataInitialized { get; private set; }

  /// <summary> The number of valid water tiles in this location. </summary>
  public int WaterTileCount => _waterTileData.Count;

  /// <summary> The number of catchable fish in this location. </summary>
  public int CatchableFishCount => CatchableFishUnordered.Count();

  /**************************/
  /*     Helper Methods     */
  /**************************/

  /// <summary> Set the last catchable count to the current catchable count. </summary>
  public void UpdateCatchableCount()
  {
    _lastCatchableFishCount = CatchableFishCount;
  }

  /// <summary> Initialize the water data for this location. </summary>
  public void InitializeWaterTileData()
  {
    WaterDataInitialized = true;
  }

  /// <summary>
  ///   Add water tile data to the cache
  /// </summary>
  /// <param name="waterTileCacheData">The new cache entry</param>
  public void AddWaterTile(WaterTileCacheData waterTileCacheData)
  {
    _waterTileData.Add(waterTileCacheData);
  }

  /// <summary>
  ///   Take a random sample of water tiles for simulation.
  /// </summary>
  /// <param name="size">The number of samples to take</param>
  /// <returns>Enumerable of water tile data</returns>
  public IEnumerable<WaterTileCacheData> GetRandomWaterDataSample(int size)
  {
    return WaterTiles.Shuffle().Take(size);
  }

  /// <summary>
  ///   Get cached FishSpawnInfo, or create and add to cache if it doesn't exist
  /// </summary>
  /// <param name="data">The data to search for</param>
  /// <returns>The FishSpawnInfo, created if necessary</returns>
  public FishSpawnInfo GetOrCreateFishInfo(SpawnFishData data)
  {
    FishSpawnInfo info;
    if (_fishInfo.TryGetValue(data.Id, out FishSpawnInfo? foundFishInfo))
    {
      info = foundFishInfo;
    }
    else
    {
      info = new FishSpawnInfoFromData(data);
      _fishInfo[data.Id] = info;
    }

    return info;
  }

  /// <summary>
  ///   Add a sample of fish catch chance variance data to the convergence tracker
  /// </summary>
  /// <param name="variance">The new variance data</param>
  public void AddVarianceData(double variance)
  {
    _fishChanceVariance.AddValue(variance);
  }

  /// <summary>
  ///   Checks if the actual catch chances for fish data have converged to a reasonable rate.
  /// </summary>
  /// <returns>If the fish probabilities have converged</returns>
  public bool FishingChancesConverged()
  {
    return _fishChanceVariance is { Count: > 1200, Variance: < 0.0002 };
  }

  /// <summary>
  ///   Checks if there is a different number of fish from before
  /// </summary>
  /// <returns>If the count is different</returns>
  public bool FishCountHasChanged()
  {
    return CatchableFishCount != _lastCatchableFishCount;
  }

  /// <summary>
  ///   Reset the variance data for this location, triggering a recalculation
  /// </summary>
  public void ResetVarianceAvg()
  {
    _fishChanceVariance.Reset();
  }
}
