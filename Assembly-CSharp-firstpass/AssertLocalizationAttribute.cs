using System;

[AttributeUsage(AttributeTargets.Field)]
public class AssertLocalizationAttribute : Attribute
{
    [Flags]
    public enum Options
    {
        None = 0x0,
        AllowEmptyString = 0x1
    }

    public readonly Options options;

    public bool allowEmptyString => HasOption(Options.AllowEmptyString);

    public AssertLocalizationAttribute()
    {
    }

    public AssertLocalizationAttribute(Options options)
    {
        this.options = options;
    }

    public bool HasOption(Options option)
    {
        return (options & option) != 0;
    }
}
