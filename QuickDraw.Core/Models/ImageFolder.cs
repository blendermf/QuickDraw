using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QuickDraw.Core.Models;

public class ImageFolder
{
    public string Path { get; }
    public int ImageCount { get; set; }

    [JsonIgnore]
    public bool IsLoading { get; set; } = false;

    public bool Selected { get; set; } = false;

    public ImageFolder(string path, int imageCount = 0, bool selected = false, bool isLoading = false)
    {
        Path = path;
        ImageCount = imageCount;
        Selected = selected;
        IsLoading = isLoading;
    }
}
