using System;
using System.Threading.Tasks;

// Compatibility layer for UniTask - provides basic functionality to resolve compilation errors
// This is a minimal implementation. For production, install the actual UniTask package.

namespace Cysharp.Threading.Tasks
{
    /// <summary>
    /// Minimal UniTask compatibility structure for compilation.
    /// </summary>
    public readonly struct UniTask
    {
        public static UniTask CompletedTask => default;

        public static UniTask Delay(int millisecondsDelay)
        {
            _ = Task.Delay(millisecondsDelay);
            return default;
        }

        public static async UniTask<T> FromResult<T>(T value)
        {
            await Task.CompletedTask;
            return value;
        }

        public static UniTask WhenAll(params UniTask[] tasks)
        {
            return default;
        }

        public UniTaskAwaiter GetAwaiter()
        {
            return new UniTaskAwaiter();
        }
    }

    /// <summary>
    /// Generic UniTask for compatibility.
    /// </summary>
    public readonly struct UniTask<T>
    {
        private readonly T _result;

        public UniTask(T result)
        {
            _result = result;
        }

        public UniTaskAwaiter<T> GetAwaiter()
        {
            return new UniTaskAwaiter<T>(_result);
        }

        public static implicit operator UniTask<T>(T value)
        {
            return new UniTask<T>(value);
        }
    }

    /// <summary>
    /// UniTaskVoid for fire-and-forget operations.
    /// </summary>
    public readonly struct UniTaskVoid
    {
        public static UniTaskVoid CompletedTask => default;
    }

    /// <summary>
    /// Awaiter for UniTask.
    /// </summary>
    public readonly struct UniTaskAwaiter : System.Runtime.CompilerServices.INotifyCompletion
    {
        public bool IsCompleted => true;

        public void GetResult() { }

        public void OnCompleted(Action continuation)
        {
            continuation?.Invoke();
        }
    }

    /// <summary>
    /// Generic awaiter for UniTask.
    /// </summary>
    public readonly struct UniTaskAwaiter<T> : System.Runtime.CompilerServices.INotifyCompletion
    {
        private readonly T _result;

        public UniTaskAwaiter(T result)
        {
            _result = result;
        }

        public bool IsCompleted => true;

        public T GetResult() => _result;

        public void OnCompleted(Action continuation)
        {
            continuation?.Invoke();
        }
    }
}

// Extension methods for Task -> UniTask conversion
namespace System.Threading.Tasks
{
    public static class TaskExtensions
    {
        public static Cysharp.Threading.Tasks.UniTask AsUniTask(this Task task)
        {
            return default;
        }

        public static Cysharp.Threading.Tasks.UniTask<T> AsUniTask<T>(this Task<T> task)
        {
            return new Cysharp.Threading.Tasks.UniTask<T>(task.IsCompletedSuccessfully ? task.Result : default(T));
        }
    }
}
