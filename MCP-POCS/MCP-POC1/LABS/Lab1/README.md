# Lab1

## Overview

This lab contains two parts in the same solution:

- An ASP.NET Core Web API project with Swagger enabled.
- A simple MCP server project exposing one greeting tool over stdio.

## What Was Implemented

### 1. ASP.NET Core Web API

The Web API was created using the .NET 8 Web API template.

- Project: `MCP-POC1`
- Framework: `.NET 8`
- API style: controller-based

Main Web API setup:

- Controllers enabled in `Program.cs`
- Swagger services added with `AddSwaggerGen()`
- Swagger UI enabled in Development with `UseSwagger()` and `UseSwaggerUI()`

### 2. Swagger Integration

Swagger was configured using the default ASP.NET Core setup.

- Swagger URL: `http://localhost:5020/swagger/index.html`
- Launch profile fixed in `Properties/launchSettings.json`
- VS Code debug configuration added so browser launch works from Run and Debug

### 3. Simple MCP Tool

A separate console project was added to host a minimal MCP server.

- Project: `MCP-POC1.McpServer`
- Package used: `ModelContextProtocol`
- Hosting pattern: stdio server

The MCP tool implemented is:

- Tool name: `SayHello`
- Input: `name`
- Output: a friendly greeting message

Implementation files:

- `MCP-POC1.McpServer/Program.cs`
- `MCP-POC1.McpServer/GreetingTools.cs`
- `.vscode/mcp.json`

## How It Works

### Web API

Run the API:

```powershell
dotnet run --project MCP-POC1.csproj
```

Open Swagger:

```text
http://localhost:5020/swagger/index.html
```

### MCP Server

Run the MCP server manually:

```powershell
dotnet run --project MCP-POC1.McpServer/MCP-POC1.McpServer.csproj
```

The workspace also registers this server in `.vscode/mcp.json` so it can be used through VS Code tooling.

## Example Prompt

Example simple prompt:

```text
say hello to mahesh kumar
```

Example response:

```text
Hello, Mahesh Kumar. Greetings from the MCP server.
```

## Validation Done

The following checks were completed:

- Web API build succeeded
- Swagger endpoint returned HTTP 200
- MCP server project build succeeded
- Full solution build succeeded

## Notes

The Web API project was updated so it does not accidentally compile files from the nested MCP server project.