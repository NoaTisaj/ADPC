using Apolon.App.Entities;
using Apolon.App.Orm.Migrations;

var builder = new SchemaModelBuilder(
    typeof(Patient), typeof(Medication), typeof(CheckupType), typeof(Checkup), typeof(Prescription));
var store = new SnapshotStore("schema_snapshot.json");

var auto = new AutoMigrationGenerator(builder, store);
auto.Generate("MyMigration");