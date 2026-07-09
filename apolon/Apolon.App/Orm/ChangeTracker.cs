using System.Reflection;

namespace Apolon.App.Orm;

public enum EntityState
{
    Added,
    Modified,
    Unchanged,
    Deleted
}

public class TrackedEntity
{
    public object Entity { get; set; } = null!;
    public EntityState State { get; set; }
    public Dictionary<string, object?> Snapshot { get; set; } = new();
}

public class ChangeTracker
{
    private readonly List<TrackedEntity> _tracked = new();
    private readonly Repository _repository;

    public ChangeTracker(Repository repository)
    {
        _repository = repository;
    }

    public void Track(object entity)
    {
        _tracked.Add(new TrackedEntity
        {
            Entity = entity,
            State = EntityState.Unchanged,
            Snapshot = TakeSnapshot(entity)
        });
    }

    public void Add(object entity)
    {
        _tracked.Add(new TrackedEntity
        {
            Entity = entity,
            State = EntityState.Added,
            Snapshot = new()
        });
    }

    public void Remove(object entity)
    {
        var tracked = _tracked.FirstOrDefault(t => ReferenceEquals(t.Entity, entity));
        if (tracked != null)
            tracked.State = EntityState.Deleted;
    }

    public List<TrackedEntity> GetTracked() => _tracked;

    private Dictionary<string, object?> TakeSnapshot(object entity)
    {
        var snapshot = new Dictionary<string, object?>();

        foreach (var property in entity.GetType().GetProperties())
        {
            if (IsSimpleProperty(property))
                snapshot[property.Name] = property.GetValue(entity);
        }

        return snapshot;
    }

    private bool IsSimpleProperty(PropertyInfo property)
    {
        var type = property.PropertyType;
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        return underlying == typeof(int)
               || underlying == typeof(string)
               || underlying == typeof(decimal)
               || underlying == typeof(double)
               || underlying == typeof(DateTime);
    }
    
    public void SaveChanges()
    {
        DetectChanges();

        foreach (var tracked in _tracked)
        {
            switch (tracked.State)
            {
                case EntityState.Added:
                    _repository.Insert(tracked.Entity);
                    break;
                case EntityState.Modified:
                    _repository.Update(tracked.Entity);
                    break;
                case EntityState.Deleted:
                    _repository.Delete(tracked.Entity);
                    break;
            }
        }

        _tracked.RemoveAll(t => t.State == EntityState.Deleted);

        foreach (var tracked in _tracked)
        {
            tracked.State = EntityState.Unchanged;
            tracked.Snapshot = TakeSnapshot(tracked.Entity);
        }
    }

    private void DetectChanges()
    {
        foreach (var tracked in _tracked)
        {
            if (tracked.State != EntityState.Unchanged)
                continue;

            if (HasChanged(tracked))
                tracked.State = EntityState.Modified;
        }
    }

    private bool HasChanged(TrackedEntity tracked)
    {
        var current = TakeSnapshot(tracked.Entity);

        foreach (var pair in tracked.Snapshot)
        {
            var currentValue = current[pair.Key];
            var originalValue = pair.Value;

            if (!Equals(currentValue, originalValue))
                return true;
        }

        return false;
    }
}