using System.Reflection;
using Npgsql;

namespace Apolon.App.Orm;

public class MigrationRunner
{
    private readonly DatabaseConnection _database;

    public MigrationRunner(DatabaseConnection database)
    {
        _database = database;
    }

    public void Migrate()
    {
        EnsureTrackingTable();

        var applied = GetAppliedVersions();
        var allMigrations = DiscoverMigrations();

        var pending = allMigrations
            .Where(m => !applied.Contains(m.Version))
            .OrderBy(m => m.Version)
            .ToList();

        if (pending.Count == 0)
        {
            Console.WriteLine("Database is up to date. Nothing to migrate.");
            return;
        }

        foreach (var migration in pending)
        {
            RunSql(migration.Up());
            RecordMigration(migration);
            Console.WriteLine($"Applied migration {migration.Version}: {migration.Name}");
        }
    }

    private void EnsureTrackingTable()
    {
        var sql = @"
            CREATE TABLE IF NOT EXISTS __migrations (
                version INTEGER PRIMARY KEY,
                name VARCHAR NOT NULL,
                applied_at TIMESTAMP NOT NULL DEFAULT NOW()
            );";
        RunSql(sql);
    }

    private List<int> GetAppliedVersions()
    {
        var versions = new List<int>();

        using var connection = _database.OpenConnection();
        using var command = new NpgsqlCommand("SELECT version FROM __migrations;", connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
            versions.Add(reader.GetInt32(0));

        return versions;
    }

    private List<Migration> DiscoverMigrations()
    {
        return Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(Migration)))
            .Select(t => (Migration)Activator.CreateInstance(t)!)
            .ToList();
    }

    private void RecordMigration(Migration migration)
    {
        using var connection = _database.OpenConnection();
        using var command = new NpgsqlCommand(
            "INSERT INTO __migrations (version, name) VALUES (@version, @name);", connection);
        command.Parameters.AddWithValue("@version", migration.Version);
        command.Parameters.AddWithValue("@name", migration.Name);
        command.ExecuteNonQuery();
    }

    private void RunSql(string sql)
    {
        using var connection = _database.OpenConnection();
        using var command = new NpgsqlCommand(sql, connection);
        command.ExecuteNonQuery();
    }
    
    public void Rollback()
    {
        EnsureTrackingTable();

        var applied = GetAppliedVersions();
        if (applied.Count == 0)
        {
            Console.WriteLine("No migrations to roll back.");
            return;
        }

        var lastVersion = applied.Max();
        var migration = DiscoverMigrations().FirstOrDefault(m => m.Version == lastVersion);

        if (migration == null)
        {
            Console.WriteLine($"Cannot find migration class for version {lastVersion}.");
            return;
        }

        RunSql(migration.Down());
        RemoveMigrationRecord(lastVersion);
        Console.WriteLine($"Rolled back migration {migration.Version}: {migration.Name}");
    }

    private void RemoveMigrationRecord(int version)
    {
        using var connection = _database.OpenConnection();
        using var command = new NpgsqlCommand("DELETE FROM __migrations WHERE version = @version;", connection);
        command.Parameters.AddWithValue("@version", version);
        command.ExecuteNonQuery();
    }
}