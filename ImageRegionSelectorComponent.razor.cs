namespace ImageToolsWindowsLibrary;
public partial class ImageRegionSelectorComponent
{
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ImagePath { get; set; } = "";
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowCroppedImage { get; set; }
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Rectangle? InitialCropRectangle { get; set; }

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RenderFragment? ChildContent { get; set; }

    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    private int _renderedImageWidth;
    private int _renderedImageHeight;
    private async Task GetRenderedImageSizeAsync()
    {
        int y = await JSRuntime.GetContainerHeight(_imageRef);
        int x = await JSRuntime.GetContainerWidth(_imageRef);
        _renderedImageWidth = x;
        _renderedImageHeight = y;
    }
    private ElementReference _imageRef;
    private Point? StartPoint { get; set; }
    private Point? EndPoint { get; set; }
    private string ImageData { get; set; } = "";
    private string CroppedImageData { get; set; } = "";
    private string? _lastImagePath = null;
    private int _naturalImageWidth;
    private int _naturalImageHeight;

    private void LoadImageSize()
    {
        using var bmp = new Bitmap(ImagePath);
        _naturalImageWidth = bmp.Width;
        _naturalImageHeight = bmp.Height;
    }
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!string.IsNullOrWhiteSpace(ImagePath) &&
            File.Exists(ImagePath) &&
            ImagePath != _lastImagePath) // only reload if path changes
        {
            _lastImagePath = ImagePath;

            LoadImageSize();

            byte[] bytes = File.ReadAllBytes(ImagePath);
            string base64 = Convert.ToBase64String(bytes);
            ImageData = $"data:image/png;base64,{base64}";
            CroppedImageData = ""; // clear old crop because image changed

            if (InitialCropRectangle is Rectangle rect)
            {
                StartPoint = new Point(rect.X, rect.Y);
                EndPoint = new Point(rect.X + rect.Width, rect.Y + rect.Height);
                GenerateCroppedImage();
            }
            else
            {
                StartPoint = null;
                EndPoint = null;
            }
        }
    }
    public void GenerateCroppedImage()
    {
        if (StartPoint is null || EndPoint is null)
        {
            return;
        }

        var rect = GetSelectionRectangle();
        if (rect is null || _renderedImageWidth == 0 || _renderedImageHeight == 0)
        {
            return;
        }

        // Compute scale factors from rendered image to actual image size
        double scaleX = (double)_naturalImageWidth / _renderedImageWidth;
        double scaleY = (double)_naturalImageHeight / _renderedImageHeight;

        // Apply scaling to get actual image-space crop
        var scaledRect = new Rectangle(
            (int)(rect.Value.X * scaleX),
            (int)(rect.Value.Y * scaleY),
            (int)(rect.Value.Width * scaleX),
            (int)(rect.Value.Height * scaleY)
        );

        using var bmp = new Bitmap(ImagePath);
        using var cropped = bmp.Clone(scaledRect, bmp.PixelFormat);
        using var ms = new MemoryStream();
        cropped.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        CroppedImageData = $"data:image/png;base64,{Convert.ToBase64String(ms.ToArray())}";
    }
    private void HandleClick(Microsoft.AspNetCore.Components.Web.MouseEventArgs e)
    {
        var point = new Point((int)e.OffsetX, (int)e.OffsetY);

        if (!StartPoint.HasValue)
        {
            StartPoint = point;
        }
        else if (!EndPoint.HasValue)
        {
            EndPoint = point;
        }

        StateHasChanged();
    }

    private void ClearSelection()
    {
        StartPoint = null;
        EndPoint = null;
    }

    private Rectangle? GetSelectionRectangle()
    {
        if (StartPoint.HasValue && EndPoint.HasValue)
        {
            int x = Math.Min(StartPoint.Value.X, EndPoint.Value.X);
            int y = Math.Min(StartPoint.Value.Y, EndPoint.Value.Y);
            int width = Math.Abs(StartPoint.Value.X - EndPoint.Value.X);
            int height = Math.Abs(StartPoint.Value.Y - EndPoint.Value.Y);
            return new Rectangle(x, y, width, height);
        }
        return null;
    }
    public string GetCroppedImageBase64() => CroppedImageData;
    public Rectangle? GetScaledCropRectangle()
    {
        var rect = GetSelectionRectangle();
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
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await GetRenderedImageSizeAsync();
        }
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
        GenerateCroppedImage();
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
        GenerateCroppedImage();
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
        GenerateCroppedImage();
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
        GenerateCroppedImage();
        StateHasChanged();
    }
    private enum EnumAdjustmentMode
    {
        Move,
        Resize,
        AdjustEdges
    }
    private EnumAdjustmentMode _currentMode = EnumAdjustmentMode.AdjustEdges;
}