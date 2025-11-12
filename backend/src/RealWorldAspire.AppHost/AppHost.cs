var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder
    .AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin(c => c.WithLifetime(ContainerLifetime.Persistent))
    ;
var postgresdb = postgres
    .WithDataVolume("postgres-data-volume")
    .AddDatabase("realworlddb")
    ;

var apiService = builder.AddProject<Projects.RealWorldAspire_ApiService>("apiservice")
    .WithReference(postgresdb)
    .WaitFor(postgresdb)
    .WithHttpHealthCheck("/health")
    ;

var angular = builder.AddViteApp("angular", "../../../frontend/rw-ng-client")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

apiService.WithReference(angular);

builder.Build().Run();