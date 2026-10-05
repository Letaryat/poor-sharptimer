namespace SharpTimer
{
    public partial class SharpTimer
    {
        // An <img> whose src is not a URL renders as a broken-image badge (a grey "?").
        // RankHUDIcon is empty for an unranked player, so only emit the tag for a URL.
        private static bool IsImageUrl(string? src)
        {
            if (string.IsNullOrWhiteSpace(src)) return false;
            var s = src.Trim();
            return s.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        // The unranked placement is already "[Unranked]" (UnrankedTitle), which the HUD
        // wrapped again as "[[Unranked]]". Only add brackets when they are missing.
        private static string BracketPlacement(string? placement)
        {
            if (string.IsNullOrWhiteSpace(placement)) return "";
            var p = placement.Trim();
            return p.StartsWith("[") && p.EndsWith("]") ? p : $"[{p}]";
        }
    }
}
