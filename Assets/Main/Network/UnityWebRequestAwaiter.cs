using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine.Networking;

public static class UnityWebRequestAwaiter
{
    public static TaskAwaiter<bool> GetAwaiter(this UnityWebRequestAsyncOperation op)
    {
        if (op == null) throw new ArgumentNullException(nameof(op));

        var tcs = new TaskCompletionSource<bool>();

        if (op.isDone)
        {
            tcs.TrySetResult(true);
            return tcs.Task.GetAwaiter();
        }

        op.completed += _ => tcs.TrySetResult(true);
        return tcs.Task.GetAwaiter();
    }
}