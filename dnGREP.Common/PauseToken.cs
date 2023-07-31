using System;
using System.Threading;

namespace dnGREP.Common
{
    public readonly struct PauseToken
    {
        /// <summary>
        /// The MRE that manages the "pause" logic, or <c>null</c> if this token can never be paused. When the MRE is set, the token is not paused; when the MRE is not set, the token is paused.
        /// </summary>
        private readonly ManualResetEventSlim pauseEvent;

        internal PauseToken(ManualResetEventSlim mre)
        {
            pauseEvent = mre;
        }

        /// <summary>
        /// Whether this token can ever possibly be paused.
        /// </summary>
        public bool CanBePaused
        {
            get { return pauseEvent != null; }
        }

        /// <summary>
        /// Whether or not this token is in the paused state.
        /// </summary>
        public bool IsPaused
        {
            get { return pauseEvent != null && !pauseEvent.IsSet; }
        }

        /// <summary>
        /// Synchronously waits until the pause token is not paused.
        /// </summary>
        public void WaitWhilePaused()
        {
            pauseEvent?.Wait();
        }

        /// <summary>
        /// Synchronously waits until the pause token is not paused, or until this wait is 
        /// canceled by the cancellation token.
        /// </summary>
        /// <param name="token">The cancellation token to observe. If the token is already 
        /// canceled, this method will first check if the pause token is unpaused, and will 
        /// return without an exception in that case.</param>
        public void WaitWhilePaused(CancellationToken token)
        {
            try
            {
                pauseEvent?.Wait(token);
            }
            catch (OperationCanceledException) { }
        }
    }
}
