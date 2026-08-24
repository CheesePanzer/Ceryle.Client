using Ceryle.Client;
using Ceryle.Client.Tests.IntegrationTest;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

// 1. Header
AnsiConsole.Write(new Rule("[bold blue]Ceryle Client Automated Integration Test[/]").LeftJustified());

// 2. Load Configuration and Setup DI
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var services = new ServiceCollection();

services.AddLogging(); // Ensure ILogger<CeryleClient> can be resolved
services.AddCeryleClient(options =>
{
    options.BaseUrl = configuration["CeryleClient:BaseUrl"] ?? throw new Exception("Ceryle Base Url is missing");
    options.ApiKey = configuration["CeryleClient:ApiKey"] ?? throw new Exception("Ceryle API Key is missing");
    options.ClientTimeoutSecs =
        int.TryParse(configuration["CeryleClient:ClientTimeoutSecs"], out var timeout) ? timeout : 30;
});

await using var serviceProvider = services.BuildServiceProvider();
var client = serviceProvider.GetRequiredService<ICeryleClient>();

AnsiConsole.MarkupLine($"[grey]Initialized CeryleClient -> [/][yellow]{configuration["CeryleClient:BaseUrl"]}[/]\n");

// 3. Construct Payload
var payload = new TestPayload(
    Value1: "Sample String",
    Value2: DateTime.Now,
    Value3: 100,
    Value4: 99.99m,
    Value5: DateTimeOffset.Now,
    Payload2: new TestPayload(
        Value1: "Nested String",
        Value2: DateTime.Now.AddDays(1),
        Value3: 200,
        Value4: 199.99m,
        Value5: DateTimeOffset.Now.AddDays(1),
        Payload2: null!
    )
);

var requestDto = RenderRequestDto.CreateFromObject("RenderedTest", payload);

try
{
    await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .StartAsync("Executing test pipeline...", async ctx =>
        {
            // ----------------------------------------------------
            // Test 1: Synchronous DOCX Generation (GenerateDocxAsync)
            // ----------------------------------------------------
            ctx.Status("[bold cyan]1/3 Testing GenerateDocxAsync (Sync)...[/]");
            
            var syncPath = Path.Combine(AppContext.BaseDirectory, "RenderedTest_Sync.docx");
            await using (var stream = await client.GenerateDocxAsync("test", requestDto))
            await using (var fileStream = File.Create(syncPath))
            {
                await stream.CopyToAsync(fileStream);
            }
            AnsiConsole.MarkupLine($"[green]✔ 1. Sync rendering completed:[/] [grey]{syncPath}[/]");

            // ----------------------------------------------------
            // Test 2: Create Async Rendering Task (CreateDocxRenderTaskAsync)
            // ----------------------------------------------------
            ctx.Status("[bold cyan]2/3 Testing CreateDocxRenderTaskAsync (Async)...[/]");
            
            var taskId = await client.CreateDocxRenderTaskAsync("test", requestDto);
            AnsiConsole.MarkupLine($"[green]✔ 2. Task created successfully! TaskId:[/] [yellow]{taskId}[/]");

            // ----------------------------------------------------
            // Test 3: Poll and Retry Downloading Task File (DownloadDocxByTaskIdAsync)
            // ----------------------------------------------------
            ctx.Status("[bold cyan]3/3 Preparing to poll for task output...[/]");

            const int maxRetries = 3;
            const int delaySeconds = 2;
            var isDownloaded = false;
            var asyncPath = Path.Combine(AppContext.BaseDirectory, $"RenderedTest_Task_{taskId}.docx");

            for (var retry = 1; retry <= maxRetries; retry++)
            {
                ctx.Status($"[bold cyan]3/3 Attempting file download (Attempt {retry}/{maxRetries})...[/]");

                try
                {
                    await using var taskStream = await client.DownloadDocxByTaskIdAsync(taskId);
                    await using var taskFileStream = File.Create(asyncPath);
                    await taskStream.CopyToAsync(taskFileStream);

                    isDownloaded = true;
                    AnsiConsole.MarkupLine($"[green]✔ 3. Async task file downloaded:[/] [grey]{asyncPath}[/]");
                    break;
                }
                catch (CeryleApiException ex) when (ex.StatusCode != System.Net.HttpStatusCode.NotFound)
                {
                    AnsiConsole.MarkupLine($"[yellow]⌛ Task in progress, waiting {delaySeconds}s before retry ({retry}/{maxRetries})...[/]");
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                }
            }

            if (!isDownloaded)
            {
                throw new Exception($"Polling timeout: File for TaskId [{taskId}] was not ready within {maxRetries * delaySeconds} seconds.");
            }
        });

    // Success Summary
    AnsiConsole.Write(new Panel("[bold green]🎉 All integration tests passed successfully![/]")
    {
        Border = BoxBorder.Rounded,
        Padding = new Padding(1, 0)
    });
}
catch (Exception ex)
{
    AnsiConsole.Write(new Panel($"[bold red]✘ Test execution aborted due to an error:[/]\n\n[red]{Markup.Escape(ex.ToString())}[/]")
    {
        Border = BoxBorder.Rounded,
        BorderStyle = new Style(Color.Red),
        Padding = new Padding(1, 1)
    });
}