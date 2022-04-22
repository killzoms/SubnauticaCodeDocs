namespace AssemblyCSharp
{
    public interface ITreeActionSender
    {
        void Progress(float progress);

        void Done();
    }
}
