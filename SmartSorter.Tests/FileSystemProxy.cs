using System.Reflection;
using System.Runtime.ExceptionServices;

namespace SmartSort.Tests;

// Fault injection around the existing filesystem mock; no production abstraction or mocking package is needed.
public class FileSystemProxy : DispatchProxy
{
    private object _target = null!;
    private Func<MethodInfo, object?[], Func<object?>, object?> _intercept = null!;

    public static T Wrap<T>(T target, Func<MethodInfo, object?[], Func<object?>, object?> intercept) where T : class
    {
        var proxy = Create<T, FileSystemProxy>();
        var state = (FileSystemProxy)(object)proxy;
        state._target = target;
        state._intercept = intercept;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var method = targetMethod!;
        return _intercept(method, args ?? [], () =>
        {
            try { return method.Invoke(_target, args); }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        });
    }
}
