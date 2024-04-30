using ExpandedHudShared.MathUtil;
using Microsoft.Xna.Framework;
using StardewValley.GameData.Locations;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Tools;

namespace ExpandedFishInfo.Framework.Containers;

// Rider lies to me when I debug a HashSet, so back it with a List in debug builds
#if DEBUG
using SpawnBlockersContainer = List<FishSpawnBlockedReason>;

#else
using SpawnBlockersContainer = HashSet<FishSpawnBlockedReason>;
#endif


/// <summary> Represents a reason that a fish is unable to spawn </summary>
public enum FishSpawnBlockedReason
{
  /// <summary> The fish data doesn't apply to the area the player is fishing. </summary>
  WrongFishingArea,

  /// <summary> The player is standing on the wrong tile to catch this fish. </summary>
  WrongPlayerPos,

  /// <summary> The fish data doesn't apply to the tile the player's bobber is on. </summary>
  WrongBobberPos,

  /// <summary> The game is not in a proper state to spawn this fish. </summary>
  WrongGameState,

  /// <summary> The player has caught too many of this type of fish. </summary>
  OverCatchLimit,

  /// <summary> The current season does not match the season required by this fish. </summary>
  WrongSeason,

  /// <summary> The current day does not match the day required by this fish. </summary>
  WrongDay,

  /// <summary> The current time does not match the time required by this fish. </summary>
  WrongTime,

  /// <summary> This fish requires rainy weather, but that condition is not met. </summary>
  RequiresRain,

  /// <summary> This fish requires sunny weather, but that condition is not met. </summary>
  RequiresSun,

  /// <summary> The player is not a high enough level to catch this fish. </summary>
  PlayerLevelTooLow,

  /// <summary> The player's fishing rod is too weak to catch this fish. </summary>
  PlayerRodTooWeak,

  /// <summary> This fish can only be caught in deeper water. </summary>
  WaterTooShallow,

  /// <summary> This fish can only be caught in shallower water. </summary>
  WaterTooDeep,

  /// <summary> This fish can only be caught with magic bait right now. </summary>
  RequiresMagicBait,

  /// <summary> The fish data provided is in an invalid format. </summary>
  InvalidFormat,

  /// <summary> This fish cannot be caught as your first catch. </summary>
  TutorialCatch,

  /// <summary> The lookup algorithm has already reached a stop point. </summary>
  ReachedGuaranteedItem,

  /// <summary> Unknown </summary>
  Unknown
}

/// <summary>
/// </summary>
public abstract class FishSpawnInfo
{
  /// <summary> A global cache of fish items, keyed by SpawnFishData or Qualified Item ID. </summary>
  protected static readonly Dictionary<string, Item> CommonFishLookupCache = new();

  private readonly RollingStatistic _actualHookChance = new();
  private readonly RollingStatistic _entryPickedChanceAverage = new();
  private readonly RollingStatistic _spawnProbabilityAverage = new();

  /// <summary> The items resolved for this FishSpawnInfo, hopefully sourced from CommonFishLookupCache. </summary>
  protected readonly Dictionary<string, Item> LookupItems = new();

  /// <summary> If the item cache needs to be reset next time we look up items. </summary>
  protected bool ResetItemCacheNextLookup;

  /// <summary>
  /// </summary>
  protected FishSpawnInfo()
  {
    ResetSpawnChecks();
    ResetProbabilities();
  }

  /// <summary> Collection of reasons the spawn was blocked </summary>
  public ICollection<FishSpawnBlockedReason> SpawnBlockReasons { get; } = new SpawnBlockersContainer();

  /// <summary>
  ///   Some unique identifier for Fish Data
  /// </summary>
  public abstract string Id { get; }

  /// <summary>
  ///   Fish data sorting order
  /// </summary>
  public abstract int Precedence { get; }

  /// <summary>
  ///   StardewValley internal SpawnFishData, if it exists
  /// </summary>
  public abstract SpawnFishData? SpawnData { get; }

  /// <summary>
  ///   If the fish is able to spawn given its conditions (e.g. is not blocked in any way)
  /// </summary>
  public virtual bool CouldSpawn => SpawnBlockReasons.Count == 0;

  /// <summary>
  ///   If the spawn conditions could not be determined
  /// </summary>
  public virtual bool IsSpawnConditionUnknown => SpawnBlockReasons.Contains(FishSpawnBlockedReason.Unknown);

