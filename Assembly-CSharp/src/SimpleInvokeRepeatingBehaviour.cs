namespace AssemblyCSharp
{
    public class SimpleInvokeRepeatingBehaviour : SimpleCounter
    {
        public void Start()
        {
            InvokeRepeating("Do", SimpleCounter.delay, SimpleCounter.delay);
        }
    }
}
