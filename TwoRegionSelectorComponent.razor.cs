namespace ImageToolsWindowsLibrary;
public partial class TwoRegionSelectorComponent(IJSRuntime JSRuntime)
{
    private enum EnumRegionStep
    {
        SelectingFirst,
        SelectingSecond,
        Done
    }
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ImagePath { get; set; } = "";
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowCroppedImages { get; set; }
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public EnumRegionLayoutMode LayoutMode { get; set; }

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RenderFragment? ChildContent { get; set; }
    private Rectangle? _firstRectangle = null;
    private Rectangle? _secondRectangle = null;
    private int _naturalImageWidth;
    private int _naturalImageHeight;
    private ImageCropHelper _cropHelper = new ();
    private string ImageData { get; set; } = "";
    private EnumRegionStep _currentStep = EnumRegionStep.SelectingFirst;


    private Point? StartPoint { get; set; }
    private Point? EndPoint { get; set; }


    protected override async Task OnParametersSetAsync()
    {
        if (!string.IsNullOrWhiteSpace(ImagePath))
        {
            _cropHelper.LoadImage(ImagePath);
            var temp = _cropHelper.GetNaturalSize();
            _naturalImageWidth = temp.width;
            _naturalImageHeight = temp.height;
            var bytes = await File.ReadAllBytesAsync(ImagePath);
            ImageData = $"data:image/png;base64,{Convert.ToBase64String(bytes)}";

            await GetRenderedImageSizeAsync();

            _firstRectangle = null;
            _secondRectangle = null;
            _currentStep = EnumRegionStep.SelectingFirst;
        }
    }

    private ElementReference _imageRef;

    private int _renderedImageWidth;
    private int _renderedImageHeight;
    private async Task GetRenderedImageSizeAsync()
    {
        int y = await JSRuntime.GetContainerHeight(_imageRef);
        int x = await JSRuntime.GetContainerWidth(_imageRef);
        _renderedImageWidth = x;
        _renderedImageHeight = y;
    }



    private Rectangle? GetScaledCropRectangle(Rectangle? rect)
    {
        if (rect is null || _renderedImageWidth == 0 || _renderedImageHeight == 0)
        {
            return null;
        }

        double scaleX = (double)_naturalImageWidth / _renderedImageWidth;
        double scaleY = (double)_naturalImageHeight / _renderedImageHeight;
        return new Rectangle(
            (int)(rect.Value.X * scaleX),
            (int)(rect.Value.Y * scaleY),
            (int)(rect.Value.Width * scaleX),
            (int)(rect.Value.Height * scaleY)
        );
    }

    public TwoRegionImageEntry GetTwoRegionEntry()
    {
        TwoRegionImageEntry output = new ()
        {
            ImageFile = ImagePath,
            FirstRegion = _firstRectangle,
            SecondRegion = _secondRectangle
        };
        _firstRectangle = null;
        _secondRectangle = null;
        _currentStep = EnumRegionStep.SelectingFirst;
        return output;
    }
    private Rectangle GetSelectionRectangle(Point p1, Point p2)
    {
        int x = Math.Min(p1.X, p2.X);
        int y = Math.Min(p1.Y, p2.Y);
        int w = Math.Abs(p1.X - p2.X);
        int h = Math.Abs(p1.Y - p2.Y);
        return new Rectangle(x, y, w, h);
    }
    private void HandleClick(Microsoft.AspNetCore.Components.Web.MouseEventArgs e)
    {
        var clickedPoint = new Point((int)e.OffsetX, (int)e.OffsetY);

        if (StartPoint.HasValue == false)
        {
            StartPoint = clickedPoint;
        }
        else if (EndPoint.HasValue == false)
        {
            EndPoint = clickedPoint;
            var rect = GetSelectionRectangle(StartPoint.Value, EndPoint.Value);
            if (_currentStep == EnumRegionStep.SelectingFirst)
            {
                _firstRectangle = rect;
            }
            else if (_currentStep == EnumRegionStep.SelectingSecond)
            {
                _secondRectangle = rect;
            }

            StartPoint = null;
            EndPoint = null;

            StateHasChanged();
        }

        StateHasChanged();
    }
    public void ClearSelection()
    {
        StartPoint = null;
        EndPoint = null;
    }
    private string GetFirstRegionImageBase64()
    {
        if (_firstRectangle is null)
        {
            return "";
        }
        var scaledRect = GetScaledCropRectangle(_firstRectangle);
        return _cropHelper.CropImageBase64(scaledRect!.Value);
    }
    private string GetSecondRegionImageBase64()
    {
        if (_secondRectangle is null)
        {
            return "";
        }
        var scaledRect = GetScaledCropRectangle(_secondRectangle);
        return _cropHelper.CropImageBase64(scaledRect!.Value);
    }
    private void LeftArrowClicked()
    {
        if (!StartPoint.HasValue || !EndPoint.HasValue)
        {
            return;
        }
        switch (_currentMode)
        {
            case EnumAdjustmentMode.Move:
                StartPoint = new Point(StartPoint.Value.X - 1, StartPoint.Value.Y);
                EndPoint = new Point(EndPoint.Value.X - 1, EndPoint.Value.Y);
                break;

            case EnumAdjustmentMode.Resize:
                EndPoint = new Point(EndPoint.Value.X - 1, EndPoint.Value.Y);
                break;

            case EnumAdjustmentMode.AdjustEdges:
                if (StartPoint.Value.X < EndPoint.Value.X)
                {
                    StartPoint = new Point(StartPoint.Value.X + 1, StartPoint.Value.Y);
                }
                else
                {
                    EndPoint = new Point(EndPoint.Value.X + 1, EndPoint.Value.Y);
                }
                break;
        }
        StateHasChanged();
    }

