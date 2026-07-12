using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using BlueprintEditorPlugin.Options;

namespace BlueprintEditorPlugin.Views.Wires
{
    /// <summary>
    /// A basic wire, which follows the <see cref="EditorOptions"/> set by the user
    /// </summary>
    public class Wire : BaseWire
    {
        private ConnectionStyle _lastWireStyle;

        protected override bool HasGeometryChanged()
        {
            return base.HasGeometryChanged() || EditorOptions.WireStyle != _lastWireStyle;
        }

        protected override void UpdateGeometryState()
        {
            base.UpdateGeometryState();
            _lastWireStyle = EditorOptions.WireStyle;
        }

        protected override void DrawWire(StreamGeometryContext context)
        {
            switch (EditorOptions.WireStyle)
            {
                case ConnectionStyle.Curvy:
                {
                    context.BeginFigure(Source, false, false);
                    double offset = Math.Max(40, Math.Min(150, Math.Abs(Target.X - Source.X) * 0.5));
                    Point curve1 = new Point(Source.X + offset, Source.Y);
                    Point curve2 = new Point(Target.X - offset, Target.Y);
                    
                    context.PolyBezierTo(new List<Point> {curve1, curve2, Target}, true, false);
                } break;
                case ConnectionStyle.Straight:
                {
                    context.BeginFigure(Source, false, false);
                    context.LineTo(Target, true, false);
                } break;
                case ConnectionStyle.StartStop:
                {
                    context.BeginFigure(Source, false, false);
                    double offset = Math.Max(40, Math.Min(150, Math.Abs(Target.X - Source.X) * 0.5));
                    Point curve1 = new Point(Source.X + offset, Source.Y);
                    context.LineTo(curve1, true, false);
            
                    Point curve2 = new Point(Target.X - offset, Target.Y);
                    context.LineTo(curve2, true, false);
                    context.LineTo(Target, true, false);
                } break;
            }
        }
    }
}