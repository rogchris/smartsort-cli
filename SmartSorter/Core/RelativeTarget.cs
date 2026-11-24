namespace SmartSort.App.Core;

/// <summary>Validates portable relative output names, independent of the storage root.</summary>
public static class RelativeTarget
{
    public static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var parts = value.Replace('\\', '/').Split('/');
        foreach (var part in parts)
        {
            var stem = part.Split('.')[0];
            var reserved = stem.Equals("CON", StringComparison.OrdinalIgnoreCase)
                || stem.Equals("PRN", StringComparison.OrdinalIgnoreCase)
                || stem.Equals("AUX", StringComparison.OrdinalIgnoreCase)
                || stem.Equals("NUL", StringComparison.OrdinalIgnoreCase)
                || (stem.Length == 4 && (stem.StartsWith("COM", StringComparison.OrdinalIgnoreCase)
                    || stem.StartsWith("LPT", StringComparison.OrdinalIgnoreCase)) && stem[3] is >= '1' and <= '9');
            if (string.IsNullOrWhiteSpace(part) || part is "." or ".." || part != part.Trim()
                || part.EndsWith('.') || reserved || part.Any(char.IsControl)
                || part.IndexOfAny([':', '*', '?', '"', '<', '>', '|']) >= 0)
                throw new ArgumentException("Target must contain safe relative path segments.", nameof(value));
        }
        return string.Join('/', parts);
    }
}
