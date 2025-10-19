using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using QuickDraw.ViewModels;
using QuickDraw.Views.Base;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace QuickDraw.Views;

public enum LoadDirection
{
    Backwards,
    Forwards
}

public record LoadData(string Path, LoadDirection Direction);

public class ChannelQueue<T>
{
    private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();

    public bool Enqueue(T data)
    {
        return _channel.Writer.TryWrite(data);
    }

    public ValueTask<T> DequeueAsync(CancellationToken token = default)
    {
        return _channel.Reader.ReadAsync(token);
    }

    public ValueTask<bool> WaitForNext(CancellationToken token = default)
    {
        return _channel.Reader.WaitToReadAsync(token);
    }
}

public partial class MFPointerGrid : Grid
{
    public MFPointerGrid() : base()
    {

    }

    public void SetCursor(InputCursor? cursor)
    {
        ProtectedCursor = cursor;
    }
}

public sealed partial class SlidePage : PageBase
{
    // TODO: Implement clicking the image to open it in explorer
    private Task? _initImageLoadTask;

    private (string, CanvasVirtualBitmap?) _currentBitmap;
    private (string, CanvasVirtualBitmap?) _nextBitmap;
    private (string, CanvasVirtualBitmap?) _prevBitmap;

    private readonly CancellationTokenSource _cts = new();
    private readonly ChannelQueue<LoadData> _imageLoadQueue = new();

    public SlideViewModel ViewModel
    {
        get;
    }

    public SlidePage() : base(App.GetService<SlideViewModel>())
    {
        ViewModel = (SlideViewModel)ViewModelBase;
        ViewModel.InvalidateCanvas += InvalidateCanvas;
        ViewModel.NextImageHandler += (sender, args) => NextImage(args.ImagePath);
        ViewModel.PreviousImageHandler += (sender, args) => PrevImage(args.ImagePath);

        CanvasDevice.DebugLevel = CanvasDebugLevel.Information;

        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        ViewModel.StartTimer(DispatcherQueue);

        _ = HandleLoads(_cts.Token);
    }

    private async Task HandleLoads(CancellationToken token)
    {
        while (await _imageLoadQueue.WaitForNext(token))
        {
            var data = await _imageLoadQueue.DequeueAsync(token);

            switch (data.Direction)
            {
                case LoadDirection.Forwards:
                    await LoadNext(SlideCanvas, data.Path);
                    break;
                case LoadDirection.Backwards:
                    await LoadPrev(SlideCanvas, data.Path);
                    break;
            }
        }
    }

    private async Task LoadNext(ICanvasResourceCreator resourceCreator, string imagePath)
    {
        _prevBitmap.Item2?.Dispose();
        _prevBitmap = _currentBitmap;
        _currentBitmap = _nextBitmap;
        _nextBitmap = (imagePath, null);
        _nextBitmap = (imagePath, await CanvasVirtualBitmap.LoadAsync(resourceCreator, imagePath));
        SlideCanvas?.Invalidate();
    }

    private async Task LoadPrev(ICanvasResourceCreator resourceCreator, string imagePath)
    {
        _nextBitmap.Item2?.Dispose();
        _nextBitmap = _currentBitmap;
        _currentBitmap = _prevBitmap;
        _prevBitmap = (imagePath, null);
        _prevBitmap = (imagePath, await CanvasVirtualBitmap.LoadAsync(resourceCreator, imagePath));
        SlideCanvas?.Invalidate();
    }

    public void NextImage(string imagePath)
    {
        _imageLoadQueue.Enqueue(new(imagePath,LoadDirection.Forwards));
    }

    public void PrevImage(string imagePath)
    {
        _imageLoadQueue.Enqueue(new(imagePath, LoadDirection.Backwards));
    }

    private void InvalidateCanvas(object? sender, EventArgs e)
    {
        SlideCanvas.Invalidate();
    }

