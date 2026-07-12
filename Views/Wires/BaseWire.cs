using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using BlueprintEditorPlugin.Options;

namespace BlueprintEditorPlugin.Views.Wires
{
    /// <summary>
    /// Base implementation for a drawn Wire which goes from 1 point to another.
    /// </summary>
    public abstract class BaseWire : Shape
    {
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof(Point), typeof(BaseWire), new FrameworkPropertyMetadata(default(Point), FrameworkPropertyMetadataOptions.AffectsRender, OnGeometryInvalidatingPropertyChanged));
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register(nameof(Target), typeof(Point), typeof(BaseWire), new FrameworkPropertyMetadata(default(Point), FrameworkPropertyMetadataOptions.AffectsRender, OnGeometryInvalidatingPropertyChanged));

        public static readonly DependencyProperty ShowDirectionalBubblesProperty = DependencyProperty.Register(
            nameof(ShowDirectionalBubbles), typeof(bool), typeof(BaseWire),
            new FrameworkPropertyMetadata(false, OnShowDirectionalBubblesChanged));

        public static readonly DependencyProperty BubbleSpacingProperty = DependencyProperty.Register(
            nameof(BubbleSpacing), typeof(double), typeof(BaseWire),
            new FrameworkPropertyMetadata(24.0d, OnBubbleAppearanceChanged));

        public static readonly DependencyProperty BubbleSizeProperty = DependencyProperty.Register(
            nameof(BubbleSize), typeof(double), typeof(BaseWire),
            new FrameworkPropertyMetadata(6.0d, OnBubbleAppearanceChanged));

        public static readonly DependencyProperty BubbleAnimationDurationProperty = DependencyProperty.Register(
            nameof(BubbleAnimationDuration), typeof(double), typeof(BaseWire),
            new FrameworkPropertyMetadata(1.2d, OnBubbleAnimationDurationChanged));

        /// <summary>
        /// Gets or sets the start point of this wire.
        /// </summary>
        public Point Source
        {
            get => (Point)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        /// <summary>
        /// Gets or sets the end point of this wire.
        /// </summary>
        public Point Target
        {
            get => (Point)GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }

        /// <summary>
        /// Gets or sets whether animated directional bubbles are drawn on the wire.
        /// </summary>
        public bool ShowDirectionalBubbles
        {
            get => (bool)GetValue(ShowDirectionalBubblesProperty);
            set => SetValue(ShowDirectionalBubblesProperty, value);
        }

        /// <summary>
        /// Gets or sets the distance between directional bubbles in pixels.
        /// </summary>
        public double BubbleSpacing
        {
            get => (double)GetValue(BubbleSpacingProperty);
            set => SetValue(BubbleSpacingProperty, value);
        }

        /// <summary>
        /// Gets or sets the diameter of each directional bubble in pixels.
        /// </summary>
        public double BubbleSize
        {
            get => (double)GetValue(BubbleSizeProperty);
            set => SetValue(BubbleSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the duration in seconds for a bubble to travel one <see cref="BubbleSpacing"/>.
        /// </summary>
        public double BubbleAnimationDuration
        {
            get => (double)GetValue(BubbleAnimationDurationProperty);
            set => SetValue(BubbleAnimationDurationProperty, value);
        }

        private readonly StreamGeometry _geometry = new StreamGeometry
        {
            FillRule = FillRule.EvenOdd
        };

        private bool _geometryDirty = true;
        private Point _lastSource;
        private Point _lastTarget;

        /// <summary>
        /// Returns whether the cached geometry needs to be redrawn. Derived classes can override this
        /// to include additional dependency properties in the dirty check.
        /// </summary>
        protected virtual bool HasGeometryChanged()
        {
            return _geometryDirty || Source != _lastSource || Target != _lastTarget;
        }

        /// <summary>
        /// Updates the cached geometry state after a redraw. Derived classes overriding
        /// <see cref="HasGeometryChanged"/> should also override this to store their own state.
        /// </summary>
        protected virtual void UpdateGeometryState()
        {
            _lastSource = Source;
            _lastTarget = Target;
            _geometryDirty = false;
        }

        private Pen _bubblePen;
        private DashStyle _bubbleDashStyle;

        protected BaseWire()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private static void OnGeometryInvalidatingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BaseWire)d)._geometryDirty = true;
        }

        private static void OnShowDirectionalBubblesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var wire = (BaseWire)d;
            if ((bool)e.NewValue)
                wire.StartBubbleAnimation();
            else
                wire.StopBubbleAnimation();
        }

        private static void OnBubbleAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var wire = (BaseWire)d;
            wire._bubblePen = null;
            wire._bubbleDashStyle = null;
            if (wire.ShowDirectionalBubbles)
                wire.StartBubbleAnimation();
        }

        private static void OnBubbleAnimationDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var wire = (BaseWire)d;
            if (wire.ShowDirectionalBubbles)
                wire.StartBubbleAnimation();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ShowDirectionalBubbles = EditorOptions.AnimatedDirectionalBubbles;
            EditorOptions.Updated += OnEditorOptionsUpdated;

            if (ShowDirectionalBubbles)
                StartBubbleAnimation();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            EditorOptions.Updated -= OnEditorOptionsUpdated;
            StopBubbleAnimation();
        }

        private void OnEditorOptionsUpdated()
        {
            ShowDirectionalBubbles = EditorOptions.AnimatedDirectionalBubbles;
        }

        private void StartBubbleAnimation()
        {
            StopBubbleAnimation();

            if (BubbleSpacing <= 0 || BubbleAnimationDuration <= 0)
                return;

            var animation = new DoubleAnimation
            {
                From = 0,
                To = -BubbleSpacing,
                Duration = TimeSpan.FromSeconds(BubbleAnimationDuration),
                RepeatBehavior = RepeatBehavior.Forever
            };

            BeginAnimation(StrokeDashOffsetProperty, animation);
        }

        private void StopBubbleAnimation()
        {
            BeginAnimation(StrokeDashOffsetProperty, null);
            StrokeDashOffset = 0;
        }

        /// <summary>
        /// Invalidates the cached wire geometry so it is redrawn on the next render pass.
        /// </summary>
        protected void InvalidateGeometry()
        {
            _geometryDirty = true;
            InvalidateVisual();
        }

        protected override Geometry DefiningGeometry
        {
            get
            {
                if (!HasGeometryChanged())
                    return _geometry;

                using (StreamGeometryContext context = _geometry.Open())
                {
                    DrawWire(context);
                }

                UpdateGeometryState();

                return _geometry;
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (!ShowDirectionalBubbles || BubbleSize <= 0 || BubbleSpacing <= 0 || Stroke == null)
                return;

            if (_bubblePen == null)
            {
                _bubbleDashStyle = new DashStyle(new DoubleCollection { 0, BubbleSpacing }, StrokeDashOffset);
                _bubblePen = new Pen(Stroke, BubbleSize)
                {
                    DashStyle = _bubbleDashStyle,
                    DashCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Round,
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round
                };
            }
            else
            {
                _bubbleDashStyle.Offset = StrokeDashOffset;
            }

            drawingContext.DrawGeometry(null, _bubblePen, DefiningGeometry);
        }

        protected abstract void DrawWire(StreamGeometryContext context);
    }
}
