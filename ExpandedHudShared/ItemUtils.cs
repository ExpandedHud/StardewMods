using StardewValley.Enchantments;

namespace ExpandedHudShared;

/// <summary>
///   Utilities to make it easier to interact with items
/// </summary>
public static class ItemUtils
{
  /// <summary>
  ///   Compare two items, along with attachments and enchantments
  /// </summary>
  /// <param name="a">First Item (Nullable)</param>
  /// <param name="b">Second Item (Nullable)</param>
  /// <returns>True if items are equal, False otherwise</returns>
  public static bool AreItemsSimilar(Item? a, Item? b)
  {
    if (a == null && b == null)
    {
      return true;
    }

    if (a == null || b == null)
    {
      return false;
    }

    if (a.GetType() != b.GetType())
    {
      return false;
    }

    if (a.canStackWith(b))
    {
      return true;
    }

    if (a is Tool toolA && b is Tool toolB)
    {
      return AreToolsSimilar(toolA, toolB);
    }

    return a.QualifiedItemId == b.QualifiedItemId && a.Name.Equals(b.Name);
  }

  /// <summary>
  ///   Compare two enchantments for equality
  /// </summary>
  /// <param name="a">First enchantment</param>
  /// <param name="b">Second enchantment</param>
  /// <returns>True if enchantments are equal, False otherwise</returns>
  public static bool AreEnchantmentsSimilar(BaseEnchantment a, BaseEnchantment b)
  {
    return a.ShouldBeDisplayed() == b.ShouldBeDisplayed() &&
           a.IsForge() == b.IsForge() &&
           a.IsSecondaryEnchantment() == b.IsSecondaryEnchantment() &&
           a.Level == b.Level &&
           a.GetName() == b.GetName();
  }

  /// <summary>
  ///   Compare two tools for equality, with enchantments and attachments
  /// </summary>
  /// <param name="a">First tool</param>
  /// <param name="b">Second tool</param>
  /// <returns>True if tools are equal, False otherwise</returns>
  public static bool AreToolsSimilar(Tool a, Tool b)
  {
    if (a.QualifiedItemId != b.QualifiedItemId ||
        !a.Name.Equals(b.Name) ||
        a.AttachmentSlotsCount != b.AttachmentSlotsCount)
    {
      return false;
    }

    // Unnecessarily expensive, but order needs to not matter here
    if (a.attachments.Any(attachmentA => !b.attachments.Any(attachmentB => AreItemsSimilar(attachmentA, attachmentB))))
    {
      return false;
    }

    if (a.enchantments.Count != b.enchantments.Count)
    {
      return false;
    }

    return a.enchantments.All(
      enchantmentsA => b.enchantments.Any(enchantmentsB => AreEnchantmentsSimilar(enchantmentsA, enchantmentsB))
    );
  }
}
