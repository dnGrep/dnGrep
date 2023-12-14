using System;
using System.ComponentModel;

namespace dnGREP.WPF
{
    public interface IScriptCommand
    {
        void Execute(string value);
    }


    public class ScriptCommand<TParameter>(Action<TParameter> execute) : IScriptCommand
    {
        public void Execute(string value)
        {
            TParameter parameter = GetValue<TParameter>(value);
            execute(parameter);
        }

        private static T GetValue<T>(string value)
        {
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
            if (converter != null && converter.ConvertFrom(value) is T typeValue)
            {
                return typeValue;
            }

            throw new Exception($"Could not convert string {value} to type " + typeof(T));
        }
    }
}
