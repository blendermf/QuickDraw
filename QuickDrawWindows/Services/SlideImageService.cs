using QuickDraw.Contracts.Services;
using QuickDraw.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace QuickDraw.Services;

static class TestRandom
{
    private static readonly Random _random = new Random(0);

    public static int GetInt32(Int32 from, Int32 to)
    {
        return _random.Next(from, to);
    }
}

static class ListExtensions
{
    public static void Shuffle<T>(this IList<T> list)
    {
        for (var i = 0; i < list.Count - 1; i++)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1, list.Count);

            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

public class SlideImageService : ISlideImageService
{
    public List<string> Images { get; private set; } = [];

    public TimerEnum SlideDuration { get; set; }

    public async Task<int> LoadImages(IEnumerable<string> folders)
    {
        try
        {
            Images = [.. await ImageFolderList.GetImagesForFolders(folders)];
            Images.Shuffle();
        }
        catch (Exception ex)
        {
            // TODO: Properly log
            Debug.WriteLine(ex);
        }

        return Images.Count;
    }
}