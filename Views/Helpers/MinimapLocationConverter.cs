using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BlueprintEditorPlugin.Views.Helpers
{
    /// <summary>
    /// Converts a node location to a minimap position by scaling it based on the minimap size and total graph bounds.
    /// </summary>
    public class MinimapLocationConverter : IMultiValueConverter
    {
        public static MinimapLocationConverter Instance { get; } = new MinimapLocationConverter();

        public MinimapLocationConverter() { }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 5)
                return new Point(0, 0);

            if (values[0] is Point nodeLocation &&
                values[1] is Rect bounds &&
                values[2] is double minimapWidth &&
                values[3] is double minimapHeight &&
                values[4] is Point viewportLocation)
            {
                if (bounds.Width <= 0 || bounds.Height <= 0 || minimapWidth <= 0 || minimapHeight <= 0)
                    return new Point(0, 0);

                // Normalize node position within bounds, then scale to minimap
                double normalizedX = (nodeLocation.X - bounds.X) / bounds.Width;
                double normalizedY = (nodeLocation.Y - bounds.Y) / bounds.Height;

                return new Point(normalizedX * minimapWidth, normalizedY * minimapHeight);
            }

            return new Point(0, 0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}