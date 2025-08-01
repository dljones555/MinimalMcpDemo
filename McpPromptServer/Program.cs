using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using PromptLoader.Fluent;
using Microsoft.Extensions.Configuration;

const string PromptsDirectory = "Prompts";

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

// Get prompts folder and load available prompts based on config rules

var promptContext = await new PromptContext().WithConfig(config).LoadAsync();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithListPromptsHandler(ListPromptsHandler)
    .WithGetPromptHandler(GetPromptHandler)
    .WithListResourcesHandler(ListResourcesHandler);

var app = builder.Build();
app.MapMcp();
app.Run();

ValueTask<ListPromptsResult> ListPromptsHandler(RequestContext<ListPromptsRequestParams> context, CancellationToken cancellationToken)
{
    // List all prompts in all sets, flattening to MCP Prompt type
    var prompts = new List<Prompt>();
    foreach (var set in promptContext.PromptSets)
    {
        foreach (var subset in set.Value)
        {
            foreach (var promptKvp in subset.Value.Prompts)
            {
                prompts.Add(new Prompt
                {
                    Name = (set.Key == "Root" ? "" : set.Key + "/") + (subset.Key == "Root" ? "" : subset.Key + "/") + promptKvp.Key,
                    Description = $"Prompt from set {set.Key}, subset {subset.Key}, file {promptKvp.Key}"
                });
            }
        }
    }
    return ValueTask.FromResult(new ListPromptsResult { Prompts = prompts });
}

async ValueTask<GetPromptResult> GetPromptHandler(RequestContext<GetPromptRequestParams> context, CancellationToken cancellationToken)
{
    // Use fluent PromptContext API for elegant prompt retrieval
    var name = context.Params?.Name ?? "";
    var promptString = promptContext.Get(name).AsString();
    if (string.IsNullOrWhiteSpace(promptString))
        throw new FileNotFoundException($"Prompt not found: {name}");
    return new GetPromptResult
    {
        Messages = new[]
        {
            new PromptMessage
            {
                Role = Role.Assistant,
                Content = new TextContentBlock { Text = promptString.Trim() }
            }
        }
    };
}

ValueTask<ListResourcesResult> ListResourcesHandler(RequestContext<ListResourcesRequestParams> context, CancellationToken cancellationToken)
{
    // List top-level sets as resources
    var resources = promptContext.PromptSets.Keys
        .Where(k => k != "Root")
        .Select(root => new Resource
        {
            Name = root,
            Description = $"Prompt set: {root}",
            Uri = $"/{root}"
        }).ToList();
    return ValueTask.FromResult(new ListResourcesResult { Resources = resources });
}