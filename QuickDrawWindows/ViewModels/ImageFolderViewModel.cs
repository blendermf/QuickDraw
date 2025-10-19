using CommunityToolkit.Mvvm.ComponentModel;
using QuickDraw.Core.Models;

namespace QuickDraw.ViewModels;

public partial class ImageFolderViewModel : ObservableObject
{
    private ImageFolder _imageFolder;
    public string Path { get => _imageFolder.Path; }

    [ObservableProperty]
    public partial int ImageCount { get; set; }

    [ObservableProperty]
    public partial bool Selected {  get; set; }

    public ImageFolderViewModel(ImageFolder imageFolder)
    {
        _imageFolder = imageFolder;

        ImageCount = imageFolder.ImageCount;
        Selected = imageFolder.Selected;
    }
}
