namespace AssemblyCSharp
{
    public interface IConstructable : IObstacle
    {
        void OnConstructedChanged(bool constructed);
    }
}