  /// <summary>
  ///   If the only thing blocking a fish spawn is an unknown reason that wasn't cleared.
  /// </summary>
  public virtual bool IsSpawnConditionOnlyUnknown => SpawnBlockReasons.Count == 1 && IsSpawnConditionUnknown;

  /// <summary>
  ///   If the fish requires the player to stand on a certain tile
  /// </summary>
  public virtual bool HasPlayerTileRequirement => false;

  /// <summary>
  ///   If the fish requires the player to fish on a certain tile
  /// </summary>
  public virtual bool HasBobberTileRequirement => false;

  /// <summary>
  ///   Does this fish data only return non-fish items
  /// </summary>
  public bool IsOnlyNonFishItems { get; set; } = true;

  /// <summary>
  ///   The probability to spawn the fish, independent of if it is picked.
  /// </summary>
  public double SpawnProbability
  {
    get => Math.Min(1.0, _spawnProbabilityAverage.Mean);
    set => _spawnProbabilityAverage.AddValue(value);
  }

  /// <summary>
  ///   The probability to pick an entry and try to spawn it
  /// </summary>
  public double EntryPickedChance
  {
    get => Math.Min(1.0, _entryPickedChanceAverage.Mean);
    set => _entryPickedChanceAverage.AddValue(value);
  }

  /// <summary>
  ///   The actual hook chance given an entry is picked, and then actually spawns.
  ///   Takes into account random orderings of fish, meant to be set repeatedly to get a good averate.
  /// </summary>
  public double ActualHookChance
  {
    get => Math.Min(1.0, _actualHookChance.Mean);
    set => _actualHookChance.AddValue(value);
  }

  /// <summary>
  ///   The variance of the actual hook chance.
  /// </summary>
  public double ActualHookChanceVariance => _actualHookChance.Variance;

  /// <summary>
  ///   The chance for a fish to spawn, *given that it is picked*
  /// </summary>
  public double ChanceToSpawn
  {
    get
    {
      if (IsOnlyNonFishItems)
      {
        return EntryPickedChance;
      }

      return SpawnProbability * EntryPickedChance;
    }
  }

  /// <summary>
  ///   The probability of a fish to *not* spawn, if it is not picked, OR if it is picked but doesn't succeed its spawn check
  /// </summary>
  public double ChanceToNotSpawn
  {
    get
    {
      double notSpawnedGivenPicked = 1.0 - SpawnProbability;
      double probabilityEntryNotPicked = 1.0 - EntryPickedChance;

      return Math.Min(1.0, notSpawnedGivenPicked * EntryPickedChance + probabilityEntryNotPicked);
    }
  }

  /// <summary>
  ///   The Display Name of the data to show to the player
  /// </summary>
  public virtual string DisplayName { get; protected set; } = null!;

  /// <summary>
  ///   Clear the cache of items resolved from fish data, requiring lookup next time.
  /// </summary>
  public void ResetResolvedItems()
  {
    ResetItemCacheNextLookup = true;
  }

  /// <summary>
  ///   Reset spawn checks from blocked / ok to unknown.
  /// </summary>
  public void ResetSpawnChecks()
  {
    SpawnBlockReasons.Clear();
    SpawnBlockReasons.Add(FishSpawnBlockedReason.Unknown);
  }

  /// <summary>
  ///   Reset all chances to catch the fish
  /// </summary>
  public void ResetProbabilities()
  {
    _spawnProbabilityAverage.Reset();
    _entryPickedChanceAverage.Reset();
    _actualHookChance.Reset();
  }

  /// <summary>
  ///   Add a reason that a fish can't spawn
  /// </summary>
  /// <param name="reason">The reason</param>
  public void AddBlockedReason(FishSpawnBlockedReason reason)
  {
    // Again, the debug type for HashSet in Rider doesn't seem to work properly right now
    // so just remove it if it already exists.
#if DEBUG
    SpawnBlockReasons.Remove(reason);
#endif

    SpawnBlockReasons.Add(reason);
    // ModEntry.LogExDebug_2($"Adding Blocker {reason}");

    if (reason != FishSpawnBlockedReason.Unknown && SpawnBlockReasons.Remove(FishSpawnBlockedReason.Unknown))
    {
      // ModEntry.LogExDebug_2($"\tRemoved unknown state from list. Count: {SpawnBlockReasons.Count}");
    }
  }

