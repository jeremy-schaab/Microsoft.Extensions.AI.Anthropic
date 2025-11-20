# Documentation Index - Microsoft.Extensions.AI.Anthropic

**Version**: 0.3.1-preview
**Last Updated**: 2025-01-19

Welcome to the complete documentation for Microsoft.Extensions.AI.Anthropic. This index will help you find the right documentation for your needs.

## Quick Navigation

| I want to... | Read this document |
|--------------|-------------------|
| **Get started quickly** | [Getting Started Guide](GETTING-STARTED.md) |
| **Understand authentication** | [Authentication Guide](AUTHENTICATION-GUIDE.md) |
| **Look up API details** | [API Reference](API-REFERENCE.md) |
| **Run example projects** | [Examples Guide](EXAMPLES-GUIDE.md) |
| **Understand the architecture** | [Architecture Documentation](ARCHITECTURE.md) |

## Documentation Files

### 1. [GETTING-STARTED.md](GETTING-STARTED.md)

**Audience**: .NET developers new to Anthropic Claude integration

**Topics Covered**:
- Prerequisites (.NET 9, API keys, Azure setup)
- Installation via NuGet
- Quick start for Azure Foundry and Standard API
- First chat application
- Configuration (models, temperature, system messages)
- Common patterns (streaming, DI, error handling, middleware)
- Troubleshooting common issues
- Next steps and learning resources

**Length**: ~600 lines

**Read this if**: You're new to the library and want to get up and running in 10 minutes.

---

### 2. [API-REFERENCE.md](API-REFERENCE.md)

**Audience**: .NET developers needing detailed API documentation

**Topics Covered**:
- **AnthropicChatClient** class (constructors, methods, properties)
- **Extension methods** (IServiceCollection, IChatClientBuilder)
- **Configuration types** (ChatOptions, ChatMessage, AIContent)
- **Authentication types** (credentials for Azure Foundry and Standard API)
- **Internal converters** (message, content, options, tool, streaming)
- **Complete code examples** for every API

**Length**: ~850 lines

**Read this if**: You need reference documentation for a specific class, method, or configuration option.

---

### 3. [AUTHENTICATION-GUIDE.md](AUTHENTICATION-GUIDE.md)

**Audience**: .NET developers deploying production AI applications

**Topics Covered**:
- **Azure Foundry authentication**:
  - Azure Identity (DefaultAzureCredential) - Recommended
  - API Key authentication
  - Bearer Token authentication
- **Standard API authentication** (API key)
- **Environment variables** configuration
- **Production deployment patterns**:
  - Azure App Service with Managed Identity
  - Azure Functions with Managed Identity
  - Azure Container Apps with Managed Identity
  - Azure Kubernetes Service (AKS) with Workload Identity
  - Azure Key Vault integration
- **Security best practices**
- **Troubleshooting authentication issues**

**Length**: ~700 lines

**Read this if**: You need to implement authentication for production deployments or troubleshoot authentication issues.

---

### 4. [EXAMPLES-GUIDE.md](EXAMPLES-GUIDE.md)

**Audience**: Developers learning by example

**Topics Covered**:
- **Overview** of all 5 example projects
- **Prerequisites** for running examples
- **Detailed walkthroughs**:
  1. **AzureFoundryBasicExample** - Azure Foundry fundamentals
  2. **StreamingChatExample** - Real-time streaming
  3. **ToolCallingExample** - Function calling / tools
  4. **VisionExample** - Image and PDF analysis
  5. **AzureFoundryManagedIdentityExample** - Production authentication
- **Quick start commands** for each example
- **Expected output** for each example
- **Key code snippets** highlighting important patterns
- **Quick start matrix** comparing examples
- **Common setup steps**
- **Troubleshooting** example-specific issues

**Length**: ~650 lines

**Read this if**: You learn best by running and modifying working code examples.

---

### 5. [ARCHITECTURE.md](ARCHITECTURE.md)

**Audience**: .NET architects, senior developers, contributors

**Topics Covered**:
- **System architecture** (high-level overview, component diagrams)
- **Component overview**:
  - AnthropicChatClient
  - Type converters (5 specialized converters)
  - Extension methods
- **Design decisions** (why dual client support, why embed SDKs, etc.)
- **Type conversion layer** (bidirectional mapping details)
- **Streaming architecture** (state machine, event flow)
- **Authentication flow** (credential chain)
- **Extension points** (middleware, custom converters, custom credentials)
- **Performance considerations** (streaming vs non-streaming, memory management)
- **Security model** (credential hierarchy, secrets management)

**Length**: ~700 lines

**Read this if**: You want to understand how the library works internally, contribute to the project, or make architectural decisions.

---

## Documentation by Role

### For Beginners

1. Start with [Getting Started Guide](GETTING-STARTED.md)
2. Run [Examples Guide](EXAMPLES-GUIDE.md) - AzureFoundryBasicExample
3. Read [Authentication Guide](AUTHENTICATION-GUIDE.md) - Environment variables section

### For Intermediate Developers

