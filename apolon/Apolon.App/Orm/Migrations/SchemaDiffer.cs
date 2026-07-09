namespace Apolon.App.Orm.Migrations;

public class SchemaDiffer
{
    public SchemaDiff Diff(SchemaModel oldModel, SchemaModel newModel)
    {
        var diff = new SchemaDiff();

        foreach (var newTable in newModel.Tables)
        {
            var oldTable = oldModel.Tables.FirstOrDefault(t => t.Name == newTable.Name);

            if (oldTable == null)
            {
                diff.NewTables.Add(newTable);
                continue;
            }

            foreach (var newColumn in newTable.Columns)
            {
                var exists = oldTable.Columns.Any(c => c.Name == newColumn.Name);
                if (!exists)
                    diff.AddedColumns.Add(new ColumnChange { TableName = newTable.Name, Column = newColumn });
            }

            foreach (var oldColumn in oldTable.Columns)
            {
                var stillExists = newTable.Columns.Any(c => c.Name == oldColumn.Name);
                if (!stillExists)
                    diff.RemovedColumns.Add(new ColumnChange { TableName = newTable.Name, Column = oldColumn });
            }
        }

        return diff;
    }
}