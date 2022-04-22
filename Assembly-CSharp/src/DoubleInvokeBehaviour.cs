namespace AssemblyCSharp
{
    public class DoubleInvokeBehaviour : SimpleCounter
    {
        private void Start()
        {
            Invoke("Repeat", SimpleCounter.delay);
        }

        private void Repeat()
        {
            Do();
            Invoke("Repeat", SimpleCounter.delay);
            Invoke("Repeat", SimpleCounter.delay);
        }
    }
}
