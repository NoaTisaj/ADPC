namespace Apolon.App.Orm.Migrations;

public class AutoMigrationGenerator
{
    private readonly SchemaModelBuilder _builder;
    private readonly SnapshotStore _snapshotStore;
    private readonly SchemaDiffer _differ = new();
    private readonly MigrationGenerator _generator = new();

    public AutoMigrationGenerator(SchemaModelBuilder builder, SnapshotStore snapshotStore)
    {
        _builder = builder;
        _snapshotStore = snapshotStore;
    }

    public void Generate(string migrationName)
    {
        var currentModel = _builder.Build();
        var oldModel = _snapshotStore.SnapshotExists() ? _snapshotStore.Load() : new SchemaModel();

        var diff = _differ.Diff(oldModel, currentModel);

        if (!diff.HasChanges)
        {
            Console.WriteLine("No schema changes detected. Nothing to generate.");
            return;
        }

        var up = _generator.GenerateUp(diff);
        var down = _generator.GenerateDown(diff);

        Console.WriteLine($"=== Generated migration: {migrationName} ===");
        Console.WriteLine("--- UP ---");
        Console.WriteLine(up);
        Console.WriteLine("--- DOWN ---");
        Console.WriteLine(down);

        _snapshotStore.Save(currentModel);
        Console.WriteLine("\nSnapshot updated.");
    }
}