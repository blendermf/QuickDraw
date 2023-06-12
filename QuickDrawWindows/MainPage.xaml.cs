using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using CommunityToolkit.WinUI.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.Storage;
using System.Collections.ObjectModel;
using System.Diagnostics.Eventing.Reader;
using System.Collections.Specialized;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace QuickDraw
{
    /// <summary>
    /// Converts the value of the internal slider into text.
    /// </summary>
    /// <remarks>Internal use only.</remarks>
    internal class StringToEnumConverter : IValueConverter
    {
        private readonly Type _enum;

        public StringToEnumConverter(Type type)
        {
            _enum = type;
        }

        public object Convert(object value,
                Type targetType,
                object parameter,
                string language)
        {
            var _name = Enum.ToObject(_enum, (int)Double.Parse((string)value));

            // Look for a 'Display' attribute.
            var _member = _enum
                .GetRuntimeFields()
                .FirstOrDefault(x => x.Name == _name.ToString());
            if (_member == null)
            {
                return _name;
            }

            var _attr = (DisplayAttribute)_member
                .GetCustomAttribute(typeof(DisplayAttribute));
            if (_attr == null)
            {
                return _name;
            }

            return _attr.Name;
        }

        public object ConvertBack(object value,
            Type targetType,
            object parameter,
            string language)
        {
            return value; // Never called
        }

    }

    /// <summary>
    /// Converts the value of the internal slider into text.
    /// </summary>
    /// <remarks>Internal use only.</remarks>
    internal class DoubleToEnumConverter : IValueConverter
    {
        private readonly Type _enum;

        public DoubleToEnumConverter(Type type)
        {
            _enum = type;
        }

        public object Convert(object value,
                Type targetType,
                object parameter,
                string language)
        {
            var _name = Enum.ToObject(_enum, (int)(double)value);

            // Look for a 'Display' attribute.
            var _member = _enum
                .GetRuntimeFields()
                .FirstOrDefault(x => x.Name == _name.ToString());
            if (_member == null)
            {
                return _name;
            }

            var _attr = (DisplayAttribute)_member
                .GetCustomAttribute(typeof(DisplayAttribute));
            if (_attr == null)
            {
                return _name;
            }

            return _attr.Name;
        }

        public object ConvertBack(object value,
            Type targetType,
            object parameter,
            string language)
        {
            return value; // Never called
        }
    }
    public static class TextBlockExtensions
    {
        public static double PreWrappedWidth(this TextBlock textBlock)
        {
            var tempTextBlock = new TextBlock { Text = textBlock.Text };

            tempTextBlock.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            tempTextBlock.Arrange(new Rect(new Point(), textBlock.DesiredSize));

            return tempTextBlock.ActualWidth;
        }
    }

    public class ImageFolder
    {
        public string Path { get; set; }
        public int ImageCount { get; set; }

        public ImageFolder(string path, int imageCount)
        {
            Path = path;
            ImageCount = imageCount;
        }
    }

    public class GridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            double val = (double)value;
            GridLength gridLength = new(val);

            return gridLength;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            GridLength val = (GridLength)value;

            return val.Value;
        }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        enum TimerEnum
        {
            [Display(Name = "30s")]
            T30s,
            [Display(Name = "1m")]
            T1m,
            [Display(Name = "2m")]
            T2m,
            [Display(Name = "5m")]
            T5m,
            [Display(Name = "No Limit")]
            NoLimit
        };

        public ObservableCollection<ImageFolder> ImageFolders { get; set; }

        public class ByWidth : IComparer<TextBlock>
        {
            public int Compare(TextBlock x, TextBlock y)
            {
                int widthCompare = x.PreWrappedWidth().CompareTo(y.PreWrappedWidth());

                if (widthCompare == 0 && !ReferenceEquals(x, y))
                {
                    return 1;
                }

                return widthCompare;
            }
        }

        private readonly SortedSet<TextBlock> _pathTexts = new(new ByWidth());
        private readonly SortedSet<TextBlock> _imageCountTexts = new(new ByWidth());
        private readonly HashSet<ColumnDefinition> _pathColumns = new();
        private readonly HashSet<ColumnDefinition> _imageCountColumns = new();

        public double PathColumnWidth { get; set; } = 1000;
        public double ImageCountColumnWidth { get; set; } = 1000;

        public MainPage()
        {
            this.InitializeComponent();
            this.DataContext = this;
            this.Resources.Add("doubleToEnumConverter", new DoubleToEnumConverter(typeof(TimerEnum)));
            this.Resources.Add("stringToEnumConverter", new StringToEnumConverter(typeof(TimerEnum)));

            this.Loaded += async (_, _) =>
            {
                await ReadFolders();
            };


        }
        Task writeTask;
        Queue<Func<Task>> writeTasksQueue = new();

        private async Task _writeFolders()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appDataFolder = await StorageFolder.GetFolderFromPathAsync(appDataPath);

            var qdDataFolder = await appDataFolder.CreateFolderAsync("MFDigitalMedia.QuickDraw", CreationCollisionOption.OpenIfExists);

            var file = await qdDataFolder.CreateFileAsync("folders.json", Windows.Storage.CreationCollisionOption.OpenIfExists);

            using var stream = await file.OpenStreamForWriteAsync();
            await JsonSerializer.SerializeAsync(stream, ImageFolders);
            stream.Dispose();
        }

        // Writes folder, makes sure we don't overlap with other writes
        void WriteFolders()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (writeTask == null)
                {
                    void WriteContinue()
                    {
                        if (writeTasksQueue.Count > 0)
                        {
                            writeTask = writeTasksQueue.Dequeue()().ContinueWith(Task =>
                            {
                                WriteContinue();
                            });
                        }
                        else
                        {
                            writeTask = null;
                        }
                    }

                    writeTask = _writeFolders().ContinueWith(Task =>
                    {
                        WriteContinue();
                    });
                }
                else
                {
                    writeTasksQueue.Enqueue(_writeFolders);
                }
            });
        }

        async Task ReadFolders()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appDataFolder = await StorageFolder.GetFolderFromPathAsync(appDataPath);

            var qdDataFolder = await appDataFolder.CreateFolderAsync("MFDigitalMedia.QuickDraw", CreationCollisionOption.OpenIfExists);

            var file = await qdDataFolder.GetFileAsync("folders.json");

            using var stream = await file.OpenStreamForReadAsync();
            try
            {
                ImageFolders = await JsonSerializer.DeserializeAsync<ObservableCollection<ImageFolder>>(stream);
                stream.Dispose();
            }
            catch
            {
                // No data or other errors
                ImageFolders = new();
            }

            this.ImageFolderListView.ItemsSource = ImageFolders;
            ImageFolders.CollectionChanged += (sender, e) =>
            {
                WriteFolders();
            };
        }

        private void UpdateColumnWidths(Grid grid)
        {
            if (_pathTexts.Count < 1 || _imageCountTexts.Count < 1) { return; }

            ImageCountColumnWidth = _imageCountTexts.Max.ActualWidth + 20;

            var gridWidth = grid.ActualWidth;
            var availableWidth = gridWidth - (grid.ColumnDefinitions[3].ActualWidth + ImageCountColumnWidth + 20);

            var maxPathColumnWidth = _pathTexts.Max != null ? _pathTexts.Max.PreWrappedWidth() : 0;
            PathColumnWidth = Math.Max(100, Math.Min(availableWidth, maxPathColumnWidth));

            foreach (ColumnDefinition pathColumn in _pathColumns)
            {
                pathColumn.Width = new GridLength(PathColumnWidth, GridUnitType.Pixel);
            }

            foreach (ColumnDefinition imageCountColumn in _imageCountColumns)
            {
                imageCountColumn.Width = new GridLength(ImageCountColumnWidth, GridUnitType.Pixel);
            }

        }

        private void PathText_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var pathText = (TextBlock)sender;
            var grid = (Grid)pathText.Parent;

            _pathTexts.Remove(pathText);
            _pathTexts.Add(pathText);

            _pathColumns.Add(grid.ColumnDefinitions[0]);

            UpdateColumnWidths(grid);
        }

        private void ImageCountText_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var imageCountText = (TextBlock)sender;
            var grid = (Grid)imageCountText.Parent;

            _imageCountTexts.Remove(imageCountText);
            _imageCountTexts.Add(imageCountText);

            _imageCountColumns.Add(grid.ColumnDefinitions[1]);

            UpdateColumnWidths(grid);
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Grid grid = (Grid)sender;

            UpdateColumnWidths(grid);
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current as App)?.Window.NavigateToSlideshow();
        }

        private static IEnumerable<string> GetFolderImages(string filepath)
        {
            IEnumerable<string> files = Directory.EnumerateFiles(filepath, "*.*", SearchOption.AllDirectories)
                                    .Where(s => s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                                            || s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                                            || s.EndsWith(".png", StringComparison.OrdinalIgnoreCase));

            return files;
        }

        private void OpenFolders()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                IFileOpenDialog dialog = null;
                uint count = 0;
                try
                {
                    dialog = new NativeFileOpenDialog();
                    dialog.SetOptions(
                        FileOpenDialogOptions.NoChangeDir
                        | FileOpenDialogOptions.PickFolders
                        | FileOpenDialogOptions.AllowMultiSelect
                        | FileOpenDialogOptions.PathMustExist
                    );
                    _ = dialog.Show(IntPtr.Zero);

                    dialog.GetResults(out IShellItemArray shellItemArray);

                    if (shellItemArray != null)
                    {
                        string filepath = null;
                        shellItemArray.GetCount(out count);

                        for (uint i = 0; i < count; i++)
                        {
                            shellItemArray.GetItemAt(i, out IShellItem shellItem);

                            if (shellItem != null)
                            {
                                shellItem.GetDisplayName(SIGDN.FILESYSPATH, out IntPtr i_result);
                                filepath = Marshal.PtrToStringAuto(i_result);
                                Marshal.FreeCoTaskMem(i_result);

                                IEnumerable<string> files = GetFolderImages(filepath);

                                var folder = new ImageFolder(filepath, files.Count());

                                var oldFolder = ImageFolders.FirstOrDefault<ImageFolder>((f) => f.Path == folder.Path);
                                var folderIndex = ImageFolders.IndexOf(oldFolder);

                                if (folderIndex != -1)
                                {
                                    ImageFolders[folderIndex] = folder;
                                }
                                else
                                {
                                    ImageFolders.Add(folder);
                                }

                            }
                        }
                    }
                }
                catch (COMException)
                {
                    // No files or other weird error, do nothing.
                }
                finally
                {
                    if (dialog != null)
                    {
                        _ = Marshal.FinalReleaseComObject(dialog);
                    }
                }
            });
        }

        private void AddFoldersButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFolders();
        }
    }
}