    private void RightArrowClicked()
    {
        if (!StartPoint.HasValue || !EndPoint.HasValue)
        {
            return;
        }
        switch (_currentMode)
        {
            case EnumAdjustmentMode.Move:
                StartPoint = new Point(StartPoint.Value.X + 1, StartPoint.Value.Y);
                EndPoint = new Point(EndPoint.Value.X + 1, EndPoint.Value.Y);
                break;

            case EnumAdjustmentMode.Resize:
                EndPoint = new Point(EndPoint.Value.X + 1, EndPoint.Value.Y);
                break;

            case EnumAdjustmentMode.AdjustEdges:
                if (StartPoint.Value.X < EndPoint.Value.X)
                {
                    StartPoint = new Point(StartPoint.Value.X - 1, StartPoint.Value.Y);
                }
                else
                {
                    EndPoint = new Point(EndPoint.Value.X - 1, EndPoint.Value.Y);
                }
                break;
        }
        StateHasChanged();
    }
    private void UpArrowClicked()
    {
        if (!StartPoint.HasValue || !EndPoint.HasValue)
        {
            return;
        }
        switch (_currentMode)
        {
            case EnumAdjustmentMode.Move:
                StartPoint = new Point(StartPoint.Value.X, StartPoint.Value.Y - 1);
                EndPoint = new Point(EndPoint.Value.X, EndPoint.Value.Y - 1);
                break;

            case EnumAdjustmentMode.Resize:
                EndPoint = new Point(EndPoint.Value.X, EndPoint.Value.Y - 1);
                break;

            case EnumAdjustmentMode.AdjustEdges:
                if (StartPoint.Value.Y < EndPoint.Value.Y)
                {
                    StartPoint = new Point(StartPoint.Value.X, StartPoint.Value.Y + 1);
                }
                else
                {
                    EndPoint = new Point(EndPoint.Value.X, EndPoint.Value.Y + 1);
                }
                break;
        }
        StateHasChanged();
    }

    private void DownArrowClicked()
    {
        if (!StartPoint.HasValue || !EndPoint.HasValue)
        {
            return;
        }
        switch (_currentMode)
        {
            case EnumAdjustmentMode.Move:
                StartPoint = new Point(StartPoint.Value.X, StartPoint.Value.Y + 1);
                EndPoint = new Point(EndPoint.Value.X, EndPoint.Value.Y + 1);
                break;

            case EnumAdjustmentMode.Resize:
                EndPoint = new Point(EndPoint.Value.X, EndPoint.Value.Y + 1);
                break;

            case EnumAdjustmentMode.AdjustEdges:
                if (StartPoint.Value.Y < EndPoint.Value.Y)
                {
                    StartPoint = new Point(StartPoint.Value.X, StartPoint.Value.Y - 1);
                }
                else
                {
                    EndPoint = new Point(EndPoint.Value.X, EndPoint.Value.Y - 1);
                }
                break;
        }
        StateHasChanged();
    }
    private enum EnumAdjustmentMode
    {
        Move,
        Resize,
        AdjustEdges
    }
    private EnumAdjustmentMode _currentMode = EnumAdjustmentMode.AdjustEdges;

    //best to have the parent (child) decide to call when the second one is needed.
    public void BeginSecondRegion()
    {
        if (_firstRectangle is not null)
        {
            _currentStep = EnumRegionStep.SelectingSecond;
            StateHasChanged();
        }
    }
}