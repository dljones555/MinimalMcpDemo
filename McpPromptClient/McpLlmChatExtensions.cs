using System.Net.Http.Headers;
using System.Net.Http.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

public static class McpLlmChatClientExtensions
{
    public static async Task<LlmChatClient> UseMcpPromptsAsync(this LlmChatClient llm, string mcpUrl)
    {
        var options = new SseClientTransportOptions
        {
            Endpoint = new Uri(mcpUrl),
            TransportMode = HttpTransportMode.Sse
        };
        var httpClient = new HttpClient();
        llm.McpClient = await McpClientFactory.CreateAsync(new SseClientTransport(options, httpClient));
       
        return llm;
    }

    public static async Task WithChatLoopAsync(this LlmChatClient llm)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", llm.ApiKey);

        Console.WriteLine("Type /resources, /root [name], /listprompts or /getprompt [name] to fetch a prompt. Type /exit to quit.");
        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) continue;

            if (input.StartsWith("/resources", StringComparison.OrdinalIgnoreCase) && llm.McpClient != null)
            {
                var resources = await llm.McpClient.ListResourcesAsync();
                foreach (var r in resources)
                    Console.WriteLine($"{r.Name} - {r.Description}");
                continue;
            }
            if (input.StartsWith("/root ", StringComparison.OrdinalIgnoreCase))
            {
                var rootName = input.Substring("/root ".Length).Trim();
                llm.CurrentRoot = string.IsNullOrWhiteSpace(rootName) ? null : rootName;
                Console.WriteLine(llm.CurrentRoot == null ? "Root cleared. Listing all prompts." : $"Root set to: {llm.CurrentRoot}");
                continue;
            }
            if (input.StartsWith("/listprompts", StringComparison.OrdinalIgnoreCase) && llm.McpClient != null)
            {
                var prompts = await llm.McpClient.ListPromptsAsync();
                foreach (var p in prompts)
                    if (llm.CurrentRoot == null || p.Name.StartsWith(llm.CurrentRoot + "/"))
                        Console.WriteLine($"{p.Name} - {p.Description}");
                continue;
            }
            if (input.StartsWith("/getprompt ", StringComparison.OrdinalIgnoreCase) && llm.McpClient != null)
            {
                var name = input.Substring("/getprompt ".Length).Trim();
                var fullName = llm.CurrentRoot != null && !name.StartsWith(llm.CurrentRoot + "/") ? $"{llm.CurrentRoot}/{name}" : name;
                var result = await llm.McpClient.GetPromptAsync(fullName);
                var promptText = result.Messages.FirstOrDefault() is { Content: TextContentBlock tcb } ? tcb.Text : null;
                Console.WriteLine($"\nPrompt Content:\n{promptText}");
                llm.ChatHistory.Clear();
                llm.ChatHistory.Add(new Dictionary<string, string>
                {
                    ["role"] = "user",
                    ["content"] = promptText ?? "No content available."
                });
                continue;
            }
            if (input.Equals("/exit", StringComparison.OrdinalIgnoreCase))
                break;

            // Add user message
            llm.ChatHistory.Add(new Dictionary<string, string>
            {
                ["role"] = "user",
                ["content"] = input
            });

            // Prepare request for LLM provider
            var requestBody = new
            {
                messages = llm.ChatHistory.Select(m => new { role = m["role"], content = m["content"] }).ToList(),
                model = llm.Model,
                max_tokens = 512
            };

            var resp = await httpClient.PostAsJsonAsync(llm.LlmEndpoint, requestBody);

            if (!resp.IsSuccessStatusCode)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"API error: {resp.StatusCode} {await resp.Content.ReadAsStringAsync()}");
                Console.ResetColor();
                continue;
            }

            var json = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var completion = json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            // Add assistant message to history
            llm.ChatHistory.Add(new Dictionary<string, string>
            {
                ["role"] = "assistant",
                ["content"] = completion ?? ""
            });

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Assistant: {completion}");
            Console.ResetColor();
        }
    }
}

public class LlmChatClient
{
    public string LlmEndpoint { get; }
    public string ApiKey { get; }
    public string Model { get; }
    public IMcpClient? McpClient { get; set; }
    public List<Dictionary<string, string>> ChatHistory { get; } = new();
    public string? CurrentRoot { get; set; } // Property for current root

    public LlmChatClient(string llmEndpoint, string apiKey, string model = "gpt-4o", string? initialRoot = null)
    {
        LlmEndpoint = llmEndpoint;
        ApiKey = apiKey;
        Model = model;
        CurrentRoot = initialRoot; // Default to null (no root)
    }
}