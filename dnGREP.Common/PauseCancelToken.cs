using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace dnGREP.Common
{
    public readonly struct PauseCancelToken : IEquatable<PauseCancelToken>
    {
        private readonly PauseCancelTokenSource? _source;
        private readonly CancellationToken? _cancelToken;
        private readonly PauseToken? _pauseToken;

        /// <summary>
        /// Returns an empty PauseCancelToken value.
        /// </summary>
        /// <remarks>
        /// The <see cref="PauseCancelToken"/> value returned by this property will be non-cancelable by default.
        /// </remarks>
        public static PauseCancelToken None => default;

        // public PauseCancelToken()
        // this constructor is implicit for structs

        /// <summary>
        /// Internal constructor only a PauseCancelTokenSource should create a PauseCancelToken
        /// </summary>
        internal PauseCancelToken(PauseCancelTokenSource? source)
        {
            _source = source;
            if (_source != null)
            {
                _cancelToken = _source.CancelToken;
                _pauseToken = _source.PauseToken;
            }
        }

        public bool Equals(PauseCancelToken other) => _source == other._source;
        public override bool Equals([NotNullWhen(true)] object? other) => other is PauseCancelToken token && Equals(token);
        public override int GetHashCode() => (_source ?? PauseCancelTokenSource.s_dummySource).GetHashCode();
        public static bool operator ==(PauseCancelToken left, PauseCancelToken right) => left.Equals(right);
        public static bool operator !=(PauseCancelToken left, PauseCancelToken right) => !left.Equals(right);

        public CancellationToken CancellationToken => _cancelToken ?? default;
        public bool IsCancellationRequested => _source != null && _source.IsCancellationRequested;
        public bool IsPaused => _source != null && _source.IsPaused;

        public void WaitWhilePaused()
        {
            if (_cancelToken != null)
            {
                _pauseToken?.WaitWhilePaused(_cancelToken.Value);
            }
            else
            {
                _pauseToken?.WaitWhilePaused();
            }
        }

        public void WaitWhilePausedOrThrowIfCancellationRequested()
        {
            _cancelToken?.ThrowIfCancellationRequested();

            WaitWhilePaused();
        }

    }
}
