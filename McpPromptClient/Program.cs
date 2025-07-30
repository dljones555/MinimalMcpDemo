var ghToken = Environment.GetEnvironmentVariable("GH_TOKEN");
if (string.IsNullOrWhiteSpace(ghToken))
{
    Console.WriteLine("Please set the GH_TOKEN environment variable.");
    return;
}

var llm = new LlmChatClient(
    "https://models.inference.ai.azure.com/chat/completions",
    ghToken,
    "gpt-4o"
);

await (await llm.UseMcpPromptsAsync("http://localhost:5000/sse"))
    .WithChatLoopAsync();