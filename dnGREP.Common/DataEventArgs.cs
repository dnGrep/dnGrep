using System;

namespace dnGREP.Common
{
    public class DataEventArgs<T>(T data) : EventArgs
    {
        public T Data { get; private set; } = data;
    }
}
