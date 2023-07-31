using System;
using System.Threading;

namespace dnGREP.Common
{
    public class PauseCancelTokenSource : IDisposable
    {
        internal static readonly PauseCancelTokenSource s_dummySource = new();

        private readonly CancellationTokenSource _cancelTokenSource;
        private readonly PauseTokenSource _pauseTokenSource;

        public PauseCancelTokenSource()
        {
            _cancelTokenSource = new();
            _pauseTokenSource = new();
        }

        public PauseCancelToken Token => new(this);

        internal CancellationToken CancelToken => _cancelTokenSource.Token;
        internal PauseToken PauseToken => _pauseTokenSource.Token;

        public void Cancel() => _cancelTokenSource.Cancel();
        public void Pause() => _pauseTokenSource.Pause();
        public void Resume() => _pauseTokenSource.Resume();

        public bool CanBeCanceled => _cancelTokenSource != null;
        public bool IsCancellationRequested => _cancelTokenSource.IsCancellationRequested;
        public bool IsPaused => _pauseTokenSource.IsPaused;


        #region Dispose
        private bool _disposed;

        /// <summary>Releases the resources used by this <see cref="PauseCancelTokenSource" />.</summary>
        /// <remarks>This method is not thread-safe for any other concurrent calls.</remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="PauseCancelTokenSource" /> class and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _cancelTokenSource.Dispose();
                _pauseTokenSource.Dispose();
            }
            _disposed = true;
        }
        #endregion
    }
}
