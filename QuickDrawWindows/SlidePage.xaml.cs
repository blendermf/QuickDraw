using CommunityToolkit.Common;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.AllJoyn;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace QuickDraw
{
    enum LoadDirection
    {
        Backwards,
        Forwards
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SlidePage : Page
    {

        private const int CACHE_SIZE = 9;
        private const int HALF_CACHE_SIZE = CACHE_SIZE / 2;

        private readonly List<string> imagePaths = new()
        {
            @"C:\Users\blendermf\Pictures\Reference\4b8e79076da0a545f5d8474d48d6aba9.jpg",
            @"C:\Users\blendermf\Pictures\Reference\b266995705bf837bd882ef7ec8a0c658.jpg",
            @"C:\Users\blendermf\Pictures\Reference\ce509886c60122d6841b5a3c40e40c13.jpg",

            @"C:\Users\blendermf\Pictures\Reference\Baton_Rouge_Louisiana (18 of 108).jpg",
            @"C:\Users\blendermf\Pictures\Reference\5ffd4bf8b2e9765d4357e39da574e65a.jpg", 
            @"C:\Users\blendermf\Pictures\Reference\ea193f9771bbe55c1562ca8587371c2f.jpg",

            @"C:\Users\blendermf\Pictures\Reference\4e671baaf8d17256d82978d04bb68275.jpg",
            @"C:\Users\blendermf\Pictures\Reference\5f1f0087ebb95483a19f3acf438248c3.jpg",
            @"C:\Users\blendermf\Pictures\Reference\9c9c5055fcfa58b4a4c8c0969a908984.jpg",

            @"D:\Reference\Image Reference\Poses\Women\00ab97eb3768fe67600e16e4e2f4be28.jpg",
            @"D:\Reference\Image Reference\Poses\Women\00d8a150d3327954bb6d217c94d93e10.jpg",
            @"D:\Reference\Image Reference\Poses\Women\0cc5e36c58c975b8f580af6ba9104fc1.jpg",

            @"D:\Reference\Image Reference\Poses\Women\0d44ac6bbc2f76ad0dbd5a6a4e49656f.jpg",
            @"D:\Reference\Image Reference\Poses\Women\01c3307c28a95405b448f8ba6b55c8c8.jpg",
            @"D:\Reference\Image Reference\Poses\Women\01d050c898dbdbfc16f8c58984f6cfb0.jpg",

            @"D:\Reference\Image Reference\Poses\Women\01d6068f148122e65c20d71700a172cd.jpg",
            @"D:\Reference\Image Reference\Poses\Women\0fe196d54935159419d425c96e084839.jpg",
            @"D:\Reference\Image Reference\Poses\Women\0d43509436a0dea9e92274def74be668.jpg",

            @"D:\Reference\Image Reference\Poses\Women\1b7cebc4eec7b5a8bd7a8eb8cbfc0561.jpg",
            @"D:\Reference\Image Reference\Poses\Women\1c91b180a9bba47b9af3e9e65f08b6e0.jpg",
            @"D:\Reference\Image Reference\Poses\Women\1bc1899d6610ed5322316abcf1ca14ea.jpg",

            @"D:\Reference\Image Reference\Poses\Women\0e6817b0c076405a00fb987f4f405eea.jpg",
            @"D:\Reference\Image Reference\Poses\Women\0e92728c5c2758332a5b141d174b5696.jpg",
            @"D:\Reference\Image Reference\Poses\Women\0ef896c25863a1a4b1fdf60b901c1fd6.jpg",

            @"D:\Reference\Image Reference\Poses\Women\2c5d9ca90e2bc17a33a15508e5e4cce6.jpg",
            @"D:\Reference\Image Reference\Poses\Women\2d897822c8ba784742b96fdf39880494.jpg",
            @"D:\Reference\Image Reference\Poses\Women\2df95b09f17abe50abffc7a1d43e2324.jpg",
        };

        private readonly LinkedList<CanvasVirtualBitmap> cachedImages = new();

        private LinkedListNode<CanvasVirtualBitmap> currentImageNode = null;
        private int imageCachePosition = 0;
        private Task imageLoadTask;

        private readonly object cachedImagesLock = new();

        public SlidePage()
        {
            this.InitializeComponent();
        }

        private void CanvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            HandleLoadExceptions();

            if (currentImageNode == null)
            {
                Debug.WriteLine("Nothing to Show");
            }
            else
            {
                CanvasVirtualBitmap bitmap;

                bitmap = currentImageNode.Value;

                Size canvasSize = new(sender.ActualWidth, sender.ActualHeight);
                double canvasAspect = canvasSize.Width / canvasSize.Height;
                double bitmapAspect = bitmap.Bounds.Width / bitmap.Bounds.Height;
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
                args.DrawingSession.DrawImage(bitmap, new Rect(imagePos, imageRenderSize), bitmap.Bounds);
            }
        }

        private void LoadImageInit()
        {
            Debug.Assert(imageLoadTask == null);

            imageLoadTask = FillImageCacheAsync(this.SlideCanvas, imageCachePosition).ContinueWith(async _ => {
                SlideCanvas.Invalidate(); 
            });
        }

        private static int Mod (int n, int d)
        {
            int r = n % d;
            return r < 0 ? r+d : r;
        }

        private async Task FillImageCacheAsync(CanvasControl resourceCreator, int index)
        {
            var bitmap = await CanvasVirtualBitmap.LoadAsync(resourceCreator, imagePaths[index]);

            currentImageNode = cachedImages.AddFirst(bitmap);

            imageCachePosition = index;

            var remainingCacheSize = Math.Min(imagePaths.Count, CACHE_SIZE) - 1;
            var numBefore = (remainingCacheSize / 2);
            var numAfter = (remainingCacheSize / 2) + (remainingCacheSize % 2);

            var beforeImages = Enumerable.Range(index - numBefore, numBefore)
                .Reverse()
                .Select(i => Mod(i, imagePaths.Count))
                .ToArray()
                .Select(i =>
                {
                    return imagePaths[i];
                });
            var afterImages = Enumerable.Range(index + 1, numAfter)
                .Select(i => Mod(i, imagePaths.Count))
                .ToArray()
                .Select(i =>
                {
                    return imagePaths[i];
                });

            async Task LoadBeforeAsync()
            {
                foreach (var image in beforeImages)
                {
                    var bitmap = await CanvasVirtualBitmap.LoadAsync(resourceCreator, image);
                    lock(cachedImages)
                    {
                        cachedImages.AddFirst(bitmap);
                    }
                }
            }

            async Task LoadAfterAsync()
            {
                foreach (var image in afterImages)
                {
                    var bitmap = await CanvasVirtualBitmap.LoadAsync(resourceCreator, image);
                    lock (cachedImages)
                    {
                        cachedImages.AddLast(bitmap);
                    }
                }
            }

            Task loadBeforeTask = Task.Run(LoadBeforeAsync);
            Task loadAfterTask = Task.Run(LoadAfterAsync);
            await loadBeforeTask;
            await loadAfterTask;
        }

        private void SlideCanvas_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
        {
            args.TrackAsyncAction(CreateResourcesAsync().AsAsyncAction());
        }

        private async Task CreateResourcesAsync()
        {
            // Cancel old load
            if (imageLoadTask != null)
            {
                imageLoadTask.AsAsyncAction().Cancel();
                try { await imageLoadTask; } catch { }
                imageLoadTask = null;

            }

            // Unload previously loaded images
            currentImageNode = null;
            cachedImages.Clear();

            LoadImageInit();
        }

        private void HandleLoadExceptions()
        {
            if (imageLoadTask == null || !imageLoadTask.IsCompleted)
                return;

            try
            {
                imageLoadTask.Wait();
            }
            catch(AggregateException aggregateException)
            {
                aggregateException.Handle(exception => { throw exception; });
            }
        }

        private async Task UpdateImageAsync(CanvasControl resourceCreator, LoadDirection direction)
        {
            var increment = direction == LoadDirection.Forwards ? 1 : -1;
            var halfCache = direction == LoadDirection.Forwards ? HALF_CACHE_SIZE : -HALF_CACHE_SIZE;

            var imageIndex = Mod(imageCachePosition + increment + halfCache, imagePaths.Count);

            var bitmap = await CanvasVirtualBitmap.LoadAsync(resourceCreator, imagePaths[imageIndex]);

            switch (direction)
            {
                case LoadDirection.Backwards:
                    {
                        cachedImages.AddFirst(bitmap);

                        cachedImages.Last.Value.Dispose();
                        cachedImages.RemoveLast();
                    }
                    break;

                case LoadDirection.Forwards:
                    {
                        cachedImages.AddLast(bitmap);

                        cachedImages.First.Value.Dispose();
                        cachedImages.RemoveFirst();
                    }
                    break;
            }

            imageCachePosition = Mod(imageCachePosition + increment, imagePaths.Count);
        }

        private async Task Move(LoadDirection direction)
        {

            if (imagePaths.Count > CACHE_SIZE)
            {

                await UpdateImageAsync(this.SlideCanvas, direction);
            }

            var increment = direction == LoadDirection.Forwards ? 1 : -1;

            if (imagePaths.Count <= CACHE_SIZE)
            {
                currentImageNode = direction == LoadDirection.Forwards ?
                    (currentImageNode.Next ?? cachedImages.First) :
                    (currentImageNode.Previous ?? cachedImages.Last);
            }
            else
            {
                currentImageNode = direction == LoadDirection.Forwards ?
                    currentImageNode.Next :
                    currentImageNode.Previous;
            }

            SlideCanvas.Invalidate();
        }

        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            await Move(LoadDirection.Backwards);
        }

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            await Move(LoadDirection.Forwards);
        }
    }
}
