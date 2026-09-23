namespace FinanceReport.Api.IntegrationTests;

internal static class TestData
{
    public static void Delete(string dataPath)
    {
        if (Directory.Exists(dataPath))
        {
            Directory.Delete(dataPath, recursive: true);
        }
    }
}
