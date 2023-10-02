using Microsoft.AspNetCore.Mvc;
using PublisherService.Infrastructure.GreetService.Orleans;
using Shared.Orleans;

namespace PublisherService.Web.GreetService
{
    [ApiController]
    [Route("[controller]")]
    public class GreetController : ControllerBase
    {
        private readonly IGrainFactory _grainFactory;

        public GreetController(IGrainFactory grainFactory)
        {
            _grainFactory = grainFactory;
        }

        [HttpPost("Send")]
        public async Task<IActionResult> Send([FromBody] GreetingRequest request, CancellationToken cancellationToken)
        {
            var greeterGrain = _grainFactory.GetGrain<IGreeterGrain>(request.From);
            using (var grainCancellationTokenSource = new GrainCancellationTokenSource())
            {
                using (grainCancellationTokenSource.Link(cancellationToken))
                {
                    await greeterGrain.Send(request.To, request.Message, grainCancellationTokenSource.Token);
                }
            }
            return Ok();
        }
    }
}