  /// <summary>
  ///   Separate out the logic for creating items via resolver vs just a standard create.
  ///   ItemQueryResolver.TryResolve is a much more expensive method than just creating a single item, so try
  ///   to do that if we can.
  /// </summary>
  /// <param name="queryContext">The query context used to resolve the items</param>
  /// <param name="bobberTile">The tile the bobber is on</param>
  /// <param name="waterDepth">Tiles away from land</param>
  /// <param name="forceReloadCache">If we need to reload the cache (shouldn't happen but who knows)</param>
  public abstract void PopulateItemsForTile(
    ItemQueryContext queryContext,
    Vector2 bobberTile,
    int waterDepth,
    bool forceReloadCache = false
  );

  /// <summary>
  ///   Mark a fish as OK to spawn, clearing the blocked reasons
  /// </summary>
  public virtual void SetSpawnAllowed()
  {
    SpawnBlockReasons.Clear();
  }

  /// <summary>
  ///   All the items that can spawn from this fish data
  /// </summary>
  /// <returns>An emumerable of items</returns>
  public IEnumerable<Item> GetItems()
  {
    return LookupItems.Values;
  }

  /// <inheritdoc />
  public override string ToString()
  {
    return
      $"DisplayName: {DisplayName}, {nameof(EntryPickedChance)}: {EntryPickedChance}, {nameof(SpawnProbability)}: {SpawnProbability}, Spawnable: {CouldSpawn}, {nameof(SpawnBlockReasons)}: [{string.Join(", ", SpawnBlockReasons)}]";
  }

  /// <summary>
  ///   Check to make sure the fish data is valid to spawn, given some player tile and bobber tile.
  ///   If the conditions are not met, set FishSpawnBlockedReason.WrongBobberPos as a blocker.
  /// </summary>
  /// <param name="playerTile">The player tile. If null, uses the location of Game1.player</param>
  /// <param name="bobberTile">The tile of the bobber. If null, uses the location of the Game1.player FishingRod.bobber</param>
  public void CheckBobberAndPlayerPos(Point? playerTile = null, Vector2? bobberTile = null)
  {
    Point playerTilePoint = playerTile ?? Game1.player.TilePoint;

    if (bobberTile == null)
    {
      Item? curItem = Game1.player.CurrentItem;
      if (curItem is FishingRod { isFishing: true } rod)
      {
        bobberTile = rod.bobber.Value;
      }
    }

    Rectangle? requiredPlayerTile = SpawnData?.PlayerPosition;
    Rectangle? requiredBobberTile = SpawnData?.BobberPosition;
    if (requiredPlayerTile.HasValue &&
        !requiredPlayerTile.GetValueOrDefault().Contains(playerTilePoint.X, playerTilePoint.Y))
    {
      AddBlockedReason(FishSpawnBlockedReason.WrongPlayerPos);
    }

    if (requiredBobberTile.HasValue &&
        (!bobberTile.HasValue ||
         !requiredBobberTile.GetValueOrDefault().Contains((int)bobberTile.Value.X, (int)bobberTile.Value.Y)))
    {
      AddBlockedReason(FishSpawnBlockedReason.WrongBobberPos);
    }
  }
}

/// <summary>
///   Fish Spawn Info generated from an Item
/// </summary>
public class FishSpawnInfoFromItem : FishSpawnInfo
{
  private readonly Item _item;

  /// <summary>
  ///   Creates FishSpawnInfo based on an item instead of SpawnFishData
  /// </summary>
  /// <param name="item">The item to generate from</param>
  /// <param name="precedence">The precedence value to sort this data.</param>
  public FishSpawnInfoFromItem(Item item, int precedence)
  {
    _item = item;
    Precedence = precedence;
  }

  /// <inheritdoc />
  public override string Id => _item.QualifiedItemId;

  /// <inheritdoc />
  public override int Precedence { get; }

  /// <summary>
  ///   Item data does not have SpawnFishData
  /// </summary>
  public override SpawnFishData? SpawnData => null;

  /// <inheritdoc />
  public override void PopulateItemsForTile(
    ItemQueryContext queryContext,
    Vector2 bobberTile,
    int waterDepth,
    bool forceReloadCache = false
  )
  {
    if (LookupItems.Count != 0 && !forceReloadCache && !ResetItemCacheNextLookup)
    {
      return;
    }

    LookupItems.Clear();
    ResetItemCacheNextLookup = false;

    LookupItems.Add(Id, _item);
  }
}

