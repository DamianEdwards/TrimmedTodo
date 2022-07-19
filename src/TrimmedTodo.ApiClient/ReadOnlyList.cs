namespace TrimmedTodo.ApiClient;

internal static class ReadOnly
{
    public static IReadOnlyList<T> EmptyList<T>() => Empty<T>.List;

    public static IReadOnlyCollection<T> EmptyCollection<T>() => Empty<T>.List;

    private static class Empty<T>
    {
        public static IReadOnlyList<T> List { get; } = new List<T>(0);
    }
}
