using System.Data;
using System.Runtime.CompilerServices;

namespace Npgsql;

public static class DataExtensions
{
    public static async ValueTask OpenIfClosedAsync(this NpgsqlConnection connection)
    {
        if (connection.State == ConnectionState.Closed)
        {
            await connection.OpenAsync();
        }
    }

    public static async Task<int> ExecuteAsync(this NpgsqlConnection connection, string commandText)
    {
        using var cmd = connection.CreateCommand(commandText);

        await connection.OpenIfClosedAsync();
        return await cmd.ExecuteNonQueryAsync();
    }

    public static async Task<int> ExecuteAsync(this NpgsqlConnection connection, string commandText, params (string Name, object? Value)[] parameters)
    {
        using var cmd = connection.CreateCommand(commandText, parameters);

        await connection.OpenIfClosedAsync();
        return await cmd.ExecuteNonQueryAsync();
    }

    public static async Task<int> ExecuteAsync(this NpgsqlConnection connection, string commandText, params NpgsqlParameter[] parameters)
    {
        using var cmd = connection.CreateCommand(commandText, parameters);

        await connection.OpenIfClosedAsync();
        return await cmd.ExecuteNonQueryAsync();
    }

    public static async Task<int> ExecuteAsync(this NpgsqlConnection connection, string commandText, Action<NpgsqlParameterCollection> configureParameters)
    {
        using var cmd = connection.CreateCommand(commandText, configureParameters);

        await connection.OpenIfClosedAsync();
        return await cmd.ExecuteNonQueryAsync();
    }

    public static async Task<T?> QuerySingleAsync<T>(this NpgsqlConnection connection, string commandText, params (string Name, object? Value)[] parameters)
        where T : IDataReaderMapper<T>
    {
        await connection.OpenIfClosedAsync();
        using var reader = await connection.QuerySingleAsync(commandText, parameters);

        return await reader.MapSingleAsync<T>();
    }

    public static async Task<T?> QuerySingleAsync<T>(this NpgsqlConnection connection, string commandText, params NpgsqlParameter[] parameters)
        where T : IDataReaderMapper<T>
    {
        await connection.OpenIfClosedAsync();
        using var reader = await connection.QuerySingleAsync(commandText, parameters);

        return await reader.MapSingleAsync<T>();
    }

    public static async Task<T?> QuerySingleAsync<T>(this NpgsqlConnection connection, string commandText, Action<NpgsqlParameterCollection>? configureParameters = null)
        where T : IDataReaderMapper<T>
    {
        using var cmd = connection.CreateCommand(commandText, configureParameters);

        using var reader = await connection.QuerySingleAsync(cmd);

        return await reader.MapSingleAsync<T>();
    }

    public static async IAsyncEnumerable<T> QueryAsync<T>(this NpgsqlConnection connection, string commandText, params NpgsqlParameter[] parameters)
        where T : IDataReaderMapper<T>
    {
        var query = connection.QueryAsync<T>(commandText, parameterCollection => parameterCollection.AddRange(parameters));

        await foreach (var item in query)
        {
            yield return item;
        }
    }

    public static async IAsyncEnumerable<T> QueryAsync<T>(this NpgsqlConnection connection, string commandText, Action<NpgsqlParameterCollection>? configureParameters = null)
        where T : IDataReaderMapper<T>
    {
        using var cmd = connection.CreateCommand(commandText, configureParameters);

        await connection.OpenIfClosedAsync();
        using var reader = await connection.QueryAsync(cmd);

        await foreach (var item in MapAsync<T>(reader))
        {
            yield return item;
        }
    }

    public static Task<T?> MapSingleAsync<T>(this NpgsqlDataReader reader)
        where T : IDataReaderMapper<T>
        => MapSingleAsync(reader, T.Map);

    public static async Task<T?> MapSingleAsync<T>(this NpgsqlDataReader reader, Func<NpgsqlDataReader, T> mapper)
    {
        if (!reader.HasRows)
        {
            return default;
        }

        await reader.ReadAsync();

        return mapper(reader);
    }

    public static IAsyncEnumerable<T> MapAsync<T>(this NpgsqlDataReader reader)
        where T : IDataReaderMapper<T>
        => MapAsync(reader, T.Map);

    public static async IAsyncEnumerable<T> MapAsync<T>(this NpgsqlDataReader reader, Func<NpgsqlDataReader, T> mapper)
    {
        if (!reader.HasRows)
        {
            yield break;
        }

        while (await reader.ReadAsync())
        {
            yield return mapper(reader);
        }
    }

