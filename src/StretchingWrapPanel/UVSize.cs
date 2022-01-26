using System.Windows.Controls;

namespace StretchingWrapPanel
{
    internal struct UVSize
    {
        internal UVSize(Orientation orientation, double width, double height)
        {
            U = V = 0d;
            _orientation = orientation;
            Width = width;
            Height = height;
        }

        internal UVSize(Orientation orientation)
        {
            U = V = 0d;
            _orientation = orientation;
        }

        internal double U;
        internal double V;
        private readonly Orientation _orientation;

        internal double Width
        {
            get => _orientation.Equals(Orientation.Horizontal) ? U : V;
            set
            {
                if (_orientation.Equals(Orientation.Horizontal))
                {
                    U = value;
                }
                else
                {
                    V = value;
                }
            }
        }

        internal double Height
        {
            get => _orientation.Equals(Orientation.Horizontal) ? V : U;
            set
            {
                if (_orientation.Equals(Orientation.Horizontal))
                {
                    V = value;
                }
                else
                {
                    U = value;
                }
            }
        }
    }
}
