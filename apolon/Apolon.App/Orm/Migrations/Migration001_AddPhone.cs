namespace Apolon.App.Orm;

public class Migration001_AddPhone : Migration
{
    public override int Version => 1;
    public override string Name => "AddPhoneToPatients";

    public override string Up()
    {
        return "ALTER TABLE patients ADD COLUMN phone VARCHAR;";
    }

    public override string Down()
    {
        return "ALTER TABLE patients DROP COLUMN phone;";
    }
}