using QuickDraw.Core.Models;
using System.Threading.Tasks;

namespace QuickDraw.Contracts.Services;

public interface ISettingsService
{
    public Settings? Settings { get; }

    public Task InitializeAsync();
    public Task ReadSettings();
    public Task WriteSettings();
}
