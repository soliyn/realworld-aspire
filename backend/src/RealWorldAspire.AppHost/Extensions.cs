using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Aspire.Hosting.Pipelines;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RealWorldAspire.AppHost;

public static class Extensions
{
    extension(IDistributedApplicationBuilder builder)
    {
        [Experimental("ASPIREPIPELINES001")]
        public IResourceBuilder<ExecutableResource> AddEfMigrate(IResourceBuilder<ProjectResource> app, IResourceBuilder<IResourceWithConnectionString> database)
        {
            var projectDirectory = Path.GetDirectoryName(app.Resource.GetProjectMetadata().ProjectPath)!;

            var efmigrate = builder.AddExecutable($"ef-migrate-{app.Resource.Name}", "dotnet", projectDirectory)
                .WithArgs("ef")
                .WithArgs("database")
                .WithArgs("update")
                .WithArgs("--no-build")
                .WithArgs("--connection")
                .WithArgs(database.Resource)
                .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
                .WaitFor(database)
                .WithReference(database);

            efmigrate.WithPipelineStepFactory(factoryContext =>
            {
                var step = new PipelineStep
                {
                    Name = $"ef-migration-bundle-{app.Resource.Name}",
                    Tags = [WellKnownPipelineTags.BuildCompute],
                    Action = async context =>
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "dotnet",
                            RedirectStandardError = true,
                            RedirectStandardOutput = true,
                            WorkingDirectory = projectDirectory
                        };
                        // dotnet ef migrations bundle --self-contained -r linux-x64
                        psi = psi.WithArgs(["ef", "migrations", "bundle", "--self-contained", "-r", "linux-x64"]);

                        await psi.ExecuteAsync(context.Logger, context.CancellationToken);
                    }
                };

                return [step];
            });

            efmigrate.WithPipelineConfiguration(context =>
            {
                var appContainerBuildSteps = context.GetSteps(app.Resource, WellKnownPipelineTags.BuildCompute);

                var migrationBundle = context.GetSteps(efmigrate.Resource, WellKnownPipelineTags.BuildCompute);

                appContainerBuildSteps.DependsOn(migrationBundle);
            });

