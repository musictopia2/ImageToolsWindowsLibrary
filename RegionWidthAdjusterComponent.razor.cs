using System.ComponentModel;

namespace ImageToolsWindowsLibrary;
public partial class RegionWidthAdjusterComponent
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Parameter]
    public string ImagePath { get; set; } = string.Empty;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Parameter]
    public Rectangle RegionBounds { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Parameter]
    public int InitialExtraWidth { get; set; } //make it flexible enough so can decide if you do extras or not.

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Parameter]
    public EventCallback<int> OnSave { get; set; } //0 means everything was fine.
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RenderFragment? ChildContent { get; set; }
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ContainerWidth { get; set; } = "40vw"; // default or user-specified

    private Rectangle _previousBounds;
    private AppKeyboardListener? _keys;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public static System.Windows.Window? MainWindow { get; set; }
    private int _widthRequested;
    private readonly ImageCropHelper _cropHelper = new();
    private string? _lastImagePath = null;
    private string? _adjustedRegionImageData;
    private string? _referenceExpandedImageData;
    protected override void OnInitialized()
    {
        if (AppKeyboardListener.MainWindow is null && MainWindow is not null)
        {
            AppKeyboardListener.MainWindow = MainWindow;
        }
        _keys = new();
        _keys.KeyUp += Keys_KeyUp;
        base.OnInitialized();
    }
    private void Keys_KeyUp(EnumKey key)
    {

        switch (key)
        {
            case EnumKey.Left:
                if (_widthRequested == 0)
                {
                    return; //you cannot go lower than the original size.
                }
                _widthRequested--;
                PopulateAdjustedRegions();
                break;
            case EnumKey.Right:
                _widthRequested++;
                PopulateAdjustedRegions();
                break;
            
            case EnumKey.F1:
                OnSave.InvokeAsync(_widthRequested);
                break;
        }
    }
    protected override void OnParametersSet()
    {
        bool regionChanged = _previousBounds != RegionBounds;
        if (!string.IsNullOrWhiteSpace(ImagePath) &&
            File.Exists(ImagePath) &&
            ImagePath != _lastImagePath) // only reload if path changes
        {
            _lastImagePath = ImagePath;
            _cropHelper.LoadImage(_lastImagePath);
            PopulateInitialRegions();
            return;
        }
        if (regionChanged)
        {
            PopulateInitialRegions();
        }
    }

    private void PopulateInitialRegions()
    {
        var referenceRegion = new Rectangle(
            RegionBounds.X,
            RegionBounds.Y,
            RegionBounds.Width + InitialExtraWidth,
            RegionBounds.Height
        );
        _referenceExpandedImageData = _cropHelper.CropImageBase64(referenceRegion);
        // Also initialize adjusted region (starts with zero extra)
        _adjustedRegionImageData = _cropHelper.CropImageBase64(RegionBounds);

        // Reset width tracking
        _widthRequested = 0;
        _previousBounds = RegionBounds;

    }
    private void PopulateAdjustedRegions()
    {
        Rectangle bounds = new()
        {
            X = _previousBounds.X,
            Y = _previousBounds.Y,
            Width = _previousBounds.Width + _widthRequested,
            Height = _previousBounds.Height
        };
        _adjustedRegionImageData = _cropHelper.CropImageBase64(bounds);
        StateHasChanged();
    }
}