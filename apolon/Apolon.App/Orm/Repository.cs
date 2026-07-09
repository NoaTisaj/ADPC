using System.Reflection;
using System.Text;
using Npgsql;
using Apolon.App.Entities;

namespace Apolon.App.Orm;

public class Repository
{
    private readonly DatabaseConnection _database;

    public Repository(DatabaseConnection database)
    {
        _database = database;
    }

    public void Insert(object entity)
    {
        var type = entity.GetType();
        var tableName = ToTableName(type.Name);

        var properties = type.GetProperties()
            .Where(p => !IsNavigationProperty(p) && p.Name != "Id")
            .ToList();

        var columnNames = properties.Select(p => ToSnakeCase(p.Name)).ToList();
        var parameterNames = properties.Select(p => "@" + p.Name).ToList();

        var sql = $"INSERT INTO {tableName} ({string.Join(", ", columnNames)}) " +
                  $"VALUES ({string.Join(", ", parameterNames)}) RETURNING id;";

        using var connection = _database.OpenConnection();
        using var command = new NpgsqlCommand(sql, connection);

        foreach (var property in properties)
        {
            var value = property.GetValue(entity) ?? DBNull.Value;
            command.Parameters.AddWithValue("@" + property.Name, value);
        }

        var newId = command.ExecuteScalar();

        var idProperty = type.GetProperty("Id");
        idProperty?.SetValue(entity, Convert.ToInt32(newId));
    }

    private bool IsNavigationProperty(PropertyInfo property)
    {
        var type = property.PropertyType;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            return true;
        if (type.Assembly == typeof(Patient).Assembly && type.IsClass && type != typeof(string))
            return true;
        return false;
    }
    
    public List<T> GetAll<T>() where T : new()
    {
        var type = typeof(T);
        var tableName = ToTableName(type.Name);

        var columnProperties = type.GetProperties()
            .Where(p => !IsNavigationProperty(p))
            .ToList();

        var sql = $"SELECT * FROM {tableName};";

        using var connection = _database.OpenConnection();
        using var command = new NpgsqlCommand(sql, connection);
        using var reader = command.ExecuteReader();

        var results = new List<T>();

        while (reader.Read())
        {
            var entity = new T();

            foreach (var property in columnProperties)
            {
                var columnName = ToSnakeCase(property.Name);
                var value = reader[columnName];

                if (value != DBNull.Value)
                    property.SetValue(entity, value);
            }

            results.Add(entity);
        }

        return results;
    }
    
    public List<T> GetWhere<T>(string columnName, string op, object value,
        string? orderByColumn = null, bool descending = false) where T : new()
    {
        var allowedOperators = new[] { "=", "!=", "<", ">", "<=", ">=" };
        if (!allowedOperators.Contains(op))
            throw new ArgumentException($"Operator '{op}' is not allowed.");

        var type = typeof(T);
        var tableName = ToTableName(type.Name);

        var columnProperties = type.GetProperties()
            .Where(p => !IsNavigationProperty(p))
            .ToList();

        var validColumns = columnProperties.Select(p => ToSnakeCase(p.Name)).ToList();
        if (!validColumns.Contains(columnName))
            throw new ArgumentException($"Column '{columnName}' does not exist on {type.Name}.");

        var sql = $"SELECT * FROM {tableName} WHERE {columnName} {op} @value";

        if (orderByColumn != null)
        {
            if (!validColumns.Contains(orderByColumn))
                throw new ArgumentException($"Column '{orderByColumn}' does not exist on {type.Name}.");

            var direction = descending ? "DESC" : "ASC";
            sql += $" ORDER BY {orderByColumn} {direction}";
        }

        sql += ";";

        using var connection = _database.OpenConnection();
        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@value", value);

        using var reader = command.ExecuteReader();
        var results = new List<T>();

        while (reader.Read())
        {
            var entity = new T();
            foreach (var property in columnProperties)
            {
                var col = ToSnakeCase(property.Name);
                var val = reader[col];
                if (val != DBNull.Value)
                    property.SetValue(entity, val);
            }
            results.Add(entity);
        }

        return results;
    }
    
    public T? GetById<T>(int id) where T : new()
    {
        var results = GetWhere<T>("id", "=", id);
        return results.Count > 0 ? results[0] : default;
    }
    
    public void Update(object entity)
    {
        var type = entity.GetType();
        var tableName = ToTableName(type.Name);

        var properties = type.GetProperties()
            .Where(p => !IsNavigationProperty(p) && p.Name != "Id")
            .ToList();

        var assignments = properties
            .Select(p => $"{ToSnakeCase(p.Name)} = @{p.Name}")
            .ToList();

        var sql = $"UPDATE {tableName} SET {string.Join(", ", assignments)} WHERE id = @Id;";

        using var connection = _database.OpenConnection();
        using var command = new NpgsqlCommand(sql, connection);

        foreach (var property in properties)
        {
            var value = property.GetValue(entity) ?? DBNull.Value;
            command.Parameters.AddWithValue("@" + property.Name, value);
        }

        var idValue = type.GetProperty("Id")!.GetValue(entity);
        command.Parameters.AddWithValue("@Id", idValue!);

        command.ExecuteNonQuery();
    }

    public void Delete(object entity)
    {
        var type = entity.GetType();
        var tableName = ToTableName(type.Name);

        var idValue = type.GetProperty("Id")!.GetValue(entity);

        var sql = $"DELETE FROM {tableName} WHERE id = @Id;";

        using var connection = _database.OpenConnection();
        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", idValue!);

        command.ExecuteNonQuery();
    }

    private string ToTableName(string entityName) => ToSnakeCase(entityName) + "s";

    private string ToSnakeCase(string name)
    {
        var result = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]) && i > 0)
                result.Append('_');
            result.Append(char.ToLower(name[i]));
        }
        return result.ToString();
    }
}