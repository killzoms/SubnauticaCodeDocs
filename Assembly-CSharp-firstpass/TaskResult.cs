public class TaskResult<T> : IOut<T>
{
    private T value;

    public void Set(T value)
    {
        this.value = value;
    }

    public T Get()
    {
        return value;
    }
}
