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

        List<string> imagePaths = new List<string>
        {
            "C:\\Users\\blendermf\\Pictures\\Reference\\4b8e79076da0a545f5d8474d48d6aba9.jpg",
            "C:\\Users\\blendermf\\Pictures\\Reference\\b266995705bf837bd882ef7ec8a0c658.jpg",
            "C:\\Users\\blendermf\\Pictures\\Reference\\ce509886c60122d6841b5a3c40e40c13.jpg",

            "C:\\Users\\blendermf\\Pictures\\Reference\\Baton_Rouge_Louisiana (18 of 108).jpg",
            "C:\\Users\\blendermf\\Pictures\\Reference\\5ffd4bf8b2e9765d4357e39da574e65a.jpg",
            "C:\\Users\\blendermf\\Pictures\\Reference\\ea193f9771bbe55c1562ca8587371c2f.jpg",

            "C:\\Users\\blendermf\\Pictures\\Reference\\4e671baaf8d17256d82978d04bb68275.jpg",
            "C:\\Users\\blendermf\\Pictures\\Reference\\5f1f0087ebb95483a19f3acf438248c3.jpg",
            "C:\\Users\\blendermf\\Pictures\\Reference\\9c9c5055fcfa58b4a4c8c0969a908984.jpg",
        };

        LinkedList<CanvasVirtualBitmap> cachedImages = new LinkedList<CanvasVirtualBitmap> ();
        
        LinkedListNode<CanvasVirtualBitmap> currentImageNode = null;
        int currentImageIndex = 0;
        Task imageLoadTask;

        public SlidePage()
        {
            this.InitializeComponent();
            Task.Run(LoadImageInit);
        }

        private void CanvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            HandleLoadExceptions();

            if (currentImageNode == null)
            {

            }
            else
            {
                CanvasVirtualBitmap bitmap = currentImageNode.Value;

                Size canvasSize = new Size(sender.ActualWidth, sender.ActualHeight);
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
                args.DrawingSession.DrawImage(currentImageNode.Value, new Rect(imagePos, imageRenderSize), bitmap.Bounds);
            }
        }

        public void LoadImageInit()
        {
            Debug.Assert(imageLoadTask == null);
            imageLoadTask = FillImageCacheAsync(this.SlideCanvas, currentImageIndex).ContinueWith(_ => SlideCanvas.Invalidate());
        }

        int mod (int n, int d)
        {
            int r = n % d;
            return r < 0 ? r+d : r;
        }

        async Task FillImageCacheAsync(CanvasControl resourceCreator, int index)
        {
            var bitmap = await CanvasVirtualBitmap.LoadAsync(resourceCreator, imagePaths[index]);
            currentImageNode = cachedImages.AddFirst(bitmap);
            currentImageIndex = index;

            var remainingCacheSize = Math.Min(imagePaths.Count, CACHE_SIZE) - 1;
            var numBefore = (remainingCacheSize / 2);
            var numAfter = (remainingCacheSize / 2) + (remainingCacheSize % 2);

            var beforeImages = Enumerable.Range(index - numBefore, numBefore)
                .AsParallel()
                .Reverse()
                .Select(i => mod(i, imagePaths.Count))
                .ToArray()
                .Select(i =>
                {
                    return imagePaths[i];
                });
            var afterImages = Enumerable.Range(index + 1, numAfter)
                .AsParallel()
                .Select(i => mod(i, imagePaths.Count))
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

                    lock (cachedImages)
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

            Task loadBeforeTask = LoadBeforeAsync();
            Task loadAfterTask = LoadAfterAsync();
            await loadBeforeTask;
            await loadAfterTask;
        }

        async Task UpdateImageAsync(CanvasControl resourceCreator, LoadDirection direction)
        {
            switch (direction)
            {
                case LoadDirection.Backwards:
                {
                    cachedImages.Last.Value.Dispose();
                    cachedImages.RemoveLast();

                    var imageIndex = mod(currentImageIndex - HALF_CACHE_SIZE, imagePaths.Count);
                    cachedImages.AddFirst(await CanvasVirtualBitmap.LoadAsync(resourceCreator, imagePaths[imageIndex]));
                }
                break;

                case LoadDirection.Forwards:
                {
                    cachedImages.First.Value.Dispose();
                    cachedImages.RemoveFirst();

                    var imageIndex = mod(currentImageIndex + HALF_CACHE_SIZE, imagePaths.Count);
                    cachedImages.AddFirst(await CanvasVirtualBitmap.LoadAsync(resourceCreator, imagePaths[imageIndex]));
                }
                break;
            }
        }

        private void SlideCanvas_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
        {
            // Initially required synchronous resources (currently none)

            args.TrackAsyncAction(CreateResourcesAsync(sender).AsAsyncAction());
        }

        async Task CreateResourcesAsync(CanvasControl sender)
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

            // Refresh cache
            LoadImageInit();
        }

        void HandleLoadExceptions()
        {
            if (imageLoadTask == null || !imageLoadTask.IsCompleted)
                return;

            try
            {
                imageLoadTask.Wait();
            }
            catch (AggregateException aggregateException)
            {
                // .NET async tasks wrap all errors in an AggregateException.
                // We unpack this so Win2D can directly see any lost device errors.
                aggregateException.Handle(exception => { throw exception; });
            }
            finally
            {
                imageLoadTask = null;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            currentImageIndex = mod(currentImageIndex - 1, imagePaths.Count);

            if (imagePaths.Count <= CACHE_SIZE)
            {
                currentImageNode = currentImageNode.Previous ?? cachedImages.Last;
            }
            else
            {
                // Incremental load of cache
                currentImageNode = currentImageNode.Previous;
            }

            SlideCanvas.Invalidate();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            currentImageIndex = mod(currentImageIndex + 1, imagePaths.Count);

            if (imagePaths.Count <= CACHE_SIZE)
            {
                currentImageNode = currentImageNode.Next ?? cachedImages.First;
            } 
            else
            {
                // Incremental load of cache
                currentImageNode = currentImageNode.Next;
            }

            SlideCanvas.Invalidate();
        }
    }
}
