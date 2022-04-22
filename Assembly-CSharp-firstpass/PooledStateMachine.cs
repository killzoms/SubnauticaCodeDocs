using System.Collections;
using UWE;

public class PooledStateMachine<T> : IEnumerator where T : IStateMachine, new()
{
    public readonly T stateMachine = new T();

    private ObjectPool<PooledStateMachine<T>> pool;

    public object Current
    {
        get
        {
            T val = stateMachine;
            return val.Current;
        }
    }

    public void Initialize(ObjectPool<PooledStateMachine<T>> pool)
    {
        this.pool = pool;
    }

    public bool MoveNext()
    {
        T val = stateMachine;
        bool num = val.MoveNext();
        if (!num)
        {
            val = stateMachine;
            val.Clear();
            pool.Return(this);
        }
        return num;
    }

    public void Reset()
    {
        T val = stateMachine;
        val.Reset();
    }
}
