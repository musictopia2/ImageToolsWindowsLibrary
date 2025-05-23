namespace ImageToolsWindowsLibrary;
public class ImageHighlightState
{
    public string Path { get; set; } = "";
    public Rectangle CropArea { get; set; }
    public Rectangle OriginalHighlight { get; set; }
    public Rectangle CurrentHighlight { get; set; }
}