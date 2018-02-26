using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GUI.Replaces.EditDialog
{
    public static class ColumnTag
    {

        public static readonly DependencyProperty TagProperty = DependencyProperty.RegisterAttached(
            "Tag",
            typeof(object),
            typeof(ColumnTag),
            new FrameworkPropertyMetadata(null));

        public static object GetTag(DependencyObject dependencyObject)
        {
            return dependencyObject.GetValue(TagProperty);
        }

        public static void SetTag(DependencyObject dependencyObject, object value)
        {
            dependencyObject.SetValue(TagProperty, value);
        }

    }
}
