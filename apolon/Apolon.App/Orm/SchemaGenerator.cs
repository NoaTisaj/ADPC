using System.Reflection;
using System.Text;
using Apolon.App.Entities;
using Npgsql;

namespace Apolon.App.Orm;

public class SchemaGenerator
{
    public void CreateTables(DatabaseConnection database, params Type[] entityTypes)
    {
        using var connection = database.OpenConnection();

        foreach (var entityType in entityTypes)
        {
            var sql = GenerateCreateTable(entityType);

            using var command = new NpgsqlCommand(sql, connection);
            command.ExecuteNonQuery();

            Console.WriteLine($"Created table for {entityType.Name}");
        }
    }
    
    public void DropTables(DatabaseConnection database, params Type[] entityTypes)
    {
        using var connection = database.OpenConnection();

        foreach (var entityType in entityTypes)
        {
            var tableName = ToTableName(entityType.Name);
            var sql = $"DROP TABLE IF EXISTS {tableName} CASCADE;";

            using var command = new NpgsqlCommand(sql, connection);
            command.ExecuteNonQuery();

            Console.WriteLine($"Dropped table {tableName}");
        }
    }
    
    public string GenerateCreateTable(Type entityType)
    {
        var tableName = ToTableName(entityType.Name);
        var lines = new List<string>();

        foreach (var property in entityType.GetProperties())
        {
            if (IsNavigationProperty(property))
                continue;

            lines.Add(BuildColumn(property));
        }

        foreach (var property in entityType.GetProperties())
        {
            if (IsForeignKeyProperty(property))
                lines.Add(BuildForeignKey(property));
        }

        var body = string.Join(",\n", lines);
        return $"CREATE TABLE {tableName} (\n{body}\n);";
    }

    private string BuildColumn(PropertyInfo property)
    {
        var columnName = ToSnakeCase(property.Name);

        if (property.Name == "Id")
            return $"    {columnName} SERIAL PRIMARY KEY";

        var underlyingType = Nullable.GetUnderlyingType(property.PropertyType);
        var isNullable = underlyingType != null;
        var actualType = underlyingType ?? property.PropertyType;

        var sqlType = MapType(actualType);
        var nullConstraint = isNullable ? "NULL" : "NOT NULL";

        return $"    {columnName} {sqlType} {nullConstraint}";
    }

    private string BuildForeignKey(PropertyInfo property)
    {
        var columnName = ToSnakeCase(property.Name);
        var targetEntity = property.Name.Substring(0, property.Name.Length - 2);
        var targetTable = ToTableName(targetEntity);

        return $"    FOREIGN KEY ({columnName}) REFERENCES {targetTable}(id)";
    }

    private bool IsForeignKeyProperty(PropertyInfo property)
    {
        return property.Name.EndsWith("Id") && property.Name != "Id";
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

    private string MapType(Type type)
    {
        if (type == typeof(int)) return "INTEGER";
        if (type == typeof(string)) return "VARCHAR";
        if (type == typeof(decimal)) return "NUMERIC";
        if (type == typeof(double)) return "DOUBLE PRECISION";
        if (type == typeof(DateTime)) return "TIMESTAMP";
        return "UNKNOWN";
    }

    private string ToTableName(string entityName)
    {
        return ToSnakeCase(entityName) + "s";
    }

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