/// <summary>
///   Fish Spawn Info generated from Game Data
/// </summary>
public class FishSpawnInfoFromData : FishSpawnInfo
{
  /// <summary>
  ///   Creates FishSpawnInfo from SpawnFishData
  /// </summary>
  /// <param name="spawnFishData">The raw fish data</param>
  public FishSpawnInfoFromData(SpawnFishData spawnFishData)
  {
    SpawnData = spawnFishData;
  }

  /// <inheritdoc />
  public override string Id => SpawnData.Id;

  /// <inheritdoc />
  public override int Precedence => SpawnData.Precedence;

  /// <inheritdoc />
  public override SpawnFishData SpawnData { get; }

  /// <inheritdoc />
  public override bool HasPlayerTileRequirement => SpawnData.PlayerPosition != null;

  /// <inheritdoc />
  public override bool HasBobberTileRequirement => SpawnData.BobberPosition != null;

  /// <inheritdoc />
  public override void PopulateItemsForTile(
    ItemQueryContext queryContext,
    Vector2 bobberTile,
    int waterDepth,
    bool forceReloadCache = false
  )
  {
    if (LookupItems.Count != 0 && !forceReloadCache && !ResetItemCacheNextLookup)
    {
      return;
    }

    LookupItems.Clear();
    ResetItemCacheNextLookup = false;

    var resolved = false;

    if (SpawnData.RandomItemId != null)
    {
      foreach (string itemId in SpawnData.RandomItemId)
      {
        // Try and use the cache to avoid a ton of CreateItem calls when iterating
        if (CommonFishLookupCache.TryGetValue(itemId, out Item? cachedItem))
        {
          LookupItems.TryAdd(itemId, cachedItem);
          continue;
        }

        ParsedItemData? itemData = ItemRegistry.GetData(itemId);
        Item? itemInstance = itemData?.ItemType.CreateItem(itemData);
        if (itemInstance == null)
        {
          continue;
        }

        // Add the item to the cache
        CommonFishLookupCache.TryAdd(itemId, itemInstance);
        LookupItems.TryAdd(itemId, itemInstance);
      }

      if (LookupItems.Count == 0)
      {
        ModEntry.Log($"SpawnData {Id} was supposed to output multiple items, but did nothing.", LogLevel.Error);
      }
      else
      {
        resolved = true;
      }
    }
    else if (ItemRegistry.IsQualifiedItemId(Id) && ItemRegistry.IsQualifiedItemId(SpawnData.ItemId))
    {
      // Try and use the cache to avoid a ton of CreateItem calls when iterating
      if (CommonFishLookupCache.TryGetValue(SpawnData.ItemId, out Item? cachedItem))
      {
        LookupItems.TryAdd(SpawnData.ItemId, cachedItem);
      }
      else
      {
        ParsedItemData? itemData = ItemRegistry.GetData(SpawnData.ItemId);
        if (itemData != null)
        {
          Item? itemInstance = itemData.ItemType.CreateItem(itemData);
          if (itemInstance == null)
          {
            ModEntry.Log(
              $"SpawnData {Id} is supposed to have a defined item, but might be an item query.",
              LogLevel.Error
            );
          }
          else
          {
            resolved = true;
            // Add to the cache
            CommonFishLookupCache.TryAdd(SpawnData.ItemId, itemInstance);
            LookupItems.TryAdd(SpawnData.ItemId, itemInstance);
          }
        }
      }
    }

    if (resolved)
    {
      DisplayName = string.Join(", ", GetItems().Select(item => item.DisplayName));
      return;
    }

    // If we really have to, fall back to the item resolver.
    // Try and avoid if we can, this is an expensive operation
    IList<ItemQueryResult>? items = ItemQueryResolver.TryResolve(SpawnData, queryContext, formatItemId: FormatItemId);
    foreach (ItemQueryResult itemQueryResult in items)
    {
      if (itemQueryResult.Item is not Item item)
      {
        continue;
      }

      CommonFishLookupCache.TryAdd(item.QualifiedItemId, item);
      LookupItems.TryAdd(item.QualifiedItemId, item);
    }

    DisplayName = string.Join(", ", GetItems().Select(item => item.DisplayName));
    return;


    string FormatItemId(string query)
    {
      return query.Replace("BOBBER_X", ((int)bobberTile.X).ToString())
                  .Replace("BOBBER_Y", ((int)bobberTile.Y).ToString())
                  .Replace("WATER_DEPTH", waterDepth.ToString());
    }
  }
}
