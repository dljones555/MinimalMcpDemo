# minimalmcpdemo 🚀

A minimal end-to-end demo for the [Model Context Protocol (MCP)](https://github.com/modelcontext/modelcontextprotocol-spec) using .NET and the official [.NET MCP SDK](https://www.nuget.org/packages/ModelContextProtocol/). This project demonstrates how to serve, discover, and consume LLM prompts over the MCP protocol, with advanced prompt management via PromptLoader.

---

## 🏗️ Project Structure

- **McpPromptServer**:  
  A minimal MCP server that exposes prompt files (`*.prompt.md` and more) via HTTP/SSE using the MCP protocol.  
  - **PromptLoader integration:** Advanced prompt management, folder-based organization (roots), prompt chaining, versioning, and filtering.
  - **Resources & root support:** Prompts are grouped by root (top-level folder) and exposed as MCP resources for discoverability and filtering.
  - Lists available prompts and resources.
  - Serves prompt content on request using a fluent API.

- **McpPromptClient**:  
  A console client that connects to the MCP server, lists available prompts/resources, fetches prompt content, and interacts with an LLM endpoint (e.g., Azure OpenAI) using those prompts.  
  - Supports root/resource selection and prompt filtering.

- **PromptLoader**:  
  A reusable library for loading, organizing, and composing prompts.  
  - **Fluent API:** Easily load prompts from folders/files, combine/chains prompts, and apply configuration in a readable style:```csharp
var ctx = await PromptContext
    .FromFolder()
    .WithConfig(config)
    .LoadAsync();
string combined = ctx.Get("Sales").CombineWithRoot().AsString();
```  - **Configurable via appsettings.json:**
    - `PromptListType`, `PromptList`, `ConstrainPromptList`, `PromptsFolder`, `PromptSetFolder`, `SupportedPromptExtensions`, `PromptSeparator`, `CascadeOverride` and more.
  - Supports prompt versioning, chaining, and multi-format (Markdown, YAML, Jinja, etc).

---

## 📦 Key Technologies

- [.NET 9](https://dotnet.microsoft.com/)
- [ModelContextProtocol (.NET MCP SDK)](https://www.nuget.org/packages/ModelContextProtocol/)
- [PromptLoader (fluent prompt management)](./PromptLoader)

---

## 📝 Usage

### 1. Start the MCP Prompt Server

dotnet run --project McpPromptServer

Prompts are served from the `Prompts/` directory (or as configured in `appsettings.json`).

### 2. Set your LLM API key

Set the `GH_TOKEN` environment variable to your LLM provider API key (e.g., Azure OpenAI):

- Windows (cmd): set GH_TOKEN=your_api_key
- Windows (PowerShell):$env:GH_TOKEN="your_api_key"
- Linux / macOS: export GH_TOKEN=your_api_key

### 3. Run the Client

dotnet run --project McpPromptClient

- Use `/resources` to see available prompt roots/resources.
- Use `/root [name]` to select a root.
- Use `/listprompts` to see available prompts in the current root.
- Use `/getprompt [name]` to fetch a prompt.
- Type your message to chat with the LLM using the selected prompt.
- Type `/exit` to quit.

---

## 📂 Prompt Format & Configuration

Prompts can be Markdown (`*.prompt.md`), plain text, YAML, Jinja, etc. Place them in the `Prompts/` or `PromptSets/` directory, or configure folders in `appsettings.json`.

**Example `appsettings.json` PromptLoader section:**"PromptLoader": {
  "PromptListType": "named",
  "PromptList": ["system", "instructions", "examples", "groundings", "guardrails", "output"],
  "ConstrainPromptList": true,
  "PromptsFolder": "Prompts",
  "PromptSetFolder": "PromptSets",
  "SupportedPromptExtensions": [".txt", ".prompt", ".yml", ".jinja", ".jinja2", ".prompt.md", ".md"],
  "PromptSeparator": "\n{filename}:\n---\n",
  "CascadeOverride": true
}- **PromptListType:** How to interpret the prompt list (named, numeric, none)
- **PromptList:** Which prompts to load/combine
- **ConstrainPromptList:** Only load prompts in the list
- **PromptSetFolder/PromptsFolder:** Where to find prompt sets/files
- **SupportedPromptExtensions:** File types to load
- **PromptSeparator:** How to join prompts when combining
- **CascadeOverride:** Whether deeper folders override root prompts

---

## 🤝 References

- [Model Context Protocol Spec](https://github.com/modelcontext/modelcontextprotocol-spec)
- [.NET MCP SDK on NuGet](https://www.nuget.org/packages/ModelContextProtocol/)
- [PromptLoader (fluent prompt management)](./PromptLoader)

---

## ✨ Why?

This project is a minimal, hackable reference for anyone looking to:
- Build or integrate MCP-compliant prompt servers/clients.
- Experiment with advanced prompt management, roots/resources, and LLM orchestration in .NET.
- Use or extend a fluent API for prompt ops.
- Learn about the MCP and prompt ops ecosystem.

---

