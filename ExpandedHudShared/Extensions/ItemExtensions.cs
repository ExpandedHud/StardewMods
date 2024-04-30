namespace ExpandedHudShared;

internal static class ItemExtensions
{
  public static bool IsSimilar(this Item item, Item? other)
  {
    return ItemUtils.AreItemsSimilar(item, other);
  }
}
