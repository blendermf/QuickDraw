using System;

namespace QuickDraw.Contracts.Services;

public interface IPageService
{
    Type GetPageType(string key);
}