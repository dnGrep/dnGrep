using System;

namespace dnGREP.WPF.Properties
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class BuildDateTimeAttribute : Attribute
    {
        public string Date { get; set; }

        public BuildDateTimeAttribute(string date)
        {
            Date = date;
        }
    }
}
