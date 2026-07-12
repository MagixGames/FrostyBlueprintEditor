using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BlueprintEditorPlugin.Views.Events;
using BlueprintEditorPlugin.Views.Helpers;

namespace BlueprintEditorPlugin.Views.Nodes
{
    /// <summary>
    /// Represents a minimap control that provides a synchronized miniature view of items in a NodifyEditor.
    /// </summary>
    public class Minimap : Control
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(Minimap));
        public static readonly DependencyProperty ViewportLocationProperty = DependencyProperty.Register(nameof(ViewportLocation), typeof(Point), typeof(Minimap), new FrameworkPropertyMetadata(BoxValue.Point, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnViewportChanged));
        public static readonly DependencyProperty ViewportSizeProperty = DependencyProperty.Register(nameof(ViewportSize), typeof(Size), typeof(Minimap), new FrameworkPropertyMetadata(BoxValue.Size, OnViewportChanged));
        public static readonly DependencyProperty ViewportStyleProperty = DependencyProperty.Register(nameof(ViewportStyle), typeof(Style), typeof(Minimap));
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(Minimap), new PropertyMetadata(BoxValue.False));
        public static readonly DependencyProperty MaxViewportOffsetProperty = DependencyProperty.Register(nameof(MaxViewportOffset), typeof(double), typeof(Minimap), new PropertyMetadata(50.0));
        public static readonly DependencyProperty ResizeToViewportProperty = DependencyProperty.Register(nameof(ResizeToViewport), typeof(bool), typeof(Minimap), new PropertyMetadata(BoxValue.True));
        public static readonly DependencyProperty MinimapItemTemplateProperty = DependencyProperty.Register(nameof(MinimapItemTemplate), typeof(DataTemplate), typeof(Minimap));
        public static readonly DependencyProperty ZoomCommandProperty = DependencyProperty.Register(nameof(ZoomCommand), typeof(ICommand), typeof(Minimap));

        public static readonly RoutedEvent ZoomEvent = EventManager.RegisterRoutedEvent(nameof(Zoom), RoutingStrategy.Bubble, typeof(EventHandler<ZoomEventArgs>), typeof(Minimap));

        private Point _dragStartPoint;
        private Point _viewportStartLocation;
        private bool _isDraggingViewport;

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public Point ViewportLocation
        {
            get => (Point)GetValue(ViewportLocationProperty);
            set => SetValue(ViewportLocationProperty, value);
        }

        public Size ViewportSize
        {
            get => (Size)GetValue(ViewportSizeProperty);
            set => SetValue(ViewportSizeProperty, value);
        }

        public Style ViewportStyle
        {
            get => (Style)GetValue(ViewportStyleProperty);
            set => SetValue(ViewportStyleProperty, value);
        }

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public double MaxViewportOffset
        {
            get => (double)GetValue(MaxViewportOffsetProperty);
            set => SetValue(MaxViewportOffsetProperty, value);
        }

        public bool ResizeToViewport
        {
            get => (bool)GetValue(ResizeToViewportProperty);
            set => SetValue(ResizeToViewportProperty, value);
        }

        public DataTemplate MinimapItemTemplate
        {
            get => (DataTemplate)GetValue(MinimapItemTemplateProperty);
            set => SetValue(MinimapItemTemplateProperty, value);
        }

        public ICommand ZoomCommand
        {
            get => (ICommand)GetValue(ZoomCommandProperty);
            set => SetValue(ZoomCommandProperty, value);
        }

        public event EventHandler<Views.Events.ZoomEventArgs> Zoom
        {
            add => AddHandler(ZoomEvent, value);
            remove => RemoveHandler(ZoomEvent, value);
        }

        static Minimap()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Minimap), new FrameworkPropertyMetadata(typeof(Minimap)));
        }

        public Minimap()
        {
            ClipToBounds = true;
        }

        private static void OnViewportChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Minimap)d).InvalidateArrange();
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (IsReadOnly)
                return;

            _isDraggingViewport = true;
            _dragStartPoint = e.GetPosition(this);
            _viewportStartLocation = ViewportLocation;
            CaptureMouse();
            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isDraggingViewport && !IsReadOnly)
            {
                Point currentPoint = e.GetPosition(this);
                Vector delta = currentPoint - _dragStartPoint;

                // Scale the delta based on the ratio of minimap size to actual content size
                double scaleX = ActualWidth > 0 ? ViewportSize.Width / ActualWidth : 1;
                double scaleY = ActualHeight > 0 ? ViewportSize.Height / ActualHeight : 1;

                ViewportLocation = new Point(
                    _viewportStartLocation.X - delta.X * scaleX,
                    _viewportStartLocation.Y - delta.Y * scaleY
                );

                e.Handled = true;
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            if (_isDraggingViewport)
            {
                _isDraggingViewport = false;
                ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            if (IsReadOnly)
                return;

            Point location = e.GetPosition(this);
            double zoom = Math.Pow(2.0, e.Delta / 3.0 / Mouse.MouseWheelDeltaForOneLine);

            ZoomCommand?.Execute(new Views.Events.ZoomEventArgs(ZoomEvent, this, zoom, location));
            RaiseEvent(new Views.Events.ZoomEventArgs(ZoomEvent, this, zoom, location));

            e.Handled = true;
        }
    }
}