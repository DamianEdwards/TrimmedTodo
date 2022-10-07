namespace Benchmarks;

class PathHelper
{
    public static string RepoRoot { get; } = GetRepoRoot();
    public static string ProjectsDir { get; } = Path.Combine(RepoRoot, "src");
    public static string ArtifactsDir { get; } = Path.Combine(RepoRoot, ".artifacts");
    public static string BenchmarkArtifactsDir { get; } = Path.Combine(ArtifactsDir, "Benchmarks", "published_apps");

    public static string GetProjectPublishDir(string projectName, string? runId)
    {
        return Path.Combine(BenchmarkArtifactsDir, projectName, runId ?? "");
    }

    private static string GetRepoRoot()
    {
        var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        DirectoryInfo? repoDir = null;

        while (true)
        {
            if (currentDir is null)
            {
                // We hit the file system root
                break;
            }

            if (File.Exists(Path.Join(currentDir.FullName, "TrimmedTodo.sln")))
            {
                // We're in the repo root
                repoDir = currentDir;
                break;
            }

            currentDir = currentDir.Parent;
        }

        return repoDir is null ? throw new InvalidOperationException("Couldn't find repo directory") : repoDir.FullName;
    }
}
