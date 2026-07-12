using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace BlueprintEditorPlugin.Views.Wires
{
    /// <summary>
    /// Collects per-wire geometries from <see cref="BaseWire"/> instances and merges them into
    /// one <see cref="GeometryGroup"/> per brush so <see cref="ConnectionsBubbleOverlay"/> can
    /// draw all bubbles in a constant number of draw calls.
    /// </summary>
    public static class BubbleOverlayManager
    {
        private class Registration
        {
            public Geometry Geometry;
            public Brush Brush;
            public GeometryGroup Group;
        }

        private static readonly Dictionary<BaseWire, Registration> _registrations = new Dictionary<BaseWire, Registration>();
        private static readonly Dictionary<Brush, GeometryGroup> _groups = new Dictionary<Brush, GeometryGroup>();
        private static ConnectionsBubbleOverlay _overlay;

        /// <summary>
        /// Gets the merged geometry groups keyed by the brush used to stroke them.
        /// </summary>
        public static IEnumerable<KeyValuePair<Brush, GeometryGroup>> Groups => _groups;

        /// <summary>
        /// Sets the active overlay. Called when a <see cref="ConnectionsBubbleOverlay"/> is loaded.
        /// </summary>
        public static void SetOverlay(ConnectionsBubbleOverlay overlay)
        {
            _overlay = overlay;
            _overlay?.InvalidateVisual();
        }

        /// <summary>
        /// Releases the active overlay only if it matches the given instance.
        /// Called when a <see cref="ConnectionsBubbleOverlay"/> is unloaded.
        /// </summary>
        public static void ReleaseOverlay(ConnectionsBubbleOverlay overlay)
        {
            if (_overlay == overlay)
            {
                _overlay = null;
            }
        }

        /// <summary>
        /// Forces the overlay to repaint. Use when a registered geometry's contents changed
        /// but its instance and brush stayed the same.
        /// </summary>
        public static void InvalidateOverlay() => _overlay?.InvalidateVisual();

        /// <summary>
        /// Registers or updates the geometry a wire contributes to the shared overlay.
        /// </summary>
        public static void UpdateGeometry(BaseWire wire, Geometry geometry, Brush brush)
        {
            if (wire == null || geometry == null || brush == null)
                return;

            if (_registrations.TryGetValue(wire, out Registration existing))
            {
                if (ReferenceEquals(existing.Geometry, geometry) && ReferenceEquals(existing.Brush, brush))
                    return;

                existing.Group.Children.Remove(existing.Geometry);
                if (existing.Group.Children.Count == 0)
                {
                    _groups.Remove(existing.Brush);
                }
            }

            if (!_groups.TryGetValue(brush, out GeometryGroup group))
            {
                group = new GeometryGroup();
                _groups[brush] = group;
            }

            group.Children.Add(geometry);

            _registrations[wire] = new Registration
            {
                Geometry = geometry,
                Brush = brush,
                Group = group
            };

            _overlay?.InvalidateVisual();
        }

        /// <summary>
        /// Removes a wire's geometry from the shared overlay.
        /// </summary>
        public static void RemoveGeometry(BaseWire wire)
        {
            if (wire == null || !_registrations.TryGetValue(wire, out Registration existing))
                return;

            existing.Group.Children.Remove(existing.Geometry);
            if (existing.Group.Children.Count == 0)
            {
                _groups.Remove(existing.Brush);
            }

            _registrations.Remove(wire);
            _overlay?.InvalidateVisual();
        }
    }
}