    public static Task<NpgsqlDataReader> QuerySingleAsync(this NpgsqlConnection connection, string commandText, params (string Name, object? Value)[] parameters)
        => QueryAsync(connection, commandText, CommandBehavior.SingleResult | CommandBehavior.SingleRow, parameters);

    public static Task<NpgsqlDataReader> QuerySingleAsync(this NpgsqlConnection connection, string commandText, params NpgsqlParameter[] parameters)
        => QueryAsync(connection, commandText, CommandBehavior.SingleResult | CommandBehavior.SingleRow, parameters);

    public static Task<NpgsqlDataReader> QuerySingleAsync(this NpgsqlConnection connection, NpgsqlCommand command)
        => QueryAsync(connection, command, CommandBehavior.SingleResult | CommandBehavior.SingleRow);

    public static Task<NpgsqlDataReader> QueryAsync(this NpgsqlConnection connection, string commandText, params (string Name, object? Value)[] parameters)
        => QueryAsync(connection, commandText, CommandBehavior.Default, parameters);

    public static async Task<NpgsqlDataReader> QueryAsync(this NpgsqlConnection connection, string commandText, CommandBehavior commandBehavior, params (string Name, object? Value)[] parameters)
    {
        using var cmd = connection.CreateCommand(commandText, parameters);

        await connection.OpenIfClosedAsync();
        return await cmd.ExecuteReaderAsync(commandBehavior);
    }

    public static async Task<NpgsqlDataReader> QueryAsync(this NpgsqlConnection connection, string commandText, CommandBehavior commandBehavior, params NpgsqlParameter[] parameters)
    {
        using var cmd = connection.CreateCommand(commandText, parameters);

        await connection.OpenIfClosedAsync();
        return await cmd.ExecuteReaderAsync(commandBehavior);
    }

    public static Task<NpgsqlDataReader> QueryAsync(this NpgsqlConnection connection, NpgsqlCommand command)
        => QueryAsync(connection, command, CommandBehavior.Default);

    public static async Task<NpgsqlDataReader> QueryAsync(this NpgsqlConnection connection, NpgsqlCommand command, CommandBehavior commandBehavior)
    {
        await connection.OpenIfClosedAsync();
        return await command.ExecuteReaderAsync(commandBehavior);
    }

    public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> enumerable, int? initialCapacity = null)
    {
        var list = initialCapacity.HasValue ? new List<T>(initialCapacity.Value) : new List<T>();

        await foreach (var item in enumerable)
        {
            list.Add(item);
        }

        return list;
    }

    public static NpgsqlParameterCollection AddTyped<T>(this NpgsqlParameterCollection parameters, T? value, [CallerArgumentExpression(nameof(value))] string name = null!)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var parameter = new NpgsqlParameter<T>
        {
            ParameterName = CleanParameterName(name),
            TypedValue = value
        };
        parameters.Add(parameter);
        return parameters;
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

    public static NpgsqlParameter AsTypedDbParameter<T>(this T value, [CallerArgumentExpression(nameof(value))] string name = null!)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var parameter = new NpgsqlParameter<T>
        {
            ParameterName = CleanParameterName(name),
            TypedValue = value
        };

        return parameter;
    }

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

    private static (string Name, object? Value) AsDbParameter(this object? value, [CallerArgumentExpression(nameof(value))] string name = null!)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        return (CleanParameterName(name), value ?? DBNull.Value);
    }

    private static NpgsqlCommand CreateCommand(this NpgsqlConnection connection, string commandText, params (string Name, object? Value)[] parameters)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = commandText;
        for (var i = 0; i < parameters.Length; i++)
        {
            var (name, value) = parameters[i];
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
        return cmd;
    }

    private static NpgsqlCommand CreateCommand(this NpgsqlConnection connection, string commandText, params NpgsqlParameter[] parameters)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = commandText;
        for (var i = 0; i < parameters.Length; i++)
        {
            cmd.Parameters.Add(parameters[i]);
        }
        return cmd;
    }

    private static NpgsqlCommand CreateCommand(this NpgsqlConnection connection, string commandText, Action<NpgsqlParameterCollection>? configureParameters = null)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = commandText;

        if (configureParameters is not null)
        {
            configureParameters(cmd.Parameters);
        }

        return cmd;
    }

    private static string CleanParameterName(string name)
    {
        var lastIndexOfPeriod = name.LastIndexOf('.');
        return lastIndexOfPeriod > 0 ? name[(lastIndexOfPeriod + 1)..] : name;
    }
}

public interface IDataReaderMapper<T> where T : IDataReaderMapper<T>
{
    abstract static T Map(NpgsqlDataReader dataReader);
}
