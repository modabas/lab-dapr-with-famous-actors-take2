using Dapper.FluentMap.Mapping;
using PublisherService.Core.Database.OutboxPattern.Entity;

namespace PublisherService.Infrastructure.Database.Postgres.Dapper.Mapper;

public class OutboxMap : EntityMap<OutboxEntity>
{
    public OutboxMap()
    {
        Map(p => p.Position).ToColumn("position");
        Map(p => p.MessageId).ToColumn("message_id");
        Map(p => p.PubSubName).ToColumn("pubsub_name");
        Map(p => p.TopicName).ToColumn("topic_name");
        Map(p => p.CreatedAt).ToColumn("created_at");
        Map(p => p.CreatedDate).ToColumn("created_date");
        Map(p => p.MessageContent).ToColumn("message_content");
        Map(p => p.MessageType).ToColumn("message_type");
    }
}
