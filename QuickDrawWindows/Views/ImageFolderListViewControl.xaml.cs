using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using QuickDraw.Core.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;
using CommunityToolkit.WinUI;
using System.Threading.Tasks;
using System.Collections.Specialized;
using QuickDraw.Utilities;
using System.Diagnostics;
using WinRT;
using System.Collections.Concurrent;
using Windows.System;
using System.Reflection;
using QuickDraw.Contracts.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace QuickDraw.Views;

public static class TextBlockExtensions
{
    public static double Width(this string value)
    {
        var tempTextBlock = new TextBlock { Text = value };

        tempTextBlock.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

        return tempTextBlock.ActualWidth;
    }
}

public sealed partial class ImageFolderListViewControl : UserControl, INotifyPropertyChanged
{
    public MFObservableCollection<ImageFolder>? ImageFolderCollection = null;
    public event PropertyChangedEventHandler? PropertyChanged;

    GridLength _desiredPathColumnWidth = new(0, GridUnitType.Auto);
    public GridLength DesiredPathColumnWidth
    {
        get => _desiredPathColumnWidth;
        set
        {
            _desiredPathColumnWidth = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DesiredPathColumnWidth)));
        }
    }

    GridLength _desiredImageCountColumnWidth = new(0, GridUnitType.Auto);
    public GridLength DesiredImageCountColumnWidth
    {
        get => _desiredImageCountColumnWidth;
        set
        {
            _desiredImageCountColumnWidth = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DesiredImageCountColumnWidth)));
        }
    }

    private Point lastPointerPos = new();
    private List<ImageFolder> ItemsDraggedSelected = [];

    private HashSet<ImageFolder> foldersBeingReplaced = [];

    private ImageFolder? maxPathWidthImageFolder;
    private ImageFolder? maxImageCountWidthImageFolder;

    private bool startedRefresh = false;

    private ISettingsService _settingsService;

    private Settings? _settings => _settingsService.Settings;

    public ImageFolderListViewControl()
    {
        _settingsService = App.GetService<ISettingsService>();
        this.InitializeComponent();


        if (_settings != null)
        {
            ImageFolderCollection = [.. _settings.ImageFolderList.ImageFolders];
            UpdateMaxColumnWidths();
        }

        ImageFolderListView.Loaded += (sender, e) =>
        {
            foreach (var i in ImageFolderCollection?.Index().Where(ft => ft.Item.Selected).Select(ft => ft.Index) ?? [])
            {
                ImageFolderListView.SelectRange(new(i, 1));
            }

            if (ImageFolderListView.SelectedItems.Count == (ImageFolderCollection?.Count ?? 0))
            {
                SelectAllCheckbox.IsChecked = true;
            }
            else
            {
                SelectAllCheckbox.IsChecked = false;
            }

            ImageFolderListView.SelectionChanged += ImageFolderListView_SelectionChanged;

        };

        if (ImageFolderCollection != null)
        {
            ImageFolderCollection.CollectionChanged += (sender, e) =>
            {
                if (!startedRefresh)
                {
                    if (e.NewItems != null)
                    {
                        foreach (ImageFolder item in e.NewItems)
                        {
                            if (item.IsLoading)
                            {
                                startedRefresh = true;
                                RefreshAll.IsEnabled = false;
                            }
                        }
                    }
                }
                else
                {
                    if (!ImageFolderCollection.Where(f => f.IsLoading).Any())
                    {
                        RefreshAll.IsEnabled = true;
                        startedRefresh = false;
                    }
                }

                if ((e as MFNotifyCollectionChangedEventArgs)?.FromModel ?? false)
                {
                    if (e.Action == NotifyCollectionChangedAction.Replace)
                    {
                        if (e.NewItems != null)
                        {
                            foreach (ImageFolder item in e.NewItems)
                            {
                                if (item.Selected)
                                {
                                    foldersBeingReplaced.Add(item);
                                }
                            }
                        }
                    }

                    return;
                }
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Remove:
                        if (e.OldItems != null)
                        {
                            _settings?.ImageFolderList.ImageFolders.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                        }

                        break;

                    case NotifyCollectionChangedAction.Add:
                        if (e.NewItems != null)
                        {
                            if (e.NewStartingIndex != -1)
                            {
                                _settings?.ImageFolderList.ImageFolders.InsertRange(e.NewStartingIndex, e.NewItems.OfType<ImageFolder>());
                            }
                            else
                            {
                                _settings?.ImageFolderList.ImageFolders.AddRange(e.NewItems.OfType<ImageFolder>());
                            }
                        }
                        break;

                    default:
                        break;
                }
                _settingsService?.WriteSettings();
            };
        }

        if (_settings?.ImageFolderList != null)
        {
            _settings.ImageFolderList.CollectionChanged += (sender, e) =>
            {
                DispatcherQueue.EnqueueAsync(() =>
                {
                    var i = 0;
                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            if (e.NewItems != null)
                            {
                                i = 0;
                                foreach (var item in e.NewItems)
                                {
                                    if (item is ImageFolder folder)
                                    {
                                        ImageFolderCollection?.AddFromModel(folder);
                                    }
                                    i++;
                                }
                                UpdateMaxColumnWidths();
                            }
   
                            break;

                        case NotifyCollectionChangedAction.Replace:
                            if (e.NewItems != null)
                            {
                                i = 0;
                                foreach (var item in e.NewItems)
                                {
                                    if (item is ImageFolder folder)
                                    {
                                        ImageFolderCollection?.SetFromModel(e.NewStartingIndex + i, folder);
                                    }
                                    i++;
                                }
                                UpdateMaxColumnWidths();
                            }
                            break;

                        case NotifyCollectionChangedAction.Remove:
                            if (e.OldItems != null)
                            {
                                foreach (var item in e.OldItems)
                                {
                                    if (item is ImageFolder folder)
                                    ImageFolderCollection?.RemoveFromModel(folder);
                                }
                                UpdateMaxColumnWidths();
                            }
                            break;

                        default:
                            break;
                    }
                });

                _settingsService?.WriteSettings();
            };
        }

    }

    private void UpdateMaxColumnWidths()
    {
        maxPathWidthImageFolder = ImageFolderCollection?.MaxBy(f => f.Path.Width());
        maxImageCountWidthImageFolder = ImageFolderCollection?.MaxBy(f => f.ImageCount.ToString().Width());

        UpdateColumnWidths();
    }

    private void UpdateColumnWidths()
    {
        if (maxPathWidthImageFolder == null || maxImageCountWidthImageFolder == null) return;

        var maxPathWidth = maxPathWidthImageFolder?.Path.Width() ?? 0;
        var maxImageCountWidth = maxImageCountWidthImageFolder?.ImageCount.ToString().Width() ?? 0;

        Grid? grid = null;

        if (maxPathWidthImageFolder != null)
        {
            grid = (ImageFolderListView.ContainerFromItem(maxPathWidthImageFolder) as FrameworkElement)?.FindDescendant<Grid>();
        }

        ProgressRing? progressRing = grid?.FindDescendant<ProgressRing>();

        var ImageCountColumnWidth = Math.Max(maxImageCountWidth, progressRing?.ActualWidth ?? 32) + 20;

        var gridWidth = grid?.ActualWidth ?? 0.0;
        var availableWidth = gridWidth - ((grid?.ColumnDefinitions[3].ActualWidth ?? 0.0) + ImageCountColumnWidth + 20);

        var maxPathColumnWidth = maxPathWidth + 1;
        var PathColumnWidth = Math.Max(100, Math.Min(availableWidth, maxPathColumnWidth));

        DesiredPathColumnWidth = new GridLength(PathColumnWidth);
        DesiredImageCountColumnWidth = new GridLength(ImageCountColumnWidth);
    }

    private void MFImageFolderControl_SizeChanged(object sender, SizeChangedEventArgs args)
    {
         UpdateColumnWidths();
    }

    private void OpenFolders()
    {
        IFileOpenDialog? dialog = null;
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
                string? folderpath = null;
                shellItemArray.GetCount(out count);

                List<string> paths = new List<string>();

                for (uint i = 0; i < count; i++)
                {
                    shellItemArray.GetItemAt(i, out IShellItem shellItem);

                    if (shellItem != null)
                    {
                        shellItem.GetDisplayName(SIGDN.FILESYSPATH, out IntPtr i_result);
                        folderpath = Marshal.PtrToStringAuto(i_result);
                        Marshal.FreeCoTaskMem(i_result);

                        if (folderpath != null)
                        {
                            paths.Add(folderpath);
                        }
                    }
                }

                /*var settings = (App.Current as App)?.Settings;
                settings?.ImageFolderList.AddFolderPaths(paths);*/
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
    }

    private void AddFoldersButton_Click(object sender, RoutedEventArgs e)
    {

        OpenFolders();
    }

    private void ImageFolderListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool settingsChanged = false;

        foreach (var item in e.AddedItems.Cast<ImageFolder>())
        {
            var (index, itemInList) = ImageFolderCollection?.Index().FirstOrDefault(t => t.Item.Path == item.Path) ?? (-1, null);

            if (foldersBeingReplaced.Any() && foldersBeingReplaced.Where(f => f.Path == item.Path).Any())
            {
                if (itemInList.Selected)
                {
                    ImageFolderListView.SelectedItems.Add(itemInList);
                }

                foldersBeingReplaced.RemoveWhere(f => f.Path == item.Path);
            }

            else if (index >= 0 && _settings != null)
            {
                _settings.ImageFolderList.ImageFolders[index].Selected = true;
                settingsChanged = true;
            }

        }

        foreach (var item in e.RemovedItems.Cast<ImageFolder>())
        {
            var (index, itemInList) = ImageFolderCollection?.Index().FirstOrDefault(t => t.Item.Path == item.Path) ?? (-1, null);

            if (ItemsDraggedSelected.Where(f => f.Path == item.Path).Any())
            {
                ImageFolderListView.SelectedItems.Add(itemInList);
                continue;
            }

            if (foldersBeingReplaced.Any() && foldersBeingReplaced.Where(f => f.Path == item.Path).Any())
            {
                if (itemInList.Selected)
                {
                    ImageFolderListView.SelectedItems.Add(itemInList);
                }
            }
            else if (index >= 0 && _settings != null)
            {
                _settings.ImageFolderList.ImageFolders[index].Selected = false;
                settingsChanged = true;
            }
        }

        if (ImageFolderListView.SelectedItems.Count == ImageFolderCollection?.Count)
        {
            SelectAllCheckbox.IsChecked = true;
        } else
        {
            SelectAllCheckbox.IsChecked = false;
        }

        if (settingsChanged)
        {
            _settingsService?.WriteSettings();
        }
    }

    private void ImageFolderListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        var items = e.Items.Cast<ImageFolder>();

        var hovereditems = VisualTreeHelper.FindElementsInHostCoordinates(lastPointerPos, ImageFolderListView);
        var hoveritemelem = hovereditems.First(i => i is ListViewItem);
        var hoveritem = ImageFolderListView.ItemFromContainer(hoveritemelem as ListViewItem);

        ItemsDraggedSelected.AddRange(items.Where(f => f.Selected));

        foreach (var item in items)
        {
            if (item != hoveritem)
            {
                var itemelem = ImageFolderListView.ContainerFromItem(item) as ListViewItem;

                VisualStateManager.GoToState(itemelem, "DragHidden", true);
            }
        }
    }

    private void ImageFolderListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        var items = args.Items.Cast<ImageFolder>();

        foreach (var item in items)
        {
            var itemelem = ImageFolderListView.ContainerFromItem(item) as ListViewItem;

            VisualStateManager.GoToState(itemelem, "DragVisible", true);
        }
        ItemsDraggedSelected.Clear();
    }

    private void ImageFolderListView_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        lastPointerPos = e.GetCurrentPoint(null).Position;
    }

    private void SelectAllCheckbox_Click(object sender, RoutedEventArgs e)
    {
        if (SelectAllCheckbox.IsChecked ?? false) { ImageFolderListView.SelectAll(); }
        else { ImageFolderListView.DeselectAll(); }

    }

    public async Task<IEnumerable<string>> GetSelectedFolders()
    {
        return await DispatcherQueue.EnqueueAsync(() =>
        {
            return ImageFolderListView.SelectedItems.Cast<ImageFolder>().Select(f => f.Path).ToList();
        });
    }

    private void RefreshAll_Click(object sender, RoutedEventArgs e)
    {
        _settings?.ImageFolderList.UpdateFolderCounts();
        RefreshAll.IsEnabled = false;
        startedRefresh = true;
    }
}
