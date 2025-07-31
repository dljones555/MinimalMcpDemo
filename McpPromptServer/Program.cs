using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

const string PromptsDirectory = "Prompts";
const string PromptExtension = ".prompt.md";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithListPromptsHandler(ListPromptsHandler)
    .WithGetPromptHandler(GetPromptHandler)
    .WithListResourcesHandler(ListResourcesHandler); // Register resource handler

var app = builder.Build();
app.MapMcp();
app.Run();

static ValueTask<ListPromptsResult> ListPromptsHandler(RequestContext<ListPromptsRequestParams> context, CancellationToken cancellationToken)
{
    var promptFiles = Directory.GetFiles(PromptsDirectory, $"*{PromptExtension}", SearchOption.AllDirectories);
    var prompts = promptFiles.Select(file =>
    {
        var relativePath = Path.GetRelativePath(PromptsDirectory, file);
        var name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(relativePath)).Replace("\\", "/");
        return new Prompt { Name = name, Description = $"Prompt from {relativePath}" };
    }).ToList();
    return ValueTask.FromResult<ListPromptsResult>(new ListPromptsResult { Prompts = prompts });
}

static async ValueTask<GetPromptResult> GetPromptHandler(RequestContext<GetPromptRequestParams> context, CancellationToken cancellationToken)
{
    var promptName = context.Params?.Name?.Replace('/', Path.DirectorySeparatorChar) ?? "";
    if (!promptName.EndsWith(PromptExtension, StringComparison.OrdinalIgnoreCase))
        promptName += PromptExtension;
    var filePath = Path.Combine(PromptsDirectory, promptName);
    if (!File.Exists(filePath))
        throw new FileNotFoundException($"Prompt file not found: {filePath}");
    var content = await File.ReadAllTextAsync(filePath, cancellationToken);
    return new GetPromptResult
    {
        Messages = new[]
        {
            new PromptMessage
            {
                Role = Role.Assistant,
                Content = new TextContentBlock { Text = content.Trim() }
            }
        }
    };
}

// New: ListResourcesHandler implementation
static ValueTask<ListResourcesResult> ListResourcesHandler(RequestContext<ListResourcesRequestParams> context, CancellationToken cancellationToken)
{
    // List first-level directories under PromptsDirectory as resources
    var resourceDirs = Directory.Exists(PromptsDirectory)
        ? Directory.GetDirectories(PromptsDirectory)
        : Array.Empty<string>();

    var resources = resourceDirs
        .Select(dir => {
            var name = Path.GetFileName(dir);
            return new Resource
            {
                Name = name,
                Description = $"Prompt set: {name}",
                Uri = $"/{name}" // Required by MCP spec and SDK
            };
        })
        .ToList();

    return ValueTask.FromResult(new ListResourcesResult { Resources = resources });
}