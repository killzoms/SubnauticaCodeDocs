namespace AssemblyCSharp
{
    public interface IObstacle
    {
        bool CanDeconstruct(out string reason);
    }
}
