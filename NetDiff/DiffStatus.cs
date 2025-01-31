namespace NetDiff
{
    public enum DiffStatus
    {
        Equal,
        Inserted,
        Deleted,
        Modified,
    }

    public static class DiffStatusExtension
    {
        public static char GetStatusChar(this DiffStatus self)
        {
            switch (self)
            {
                case DiffStatus.Equal: return '=';
                case DiffStatus.Inserted: return '+';
                case DiffStatus.Deleted: return '-';
                case DiffStatus.Modified: return 'M';
            }

            throw new System.Exception();
        }
    }
}
