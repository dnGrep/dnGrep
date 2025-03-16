using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace dnGREP.WPF
{
    public class InputBindingsBehavior
    {
        public static readonly DependencyProperty InputBindingsProperty = DependencyProperty.RegisterAttached(
            "InputBindings", typeof(IEnumerable<InputBinding>), typeof(InputBindingsBehavior),
            new PropertyMetadata(null,
            new PropertyChangedCallback(Callback)));

        public static void SetInputBindings(UIElement element, IEnumerable<InputBinding> value)
        {
            element.SetValue(InputBindingsProperty, value);
        }
        public static IEnumerable<InputBinding> GetInputBindings(UIElement element)
        {
            return (IEnumerable<InputBinding>)element.GetValue(InputBindingsProperty);
        }

        private static void Callback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement uiElement = (UIElement)d;
            uiElement.InputBindings.Clear();
            if (e.NewValue is IEnumerable<InputBinding> inputBindings)
            {
                foreach (InputBinding inputBinding in inputBindings)
                    uiElement.InputBindings.Add(inputBinding);
            }

            if (e.NewValue is ObservableCollectionEx<InputBinding> observableCollection)
            {
                observableCollection.AfterCollectionChanged += (s, e) =>
                {
                    uiElement.InputBindings.Clear();
                    foreach (InputBinding inputBinding in observableCollection)
                        uiElement.InputBindings.Add(inputBinding);
                };
            }
        }
    }
}