            return efmigrate;
        }
    }
    
    extension(ProcessStartInfo psi)
    {
        public ProcessStartInfo WithArgs(string[] args)
        {
            foreach (var arg in args)
            {
                psi.ArgumentList.Add(arg);
            }
            return psi;
        }

        // Exec with logs

        public Task<int> ExecuteAsync(ILogger logger, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<int>();

            var process = new Process
            {
                StartInfo = psi,
                EnableRaisingEvents = true
            };

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    logger.LogDebug(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    logger.LogDebug(e.Data);
                }
            };

            process.Exited += (sender, e) =>
            {
                tcs.SetResult(process.ExitCode);
                process.Dispose();
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            cancellationToken.Register(() =>
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            });

            return tcs.Task;
        }
    }

    extension(IResourceBuilder<ProjectResource> resourceBuilder)
    {
        public IResourceBuilder<ProjectResource> WithDataPopulation()
        {
            return resourceBuilder.WithCommand("seed-data", "Seed the database with fake data using Bogus", async context =>
            {
                await SeedDatabaseAsync(resourceBuilder, context);
                return new ExecuteCommandResult { Success = true };
            });

            static async Task SeedDatabaseAsync(IResourceBuilder<ProjectResource> app, ExecuteCommandContext context)
            {
                var cancellationToken = context.CancellationToken;
                var logger = context.ServiceProvider.GetRequiredService<ResourceLoggerService>().GetLogger(app.Resource);

                logger.LogInformation("🌱 Starting database seeding with Bogus...");

                // Wait a bit for the app to be fully ready
                using var httpClient = new HttpClient();

                // Get the actual endpoint URL dynamically
                var httpEndpoint = app.GetEndpoint("http");
                var baseUrl = await httpEndpoint.GetValueAsync(cancellationToken);

                var faker = new Faker();

                logger.LogInformation($"📊 Generating 5 fake users. Starting to seed...");
                logger.LogInformation("🔗 Using endpoint: {baseUrl}", baseUrl);

                int successCount = 0;
                int errorCount = 0;

                for (int i = 1; i <= 5; i++)
                {
                    try
                    {
                        // Generate fake person data
                        var username = $"author{i}";
                        var email = $"{username}@example.com";
                        var password = "Pwd123";

                        var user = new
                        {
                            username,
                            email,
                            password,
                        };

                        var json = JsonSerializer.Serialize(new { user });
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        var response = await httpClient.PostAsync($"{baseUrl}/api/users", content, cancellationToken);

                        if (response.IsSuccessStatusCode)
                        {
                            successCount++;
                            logger.LogInformation("✅ Created: {username} ({email})", username, email);
                        }
                        else
                        {
                            errorCount++;
                            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                            logger.LogError("❌ Failed to create {username}: {StatusCode} - {ErrorContent}", username, response.StatusCode, errorContent);
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        logger.LogError("💥 Exception creating user {Index}: {Message}", i, ex.Message);
                    }
                }
                logger.LogInformation("Created {SuccessCount} users, {ErrorCount} errors.", successCount, errorCount);

                // Login each user and get their tokens
                logger.LogInformation("🔐 Logging in users to get authentication tokens...");
                var userTokens = new Dictionary<int, string>();

                for (int i = 1; i <= 5; i++)
                {
                    try
                    {
                        var email = $"author{i}@example.com";
                        var password = "Pwd123";

                        var loginRequest = new
                        {
                            user = new
                            {
                                email,
                                password
                            }
                        };

                        var json = JsonSerializer.Serialize(loginRequest);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        var response = await httpClient.PostAsync($"{baseUrl}/api/users/login", content, cancellationToken);

                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                            var loginResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                            var token = loginResponse.GetProperty("user").GetProperty("token").GetString();

                            if (!string.IsNullOrEmpty(token))
                            {
                                userTokens[i] = token;
                                logger.LogInformation("✅ Logged in: author{Index}", i);
                            }
                        }
                        else
                        {
                            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                            logger.LogError("❌ Failed to login author{Index}: {StatusCode} - {ErrorContent}", i, response.StatusCode, errorContent);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("💥 Exception logging in user {Index}: {Message}", i, ex.Message);
                    }
                }

                logger.LogInformation("📝 Creating 20 articles for each of the {UserCount} users...", userTokens.Count);

                successCount = 0;
                errorCount = 0;

                // Create 20 articles for each user
                foreach (var (userId, token) in userTokens)
                {
                    for (int articleNum = 1; articleNum <= 20; articleNum++)
                    {
                        try
                        {
                            // Generate fake article data
                            var title = faker.Lorem.Sentence(3, 1);
                            if (title.Length > 50) title = title[..50];

                            var description = faker.Lorem.Paragraph();
                            if (description.Length > 200) description = description[..200];

                            var body = faker.Lorem.Text();
                            var tagList = faker.Lorem.Words();

                            var article = new
                            {
                                title,
                                description,
                                body,
                                tagList,
                            };

                            var json = JsonSerializer.Serialize(new { article });
                            var content = new StringContent(json, Encoding.UTF8, "application/json");

                            // Add authorization header with token
                            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/articles")
                            {
                                Content = content
                            };
                            request.Headers.Add("Authorization", $"Token {token}");

                            var response = await httpClient.SendAsync(request, cancellationToken);

                            if (response.IsSuccessStatusCode)
                            {
                                successCount++;
                                logger.LogInformation("✅ Created article {ArticleNum}/20 for author{UserId}: {title}", articleNum, userId, title);
                            }
                            else
                            {
                                errorCount++;
                                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                                logger.LogError("❌ Failed to create article for author{UserId}: {StatusCode} - {ErrorContent}", userId, response.StatusCode, errorContent);
                            }
                        }
                        catch (Exception ex)
                        {
                            errorCount++;
                            logger.LogError("💥 Exception creating article {ArticleNum} for author{UserId}: {Message}", articleNum, userId, ex.Message);
                        }
                    }
                }

                logger.LogInformation("Created {SuccessCount} articles, {ErrorCount} errors.", successCount, errorCount);

                logger.LogInformation("🎉 Seeding complete!");
            }
        }
    }

    extension(IResourceBuilder<PostgresDatabaseResource> resourceBuilder)
    {
        [Experimental("ASPIREINTERACTION001")]
        public IResourceBuilder<PostgresDatabaseResource> WithResetDbCommand()
        {
            return resourceBuilder.WithCommand("reset", "Reset Database", async context =>
            {
                var interactionService = context.ServiceProvider.GetRequiredService<IInteractionService>();

                var result = await interactionService.PromptConfirmationAsync("Are you sure you want to reset the database? This action cannot be undone.",
                    "Confirm Reset");

                if (!result.Data || result.Canceled)
                {
                    return new ExecuteCommandResult { Success = false, ErrorMessage = "Database reset cancelled by user." };
                }
                
                var rcs = context.ServiceProvider.GetRequiredService<ResourceCommandService>();
                await rcs.ExecuteCommandAsync(
                    resourceBuilder.Resource.Parent,
                    KnownResourceCommands.RestartCommand,
                    context.CancellationToken
                );

                // Custom reset logic if needed
                return new ExecuteCommandResult { Success = true };
            });
        }
    }

}