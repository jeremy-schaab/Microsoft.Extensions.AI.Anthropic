# Tool Calling Example

This example demonstrates comprehensive tool/function calling capabilities with **Microsoft.Extensions.AI.Anthropic**.

## Overview

Tool calling (also known as function calling) enables Claude to interact with external systems, retrieve real-time data, and perform calculations by invoking developer-defined functions.

## Features Demonstrated

1. **Basic Tool Calling**: Single tool invocation with argument passing
2. **Multiple Tools**: Claude calling multiple tools in a single turn
3. **Multi-Turn Conversations**: Stateful conversations with tool results
4. **Tool Choice Modes**: Auto, RequireAny, and RequireSpecific modes
5. **Error Handling**: Graceful handling of tool execution errors
6. **Automatic Tool Invocation**: Using `AIFunctionFactory.Create()` for seamless integration

## Tools Implemented

### 1. Weather Tool
```csharp
GetWeather(string location, string unit = "celsius")
```
Returns simulated weather data for a given location with temperature, conditions, humidity, and wind speed.

### 2. Calculator Tool
```csharp
Calculate(string operation, double a, double b)
```
Performs mathematical operations (add, subtract, multiply, divide) with error handling for division by zero.

### 3. Time Tool
```csharp
GetCurrentTime(string? timezone = null)
```
Returns current date/time information with timezone support, ISO8601 format, and Unix timestamp.

## Prerequisites

**Required Environment Variable:**
```bash
ANTHROPIC_FOUNDRY_RESOURCE=<your-azure-anthropic-foundry-resource>
```

**Optional:**
```bash
ANTHROPIC_FOUNDRY_API_KEY=<your-api-key>
```
If not provided, Azure DefaultAzureCredential will be used.

## Running the Example

```bash
# Set environment variable
$env:ANTHROPIC_FOUNDRY_RESOURCE = "your-resource-name"

# Run the example
dotnet run
```

## Code Structure

### Tool Registration

Tools are registered using `AIFunctionFactory.Create()` which automatically generates function metadata from C# method signatures:

```csharp
var tools = new List<AITool>
{
    AIFunctionFactory.Create(weatherTool.GetWeather),
    AIFunctionFactory.Create(calculatorTool.Calculate),
    AIFunctionFactory.Create(timeTool.GetCurrentTime)
};
```

### Tool Metadata

The `[Description]` attribute on methods and parameters provides Claude with context about tool purpose and argument types:

```csharp
[Description("Get the current weather for a specified location")]
public async Task<string> GetWeather(
    [Description("The city name (e.g., 'Seattle', 'London')")] string location,
    [Description("Temperature unit: 'celsius' or 'fahrenheit'")] string unit = "celsius")
```

### Tool Invocation Flow

1. **User Request**: "What's the weather in Seattle?"
2. **Claude Response**: Returns `FunctionCallContent` with tool name and arguments
3. **Tool Execution**: Developer invokes the tool with provided arguments
4. **Result Submission**: Tool result added as `FunctionResultContent` to conversation
5. **Final Response**: Claude synthesizes natural language response from tool results

```csharp
// Detect tool calls in response
var toolCalls = response.Message.Contents.OfType<FunctionCallContent>();

foreach (var toolCall in toolCalls)
{
    // Find matching tool
    var tool = tools.OfType<AIFunction>()
        .FirstOrDefault(t => t.Metadata.Name == toolCall.Name);

    // Invoke tool
    var result = await tool.InvokeAsync(toolCall.Arguments);

    // Add result to conversation
    messages.Add(new ChatMessage(ChatRole.Tool,
        [new FunctionResultContent(toolCall.CallId, toolCall.Name, result)]));
}

// Get final response with tool results
var finalResponse = await chatClient.CompleteAsync(messages, options);
```

## Tool Choice Modes

### Auto (Default)
Claude decides whether to use tools based on the user's request:
```csharp
var options = new ChatOptions
{
    Tools = tools,
    ToolMode = ChatToolMode.Auto
};
```

### RequireAny
Force Claude to use at least one tool:
```csharp
var options = new ChatOptions
{
    Tools = tools,
    ToolMode = ChatToolMode.RequireAny
};
```

### RequireSpecific
Force Claude to use a specific tool:
```csharp
var options = new ChatOptions
{
    Tools = tools,
    ToolMode = ChatToolMode.RequireSpecific("get_current_time")
};
```

## Example Conversations

### Single Tool Call
```
User: What's the weather like in Seattle?
Assistant: [Tool Call: get_weather({"location":"Seattle","unit":"celsius"})]
[Executing: get_weather]
Assistant: The weather in Seattle is currently 18°C and cloudy with 65% humidity...
```

### Multiple Tool Calls
```
User: What's the weather in London and Paris? Also, what time is it?
Assistant: [Tool Call: get_weather({"location":"London"})]
          [Tool Call: get_weather({"location":"Paris"})]
          [Tool Call: get_current_time({})]
[Processing 3 tool calls]
Assistant: In London, it's 15°C and rainy. In Paris, it's 22°C and sunny.
          The current time is 2025-01-19 14:30:00 UTC.
```

### Multi-Turn with Context
```
User: Calculate 15 * 24
Assistant: [Tool Call: calculate({"operation":"multiply","a":15,"b":24})]
Assistant: The result is 360.

User: Now add 100 to that result
Assistant: [Tool Call: calculate({"operation":"add","a":360,"b":100})]
Assistant: Adding 100 to 360 gives you 460.
```

## Error Handling

Tools implement error handling for invalid inputs:

```csharp
public Task<string> Calculate(string operation, double a, double b)
{
    try
    {
        double result = operation.ToLower() switch
        {
            "divide" when b == 0 => throw new DivideByZeroException(),
            // ... other operations
        };
        return Task.FromResult(JsonSerializer.Serialize(new { result }));
    }
    catch (Exception ex)
    {
        return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
    }
}
```

## Best Practices

1. **Clear Descriptions**: Use `[Description]` attributes on methods and parameters
2. **Structured Results**: Return JSON-serialized objects for consistent parsing
3. **Error Handling**: Handle exceptions gracefully and return error details
4. **Async Operations**: Use `async/await` for I/O-bound operations
5. **Type Safety**: Leverage C# type system for argument validation
6. **Stateless Tools**: Design tools to be stateless and reusable
7. **CallId Tracking**: Always include `CallId` when returning `FunctionResultContent`

## Production Considerations

- **Authentication**: Use Azure Managed Identity instead of API keys
- **Tool Timeout**: Implement timeout handling for long-running tools
- **Rate Limiting**: Add rate limiting for external API calls
- **Caching**: Cache tool results when appropriate
- **Logging**: Log tool invocations and results for debugging
- **Validation**: Validate tool arguments before execution
- **Security**: Sanitize inputs to prevent injection attacks

## Next Steps

- Implement real weather API integration (OpenWeatherMap, Weather.gov)
- Add database query tools for data retrieval
- Create custom tools for your domain (e.g., CRM, inventory, analytics)
- Implement tool result streaming for long operations
- Add tool execution metrics and monitoring

## References

- [Microsoft.Extensions.AI Documentation](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai)
- [Anthropic Tool Use Documentation](https://docs.anthropic.com/en/docs/build-with-claude/tool-use)
- [AIFunctionFactory API Reference](https://learn.microsoft.com/dotnet/api/microsoft.extensions.ai.aifunctionfactory)
