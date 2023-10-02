using ConsumerService.Infrastructure.GreetProcessorService.Orleans;
using Dapr;
using Microsoft.AspNetCore.Mvc;
using Shared.GreetService.Events;
using Shared.Orleans;
using Shared.OutboxPattern;

namespace ConsumerService.Web.GreetProcessorService
{
    [ApiController]
    [Route("[controller]")]
    public class GreetProcessorController : ControllerBase
    {
        private readonly IGrainFactory _grainFactory;

        public GreetProcessorController(IGrainFactory grainFactory)
        {
            _grainFactory = grainFactory;
        }

        [HttpPost("Receive")]
        [Topic("take2pubsub", "greetings")]
        public async Task<IActionResult> Receive([FromBody] OutboxMessage<GreetingReceived> request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Message is null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var greetedGrain = _grainFactory.GetGrain<IGreetedGrain>(request.Message.To);
            using (var grainCancellationTokenSource = new GrainCancellationTokenSource())
            {
                using (grainCancellationTokenSource.Link(cancellationToken))
                {
                    await greetedGrain.Process(request.Message.From, request.Message.Message, grainCancellationTokenSource.Token);
                }
            }
            return Ok();
        }
    }
}