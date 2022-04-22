namespace AssemblyCSharp
{
    public interface ITreeActionReceiver
    {
        bool inProgress { get; }

        bool PerformAction(ITreeActionSender sender, TechType techType);
    }
}
