using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using PromptLoader.Fluent;
using Microsoft.Extensions.Configuration;

const string PromptsDirectory = "Prompts";

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

var promptContext = new PromptContext().WithConfig(config);
await promptContext.LoadAsync();

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
    // Parse name as set/subset/prompt or set/prompt or prompt
    var name = context.Params?.Name ?? "";
    var parts = name.Split('/', StringSplitOptions.RemoveEmptyEntries);
    string? set = null, subset = null, promptKey = null;
    if (parts.Length == 1) 
    { 
        promptKey = parts[0]; 
        subset = "Root"; 
        set = "Root"; 
    }
    else if (parts.Length == 2) 
    { 
        set = parts[0]; 
        promptKey = parts[1]; 
        subset = "Root"; 
    }
    else if (parts.Length == 3)
    { 
        set = parts[0]; 
        subset = parts[1]; 
        promptKey = parts[2]; 
    }
    else 
    { 
        throw new FileNotFoundException($"Prompt not found: {name}"); 
    }
    if (!promptContext.PromptSets.TryGetValue(set!, out var subsets) ||
        !subsets.TryGetValue(subset!, out var promptSet) ||
        !promptSet.Prompts.TryGetValue(promptKey!, out var prompt))
        throw new FileNotFoundException($"Prompt not found: {name}");
    return new GetPromptResult
    {
        Messages = new[]
        {
            new PromptMessage
            {
                Role = Role.Assistant,
                Content = new TextContentBlock { Text = prompt.Text.Trim() }
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