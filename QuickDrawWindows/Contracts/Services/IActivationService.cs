using System.Threading.Tasks;

namespace QuickDraw.Contracts.Services;

public interface IActivationService
{
    Task ActivateAsync(object activationArgs);
}
