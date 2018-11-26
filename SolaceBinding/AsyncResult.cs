using System;
using System.Diagnostics;
using System.Threading;

namespace Solace.Channels
{
    /// <summary>
    /// A generic base class for IAsyncResult implementations
    /// that wraps a ManualResetEvent.
    /// </summary>
    internal abstract class AsyncResult : IAsyncResult
    {
        private AsyncCallback callback;
        private bool endCalled;
        private Exception exception;
        private ManualResetEvent manualResetEvent;

        protected AsyncResult(AsyncCallback callback, object state)
        {
            this.callback = callback;
            AsyncState = state;
            ThisLock = new object();
        }

        public object AsyncState { get; }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                if (manualResetEvent != null)
                    return manualResetEvent;

                lock (ThisLock)
                    if (manualResetEvent == null)
                        manualResetEvent = new ManualResetEvent(IsCompleted);

                return manualResetEvent;
            }
        }

        public bool CompletedSynchronously { get; private set; }

        public bool IsCompleted { get; private set; }

        private object ThisLock { get; }

        /// <summary>
        /// Call this version of complete when your asynchronous operation is complete.
        /// This will update the state of the operation and notify the callback.
        /// </summary>
        protected void Complete(bool completedSynchronously)
        {
            if (IsCompleted) // It's a bug to call Complete twice.
                throw new InvalidOperationException("Cannot call Complete twice");

            CompletedSynchronously = completedSynchronously;

            if (completedSynchronously)
            {
                // If we completedSynchronously, then there is no chance that the manualResetEvent was created so
                // we do not need to worry about a race condition.
                Debug.Assert(manualResetEvent == null, "No ManualResetEvent should be created for a synchronous AsyncResult.");
                IsCompleted = true;
            }
            else
                lock (ThisLock)
                {
                    IsCompleted = true;
                    if (manualResetEvent != null)
                        manualResetEvent.Set();
                }

            // If the callback throws, there is a bug in the callback implementation
            callback?.Invoke(this);
        }

        /// <summary>
        /// Call this version of complete if you raise an exception during processing.
        /// In addition to notifying the callback, it will capture the exception and store it to be thrown during AsyncResult.End.
        /// </summary>
        protected void Complete(bool completedSynchronously, Exception exception)
        {
            this.exception = exception;
            Complete(completedSynchronously);
        }

        /// <summary>
        /// End should be called when the End function for the asynchronous operation is complete.
        /// It ensures the asynchronous operation is complete, and does some common validation.
        /// </summary>
        protected static TAsyncResult End<TAsyncResult>(IAsyncResult result)
            where TAsyncResult : AsyncResult
        {
            if (result == null)
                throw new ArgumentNullException("result");

            if (!(result is TAsyncResult asyncResult))
                throw new ArgumentException("Invalid async result.", "result");

            if (asyncResult.endCalled)
                throw new InvalidOperationException("Async object already ended.");

            asyncResult.endCalled = true;

            if (!asyncResult.IsCompleted)
                asyncResult.AsyncWaitHandle.WaitOne();

            if (asyncResult.manualResetEvent != null)
                asyncResult.manualResetEvent.Close();

            if (asyncResult.exception != null)
                throw asyncResult.exception;

            return asyncResult;
        }
    }

    /// <summary>
    /// An AsyncResult that completes as soon as it is instantiated.
    /// </summary>
    internal class CompletedAsyncResult : AsyncResult
    {
        public CompletedAsyncResult(AsyncCallback callback, object state)
            : base(callback, state)
        {
            Complete(true);
        }

        public static void End(IAsyncResult result) =>
            End<CompletedAsyncResult>(result);
    }

    /// <summary>
    /// A strongly typed AsyncResult
    /// </summary>
    internal abstract class TypedAsyncResult<T> : AsyncResult
    {
        protected TypedAsyncResult(AsyncCallback callback, object state)
            : base(callback, state)
        {
        }

        public T Data { get; private set; }

        protected void Complete(T data, bool completedSynchronously)
        {
            Data = data;
            Complete(completedSynchronously);
        }

        public static T End(IAsyncResult result) =>
            End<TypedAsyncResult<T>>(result).Data;
    }

    /// <summary>
    /// A strongly typed AsyncResult that completes as soon as it is instantiated.
    /// </summary>
    internal class TypedCompletedAsyncResult<T> : TypedAsyncResult<T>
    {
        public TypedCompletedAsyncResult(T data, AsyncCallback callback, object state)
            : base(callback, state)
        {
            Complete(data, true);
        }

        public new static T End(IAsyncResult result) =>
            (result is TypedCompletedAsyncResult<T> completedResult)
                ? TypedAsyncResult<T>.End(completedResult)
                : throw new ArgumentException("Invalid async result.", "result");
    }
}
