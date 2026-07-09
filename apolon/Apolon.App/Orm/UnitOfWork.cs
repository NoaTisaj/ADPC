using System.Reflection;
using System.Text;
using Npgsql;
using Apolon.App.Entities;

namespace Apolon.App.Orm;

public class UnitOfWork : IDisposable
{
    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;
    private bool _finished;

    public UnitOfWork(DatabaseConnection database)
    {
        _connection = database.OpenConnection();
        _transaction = _connection.BeginTransaction();
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

        using var command = new NpgsqlCommand(sql, _connection, _transaction);
        foreach (var property in properties)
        {
            var value = property.GetValue(entity) ?? DBNull.Value;
            command.Parameters.AddWithValue("@" + property.Name, value);
        }

        var newId = command.ExecuteScalar();
        type.GetProperty("Id")?.SetValue(entity, Convert.ToInt32(newId));
    }

    public void Commit()
    {
        _transaction.Commit();
        _finished = true;
    }

    public void Rollback()
    {
        _transaction.Rollback();
        _finished = true;
    }

    public void Dispose()
    {
        if (!_finished)
            _transaction.Rollback();

        _transaction.Dispose();
        _connection.Dispose();
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