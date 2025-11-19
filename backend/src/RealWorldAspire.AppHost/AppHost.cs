using RealWorldAspire.AppHost;
#pragma warning disable ASPIREPIPELINES001

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
    .WithUrls(context =>
    {
        foreach (var u in context.Urls)
        {
            u.DisplayLocation = UrlDisplayLocation.DetailsOnly;
        }

        // Only show the /scalar URL in the UI
        context.Urls.Add(new ResourceUrlAnnotation()
        {
            Url = "/scalar",
            DisplayText = "OpenAPI Docs",
            Endpoint = context.GetEndpoint("http"),
        });
    })    ;

var efmigrate = builder.AddEfMigrate(apiService, postgresdb);

apiService.WaitForCompletion(efmigrate);
apiService.WithChildRelationship(efmigrate);

var angular = builder.AddJavaScriptApp("angular", "../../../frontend/rw-ng-client")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .WithRunScript("start")
    .PublishAsDockerFile();

apiService.WithReference(angular);

// Add a seed-data command
apiService.WithDataPopulation();

builder.Build().Run();