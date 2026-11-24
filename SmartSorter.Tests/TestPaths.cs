namespace SmartSort.Tests;

internal static class TestPaths
{
    public static string Get(string relativePath) =>
        Path.GetFullPath(Path.Combine(Path.GetTempPath(), "smartsort-tests", relativePath.Replace('\\', Path.DirectorySeparatorChar)));
}
