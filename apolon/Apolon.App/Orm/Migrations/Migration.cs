namespace Apolon.App.Orm;

public abstract class Migration
{
    public abstract int Version { get; }
    public abstract string Name { get; }

    public abstract string Up();
    public abstract string Down();
}