namespace be.Tests.Architecture;
internal static class RepositoryRoot
{
    public static string Path { get; } = Find();

    private static string Find()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            if (File.Exists(System.IO.Path.Combine(directory.FullName, "be", "be.csproj")))
                return directory.FullName;
        throw new InvalidOperationException("Architecture checks require the repository checkout.");
    }
}
