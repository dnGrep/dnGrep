using System;

namespace dnGREP.WPF.Properties
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class BuildDateTimeAttribute(string date) : Attribute
    {
        public string Date { get; set; } = date;
    }
}
