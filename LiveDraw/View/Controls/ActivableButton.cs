﻿using System.Windows;
using System.Windows.Controls;

namespace AntFu7.LiveDraw
{
    internal class ActivableButton : Button
    {
        public static readonly DependencyProperty IsActivedProperty = DependencyProperty.Register(
            "IsActived", typeof(bool), typeof(ActivableButton), new FrameworkPropertyMetadata(default(bool))
            { AffectsRender = true, DefaultUpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged });

        public bool IsActived
        {
            get { return (bool)GetValue(IsActivedProperty); }
            set { SetValue(IsActivedProperty, value); }
        }
    }
}