    void SlidePage_Unloaded(object sender, RoutedEventArgs e)
    {
        _cts.Cancel();

        _currentBitmap.Item2?.Dispose();
        _prevBitmap.Item2?.Dispose();
        _nextBitmap.Item2?.Dispose();

        this.SlideCanvas.RemoveFromVisualTree();
        this.SlideCanvas = null;
    }

    private void CanvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        if (!IsLoadInProgress())
        {
            var bitmap = _currentBitmap.Item2;

            _ = DrawBitmapToView(args.DrawingSession, bitmap, new Size(sender.ActualWidth, sender.ActualHeight), ViewModel.Grayscale);
        }
    }

    private bool IsLoadInProgress()
    {
        // No loading task?
        if (_initImageLoadTask == null)
            return false;

        // Loading task is still running?
        if (!_initImageLoadTask.IsCompleted)
            return true;

        // Query the load task results and re-throw any exceptions
        // so Win2D can see them. This implements requirement #2.
        try
        {
            _initImageLoadTask.Wait();
        }
        catch (AggregateException aggregateException)
        {
            // .NET async tasks wrap all errors in an AggregateException.
            // We unpack this so Win2D can directly see any lost device errors.
            aggregateException.Handle(exception => { throw exception; });
        }
        finally
        {
            _initImageLoadTask = null;
        }

        return false;
    }

    private static Rect? DrawBitmapToView(CanvasDrawingSession session, CanvasVirtualBitmap? bitmap, Size canvasSize, bool grayscale)
    {
        if (bitmap == null)
            return null;

        double canvasAspect = canvasSize.Width / canvasSize.Height;
        double bitmapAspect = (bitmap?.Bounds.Width ?? 1.0) / (bitmap?.Bounds.Height ?? 1.0);
        Size imageRenderSize;
        Point imagePos;

        if (bitmapAspect > canvasAspect)
        {
            imageRenderSize = new Size(
                canvasSize.Width,
                canvasSize.Width / bitmapAspect
            );
            imagePos = new Point(0, (canvasSize.Height - imageRenderSize.Height) / 2);
        }
        else
        {
            imageRenderSize = new Size(
                canvasSize.Height * bitmapAspect,
                canvasSize.Height
            );
            imagePos = new Point((canvasSize.Width - imageRenderSize.Width) / 2, 0);
        }

        Rect destBounds = new(imagePos, imageRenderSize);

        ICanvasImage? finalImage = grayscale ? new GrayscaleEffect() { Source = bitmap } : bitmap;

        session.DrawImage(finalImage, destBounds, bitmap?.Bounds ?? new Rect());

        return destBounds;
    }

    private void SlideCanvas_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
    {
        args.TrackAsyncAction(CreateResourceAsync(sender).AsAsyncAction());
    }

    async Task CreateResourceAsync(CanvasControl sender)
    {
        // Cancel old load
        if (_initImageLoadTask != null)
        {
            _initImageLoadTask.AsAsyncAction().Cancel();
            try { await _initImageLoadTask; } catch { }
            _initImageLoadTask = null;

        }

        _initImageLoadTask = FillImageCacheAsync(sender).ContinueWith(_ => SlideCanvas.Invalidate());
    }

    private async Task FillImageCacheAsync(CanvasControl resourceCreator)
    {
        ViewModel.UpdateCurrentImagesCommand?.Execute(null);

        var prevBitmapTask = CanvasVirtualBitmap.LoadAsync(resourceCreator, ViewModel.PreviousImagePath!);
        var currBitmapTask = CanvasVirtualBitmap.LoadAsync(resourceCreator, ViewModel.CurrentImagePath!);
        var nextBitmapTask = CanvasVirtualBitmap.LoadAsync(resourceCreator, ViewModel.NextImagePath!);

        _prevBitmap = (ViewModel.PreviousImagePath!, await prevBitmapTask);
        _currentBitmap = (ViewModel.CurrentImagePath!, await currBitmapTask);
        _nextBitmap = (ViewModel.NextImagePath!, await nextBitmapTask);
    }
}
