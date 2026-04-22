using System.Windows;
using System.Windows.Controls;

namespace dnGREP.WPF
{
    /// <summary>
    /// Attached properties for themed menu controls.
    /// </summary>
    public static class MenuProperties
    {
        /// <summary>
        /// Gets or sets whether the icon/glyph column (column 0) is visible
        /// in the child MenuItems of a Menu or ContextMenu.
        /// Set this on the parent Menu or ContextMenu.
        /// </summary>
        public static readonly DependencyProperty ShowIconColumnProperty =
            DependencyProperty.RegisterAttached(
                "ShowIconColumn",
                typeof(bool),
                typeof(MenuProperties),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));

        public static bool GetShowIconColumn(DependencyObject obj)
            => (bool)obj.GetValue(ShowIconColumnProperty);

        public static void SetShowIconColumn(DependencyObject obj, bool value)
            => obj.SetValue(ShowIconColumnProperty, value);
    }
}