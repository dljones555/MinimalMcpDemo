using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using PromptLoader.Fluent;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using System.Text.Json;

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
                // Get the prompt text
                var promptText = promptKvp.Value.Text;
                // Find all placeholders in the format {argument}
                var matches = Regex.Matches(promptText, "\\{(\\w+)\\}");
                var arguments = matches
                    .Cast<Match>()
                    .Select(m => new PromptArgument {
                        Name = m.Groups[1].Value,
                        Required = true,
                        Title = m.Groups[1].Value,
                        Description = $"Parameter for {m.Groups[1].Value}" // Add Description property
                    })
                    .DistinctBy(a => a.Name)
                    .ToList();
                prompts.Add(new Prompt
                {
                    Name = (set.Key == "Root" ? "" : set.Key + "/") + (subset.Key == "Root" ? "" : subset.Key + "/") + promptKvp.Key,
                    Description = $"Prompt from set {set.Key}, subset {subset.Key}, file {promptKvp.Key}",
                    Arguments = arguments.Count > 0 ? arguments : null
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

    // Argument replacement logic
    var arguments = context.Params?.Arguments;
    if (arguments != null && arguments.Count > 0)
    {
        // Replace {argument} placeholders with provided values
        promptString = Regex.Replace(
            promptString,
            "\\{(\\w+)\\}",
            m =>
            {
                var key = m.Groups[1].Value;
                if (arguments.TryGetValue(key, out var value))
                {
                    // Handle JsonElement extraction
                    if (value.ValueKind == JsonValueKind.String)
                        return value.GetString() ?? "";
                    else if (value.ValueKind == JsonValueKind.Number)
                        return value.GetRawText();
                    else if (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
                        return value.GetRawText();
                    else
                        return value.ToString();
                }
                return m.Value;
            });
    }

    return new GetPromptResult
    {
        Messages = new[]
        {
            new PromptMessage
            {
                Role = Role.User,
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