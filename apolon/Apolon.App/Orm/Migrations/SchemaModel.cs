namespace Apolon.App.Orm.Migrations;

public class SchemaModel
{
    public List<TableModel> Tables { get; set; } = new();
}

public class TableModel
{
    public string Name { get; set; } = "";
    public List<ColumnModel> Columns { get; set; } = new();
}

public class ColumnModel
{
    public string Name { get; set; } = "";
    public string SqlType { get; set; } = "";
    public bool IsNullable { get; set; }
}