namespace ImageToolsWindowsLibrary;
public partial class PartialTrimSelectorComponent(IJSRuntime js) : IAsyncDisposable
{
    private enum EnumTrimViewModel
    {
        None,
        Top,
        Bottom
    }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Parameter] public string ImagePath { get; set; } = string.Empty;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Parameter] public Rectangle RegionBounds { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Parameter] public int SuggestedTrimHeight { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Parameter] public EventCallback<BasicList<Rectangle>> OnTrimsConfirmed { get; set; }
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RenderFragment? ChildContent { get; set; }
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public float ZoomLevel { get; set; } = 4;
    private KeySuppressorClass? _suppress;
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ContainerHeight { get; set; } = "80vh";
    private ElementReference _imageRef; //not sure if i need this or not (?)
    private string? _referenceImage;
    private string? _regionImage;
    private readonly ImageCropHelper _cropHelper = new();
    private EnumTrimViewModel _currentMode;
    private Point? _startPoint;
    private Point? _endPoint;
    private string? _topImageData;
    private string? _bottomImageData;
    private readonly BasicList<Rectangle> _topRemovals = [];
    private readonly BasicList<Rectangle> _bottomRemovals = [];
    private AppKeyboardListener? _keys;
    private string? _lastImagePath = null;
    private Rectangle _previousBounds;
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
        //this is where i have the hotkeys.
        switch (key)
        {
            case EnumKey.Down:
                DownArrowClicked();
                break;
            case EnumKey.Up:
                UpArrowClicked();
                break;
            case EnumKey.Left:
                LeftArrowClicked();
                break;
            case EnumKey.Right:
                RightArrowClicked();
                break;
            case EnumKey.F1:
                ConfirmAllTrims();
                break;
            case EnumKey.F2:
                ConfirmCurrentSelection();
                StateHasChanged();
                break;
            case EnumKey.F3:
                ShowPreview();
                StateHasChanged();
                break;
            case EnumKey.F4:
                SetMode(EnumAdjustmentMode.Move, true);
                break;
            case EnumKey.F5:
                SetMode(EnumAdjustmentMode.Resize, true);
                break;
            case EnumKey.F6:
                SetMode(EnumAdjustmentMode.AdjustEdges, true);
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
        _referenceImage = _cropHelper.CropImageBase64(RegionBounds);

        _previousBounds = RegionBounds;
        LoadZoomedViews();
        ClearRegions();
    }
    private string CurrentImage
    {
        get
        {
            switch (_currentMode)
            {
                case EnumTrimViewModel.None:
                    return _regionImage!;
                case EnumTrimViewModel.Top:
                    return _topImageData!;
                case EnumTrimViewModel.Bottom:
                    return _bottomImageData!;
                default:
                    break;
            }
            throw new CustomBasicException("No trim mode selected");
        }
    }
    private void ConfirmAllTrims()
    {
        if (_startPoint.HasValue || _endPoint.HasValue)
        {
            return; //can't do anything because you are in the middle of something.
        }
        if (_currentMode != EnumTrimViewModel.None)
        {
            return; //should not confirm.  to force me to review the final results first.
        }
        BasicList<Rectangle> absoluteTrims = [];

        foreach (var topTrim in _topRemovals)
        {
            absoluteTrims.Add(MapRemovalToFullImage(topTrim, EnumTrimViewModel.Top));
        }

        foreach (var bottomTrim in _bottomRemovals)
        {
            absoluteTrims.Add(MapRemovalToFullImage(bottomTrim, EnumTrimViewModel.Bottom));
        }

        OnTrimsConfirmed.InvokeAsync(absoluteTrims);
    }
    private void ClearRegions()
    {
        _topRemovals.Clear();
        _bottomRemovals.Clear();
        _regionImage = _cropHelper.CropImageBase64(RegionBounds);
    }
    private void ClearCurrentSelection()
    {
        _startPoint = null;
        _endPoint = null;
    }
    private void LoadZoomedViews()
    {
        int trimHeight = Math.Max(1, SuggestedTrimHeight);

        var topSlice = new Rectangle(
            RegionBounds.X,
            RegionBounds.Y,
            RegionBounds.Width,
            trimHeight
        );

        var bottomSlice = new Rectangle(
            RegionBounds.X,
            RegionBounds.Y + RegionBounds.Height - trimHeight,
            RegionBounds.Width,
            trimHeight
        );

        _topImageData = GetZoomedBase64(topSlice);
        _bottomImageData = GetZoomedBase64(bottomSlice);
    }
    private string GetZoomedBase64(Rectangle region)
    {
        using var bmp = _cropHelper.GetRegionBitmap(region);
        int newWidth = (int)(bmp.Width * ZoomLevel);
        int newHeight = (int)(bmp.Height * ZoomLevel);

        using var zoomedBmp = new Bitmap(newWidth, newHeight);
        using (var g = Graphics.FromImage(zoomedBmp))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            g.DrawImage(bmp, 0, 0, newWidth, newHeight);
        }

        return ImageCropHelper.BitmapToBase64(zoomedBmp);
    }
    private async Task HandleImageClickAsync(Microsoft.AspNetCore.Components.Web.MouseEventArgs e)
    {

        // Scale down coordinates based on the zoom level
        //int clickedY = (int)(e.OffsetY / ZoomLevel);
        //int clickedX = (int)(e.OffsetX / ZoomLevel);
        int clickedY = (int)e.OffsetY;
        int clickedX = (int)e.OffsetX;

        if (_currentMode == EnumTrimViewModel.None)
        {
            float rawZoomHeight = RegionBounds.Height / ZoomLevel;
            int zoomHeight = Math.Max(1, (int)Math.Round(rawZoomHeight));

            if (clickedY < zoomHeight / 2)
            {
                _currentMode = EnumTrimViewModel.Top;
            }
            else
            {
                _currentMode = EnumTrimViewModel.Bottom;
            }

            StateHasChanged();
            return;
        }

        // Handle selection based on current mode
        if (!_startPoint.HasValue)
        {
            int startY = clickedY;

            if (_currentMode == EnumTrimViewModel.Top)
            {
                startY = 0; // Force top alignment
            }

            _startPoint = new Point(clickedX, startY);
        }
        else if (!_endPoint.HasValue)
        {
            int endY = clickedY;

            if (_currentMode == EnumTrimViewModel.Bottom)
            {
                int heights = await js!.GetContainerHeight(_imageRef);
                if (heights == 0)
                {
                    throw new CustomBasicException("Cannot be 0 for the heights");
                }
                endY = heights;
                // Calculate zoomed height to get bottom
                //int zoomHeight = Math.Max(1, (int)Math.Round(RegionBounds.Height / ZoomLevel));
                //endY = RegionBounds.Height * (int)ZoomLevel; // Force bottom alignment
            }

            _endPoint = new Point(clickedX, endY);
        }
    }
    private static string ResizeIcon()
    {
        return """
            <svg xmlns="http://www.w3.org/2000/svg" 
                 fill="none" 
                 viewBox="0 0 24 24" 
                 stroke="blue" 
                 stroke-width="2" 
                 width="24" 
                 height="24">
              <path stroke-linecap="round" stroke-linejoin="round" d="M4 12h16M12 4v16M6 6l12 12M6 18L18 6" />
            </svg>
            """;
    }
    private enum EnumAdjustmentMode
    {
        Move,
        Resize,
        AdjustEdges
    }
    private EnumAdjustmentMode _adjustmentMode = EnumAdjustmentMode.Move;
    private void SetMode(EnumAdjustmentMode newMode, bool forceRender = true)
    {
        if (_startPoint.HasValue == false || _endPoint.HasValue == false)
        {
            return; //ignore because you are not even editing something.
        }

        _adjustmentMode = newMode; //can be anything no matter what (no exceptions).
        if (forceRender)
        {
            StateHasChanged();
        }
    }
    private void LeftArrowClicked()
    {
        if (!_startPoint.HasValue || !_endPoint.HasValue)
        {
            return;
        }
        switch (_adjustmentMode)
        {
            case EnumAdjustmentMode.Move:
                _startPoint = new Point(_startPoint.Value.X - 1, _startPoint.Value.Y);
                _endPoint = new Point(_endPoint.Value.X - 1, _endPoint.Value.Y);
                break;

            case EnumAdjustmentMode.Resize:
                _endPoint = new Point(_endPoint.Value.X - 1, _endPoint.Value.Y);
                break;

            case EnumAdjustmentMode.AdjustEdges:
                if (_startPoint.Value.X < _endPoint.Value.X)
                {
                    _startPoint = new Point(_startPoint.Value.X + 1, _startPoint.Value.Y);
                }
                else
                {
                    _endPoint = new Point(_endPoint.Value.X + 1, _endPoint.Value.Y);
                }
                break;
        }
        StateHasChanged();
    }
    private void RightArrowClicked()
    {
        if (!_startPoint.HasValue || !_endPoint.HasValue)
        {
            return;
        }
        switch (_adjustmentMode)
        {
            case EnumAdjustmentMode.Move:
                _startPoint = new Point(_startPoint.Value.X + 1, _startPoint.Value.Y);
                _endPoint = new Point(_endPoint.Value.X + 1, _endPoint.Value.Y);
                break;

            case EnumAdjustmentMode.Resize:
                _endPoint = new Point(_endPoint.Value.X + 1, _endPoint.Value.Y);
                break;

            case EnumAdjustmentMode.AdjustEdges:
                if (_startPoint.Value.X < _endPoint.Value.X)
                {
                    _startPoint = new Point(_startPoint.Value.X - 1, _startPoint.Value.Y);
                }
                else
                {
                    _endPoint = new Point(_endPoint.Value.X - 1, _endPoint.Value.Y);
                }
                break;
        }
        StateHasChanged();
    }
    private void UpArrowClicked()
    {
        if (!_startPoint.HasValue || !_endPoint.HasValue)
        {
            return;
        }
        // Always move top edge up by 1 (decreasing Y), increasing height
        _startPoint = new Point(_startPoint.Value.X, _startPoint.Value.Y - 1);

        StateHasChanged();
    }
    private void DownArrowClicked()
    {
        if (!_startPoint.HasValue || !_endPoint.HasValue)
        {
            return;
        }
        // Always move top edge down by 1 (increasing Y), decreasing height
        _startPoint = new Point(_startPoint.Value.X, _startPoint.Value.Y + 1);

        StateHasChanged();
    }


    private void ConfirmCurrentSelection()
    {
        if (!_startPoint.HasValue || !_endPoint.HasValue)
        {
            return;
        }

        int x = Math.Min(_startPoint.Value.X, _endPoint.Value.X);
        int y = Math.Min(_startPoint.Value.Y, _endPoint.Value.Y);
        int width = Math.Abs(_endPoint.Value.X - _startPoint.Value.X);
        int height = Math.Abs(_endPoint.Value.Y - _startPoint.Value.Y);
        var localRect = new Rectangle(x, y, width, height);
        if (_currentMode == EnumTrimViewModel.Top)
        {
            _topRemovals.Add(localRect);
        }
        else if (_currentMode == EnumTrimViewModel.Bottom)
        {
            _bottomRemovals.Add(localRect);
        }
        _startPoint = null;
        _endPoint = null;
    }
    private BasicList<Rectangle> GetRemovalsForCurrentSlice
    {
        get
        {
            if (_currentMode == EnumTrimViewModel.None)
            {
                throw new CustomBasicException("No mode");
            }
            if (_currentMode == EnumTrimViewModel.Top)
            {
                return _topRemovals;
            }
            if (_currentMode == EnumTrimViewModel.Bottom)
            {
                return _bottomRemovals;
            }
            throw new CustomBasicException("No mode");
        }
    }
    public void ShowPreview()
    {
        if (_currentMode == EnumTrimViewModel.None)
        {
            return;
        }
        if (_startPoint.HasValue || _endPoint.HasValue)
        {
            return; //can't do anything because you are in the middle of something.
        }
        _currentMode = EnumTrimViewModel.None; // Turn off zoom mode
        ClearCurrentSelection();
        using var bmp = _cropHelper.GetRegionBitmap(RegionBounds);
        using var g = Graphics.FromImage(bmp);
        using var brush = new SolidBrush(Color.White);
        var allMapped = _topRemovals
            .Select(x => MapRemovalToFullImage(x, EnumTrimViewModel.Top))
            .Concat(_bottomRemovals.Select(x => MapRemovalToFullImage(x, EnumTrimViewModel.Bottom)));
        foreach (var rect in allMapped)
        {
            int localX = rect.X - RegionBounds.X;
            int localY = rect.Y - RegionBounds.Y;
            g.FillRectangle(brush, localX, localY, rect.Width, rect.Height);
        }
        _regionImage = ImageCropHelper.BitmapToBase64(bmp);
        StateHasChanged();
    }
    private Rectangle MapRemovalToFullImage(Rectangle removal, EnumTrimViewModel mode)
    {
        int trimHeight = Math.Max(1, SuggestedTrimHeight);
        // Slice of full image shown in zoom
        Rectangle slice = mode switch
        {
            EnumTrimViewModel.Top => new Rectangle(
                RegionBounds.X,
                RegionBounds.Y,
                RegionBounds.Width,
                trimHeight),

            EnumTrimViewModel.Bottom => new Rectangle(
                RegionBounds.X,
                RegionBounds.Y + RegionBounds.Height - trimHeight,
                RegionBounds.Width,
                trimHeight),

            _ => throw new CustomBasicException("Invalid mode")
        };

        // Unzoomed removal inside the slice
        int unzoomedX = (int)Math.Round(removal.X / ZoomLevel);
        int unzoomedY = (int)Math.Round(removal.Y / ZoomLevel);
        int unzoomedWidth = (int)Math.Round(removal.Width / ZoomLevel);
        int unzoomedHeight = (int)Math.Round(removal.Height / ZoomLevel);

        return new Rectangle(
            slice.X + unzoomedX,
            slice.Y + unzoomedY,
            unzoomedWidth,
            unzoomedHeight);
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _suppress = new(js);
            await _suppress.DisableArrowKeysAsync();
            await _suppress.DisableFunctionKeysAsync();
        }
    }
    public async ValueTask DisposeAsync()
    {
        if (_suppress is not null)
        {
            await _suppress.DisposeAsync();
        }
        // Add this line to suppress finalization
        GC.SuppressFinalize(this);
    }
}