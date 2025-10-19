using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QuickDraw.Core.Models;

public enum TimerEnum
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

public static class TimerEnumExtension
{
    private static Dictionary<TimerEnum, uint> TimerEnumToSeconds { get; } = new()
    {
        { TimerEnum.T30s, 30 },
        { TimerEnum.T1m, 60 },
        { TimerEnum.T2m, 120 },
        { TimerEnum.T5m, 300 },
        { TimerEnum.NoLimit, 0 }
    };

    public static uint ToSeconds(this TimerEnum e)
    {
        return TimerEnumToSeconds[e];
    }

    public static double ToSliderValue(this TimerEnum e)
    {
        return (double)((int)e);
    }

    public static TimerEnum ToTimerEnum(this double e)
    {
        return (TimerEnum)(Math.Clamp((int)e,0, 4));
    }
}

public class Settings
{
    public ImageFolderList ImageFolderList { get; set; } = new ImageFolderList();

    public TimerEnum SlideTimerDuration { get; set; }
}
