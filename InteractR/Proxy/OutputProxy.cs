using System.Reflection;

namespace InteractR.Proxy;

public class OutputProxy<T> : DispatchProxy
{
    private T _outputPort;
    public static T Create(T decorated)
    {
        object proxy = Create<T, OutputProxy<T>>();
        ((OutputProxy<T>)proxy).SetOutputPort(decorated);

        return (T)proxy;
    }

    private void SetOutputPort(T outputPort)
    {
        _outputPort = outputPort;
    }

    protected override object Invoke(MethodInfo targetMethod, object[] args)
    {
        return targetMethod.Invoke(_outputPort, args);
    }
}