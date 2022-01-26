using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace StretchingWrapPanel
{
    public class StretchingWrapPanel : Panel
    {
        public static readonly DependencyProperty ItemWidthProperty = DependencyProperty.Register(nameof(ItemWidth),
                typeof(double), typeof(StretchingWrapPanel), new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsMeasure));

        [TypeConverter(typeof(LengthConverter))]
        public double ItemWidth
        {
            get => (double)GetValue(ItemWidthProperty);
            set => SetValue(ItemWidthProperty, value);
        }

        public static readonly DependencyProperty ItemHeightProperty = DependencyProperty.Register(nameof(ItemHeight),
                typeof(double), typeof(StretchingWrapPanel), new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsMeasure));

        [TypeConverter(typeof(LengthConverter))]
        public double ItemHeight
        {
            get => (double)GetValue(ItemHeightProperty);
            set => SetValue(ItemHeightProperty, value);
        }

        public static readonly DependencyProperty OrientationProperty = StackPanel.OrientationProperty.AddOwner(typeof(StretchingWrapPanel),
                new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsMeasure, OnOrientationChanged));

        public Orientation Orientation
        {
            get => _orientation;
            set => SetValue(OrientationProperty, value);
        }

        private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((StretchingWrapPanel)d)._orientation = (Orientation)e.NewValue;
        }

        private Orientation _orientation = Orientation.Horizontal;

        public static readonly DependencyProperty StretchProportionallyProperty = DependencyProperty.Register(nameof(StretchProportionally), typeof(bool),
                typeof(StretchingWrapPanel), new PropertyMetadata(true, OnStretchProportionallyChanged));

        public bool StretchProportionally
        {
            get => _stretchProportionally;
            set => SetValue(StretchProportionallyProperty, value);
        }

        private static void OnStretchProportionallyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((StretchingWrapPanel)o)._stretchProportionally = (bool)e.NewValue;
        }

        private bool _stretchProportionally = true;       

        protected override Size MeasureOverride(Size constraint)
        {
            var curLineSize = new UVSize(Orientation);
            var panelSize = new UVSize(Orientation);
            var uvConstraint = new UVSize(Orientation, constraint.Width, constraint.Height);
            var itemWidth = ItemWidth;
            var itemHeight = ItemHeight;
            var itemWidthSet = !double.IsNaN(itemWidth);
            var itemHeightSet = !double.IsNaN(itemHeight);

            var childConstraint = new Size(
                    itemWidthSet ? itemWidth : constraint.Width,
                    itemHeightSet ? itemHeight : constraint.Height);

            
            foreach (UIElement child in InternalChildren)
            {
                if (child == null) continue;

                // Flow passes its own constrint to children
                child.Measure(childConstraint);

                // This is the size of the child in UV space
                var sz = new UVSize(Orientation,
                        itemWidthSet ? itemWidth : child.DesiredSize.Width,
                        itemHeightSet ? itemHeight : child.DesiredSize.Height);

                if (curLineSize.U + sz.U > uvConstraint.U)
                {
                    // Need to switch to another line
                    panelSize.U = Math.Max(curLineSize.U, panelSize.U);
                    panelSize.V += curLineSize.V;
                    curLineSize = sz;

                    if (sz.U > uvConstraint.U)
                    {
                        // The element is wider then the constrint - give it a separate line             
                        panelSize.U = Math.Max(sz.U, panelSize.U);
                        panelSize.V += sz.V;
                        curLineSize = new UVSize(Orientation);
                    }
                }
                else
                {
                    // Continue to accumulate a line
                    curLineSize.U += sz.U;
                    curLineSize.V = Math.Max(sz.V, curLineSize.V);
                }
            }

            // The last line size, if any should be added
            panelSize.U = Math.Max(curLineSize.U, panelSize.U);
            panelSize.V += curLineSize.V;

            // Go from UV space to W/H space
            return new Size(panelSize.Width, panelSize.Height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var firstInLine = 0;
            var itemWidth = ItemWidth;
            var itemHeight = ItemHeight;
            double accumulatedV = 0;
            var itemU = Orientation.Equals(Orientation.Horizontal) ? itemWidth : itemHeight;
            var curLineSize = new UVSize(Orientation);
            var uvFinalSize = new UVSize(Orientation, finalSize.Width, finalSize.Height);
            var itemWidthSet = !double.IsNaN(itemWidth);
            var itemHeightSet = !double.IsNaN(itemHeight);
            var useItemU = Orientation.Equals(Orientation.Horizontal) ? itemWidthSet : itemHeightSet;


            foreach(UIElement child in InternalChildren)           
            {
                if (child == null) continue;

                var index = InternalChildren.IndexOf(child);

                var size = new UVSize(Orientation, itemWidthSet ? itemWidth : child.DesiredSize.Width, itemHeightSet ? itemHeight : child.DesiredSize.Height);
                if (curLineSize.U + size.U > uvFinalSize.U)
                {
                    // Need to switch to another line
                    if (!useItemU && StretchProportionally)
                    {
                        ArrangeLineProportionally(accumulatedV, curLineSize.V, firstInLine, index, uvFinalSize.Width);
                    }
                    else
                    {
                        ArrangeLine(accumulatedV, curLineSize.V, firstInLine, index, true, useItemU ? itemU : uvFinalSize.Width / Math.Max(1, index - firstInLine - 1));
                    }

                    accumulatedV += curLineSize.V;
                    curLineSize = size;

                    if (size.U > uvFinalSize.U)
                    {
                        // The element is wider then the constraint - give it a separate line     
                        // Switch to next line which only contain one element
                        if (!useItemU && StretchProportionally)
                        {
                            ArrangeLineProportionally(accumulatedV, size.V, index, ++index, uvFinalSize.Width);
                        }
                        else
                        {
                            ArrangeLine(accumulatedV, size.V, index, ++index, true, useItemU ? itemU : uvFinalSize.Width);
                        }

                        accumulatedV += size.V;
                        curLineSize = new UVSize(Orientation);
                    }
                    firstInLine = index;
                }
                else
                {
                    // Continue to accumulate a line
                    curLineSize.U += size.U;
                    curLineSize.V = Math.Max(size.V, curLineSize.V);
                }
            }

            // Arrange the last line, if any
            if (firstInLine < InternalChildren.Count)
            {
                if (!useItemU && StretchProportionally)
                {
                    ArrangeLineProportionally(accumulatedV, curLineSize.V, firstInLine, InternalChildren.Count, uvFinalSize.Width);
                }
                else
                {
                    ArrangeLine(accumulatedV, curLineSize.V, firstInLine, InternalChildren.Count, true,
                            useItemU ? itemU : uvFinalSize.Width / Math.Max(1, InternalChildren.Count - firstInLine - 1));
                }
            }

            return finalSize;
        }

        private void ArrangeLineProportionally(double v, double lineV, int start, int end, double limitU)
        {
            var u = 0d;
            var isHorizontal = Orientation.Equals(Orientation.Horizontal);
            var children = InternalChildren;

            var total = 0d;
            for (var i = start; i < end; i++)
            {
                total += isHorizontal ? children[i].DesiredSize.Width : children[i].DesiredSize.Height;
            }

            var uMultipler = limitU / total;
            for (var i = start; i < end; i++)
            {
                var child = children[i];
                if (child != null)
                {
                    var childSize = new UVSize(Orientation, child.DesiredSize.Width, child.DesiredSize.Height);
                    var layoutSlotU = childSize.U * uMultipler;
                    child.Arrange(new Rect(isHorizontal ? u : v, isHorizontal ? v : u,
                            isHorizontal ? layoutSlotU : lineV, isHorizontal ? lineV : layoutSlotU));
                    u += layoutSlotU;
                }
            }
        }

        private void ArrangeLine(double v, double lineV, int start, int end, bool useItemU, double itemU)
        {
            var u = 0d;
            var horizontal = Orientation.Equals(Orientation.Horizontal);
            var children = InternalChildren;
            for (var i = start; i < end; i++)
            {
                var child = children[i];
                if (child != null)
                {
                    var childSize = new UVSize(Orientation, child.DesiredSize.Width, child.DesiredSize.Height);
                    var layoutSlotU = useItemU ? itemU : childSize.U;
                    child.Arrange(new Rect(horizontal ? u : v, horizontal ? v : u,
                            horizontal ? layoutSlotU : lineV, horizontal ? lineV : layoutSlotU));
                    u += layoutSlotU;
                }
            }
        }
    }
}