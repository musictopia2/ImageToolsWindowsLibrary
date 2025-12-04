namespace ImageToolsWindowsLibrary;
public static class RectangleExtensions
{
    extension (Rectangle payLoad)
    {
        /// <summary>
        /// Gets the remaining area of the suggestion rectangle after subtracting the manual one vertically.
        /// Optionally applies a vertical offset between them.
        /// </summary>
        public Rectangle GetRemainingBelow(Rectangle manual, int verticalOffset = 0)
        {
            // Ensure overlap in horizontal space, otherwise invalid for column-style layout
            if (payLoad.IntersectsWith(manual) == false)
            {
                throw new CustomBasicException("No overlap");
            }

            int topY = manual.Bottom + verticalOffset;
            int maxY = payLoad.Bottom;

            if (topY >= maxY)
            {
                throw new CustomBasicException("Invalid manual rectangle");
            }

            int height = maxY - topY;
            return new Rectangle(payLoad.X, topY, payLoad.Width, height);
        }
        public Rectangle TrimFromTop(int trimHeight)
        {
            if (trimHeight < 0 || trimHeight >= payLoad.Height)
            {
                throw new ArgumentOutOfRangeException(nameof(trimHeight));
            }

            return new Rectangle(
                payLoad.X,
                payLoad.Y + trimHeight,
                payLoad.Width,
                payLoad.Height - trimHeight
            );
        }
        /// <summary>
        /// Returns a new rectangle widened by <paramref name="widthRequested"/> pixels to the right.
        /// If <paramref name="widthRequested"/> is 0, returns the original rectangle.
        /// Throws if <paramref name="widthRequested"/> is less than 0.
        /// </summary>
        /// <param name="widthRequested">Number of pixels to widen to the right</param>
        /// <returns>New widened rectangle</returns>
        public Rectangle WidenToRight(int widthRequested)
        {
            if (widthRequested < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(widthRequested), "Width requested must be zero or positive.");
            }

            if (widthRequested == 0)
            {
                return payLoad;
            }

            return new Rectangle(
                payLoad.X,
                payLoad.Y,
                payLoad.Width + widthRequested,
                payLoad.Height
            );
        }
        /// <summary>
        /// Returns a new rectangle with its height adjusted from the bottom by <paramref name="heightDelta"/>.
        /// Positive values extend the height downward.
        /// Negative values trim the height upward from the bottom.
        /// Throws if the resulting height is less than 1.
        /// </summary>
        /// <param name="heightDelta">Amount to adjust height by (positive to extend, negative to trim)</param>
        /// <returns>Adjusted rectangle</returns>
        public Rectangle AdjustHeightFromBottom(int heightDelta)
        {
            int newHeight = payLoad.Height + heightDelta;

            if (newHeight < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(heightDelta), "Resulting height must be at least 1.");
            }

            return new Rectangle(
                payLoad.X,
                payLoad.Y,
                payLoad.Width,
                newHeight
            );
        }
    }
    
}