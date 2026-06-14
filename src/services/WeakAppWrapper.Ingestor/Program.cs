using Microsoft.Extensions.Options;
using WeakAppWrapper.Ingestor.Configuration;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder
    .Services.AddOptions<IngestorConfiguration>()
    .Bind(builder.Configuration.GetSection(IngestorConfiguration.SectionName))
    .Validate(
        configuration => configuration.PollIntervalSeconds > 0,
        "Ingestor:PollIntervalSeconds must be greater than zero"
    )
    .ValidateOnStart();

builder
    .Services.AddOptions<WeakAppConfiguration>()
    .Bind(builder.Configuration.GetSection(WeakAppConfiguration.SectionName))
    .Validate(
        configuration =>
            !string.IsNullOrWhiteSpace(configuration.BaseUrl)
            && Uri.TryCreate(configuration.BaseUrl, UriKind.Absolute, out _),
        "WeakApp:BaseUrl must be an absolute URI"
    )
    .Validate(configuration => !string.IsNullOrWhiteSpace(configuration.ApiKey), "WeakApp:ApiKey must be provided")
    .Validate(configuration => configuration.TimeoutSeconds > 0, "WeakApp:TimeoutSeconds must be greater than zero")
    .Validate(configuration => configuration.Retry is not null, "WeakApp:Retry must be provided")
    .Validate(configuration => configuration.Retry?.MaxRetries >= 0, "WeakApp:Retry:MaxRetries must be zero or greater")
    .Validate(
        configuration => configuration.Retry?.DelaySeconds > 0,
        "WeakApp:Retry:DelaySeconds must be greater than zero"
    )
    .ValidateOnStart();

builder
    .Services.AddOptions<KafkaConfiguration>()
    .Bind(builder.Configuration.GetSection(KafkaConfiguration.SectionName))
    .Validate(
        configuration => !string.IsNullOrWhiteSpace(configuration.BootstrapServers),
        "Kafka:BootstrapServers must be provided"
    )
    .Validate(configuration => !string.IsNullOrWhiteSpace(configuration.Topic), "Kafka:Topic must be provided")
    .Validate(configuration => !string.IsNullOrWhiteSpace(configuration.ClientId), "Kafka:ClientId must be provided")
    .ValidateOnStart();

IHost host = builder.Build();
host.Run();
