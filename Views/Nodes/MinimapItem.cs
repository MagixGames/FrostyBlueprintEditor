using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlueprintEditorPlugin.Views.Helpers;

namespace BlueprintEditorPlugin.Views.Nodes
{
    /// <summary>
    /// Represents an item in the <see cref="Minimap"/>.
    /// </summary>
    public class MinimapItem : Control
    {
        public static readonly DependencyProperty LocationProperty = DependencyProperty.Register(nameof(Location), typeof(Point), typeof(MinimapItem), new PropertyMetadata(BoxValue.Point));
        public static new readonly DependencyProperty WidthProperty = DependencyProperty.Register(nameof(Width), typeof(double), typeof(MinimapItem), new PropertyMetadata(10.0));
        public static new readonly DependencyProperty HeightProperty = DependencyProperty.Register(nameof(Height), typeof(double), typeof(MinimapItem), new PropertyMetadata(10.0));

        public Point Location
        {
            get => (Point)GetValue(LocationProperty);
            set => SetValue(LocationProperty, value);
        }

        public new double Width
        {
            get => (double)GetValue(WidthProperty);
            set => SetValue(WidthProperty, value);
        }

        public new double Height
        {
            get => (double)GetValue(HeightProperty);
            set => SetValue(HeightProperty, value);
        }

        static MinimapItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MinimapItem), new FrameworkPropertyMetadata(typeof(MinimapItem)));
        }
    }
}