using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithListPromptsHandler(ListPromptsHandler)
    .WithGetPromptHandler(GetPromptHandler);

var app = builder.Build();
app.MapMcp();
app.Run();

static ValueTask<ListPromptsResult> ListPromptsHandler(RequestContext<ListPromptsRequestParams> context, CancellationToken cancellationToken)
{
    var rootDir = "Prompts";
    var promptFiles = Directory.GetFiles(rootDir, "*.prompt.md", SearchOption.AllDirectories);
    var prompts = promptFiles.Select(file =>
    {
        var relativePath = Path.GetRelativePath(rootDir, file);
        var name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(relativePath)).Replace("\\", "/");
        return new Prompt { Name = name, Description = $"Prompt from {relativePath}" };
    }).ToList();
    return ValueTask.FromResult<ListPromptsResult>(new ListPromptsResult { Prompts = prompts });
}

static async ValueTask<GetPromptResult> GetPromptHandler(RequestContext<GetPromptRequestParams> context, CancellationToken cancellationToken)
{
    var rootDir = "Prompts";
    var promptName = context.Params?.Name?.Replace('/', Path.DirectorySeparatorChar) ?? "";
    if (!promptName.EndsWith(".prompt.md", StringComparison.OrdinalIgnoreCase))
        promptName += ".prompt.md";
    var filePath = Path.Combine(rootDir, promptName);
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