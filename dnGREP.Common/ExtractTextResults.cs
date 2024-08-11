using System.Text;

namespace dnGREP.Common
{
    public record ExtractTextResults(string Text, Encoding Encoding) 
    { 
    }

    public record Sheet(string Name, string Content)
    { 
    }
}
