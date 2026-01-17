namespace Upload.Api.Messaging
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(T @event, string routingKey, CancellationToken ct = default) where T : class;
    }
}
