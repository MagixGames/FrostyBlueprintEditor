using System.Windows;

namespace BlueprintEditorPlugin.Views.Events
{
    /// <summary>
    /// Provides data for zoom events.
    /// </summary>
    public class ZoomEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Gets the zoom factor.
        /// </summary>
        public double Zoom { get; }

        /// <summary>
        /// Gets the location in graph space coordinates where the zoom occurred.
        /// </summary>
        public Point Location { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZoomEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="source">The source.</param>
        /// <param name="zoom">The zoom factor.</param>
        /// <param name="location">The location.</param>
        public ZoomEventArgs(RoutedEvent routedEvent, object source, double zoom, Point location)
            : base(routedEvent, source)
        {
            Zoom = zoom;
            Location = location;
        }
    }
}