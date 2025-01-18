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
            return string.Format("Obj1:{0} Obj2:{1} Status:{2}", Obj1.ToString() ?? string.Empty, Obj2.ToString() ?? string.Empty, Status)
                .Replace("\0", "");
        }

        public string ToFormatString()
        {
            var obj = Status == DiffStatus.Deleted ? Obj1 : Obj2;

            return $"{Status.GetStatusChar()} {obj}";
        }
    }
}
