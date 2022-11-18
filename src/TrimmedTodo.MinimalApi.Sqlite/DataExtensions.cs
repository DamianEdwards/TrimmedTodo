using System.Runtime.CompilerServices;

namespace Microsoft.Data.Sqlite;

public static class DataExtensions
{
    public static async Task<int> ExecuteAsync(this SqliteConnection connection, string commandText, params (string Name, object? Value)[] parameters)
    {
        using var cmd = connection.CreateCommand(commandText, parameters);
        await connection.OpenAsync();
        return await cmd.ExecuteNonQueryAsync();
    }

    public static async Task<T?> QuerySingleAsync<T>(this SqliteConnection connection,
        string commandText,
        params (string Name, object? Value)[] parameters)
        where T : IDataReaderMapper<T>
    {
        var enumerable = connection.QueryAsync<T>(commandText, parameters);
        var enumerator = enumerable.GetAsyncEnumerator();

        return await enumerator.MoveNextAsync()
            ? enumerator.Current
            : default;
    }

    public static async IAsyncEnumerable<T?> QueryAsync<T>(this SqliteConnection connection,
        string commandText,
        params (string Name, object? Value)[] parameters)
        where T : IDataReaderMapper<T>
    {
        using var reader = await connection.QueryAsync(commandText, parameters);

        if (!reader.HasRows)
        {
            yield break;
        }

        while (await reader.ReadAsync())
        {
            yield return T.Map(reader);
        }
    }

    public static (string Name, object? Value) AsDbParameter(this string? value, [CallerArgumentExpression(nameof(value))] string name = null!) =>
        AsDbParameter((object?)value, name);

    public static (string Name, object? Value) AsDbParameter(this DateTime? value, [CallerArgumentExpression(nameof(value))] string name = null!) =>
        AsDbParameter((object?)value, name);

    public static (string Name, object? Value) AsDbParameter(this DateTimeOffset? value, [CallerArgumentExpression(nameof(value))] string name = null!) =>
        AsDbParameter((object?)value, name);

    public static (string Name, object? Value) AsDbParameter(this bool value, [CallerArgumentExpression(nameof(value))] string name = null!) =>
        AsDbParameter((object)value, name);

    public static (string Name, object? Value) AsDbParameter(this bool? value, [CallerArgumentExpression(nameof(value))] string name = null!) =>
        AsDbParameter((object?)value, name);

    public static (string Name, object? Value) AsDbParameter(this int value, [CallerArgumentExpression(nameof(value))] string name = null!) =>
        AsDbParameter((object)value, name);

    public static (string Name, object? Value) AsDbParameter(this int? value, [CallerArgumentExpression(nameof(value))] string name = null!) =>
        AsDbParameter((object?)value, name);

    public static (string Name, object? Value) AsDbParameter(this long value, [CallerArgumentExpression(nameof(value))] string name = null!) =>
        AsDbParameter((object)value, name);

    public static (string Name, object? Value) AsDbParameter(this long? value, [CallerArgumentExpression(nameof(value))] string name = null!) =>
        AsDbParameter((object?)value, name);

    public static (string Name, object? Value) AsDbParameter(this double value, [CallerArgumentExpression(nameof(value))] string name = null!) =>
        AsDbParameter((object)value, name);

    public static (string Name, object? Value) AsDbParameter(this double? value, [CallerArgumentExpression(nameof(value))] string name = null!) =>
        AsDbParameter((object?)value, name);

    private static (string Name, object? Value) AsDbParameter(this object? value, [CallerArgumentExpression(nameof(value))] string name = null!) =>
        (CleanParameterName(name), value ?? DBNull.Value);

    private static async Task<SqliteDataReader> QueryAsync(this SqliteConnection connection,
        string commandText,
        params (string Name, object? Value)[] parameters)
    {
        var cmd = connection.CreateCommand(commandText, parameters);
        await connection.OpenAsync();
        return await cmd.ExecuteReaderAsync();
    }

    private static SqliteCommand CreateCommand(this SqliteConnection connection, string commandText, params (string Name, object? Value)[] parameters)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = commandText;
        for (var i = 0; i < parameters.Length; i++)
        {
            var (name, value) = parameters[i];
            cmd.Parameters.AddWithValue(name, value);
        }
        return cmd;
    }

    private static string CleanParameterName(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var lastIndexOfPeriod = name.LastIndexOf('.');
        if (lastIndexOfPeriod > 0)
        {
            return name.Substring(lastIndexOfPeriod + 1);
        }
        return name;
    }
}

public interface IDataReaderMapper<T> where T : IDataReaderMapper<T>
{
    abstract static T Map(SqliteDataReader dataReader);
}
