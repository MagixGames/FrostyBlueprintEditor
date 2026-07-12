using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BlueprintEditorPlugin.Views.Helpers
{
    public class MinimapViewportConverter : IMultiValueConverter
    {
        public static MinimapViewportConverter Instance { get; } = new MinimapViewportConverter();

        public MinimapViewportConverter() { }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 4)
                return new Rect(0, 0, 50, 30);

            if (values[0] is Point viewportLocation &&
                values[1] is Size viewportSize &&
                values[2] is Rect graphBounds &&
                values[3] is Size minimapSize)
            {
                if (graphBounds.Width <= 0 || graphBounds.Height <= 0)
                    return new Rect(0, 0, 50, 30);

                double padding = 100;
                graphBounds.Inflate(padding, padding);

                double normalizedX = (viewportLocation.X - graphBounds.Left) / graphBounds.Width;
                double normalizedY = (viewportLocation.Y - graphBounds.Top) / graphBounds.Height;

                double normalizedWidth = viewportSize.Width / graphBounds.Width;
                double normalizedHeight = viewportSize.Height / graphBounds.Height;

                if (parameter != null && parameter.ToString() == "Left")
                    return normalizedX * minimapSize.Width;

                if (parameter != null && parameter.ToString() == "Top")
                    return normalizedY * minimapSize.Height;

                if (parameter != null && parameter.ToString() == "Width")
                    return Math.Max(10, normalizedWidth * minimapSize.Width);

                if (parameter != null && parameter.ToString() == "Height")
                    return Math.Max(8, normalizedHeight * minimapSize.Height);

                return new Rect(
                    normalizedX * minimapSize.Width,
                    normalizedY * minimapSize.Height,
                    Math.Max(10, normalizedWidth * minimapSize.Width),
                    Math.Max(8, normalizedHeight * minimapSize.Height)
                );
            }

            return new Rect(0, 0, 50, 30);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}