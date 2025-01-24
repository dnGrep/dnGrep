namespace NetDiff
{
    public class DiffResult<T>
    {
        public T Obj1 { get; }
        public T Obj2 { get; }
        public DiffStatus Status { get; }

        public DiffResult(T obj1, T obj2, DiffStatus status)
        {
            Obj1 = obj1;
            Obj2 = obj2;
            Status = status;
        }

        public override string ToString()
        {
            return Status switch
            {
                DiffStatus.Equal => string.Format("= {0}", Obj1?.ToString() ?? string.Empty),
                DiffStatus.Inserted => string.Format("+ {0}", Obj2?.ToString() ?? string.Empty),
                DiffStatus.Deleted => string.Format("- {0}", Obj1?.ToString() ?? string.Empty),
                DiffStatus.Modified => string.Format("m {0} -> {1}", Obj1?.ToString() ?? string.Empty, Obj2?.ToString() ?? string.Empty),
                _ => string.Empty,
            };
        }

        public string ToFormatString()
        {
            var obj = Status == DiffStatus.Deleted ? Obj1 : Obj2;

            return $"{Status.GetStatusChar()} {obj}";
        }
    }
}
