namespace ImageToolsWindowsLibrary;
public partial class TopRegionCropSelectorComponent
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Parameter]
    public string ImagePath { get; set; } = string.Empty;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Parameter]
    public Rectangle RegionBounds { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Parameter]
    public int? SuggestedTrimHeight { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Parameter]
    public bool ShowRetainedOnly { get; set; } = false;

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RenderFragment? ChildContent { get; set; }


    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int? CropHeight { get; set; }

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public EventCallback<int?> CropHeightChanged { get; set; }


    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public EventCallback OnSave { get; set; }

    private readonly ImageCropHelper _cropHelper = new();
    private string? _lastImagePath = null;

    //when doing for reals, will set the suggestion to 60.
    //can change to what it really is.


    private string? _desiredImagedData = null;
    //private int _naturalImageWidth;
    //private int _naturalImageHeight;

    private AppKeyboardListener? _keys;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public static System.Windows.Window? MainWindow { get; set; }
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
        if (CropHeight is null)
        {
            return; // no regions selected yet
        }
        switch (key)
        {
            case EnumKey.Down:
                DownArrowClicked();
                break;
            case EnumKey.Up:
                UpArrowClicked();
                break;
            case EnumKey.F1:
                OnSave.InvokeAsync();
                break;
        }
    }
    private void DownArrowClicked()
    {
        CropHeight++;
        CropHeightChanged.InvokeAsync(CropHeight);
        StateHasChanged();
    }
    private void UpArrowClicked()
    {
        CropHeight--;
        CropHeightChanged.InvokeAsync(CropHeight);
        StateHasChanged();
    }
    private Rectangle _previousBounds;
    protected override Task OnParametersSetAsync()
    {
        if (_previousBounds != RegionBounds)
        {
            _previousBounds = RegionBounds;
            CropHeight = SuggestedTrimHeight;
        }
        if (!string.IsNullOrWhiteSpace(ImagePath) &&
            File.Exists(ImagePath) &&
            ImagePath != _lastImagePath) // only reload if path changes
        {
            _lastImagePath = ImagePath;
            _cropHelper.LoadImage(_lastImagePath);
            CropHeight = SuggestedTrimHeight;
            //var (width, height) = _cropHelper.GetNaturalSize();
            //_naturalImageWidth = width;
            //_naturalImageHeight = height;
            _desiredImagedData = _cropHelper.CropImageBase64(RegionBounds);
        }
        return base.OnParametersSetAsync();
    }
    private Rectangle GetTrimmedRegion()
    {
        return new Rectangle(
            RegionBounds.X,
            RegionBounds.Y + CropHeight!.Value,
            RegionBounds.Width,
            RegionBounds.Height - CropHeight.Value
        );
    }
    private string GetRemainingImageBase64()
    {
        if (CropHeight is null)
        {
            return "";
        }
        return _cropHelper.CropImageBase64(GetTrimmedRegion());
    }
    private void HandleClick(Microsoft.AspNetCore.Components.Web.MouseEventArgs e)
    {
        if (SuggestedTrimHeight is not null)
        {
            return; //has to ignore because you already set the suggestion.  you have to use the keyboard from here
        }
        CropHeight = (int) e.OffsetY;
        CropHeightChanged.InvokeAsync(CropHeight);
    }
    private void ClearSelection()
    {
        CropHeight = null;
        CropHeightChanged.InvokeAsync(CropHeight);
    }
}