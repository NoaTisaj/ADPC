using System.Reflection;
using System.Text;

namespace Apolon.App.Orm.Migrations;

public class SchemaModelBuilder
{
    private readonly Type[] _entityTypes;

    public SchemaModelBuilder(params Type[] entityTypes)
    {
        _entityTypes = entityTypes;
    }

    public SchemaModel Build()
    {
        var model = new SchemaModel();

        foreach (var entityType in _entityTypes)
        {
            var table = new TableModel { Name = ToTableName(entityType.Name) };

            foreach (var property in entityType.GetProperties())
            {
                if (IsNavigationProperty(property))
                    continue;

                var underlying = Nullable.GetUnderlyingType(property.PropertyType);
                var isNullable = underlying != null;
                var actualType = underlying ?? property.PropertyType;

                table.Columns.Add(new ColumnModel
                {
                    Name = ToSnakeCase(property.Name),
                    SqlType = property.Name == "Id" ? "SERIAL PRIMARY KEY" : MapType(actualType),
                    IsNullable = property.Name != "Id" && isNullable
                });
            }

            model.Tables.Add(table);
        }

        return model;
    }

    private bool IsNavigationProperty(PropertyInfo property)
    {
        var type = property.PropertyType;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            return true;
        if (type.Assembly == _entityTypes[0].Assembly && type.IsClass && type != typeof(string))
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