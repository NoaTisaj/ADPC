using Apolon.App.Entities;
using Apolon.App.Orm;
using Apolon.App.Orm.Migrations;

var connectionString = "Host=localhost;Port=5432;Username=apolon;Password=apolon_dev_pass;Database=apolon";
var database = new DatabaseConnection(connectionString);
var generator = new SchemaGenerator();
var repository = new Repository(database);

var entityOrderCreate = new[] { typeof(Patient), typeof(Medication), typeof(CheckupType), typeof(Checkup), typeof(Prescription) };
var entityOrderDrop = new[] { typeof(Prescription), typeof(Checkup), typeof(CheckupType), typeof(Medication), typeof(Patient) };

DemoSchema();
DemoCrud();
DemoFilteringAndOrdering();
DemoRelationships();
DemoTransactions();
DemoChangeTracking();
DemoMigrations();
DemoAutoMigration();

Console.WriteLine("\n===== DEMO COMPLETE =====");


void DemoSchema()
{
    Header("1. SCHEMA GENERATION", "The ORM reads entity classes via reflection and generates CREATE TABLE SQL, then runs it against Postgres.");
    generator.DropTables(database, entityOrderDrop);
    generator.CreateTables(database, entityOrderCreate);
    Console.WriteLine("→ 5 tables created from C# classes (types, PKs, NULL/NOT NULL, foreign keys, snake_case).");
}

void DemoCrud()
{
    Header("2. CREATE (Insert)", "Generic reflection-based insert. Postgres generates the id (SERIAL); RETURNING id writes it back onto the object.");
    var ana = new Patient { FirstName = "Ana", LastName = "Horvat", DateOfBirth = new DateTime(1990, 5, 12), WeightKg = 65.5 };
    repository.Insert(ana);
    Console.WriteLine($"→ Inserted Ana, DB-generated id = {ana.Id}");

    repository.Insert(new Patient { FirstName = "Ivan", LastName = "Kovač", DateOfBirth = new DateTime(1985, 3, 20), WeightKg = 82.0 });
    repository.Insert(new Patient { FirstName = "Maja", LastName = "Novak", DateOfBirth = new DateTime(1995, 7, 8), WeightKg = 58.0 });
    Console.WriteLine($"→ Total patients now in DB: {repository.GetAll<Patient>().Count}");
}

void DemoFilteringAndOrdering()
{
    Header("3. READ, FILTER, ORDER", "SELECT with WHERE and ORDER BY. Operators are whitelisted, columns validated, values parameterized (SQL-injection safe).");

    Console.WriteLine("Patients over 60kg:");
    foreach (var p in repository.GetWhere<Patient>("weight_kg", ">", 60))
        Console.WriteLine($"   {p.FirstName} {p.LastName} — {p.WeightKg}kg");

    Console.WriteLine("All patients, heaviest first:");
    foreach (var p in repository.GetWhere<Patient>("weight_kg", ">", 0, "weight_kg", descending: true))
        Console.WriteLine($"   {p.FirstName} {p.LastName} — {p.WeightKg}kg");
}

void DemoRelationships()
{
    Header("4. RELATIONSHIPS (navigation properties)", "Loading related data across cardinalities: 1-many (patient→checkups) and many-1 (checkup→patient/type).");

    var gp = new CheckupType { Code = "GP" };
    var blood = new CheckupType { Code = "BLOOD" };
    repository.Insert(gp);
    repository.Insert(blood);

    var ana = repository.GetWhere<Patient>("last_name", "=", "Horvat")[0];
    repository.Insert(new Checkup { PatientId = ana.Id, CheckupTypeId = gp.Id, CheckupDate = new DateTime(2026, 1, 10) });
    repository.Insert(new Checkup { PatientId = ana.Id, CheckupTypeId = blood.Id, CheckupDate = new DateTime(2026, 2, 3) });

    ana.Checkups = repository.GetWhere<Checkup>("patient_id", "=", ana.Id);
    Console.WriteLine($"→ 1-many: Ana has {ana.Checkups.Count} checkups");

    foreach (var c in ana.Checkups)
    {
        c.CheckupType = repository.GetById<CheckupType>(c.CheckupTypeId);
        c.Patient = repository.GetById<Patient>(c.PatientId);
        Console.WriteLine($"   many-1: checkup #{c.Id} → type {c.CheckupType?.Code}, patient {c.Patient?.FirstName}");
    }
}

