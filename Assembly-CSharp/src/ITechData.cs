namespace AssemblyCSharp
{
    public interface ITechData
    {
        int craftAmount { get; }

        int ingredientCount { get; }

        int linkedItemCount { get; }

        IIngredient GetIngredient(int index);

        TechType GetLinkedItem(int index);
    }
}
