using GrpcSocks;
using GrpcSocks.Client;
using GrpcSocks.Interceptors;
using GrpcSocks.Services;


await SocksSettingsExtension.SetStatic();
switch (SocksSettings.Mode)
{
    case "Server":
        RunServer();
        break;
    case "Client":
        await RunClient();
        break;
    default:
        _ = RunClient();
        RunServer();
        break;
}

void RunServer()
{
    var builder = WebApplication.CreateBuilder(args);

    // Additional configuration is required to successfully run gRPC on macOS.
    // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

    // Add services to the container.
    builder.Services.AddGrpc(options =>
    {
        options.Interceptors.Add<ServerInterceptor>();
    });
    var app = builder.Build();

    // Configure the HTTP request pipeline.
    app.MapGrpcService<SocksService>();
    app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
    app.Run();
}

async Task RunClient()
{
    var client = new SocksClient();
    await client.StartAsync();
}