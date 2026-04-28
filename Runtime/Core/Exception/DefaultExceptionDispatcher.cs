using System;
using R3;
using UnityEngine;

namespace CFramework
{
    /// <summary>
    ///     默认异常分发器实现
    /// </summary>
    public sealed class DefaultExceptionDispatcher : IExceptionDispatcher, IDisposable
    {
        private readonly Subject<Exception> _errorStream = new();
        private readonly CompositeDisposable _handlers = new();
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _handlers.Dispose();
            _errorStream.Dispose();
        }

        public void Dispatch(Exception exception, string context = null)
        {
            if (exception == null || _disposed) return;

            var contextMessage = string.IsNullOrEmpty(context) ? "" : $" Context: {context}";
            var exceptionMessage = $"{exception.GetType().Name}: {exception.Message}";
            LogUtility.Error("Exception", $"{contextMessage}\n{exceptionMessage}\n{exception.StackTrace}");

            _errorStream.OnNext(exception);
        }

        public IDisposable RegisterHandler(Action<Exception> handler)
        {
            if (handler == null) return Disposable.Empty;

            var subscription = _errorStream.Subscribe(handler);
            _handlers.Add(subscription);
            return subscription;
        }
    }
}