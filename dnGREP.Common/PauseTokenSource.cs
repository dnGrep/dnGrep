using System;
using System.Threading;

namespace dnGREP.Common
{
    public class PauseTokenSource : IDisposable
    {
        private readonly ManualResetEventSlim pauseEvent = new(true);
        public PauseToken Token
        {
            get
            {
                ThrowIfDisposed();
                return new PauseToken(pauseEvent);
            }
        }

        public ManualResetEventSlim PauseEvent => pauseEvent;

        public bool IsPaused => !pauseEvent.IsSet;

        public void Pause()
        {
            if (pauseEvent.IsSet)
            {
                pauseEvent.Reset();
            }
        }

        public void Resume()
        {
            if (!pauseEvent.IsSet)
            {
                pauseEvent.Set();
            }
        }

        /// <summary>Throws an exception if the source has been disposed.</summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PauseTokenSource));
            }
        }

        #region Dispose

        private bool _disposed;

        /// <summary>Releases the resources used by this <see cref="PauseTokenSource" />.</summary>
        /// <remarks>This method is not thread-safe for any other concurrent calls.</remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="PauseTokenSource" /> class and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                pauseEvent?.Dispose();
                _disposed = true;
            }
        }

        #endregion
    }
}
