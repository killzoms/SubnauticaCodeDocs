namespace AssemblyCSharp
{
    public interface IIngredient
    {
        TechType techType { get; }

        int amount { get; }
    }
}
