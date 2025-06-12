namespace ImageToolsWindowsLibrary;
public partial class MultiRegionViewerComponent
{
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ImagePath { get; set; } = "";
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public BasicList<Rectangle> Regions { get; set; } = [];
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public EnumRegionLayoutMode LayoutMode { get; set; }
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RenderFragment? ChildContent { get; set; }

    string _desiredImagePath = "";
    private string? _lastImagePath = null;
    private readonly ImageCropHelper _cropHelper = new();
    protected override void OnParametersSet()
    {
        _desiredImagePath = ImagePath;
        if (!string.IsNullOrWhiteSpace(_desiredImagePath) &&
            File.Exists(_desiredImagePath) &&
            _desiredImagePath != _lastImagePath) // only reload if path changes
        {
            _lastImagePath = _desiredImagePath;
            _cropHelper.LoadImage(_desiredImagePath);
        }
    }
    private string GetRegionImageBase64(Rectangle bounds)
    {
        return _cropHelper.CropImageBase64(bounds);
    }
}