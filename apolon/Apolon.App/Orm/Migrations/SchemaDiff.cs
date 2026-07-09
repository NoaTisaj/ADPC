namespace Apolon.App.Orm.Migrations;

public class SchemaDiff
{
    public List<TableModel> NewTables { get; set; } = new();
    public List<ColumnChange> AddedColumns { get; set; } = new();
    public List<ColumnChange> RemovedColumns { get; set; } = new();

    public bool HasChanges =>
        NewTables.Count > 0 || AddedColumns.Count > 0 || RemovedColumns.Count > 0;
}

public class ColumnChange
{
    public string TableName { get; set; } = "";
    public ColumnModel Column { get; set; } = null!;
}