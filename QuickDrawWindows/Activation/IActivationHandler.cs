using System.Threading.Tasks;

namespace QuickDraw.Activation;

public interface IActivationHandler
{
    bool CanHandle(object args);

    Task HandleAsync(object args);
}
