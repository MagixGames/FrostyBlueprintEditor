using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BlueprintEditorPlugin.Views.Helpers
{
    /// <summary>
    /// Scales a single coordinate value to minimap coordinates.
    /// Uses ItemsExtent from Editor and calculates scaling based on typical graph bounds.
    /// Receives: [0] = coordinate value (double)
    /// </summary>
    public class MinimapCoordConverter : IMultiValueConverter
    {
        private static Rect s_graphBounds = new Rect(0, 0, 2000, 1500);
        private static Size s_minimapSize = new Size(250, 180);

        public static void UpdateBounds(Rect bounds, Size minimapSize)
        {
            s_graphBounds = bounds;
            s_minimapSize = minimapSize;
        }

        public static MinimapCoordConverter Instance { get; } = new MinimapCoordConverter();

        public MinimapCoordConverter() { }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 1)
                return 0.0;

            if (values[0] == DependencyProperty.UnsetValue)
                return 0.0;

            if (!double.TryParse(values[0].ToString(), out double coord))
                return 0.0;

            bool isY = parameter != null && parameter.ToString() == "Y";

            if (s_graphBounds.Width <= 0 || s_graphBounds.Height <= 0)
                return 0.0;

            double padding = 100;
            double minBound = isY ? s_graphBounds.Top - padding : s_graphBounds.Left - padding;
            double range = isY ? s_graphBounds.Height + 2 * padding : s_graphBounds.Width + 2 * padding;
            double minimapSize = isY ? s_minimapSize.Height : s_minimapSize.Width;

            double result = ((coord - minBound) / range) * minimapSize;
            return Math.Max(0, result);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Scales viewport rectangle properties to minimap coordinates.
    /// Uses static bounds from MinimapCoordConverter for consistency.
    /// Receives: [0] = viewport value (X, Y, Width, or Height)
    /// </summary>
    public class MinimapViewportCoordConverter : IMultiValueConverter
    {
        public static MinimapViewportCoordConverter Instance { get; } = new MinimapViewportCoordConverter();

        public MinimapViewportCoordConverter() { }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 1)
                return 0.0;

            if (values[0] == DependencyProperty.UnsetValue)
                return 0.0;

            if (!double.TryParse(values[0].ToString(), out double viewportValue))
                return 0.0;

            Rect bounds = MinimapCoordConverter.Instance.Convert(new object[0], typeof(Rect), null, culture) is Rect r ? r : new Rect(0, 0, 2000, 1500);
            Size minimapSize = new Size(250, 180);

            string param = parameter?.ToString() ?? "X";

            double padding = 100;
            bounds.Inflate(padding, padding);

            if (param == "X")
                return ((viewportValue - bounds.Left) / bounds.Width) * minimapSize.Width;

            if (param == "Y")
                return ((viewportValue - bounds.Top) / bounds.Height) * minimapSize.Height;

            if (param == "Width")
                return Math.Max(10, (viewportValue / bounds.Width) * minimapSize.Width);

            if (param == "Height")
                return Math.Max(8, (viewportValue / bounds.Height) * minimapSize.Height);

            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}