using System;

[AttributeUsage(AttributeTargets.Field)]
public class AssertNotNullAttribute : Attribute
{
    [Flags]
    public enum Options
    {
        None = 0x0,
        IgnorePrefabs = 0x1,
        IgnoreScenes = 0x2,
        AllowEmptyCollection = 0x4
    }

    public readonly Options options;

    public bool ignorePrefabs => HasOption(Options.IgnorePrefabs);

    public bool ignoreScenes => HasOption(Options.IgnoreScenes);

    public bool allowEmptyCollection => HasOption(Options.AllowEmptyCollection);

    public AssertNotNullAttribute()
    {
    }

    public AssertNotNullAttribute(Options options)
    {
        this.options = options;
    }

    public bool HasOption(Options option)
    {
        return (options & option) != 0;
    }
}
