using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BlueprintEditorPlugin.Views.Helpers
{
    /// <summary>
    /// Converts a vertex location and size to minimap coordinates.
    /// Receives: [0] = Vertex (with Location), [1] = GraphBounds (Rect), [2] = MinimapSize (Size)
    /// Returns: Point for Canvas.Left/Top or Size for Width/Height
    /// </summary>
    public class MinimapItemPositionConverter : IMultiValueConverter
    {
        public MinimapItemPositionConverter() { }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3)
                return new Point(0, 0);

            if (values[0] == DependencyProperty.UnsetValue || values[1] == DependencyProperty.UnsetValue || values[2] == DependencyProperty.UnsetValue)
                return new Point(0, 0);

            object vertex = values[0];
            Rect graphBounds;
            Size minimapSize;

            if (values[1] is Rect bounds)
                graphBounds = bounds;
            else
                return new Point(0, 0);

            if (values[2] is Size size)
                minimapSize = size;
            else
                return new Point(0, 0);

            // Get location and size from vertex
            Point location = GetLocation(vertex);
            Size nodeSize = GetSize(vertex);

            if (graphBounds.Width <= 0 || graphBounds.Height <= 0)
                return new Point(0, 0);

            // Add padding to graph bounds
            double padding = 100;
            graphBounds.Inflate(padding, padding);

            // Normalize to minimap coordinates
            double normalizedX = (location.X - graphBounds.Left) / graphBounds.Width;
            double normalizedY = (location.Y - graphBounds.Top) / graphBounds.Height;

            // Also normalize node size
            double normalizedWidth = nodeSize.Width / graphBounds.Width;
            double normalizedHeight = nodeSize.Height / graphBounds.Height;

            if (parameter != null && parameter.ToString() == "Size")
            {
                return new Size(
                    Math.Max(4, normalizedWidth * minimapSize.Width),
                    Math.Max(3, normalizedHeight * minimapSize.Height)
                );
            }

            return new Point(
                normalizedX * minimapSize.Width,
                normalizedY * minimapSize.Height
            );
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private Point GetLocation(object vertex)
        {
            var locationProperty = vertex.GetType().GetProperty("Location");
            if (locationProperty != null)
            {
                var result = locationProperty.GetValue(vertex);
                if (result is Point p)
                    return p;
            }
            return new Point(0, 0);
        }

        private Size GetSize(object vertex)
        {
            var sizeProperty = vertex.GetType().GetProperty("Size");
            if (sizeProperty != null)
            {
                var result = sizeProperty.GetValue(vertex);
                if (result is Size s)
                    return s;
            }
            return new Size(50, 30);
        }
    }
}