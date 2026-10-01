using UnityEngine;

namespace ZS.UI.Navigation
{
    internal static class AwaitableUtility
    {
        public static Awaitable Completed()
        {
            var source = new AwaitableCompletionSource();
            source.SetResult();
            return source.Awaitable;
        }

        public static Awaitable<T> FromResult<T>(T value)
        {
            var source = new AwaitableCompletionSource<T>();
            source.SetResult(value);
            return source.Awaitable;
        }
    }
}
