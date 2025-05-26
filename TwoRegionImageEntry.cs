namespace ImageToolsWindowsLibrary;
public class TwoRegionImageEntry
{
    public string ImageFile { get; set; } = "";
    public Rectangle? FirstRegion { get; set; }
    public Rectangle? SecondRegion { get; set; }
}