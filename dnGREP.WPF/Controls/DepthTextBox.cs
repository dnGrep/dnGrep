using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace dnGREP.WPF
{
    public class DepthTextBox : TextBox
    {
        public DepthTextBox()
            : base()
        {
            DataObject.AddPastingHandler(this, PastingHandler);
        }

        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            if (e.Text == "0")
            {
                if (e.Source is TextBox tb && string.IsNullOrEmpty(tb.Text))
                {
                    // disallow a depth of zero
                    e.Handled = true;
                }
            }

            base.OnPreviewTextInput(e);
        }
        
        private void PastingHandler(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string? text = e.DataObject.GetData(typeof(string)) as string;
                if (!string.IsNullOrEmpty(text) && int.TryParse(text, out int result) && result == 0)
                {
                    e.CancelCommand();
                }
            }
        }
       
    }
}
