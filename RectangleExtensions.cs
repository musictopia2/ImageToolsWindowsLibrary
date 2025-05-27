namespace ImageToolsWindowsLibrary;
public static class RectangleExtensions
{
    /// <summary>
    /// Gets the remaining area of the suggestion rectangle after subtracting the manual one vertically.
    /// Optionally applies a vertical offset between them.
    /// </summary>
    public static Rectangle GetRemainingBelow(this Rectangle suggestion, Rectangle manual, int verticalOffset = 0)
    {
        // Ensure overlap in horizontal space, otherwise invalid for column-style layout
        if (!suggestion.IntersectsWith(manual))
        {
            throw new CustomBasicException("No overlap");
        }

        int topY = manual.Bottom + verticalOffset;
        int maxY = suggestion.Bottom;

        if (topY >= maxY)
        {
            throw new CustomBasicException("Invalid manual rectangle");
        }

        int height = maxY - topY;
        return new Rectangle(suggestion.X, topY, suggestion.Width, height);
    }
}