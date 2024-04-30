using Microsoft.Xna.Framework;

namespace ExpandedFishInfo.Framework.Containers;

public record WaterTileCacheData(int X, int Y, int WaterDepth)
{
  public Vector2 BobberTile => new(X, Y);
}
