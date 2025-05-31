namespace ImageToolsWindowsLibrary;
public partial class PartialTrimSelectorComponent
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
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ContainerWidth { get; set; } = "40vw";
    private ElementReference _imageRef; //not sure if i need this or not (?)
    private string? _referenceImage;
    private string? _regionImage;
    private readonly ImageCropHelper _cropHelper = new();
    private EnumTrimViewModel _currentMode;
    private Point? _startPoint;
    private Point? _endPoint;
    private string? _topImageData;
    private string? _bottomImageData;
    private BasicList<Rectangle> _topRemovals = [];
    private BasicList<Rectangle> _bottomRemovals = [];
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
        //lets give the parent both
        BasicList<Rectangle> bounds = [];
        bounds.AddRange(_topRemovals);
        bounds.AddRange(_bottomRemovals);
        OnTrimsConfirmed.InvokeAsync(bounds);
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
        float rawZoomHeight = RegionBounds.Height / ZoomLevel;
        int zoomHeight = Math.Max(1, (int)Math.Round(rawZoomHeight));

        var topSlice = new Rectangle(
            RegionBounds.X,
            RegionBounds.Y,
            RegionBounds.Width,
            zoomHeight
        );

        var bottomSlice = new Rectangle(
            RegionBounds.X,
            RegionBounds.Y + RegionBounds.Height - zoomHeight,
            RegionBounds.Width,
            zoomHeight
        );
        _topImageData = _cropHelper.CropImageBase64(topSlice);
        _bottomImageData = _cropHelper.CropImageBase64(bottomSlice);
    }
    private void HandleImageClick(Microsoft.AspNetCore.Components.Web.MouseEventArgs e)
    {
        int clickedY = (int)e.OffsetY;
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
            return; //if you need to choose a mode, this is used to choose a mode you need.
        }
        if (_startPoint.HasValue == false)
        {
            _startPoint = new Point((int)e.OffsetX, clickedY);
        }
        else if (_endPoint.HasValue == false)
        {
            _endPoint = new Point((int)e.OffsetX, clickedY);
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
        switch (_adjustmentMode)
        {
            case EnumAdjustmentMode.Move:
                _startPoint = new Point(_startPoint.Value.X, _startPoint.Value.Y - 1);
                _endPoint = new Point(_endPoint.Value.X, _endPoint.Value.Y - 1);
                break;

            case EnumAdjustmentMode.Resize:
                _endPoint = new Point(_endPoint.Value.X, _endPoint.Value.Y - 1);
                break;

            case EnumAdjustmentMode.AdjustEdges:
                if (_startPoint.Value.Y < _endPoint.Value.Y)
                {
                    _startPoint = new Point(_startPoint.Value.X, _startPoint.Value.Y + 1);
                }
                else
                {
                    _endPoint = new Point(_endPoint.Value.X, _endPoint.Value.Y + 1);
                }
                break;
        }
        StateHasChanged();
    }
    private void DownArrowClicked()
    {
        if (!_startPoint.HasValue || !_endPoint.HasValue)
        {
            return;
        }
        switch (_adjustmentMode)
        {
            case EnumAdjustmentMode.Move:
                _startPoint = new Point(_startPoint.Value.X, _startPoint.Value.Y + 1);
                _endPoint = new Point(_endPoint.Value.X, _endPoint.Value.Y + 1);
                break;

            case EnumAdjustmentMode.Resize:
                _endPoint = new Point(_endPoint.Value.X, _endPoint.Value.Y + 1);
                break;

            case EnumAdjustmentMode.AdjustEdges:
                if (_startPoint.Value.Y < _endPoint.Value.Y)
                {
                    _startPoint = new Point(_startPoint.Value.X, _startPoint.Value.Y - 1);
                }
                else
                {
                    _endPoint = new Point(_endPoint.Value.X, _endPoint.Value.Y - 1);
                }
                break;
        }
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

        Rectangle translatedRect = _currentMode switch
        {
            EnumTrimViewModel.Top => new Rectangle(RegionBounds.X + localRect.X, RegionBounds.Y + localRect.Y, localRect.Width, localRect.Height),
            EnumTrimViewModel.Bottom => new Rectangle(RegionBounds.X + localRect.X, RegionBounds.Y + RegionBounds.Height - (int)(RegionBounds.Height / ZoomLevel) + localRect.Y, localRect.Width, localRect.Height),
            _ => throw new CustomBasicException("Invalid mode for confirming selection")
        };

        if (_currentMode == EnumTrimViewModel.Top)
        {
            _topRemovals.Add(translatedRect);
        }
        else if (_currentMode == EnumTrimViewModel.Bottom)
        {
            _bottomRemovals.Add(translatedRect);
        }

        _startPoint = null;
        _endPoint = null;

        //the top or bottom would have to change accordingly.
    }
    public void ShowPreview()
    {
        _currentMode = EnumTrimViewModel.None;
        //has to adjust the entire region now somehow or another.
        ClearCurrentSelection(); //obviously means needs to clear the current selection
        using var bmp = _cropHelper.GetRegionBitmap(RegionBounds);
        using var g = Graphics.FromImage(bmp);
        using var brush = new SolidBrush(Color.White);

        foreach (var rect in _topRemovals.Concat(_bottomRemovals))
        {
            int localX = rect.X - RegionBounds.X;
            int localY = rect.Y - RegionBounds.Y;
            g.FillRectangle(brush, localX, localY, rect.Width, rect.Height);
        }

        _regionImage = ImageCropHelper.BitmapToBase64(bmp);
    }

    private void RebuildZoomedImage(EnumTrimViewModel mode)
    {
        float rawZoomHeight = RegionBounds.Height / ZoomLevel;
        int zoomHeight = Math.Max(1, (int)Math.Round(rawZoomHeight));

        Rectangle slice = mode switch
        {
            EnumTrimViewModel.Top => new Rectangle(
                RegionBounds.X,
                RegionBounds.Y,
                RegionBounds.Width,
                zoomHeight),

            EnumTrimViewModel.Bottom => new Rectangle(
                RegionBounds.X,
                RegionBounds.Y + RegionBounds.Height - zoomHeight,
                RegionBounds.Width,
                zoomHeight),

            _ => throw new CustomBasicException("Invalid mode for rebuilding image")
        };

        using var bmp = _cropHelper.GetRegionBitmap(slice);
        using var g = Graphics.FromImage(bmp);
        using var brush = new SolidBrush(Color.White);

        var removals = mode == EnumTrimViewModel.Top ? _topRemovals : _bottomRemovals;

        foreach (var rect in removals)
        {
            int localX = rect.X - slice.X;
            int localY = rect.Y - slice.Y;
            g.FillRectangle(brush, localX, localY, rect.Width, rect.Height);
        }

        string result = ImageCropHelper.BitmapToBase64(bmp);
        if (mode == EnumTrimViewModel.Top)
            _topImageData = result;
        else
            _bottomImageData = result;

        StateHasChanged();
    }

}