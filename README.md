# lab-dapr-with-famous-actors-take2
Dapr enabled Microsoft Orleans services co-hosted with grpc service in same generic host.

WebApi service layer doesn't use orleans client to reach orleans silo, since it can call orleans silo directly since they are hosted in same generic host.

Publisher and consumer services communicate over Dapr event publishing utilizing outbox pattern over Postgresql database and Logical Replication. Postgresql replication messages are processed by a grain service in same Orleans silo as publisher, these messages are written to RabbitMq over Dapr pub/sub.

## Microsoft Orleans:
[Dapr has actor support](https://docs.dapr.io/concepts/faq/#what-is-the-relationship-between-dapr-orleans-and-service-fabric-reliable-actors) based on [Microsoft Orleans](https://dotnet.github.io/orleans/). 

But since this is a .Net project, we can cast most famous actors for our project by utilizing Microsoft Orleans for our dapr enabled services. Since Orleans is around for much longer, it has many more features not yet implemented by Dapr actors.

## Outbox Pattern:
Whenever data is written to a table in Postgres db, an event can be written to an outbox table in same Postgres Db but in a different schema within same DbTransaction. Records in outbox table are processed seperately and published to Dapr.

## Debugging:
[Dapr Sidekick for .Net](https://github.com/man-group/dapr-sidekick-dotnet) helps immensely to setting up a Dapr enabled .Net application project and also debugging experience.

## Distributed tracing & logging support:
Asp.Net Core, Postgresql and Dapr has distributed tracing support out of the box. Also Microsoft Orleans supports it starting from 7.0. 

This project uses OpenTelemetry tracing/logging and exports them to Elasticsearch via an open telemetry collector. Docker compose file to spin up an Otel collector, Elasticsearch and Kibana instance on docker desktop for development environment is in [dockercompose-observability folder](https://github.com/modabas/lab-dapr-with-famous-actors-take2/tree/master/dockercompose-observability).

[OpenTelemetry documents](https://opentelemetry.io/docs/instrumentation/net/getting-started/) provide an excellent starting point on how to add OpenTelemetry supoort to a .Net project.

## Setup
1. Start up a Postgresql instance with logical replication enabled. Such a docker container can be started with following command:
```powershell
docker run -dt --restart unless-stopped -d -p 5432:5432 --name=postgres15.1 -e POSTGRES_PASSWORD=password postgres:15.1 -c 'wal_level=logical'
```

2. [Script](https://github.com/modabas/lab-dapr-with-famous-actors-take2/blob/master/src/PublisherService/Infrastructure/Database/Postgres/OutboxPattern/Scripts/Init.txt) for creating Postgresql Db tables used in this project.

3. Dapr configuration files are in [components folder](https://github.com/modabas/lab-dapr-with-famous-actors-take2/tree/master/components). Projects are configured to use these files on startup by Dapr Sidekick configuration.

4. Rabbitmq is used as Dapr pubsub component. A docker container running Rabbitmq with management support can be started with following command:
```powershell
docker run -dt --restart unless-stopped -d -p 5672:5672 -p 15672:15672 --hostname my-rabbit --name rabbitmq3 rabbitmq:3-management
```
Rabbitmq user/password used by Dapr is configured in "host" parameter value in dapr pubsub.yaml configuration file. A user/password pair with these values can be created via RabbitMq management.

## More References
1. [Postgres Logical Replication](https://www.npgsql.org/doc/replication.html)
2. [Push-based Outbox Pattern with Postgres Logical Replication](https://event-driven.io/en/push_based_outbox_pattern_with_postgres_logical_replication/)
