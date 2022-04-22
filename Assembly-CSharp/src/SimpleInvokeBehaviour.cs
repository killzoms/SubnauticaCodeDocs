namespace AssemblyCSharp
{
    public class SimpleInvokeBehaviour : SimpleCounter
    {
        private void Start()
        {
            Invoke("Repeat", SimpleCounter.delay);
        }

        private void Repeat()
        {
            Do();
            Invoke("Repeat", SimpleCounter.delay);
        }
    }
}
