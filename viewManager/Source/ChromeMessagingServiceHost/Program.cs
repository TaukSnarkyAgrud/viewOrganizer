using ChromeMessagingServiceHost;
using Serilog;

IHost host = Host.CreateDefaultBuilder(args)
    .UseSerilog((hostingContext, loggerConfiguration) =>
    {
        var config = hostingContext.Configuration;
        loggerConfiguration.ReadFrom.Configuration(config);
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<MessagingServiceViewOrganizer>();
    })
    .Build();

host.Run();