1. Review [API Reference](API-REFERENCE.md) - AnthropicChatClient
2. Read [Authentication Guide](AUTHENTICATION-GUIDE.md) - Azure Foundry section
3. Run [Examples Guide](EXAMPLES-GUIDE.md) - StreamingChatExample and ToolCallingExample

### For Advanced Developers

1. Study [Architecture Documentation](ARCHITECTURE.md)
2. Read [API Reference](API-REFERENCE.md) - Extension methods
3. Implement custom middleware (see Architecture - Extension Points)

### For Production Deployments

1. Read [Authentication Guide](AUTHENTICATION-GUIDE.md) - Production deployment patterns
2. Review [Architecture Documentation](ARCHITECTURE.md) - Security model
3. Run [Examples Guide](EXAMPLES-GUIDE.md) - AzureFoundryManagedIdentityExample

### For Contributors

1. Study [Architecture Documentation](ARCHITECTURE.md) - All sections
2. Review [API Reference](API-REFERENCE.md) - Internal converters
3. Read codebase with architecture knowledge

## Documentation by Use Case

### Use Case: Basic Chat Application

**Path**:
1. [Getting Started](GETTING-STARTED.md) - Your First Chat
2. [API Reference](API-REFERENCE.md) - GetResponseAsync
3. [Examples](EXAMPLES-GUIDE.md) - AzureFoundryBasicExample

### Use Case: Streaming Chat Application

**Path**:
1. [Getting Started](GETTING-STARTED.md) - Pattern 1: Streaming Responses
2. [API Reference](API-REFERENCE.md) - GetStreamingResponseAsync
3. [Examples](EXAMPLES-GUIDE.md) - StreamingChatExample
4. [Architecture](ARCHITECTURE.md) - Streaming Architecture

### Use Case: Function Calling / Tools

**Path**:
1. [Getting Started](GETTING-STARTED.md) - Next Steps - Add Advanced Capabilities
2. [API Reference](API-REFERENCE.md) - ChatOptions.Tools
3. [Examples](EXAMPLES-GUIDE.md) - ToolCallingExample

### Use Case: Image Analysis (Vision)

**Path**:
1. [Getting Started](GETTING-STARTED.md) - Next Steps - Vision
2. [API Reference](API-REFERENCE.md) - DataContent (Images)
3. [Examples](EXAMPLES-GUIDE.md) - VisionExample

### Use Case: Azure Production Deployment

**Path**:
1. [Authentication Guide](AUTHENTICATION-GUIDE.md) - Production Deployment Patterns
2. [Architecture](ARCHITECTURE.md) - Security Model
3. [Examples](EXAMPLES-GUIDE.md) - AzureFoundryManagedIdentityExample

### Use Case: Dependency Injection

**Path**:
1. [Getting Started](GETTING-STARTED.md) - Pattern 2: Dependency Injection
2. [API Reference](API-REFERENCE.md) - Extension Methods
3. [Examples](EXAMPLES-GUIDE.md) - All examples use DI

### Use Case: Custom Middleware

**Path**:
1. [Getting Started](GETTING-STARTED.md) - Pattern 4: Middleware Pipeline
2. [API Reference](API-REFERENCE.md) - IChatClientBuilder Extensions
3. [Architecture](ARCHITECTURE.md) - Extension Points - Middleware Pipeline

## Additional Resources

### Project Files

- **[README.md](../README.md)** - Project overview and features
- **[CLAUDE.md](../CLAUDE.md)** - Claude Code guidance (for contributors)
- **[GAP-ANALYSIS.md](GAP-ANALYSIS.md)** - Implementation status
- **[IMPLEMENTATION-SUMMARY.md](IMPLEMENTATION-SUMMARY.md)** - Recent changes

### External Documentation

- **Microsoft.Extensions.AI**: https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai
- **Anthropic API**: https://docs.anthropic.com/en/api
- **Azure Foundry**: https://learn.microsoft.com/azure/ai-services/anthropic
- **Azure Identity**: https://learn.microsoft.com/azure/active-directory/managed-identities-azure-resources/

### Community

- **GitHub Repository**: https://github.com/jeremy-schaab/Microsoft.Extensions.AI.Anthropic
- **Issues & Discussions**: https://github.com/jeremy-schaab/Microsoft.Extensions.AI.Anthropic/issues

## Documentation Statistics

| Document | Lines | Words (approx) | Reading Time |
|----------|-------|---------------|--------------|
| GETTING-STARTED.md | ~600 | 4,500 | 15-20 min |
| API-REFERENCE.md | ~850 | 6,000 | 20-25 min |
| AUTHENTICATION-GUIDE.md | ~700 | 5,200 | 18-22 min |
| EXAMPLES-GUIDE.md | ~650 | 4,800 | 16-20 min |
| ARCHITECTURE.md | ~700 | 5,000 | 18-22 min |
| **Total** | **~3,500** | **~25,500** | **~90 min** |

## Feedback

Found an issue with the documentation? Have a suggestion?

- **GitHub Issues**: https://github.com/jeremy-schaab/Microsoft.Extensions.AI.Anthropic/issues
- **Pull Requests**: Contributions welcome!

---

**Last Updated**: 2025-01-19 | **Version**: 0.3.1-preview
