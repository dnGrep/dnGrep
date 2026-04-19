using System.ComponentModel;
using dnGREP.Common;

namespace dnGREP.WPF
{
    public record SortColumnRequest(SortType SortType, ListSortDirection Direction);
}
