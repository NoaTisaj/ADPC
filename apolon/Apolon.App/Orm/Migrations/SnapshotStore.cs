using System.Text.Json;

namespace Apolon.App.Orm.Migrations;

public class SnapshotStore
{
    private readonly string _filePath;

    public SnapshotStore(string filePath)
    {
        _filePath = filePath;
    }

    public bool SnapshotExists() => File.Exists(_filePath);

    public void Save(SchemaModel model)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(model, options);
        File.WriteAllText(_filePath, json);
    }

    public SchemaModel Load()
    {
        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<SchemaModel>(json) ?? new SchemaModel();
    }
}