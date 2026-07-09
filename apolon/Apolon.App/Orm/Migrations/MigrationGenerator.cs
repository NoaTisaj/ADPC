using System.Text;

namespace Apolon.App.Orm.Migrations;

public class MigrationGenerator
{
    public string GenerateUp(SchemaDiff diff)
    {
        var statements = new List<string>();

        foreach (var table in diff.NewTables)
            statements.Add(CreateTableSql(table));

        foreach (var change in diff.AddedColumns)
            statements.Add($"ALTER TABLE {change.TableName} ADD COLUMN {ColumnSql(change.Column)};");

        foreach (var change in diff.RemovedColumns)
            statements.Add($"ALTER TABLE {change.TableName} DROP COLUMN {change.Column.Name};");

        return string.Join("\n", statements);
    }

    public string GenerateDown(SchemaDiff diff)
    {
        var statements = new List<string>();

        foreach (var change in diff.RemovedColumns)
            statements.Add($"ALTER TABLE {change.TableName} ADD COLUMN {ColumnSql(change.Column)};");

        foreach (var change in diff.AddedColumns)
            statements.Add($"ALTER TABLE {change.TableName} DROP COLUMN {change.Column.Name};");

        foreach (var table in diff.NewTables)
            statements.Add($"DROP TABLE {table.Name};");

        return string.Join("\n", statements);
    }

    private string CreateTableSql(TableModel table)
    {
        var columns = table.Columns.Select(ColumnSql);
        return $"CREATE TABLE {table.Name} (\n    {string.Join(",\n    ", columns)}\n);";
    }

    private string ColumnSql(ColumnModel column)
    {
        if (column.SqlType.Contains("SERIAL"))
            return $"{column.Name} {column.SqlType}";

        var nullability = column.IsNullable ? "NULL" : "NOT NULL";
        return $"{column.Name} {column.SqlType} {nullability}";
    }
}