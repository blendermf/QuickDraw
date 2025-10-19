using QuickDraw.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuickDraw.Contracts.Services;

public interface ISlideImageService
{
    public Task<int> LoadImages(IEnumerable<string> folders);

    public List<string> Images { get; }

    public TimerEnum SlideDuration { get; set; }
}

