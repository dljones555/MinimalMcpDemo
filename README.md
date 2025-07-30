# minimalmcpdemo 🚀

A minimal end-to-end demo for the [Model Context Protocol (MCP)](https://github.com/modelcontext/modelcontextprotocol-spec) using .NET and the official [.NET MCP SDK](https://www.nuget.org/packages/ModelContextProtocol/). This project demonstrates how to serve, discover, and consume LLM prompts over the MCP protocol.

---

## 🏗️ Project Structure

- **McpPromptServer**:  
  A minimal MCP server that exposes prompt files (`*.prompt.md`) via HTTP/SSE using the MCP protocol.  
  - Static prompt file serving demonstration.
  - Lists available prompts.
  - Serves prompt content on request.

- **McpPromptClient**:  
  A console client that connects to the MCP server, lists available prompts, fetches prompt content, and interacts with an LLM endpoint (e.g., Azure OpenAI) using those prompts.

---

## 📦 Key Technologies

- [.NET 9](https://dotnet.microsoft.com/)
- [ModelContextProtocol (.NET MCP SDK)](https://www.nuget.org/packages/ModelContextProtocol/)
- [Model Context Protocol Spec](https://github.com/modelcontext/modelcontextprotocol-spec)

---

## 📝 Usage

### 1. Start the MCP Prompt Server

dotnet run --project McpPromptServer

Prompts are served from the `Prompts/` directory.

### 2. Set your LLM API key

Set the `GH_TOKEN` environment variable to your LLM provider API key (e.g., Azure OpenAI):

- Windows (cmd): set GH_TOKEN=your_api_key
- Windows (PowerShell):$env:GH_TOKEN="your_api_key"
- Linux / macOS: export GH_TOKEN=your_api_key

### 3. Run the Client

dotnet run --project McpPromptClient

- Use `/listprompts` to see available prompts.
- Use `/getprompt [name]` to fetch a prompt.
- Type your message to chat with the LLM using the selected prompt.
- Type `/exit` to quit.

---

## 📂 Prompt Format

Prompts are Markdown files (`*.prompt.md`) placed in the `Prompts/` directory of the server. Each file represents a reusable LLM prompt.

---

## 🤝 References

- [Model Context Protocol Spec](https://github.com/modelcontext/modelcontextprotocol-spec)
- [.NET MCP SDK on NuGet](https://www.nuget.org/packages/ModelContextProtocol/)

---

## ✨ Why?

This project is a minimal, hackable reference for anyone looking to:
- Build or integrate MCP-compliant prompt servers/clients.
- Experiment with prompt management and LLM orchestration in .NET.
- Learn about the MCP ecosystem.

---