void DemoTransactions()
{
    Header("5. TRANSACTIONS (Unit of Work)", "All-or-nothing. One batch commits; one throws mid-way and auto-rolls-back on dispose.");

    using (var uow = new UnitOfWork(database))
    {
        uow.Insert(new Medication { Name = "Ibuprofen" });
        uow.Insert(new Medication { Name = "Paracetamol" });
        uow.Commit();
        Console.WriteLine("→ Committed batch of 2 medications.");
    }
    Console.WriteLine($"   Medications in DB: {repository.GetAll<Medication>().Count}");

    try
    {
        using var uow = new UnitOfWork(database);
        uow.Insert(new Medication { Name = "ShouldVanish" });
        throw new Exception("boom before commit");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"→ Exception ({ex.Message}) — batch rolled back.");
    }
    Console.WriteLine($"   Medications still: {repository.GetAll<Medication>().Count} (the failed one was NOT saved)");
}

void DemoChangeTracking()
{
    Header("6. CHANGE TRACKING", "The tracker snapshots objects, then SaveChanges auto-detects what changed and persists only that.");

    var tracker = new ChangeTracker(repository);

    var luka = new Patient { FirstName = "Luka", LastName = "Marić", DateOfBirth = new DateTime(1992, 8, 1), WeightKg = 78.0 };
    tracker.Add(luka);
    tracker.SaveChanges();
    Console.WriteLine($"→ Added Luka via tracker (id {luka.Id}).");

    luka.WeightKg = 80.0;
    tracker.SaveChanges();
    var lukaDb = repository.GetById<Patient>(luka.Id);
    Console.WriteLine($"→ Changed weight; auto-detected. DB now: {lukaDb?.WeightKg}kg");

    tracker.SaveChanges();
    Console.WriteLine("→ SaveChanges with no changes: nothing written (no-op).");

    tracker.Remove(luka);
    tracker.SaveChanges();
    Console.WriteLine("→ Removed Luka via tracker; delete persisted.");
}

void DemoMigrations()
{
    Header("7. MIGRATIONS (apply + rollback)", "Hand-authored Up/Down migrations, tracked in __migrations, applied in order and reversible.");

    var runner = new MigrationRunner(database);
    runner.Migrate();
    Console.WriteLine("→ Applied pending migrations (adds 'phone' column, tracked).");
    runner.Rollback();
    Console.WriteLine("→ Rolled back the last migration (removes 'phone', un-tracked).");
}

void DemoAutoMigration()
{
    Header("8. AUTO-MIGRATION GENERATION", "Compares current classes to a saved JSON snapshot and generates Up/Down SQL for the differences.");

    var builder = new SchemaModelBuilder(entityOrderCreate);
    var differ = new SchemaDiffer();
    var migGen = new MigrationGenerator();

    var oldModel = builder.Build();
    var newModel = builder.Build();
    newModel.Tables.First(t => t.Name == "patients")
        .Columns.Add(new ColumnModel { Name = "email", SqlType = "VARCHAR", IsNullable = true });

    var diff = differ.Diff(oldModel, newModel);
    Console.WriteLine("→ Simulated adding an 'email' property. Auto-generated migration:");
    Console.WriteLine("   UP:   " + migGen.GenerateUp(diff));
    Console.WriteLine("   DOWN: " + migGen.GenerateDown(diff));
}

void Header(string title, string explanation)
{
    Console.WriteLine($"\n=== {title} ===");
    Console.WriteLine(explanation);
}