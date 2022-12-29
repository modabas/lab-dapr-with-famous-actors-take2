using Orleans;
using System.Threading;

namespace Shared.Orleans;

public static class GrainCancellationTokenSourceExtensions
{
    public static CancellationTokenRegistration Link(this GrainCancellationTokenSource grainCancellationTokenSource, CancellationToken cancellationToken)
    {
        //link cancellation token to grain cancellation token source for this scope, so grain token will be cancelled if token is cancelled
        return cancellationToken.Register(async () => await grainCancellationTokenSource.Cancel());
    }
}
