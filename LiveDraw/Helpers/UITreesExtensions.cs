using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace AntFu7.LiveDraw.Helpers
{
    public static class UITreesExtensions
    {
        public static T FindVisualDescendant<T>(this DependencyObject parent, Predicate<T> predicate)
            where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                var result = (child as T) ?? FindVisualDescendant<T>(child, predicate);
                if (result != null && predicate(result)) return result;
            }
            return null;
        }
    }
}
