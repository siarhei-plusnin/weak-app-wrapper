using Confluent.Kafka;
using KafkaFlow;
using KafkaFlow.Serializer;
using Microsoft.EntityFrameworkCore;
using WeakAppWrapper.Contracts.Messages;
using WeakAppWrapper.Processor.Application;
using WeakAppWrapper.Processor.Configuration;
using WeakAppWrapper.Processor.Infrastructure.Kafka;
using WeakAppWrapper.Processor.Infrastructure.Persistence;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder
    .Services.AddOptions<KafkaConfiguration>()
    .Bind(builder.Configuration.GetSection(KafkaConfiguration.SectionName))
    .Validate(
        configuration => !string.IsNullOrWhiteSpace(configuration.BootstrapServers),
        "Kafka:BootstrapServers must be provided"
    )
    .Validate(configuration => !string.IsNullOrWhiteSpace(configuration.RawTopic), "Kafka:RawTopic must be provided")
    .Validate(
        configuration => !string.IsNullOrWhiteSpace(configuration.RawDeadLetterTopic),
        "Kafka:RawDeadLetterTopic must be provided"
    )
    .Validate(
        configuration => !string.IsNullOrWhiteSpace(configuration.ProcessedTopic),
        "Kafka:ProcessedTopic must be provided"
    )
    .Validate(
        configuration => !string.IsNullOrWhiteSpace(configuration.ConsumerGroup),
        "Kafka:ConsumerGroup must be provided"
    )
    .Validate(
        configuration => !string.IsNullOrWhiteSpace(configuration.ConsumerName),
        "Kafka:ConsumerName must be provided"
    )
    .Validate(configuration => !string.IsNullOrWhiteSpace(configuration.ClientId), "Kafka:ClientId must be provided")
    .Validate(configuration => configuration.WorkersCount > 0, "Kafka:WorkersCount must be greater than zero")
    .Validate(configuration => configuration.BufferSize > 0, "Kafka:BufferSize must be greater than zero")
    .ValidateOnStart();

string connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string is required");

KafkaConfiguration kafka =
    builder.Configuration.GetRequiredSection(KafkaConfiguration.SectionName).Get<KafkaConfiguration>()
    ?? throw new InvalidOperationException("Kafka configuration section is required");

builder.Services.AddDbContext<ProcessorDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsql =>
            npgsql.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null
            )
    )
);

builder.Services.AddSingleton<IProcessingEventPublisher, KafkaProcessingEventPublisher>();
builder.Services.AddScoped<WeakAppMetersProcessingService>();
builder.Services.AddKafkaFlowHostedService(kafkaFlow =>
    kafkaFlow.AddCluster(cluster =>
        cluster
            .WithBrokers(
                kafka.BootstrapServers.Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                )
            )
            .CreateTopicIfNotExists(kafka.RawTopic, numberOfPartitions: 1, replicationFactor: 1)
            .CreateTopicIfNotExists(kafka.RawDeadLetterTopic, numberOfPartitions: 1, replicationFactor: 1)
            .CreateTopicIfNotExists(kafka.ProcessedTopic, numberOfPartitions: 1, replicationFactor: 1)
            .AddConsumer(consumer =>
                consumer
                    .WithName(kafka.ConsumerName)
                    .Topic(kafka.RawTopic)
                    .WithGroupId(kafka.ConsumerGroup)
                    .WithWorkersCount(kafka.WorkersCount)
                    .WithBufferSize(kafka.BufferSize)
                    .WithAutoOffsetReset(KafkaFlow.AutoOffsetReset.Earliest)
                    .WithConsumerConfig(new ConsumerConfig { ClientId = kafka.ClientId })
                    .AddMiddlewares(middlewares =>
                        middlewares
                            .Add<RawMessageDeadLetterMiddleware>(MiddlewareLifetime.Message)
                            .AddSingleTypeDeserializer<WeakAppMetersPolledMessage, JsonCoreDeserializer>()
                            .AddTypedHandlers(handlers =>
                                handlers
                                    .WithHandlerLifetime(InstanceLifetime.Scoped)
                                    .AddHandler<WeakAppMetersMessageHandler>()
                            )
                    )
            )
            .AddProducer(
                KafkaProcessingEventPublisher.ProcessedProducerName,
                producer =>
                    producer
                        .DefaultTopic(kafka.ProcessedTopic)
                        .WithAcks(KafkaFlow.Acks.All)
                        .WithProducerConfig(new ProducerConfig { ClientId = kafka.ClientId })
                        .AddMiddlewares(middlewares => middlewares.AddSerializer<JsonCoreSerializer>())
            )
            .AddProducer(
                KafkaProcessingEventPublisher.RawDeadLetterProducerName,
                producer =>
                    producer
                        .DefaultTopic(kafka.RawDeadLetterTopic)
                        .WithAcks(KafkaFlow.Acks.All)
                        .WithProducerConfig(new ProducerConfig { ClientId = kafka.ClientId })
                        .AddMiddlewares(middlewares => middlewares.AddSerializer<JsonCoreSerializer>())
            )
    )
);

IHost host = builder.Build();

await host.MigrateProcessorDatabaseAsync();
await host.RunAsync();
