using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text.Json;

namespace ToolCallingExample;

/// <summary>
/// Comprehensive example demonstrating tool/function calling with Microsoft.Extensions.AI.Anthropic
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Microsoft.Extensions.AI.Anthropic - Tool Calling Example ===\n");

        // Check for required environment variable
        var resourceName = Environment.GetEnvironmentVariable("ANTHROPIC_FOUNDRY_RESOURCE");
        if (string.IsNullOrEmpty(resourceName))
        {
            Console.WriteLine("Error: ANTHROPIC_FOUNDRY_RESOURCE environment variable not set.");
            Console.WriteLine("Set it to your Azure Anthropic Foundry resource name.");
            return;
        }

        // Setup dependency injection with logging
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning); // Reduce noise
        });

        // Register Anthropic Foundry chat client
        services.AddAnthropicFoundryChatClient(resourceName);

        var serviceProvider = services.BuildServiceProvider();
        var chatClient = serviceProvider.GetRequiredService<IChatClient>();

        // Create tool instances
        var weatherTool = new WeatherTool();
        var calculatorTool = new CalculatorTool();
        var timeTool = new TimeTool();

        // Create function declarations from tool methods
        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(weatherTool.GetWeather),
            AIFunctionFactory.Create(calculatorTool.Calculate),
            AIFunctionFactory.Create(timeTool.GetCurrentTime)
        };

        Console.WriteLine($"Registered {tools.Count} tools:");
        foreach (var tool in tools)
        {
            if (tool is AIFunction func)
            {
                Console.WriteLine($"  - {func.Metadata.Name}: {func.Metadata.Description}");
            }
        }
        Console.WriteLine();

        // Run different examples
        await RunBasicToolExample(chatClient, tools);
        Console.WriteLine("\n" + new string('=', 80) + "\n");

        await RunMultiToolExample(chatClient, tools);
        Console.WriteLine("\n" + new string('=', 80) + "\n");

        await RunMultiTurnExample(chatClient, tools);
        Console.WriteLine("\n" + new string('=', 80) + "\n");

        await RunToolChoiceModes(chatClient, tools);
    }

    /// <summary>
    /// Example 1: Basic single tool call
    /// </summary>
    static async Task RunBasicToolExample(IChatClient chatClient, List<AITool> tools)
    {
        Console.WriteLine("--- Example 1: Basic Tool Call ---\n");

        var options = new ChatOptions
        {
            Tools = tools
        };

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "What's the weather like in Seattle?")
        };

        PrintUserMessage(messages[0]);

        var response = await chatClient.CompleteAsync(messages, options);
        PrintAssistantResponse(response);

        // Check if tool was called
        if (response.Message.Contents.Any(c => c is FunctionCallContent))
        {
            Console.WriteLine("\n[Tool calls detected - handling automatically with AIFunction]\n");

            // Add assistant's response (with tool calls) to conversation
            messages.Add(response.Message);

            // Execute tool calls and add results to conversation
            foreach (var content in response.Message.Contents.OfType<FunctionCallContent>())
            {
                Console.WriteLine($"Executing tool: {content.Name}");
                Console.WriteLine($"Arguments: {JsonSerializer.Serialize(content.Arguments)}");

                // Find and invoke the tool
                var tool = tools.OfType<AIFunction>().FirstOrDefault(t => t.Metadata.Name == content.Name);
                if (tool != null)
                {
                    var result = await tool.InvokeAsync(content.Arguments);
                    var resultContent = new FunctionResultContent(content.CallId, content.Name, result);
                    messages.Add(new ChatMessage(ChatRole.Tool, [resultContent]));

                    Console.WriteLine($"Result: {result}\n");
                }
            }

            // Get final response after tool execution
            var finalResponse = await chatClient.CompleteAsync(messages, options);
            PrintAssistantResponse(finalResponse);
        }
    }

    /// <summary>
    /// Example 2: Multiple tool calls in single turn
    /// </summary>
    static async Task RunMultiToolExample(IChatClient chatClient, List<AITool> tools)
    {
        Console.WriteLine("--- Example 2: Multiple Tool Calls ---\n");

        var options = new ChatOptions
        {
            Tools = tools
        };

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "What's the weather in London and Paris? Also, what time is it now?")
        };

        PrintUserMessage(messages[0]);

        var response = await chatClient.CompleteAsync(messages, options);
        PrintAssistantResponse(response);

        // Process tool calls
        var toolCalls = response.Message.Contents.OfType<FunctionCallContent>().ToList();
        if (toolCalls.Any())
        {
            Console.WriteLine($"\n[Processing {toolCalls.Count} tool calls]\n");
            messages.Add(response.Message);

            foreach (var toolCall in toolCalls)
            {
                Console.WriteLine($"Tool: {toolCall.Name}");
                Console.WriteLine($"Arguments: {JsonSerializer.Serialize(toolCall.Arguments)}");

                var tool = tools.OfType<AIFunction>().FirstOrDefault(t => t.Metadata.Name == toolCall.Name);
                if (tool != null)
                {
                    var result = await tool.InvokeAsync(toolCall.Arguments);
                    messages.Add(new ChatMessage(ChatRole.Tool,
                        [new FunctionResultContent(toolCall.CallId, toolCall.Name, result)]));
                    Console.WriteLine($"Result: {result}\n");
                }
            }

            var finalResponse = await chatClient.CompleteAsync(messages, options);
            PrintAssistantResponse(finalResponse);
        }
    }

    /// <summary>
    /// Example 3: Multi-turn conversation with tools
    /// </summary>
    static async Task RunMultiTurnExample(IChatClient chatClient, List<AITool> tools)
    {
        Console.WriteLine("--- Example 3: Multi-Turn Conversation with Tools ---\n");

        var options = new ChatOptions
        {
            Tools = tools
        };

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are a helpful assistant. Use the available tools to answer questions accurately.")
        };

        // Turn 1
        messages.Add(new ChatMessage(ChatRole.User, "Calculate 15 * 24"));
        PrintUserMessage(messages[^1]);
        await ProcessTurnWithTools(chatClient, messages, options, tools);

        // Turn 2
        messages.Add(new ChatMessage(ChatRole.User, "Now add 100 to that result"));
        PrintUserMessage(messages[^1]);
        await ProcessTurnWithTools(chatClient, messages, options, tools);

        // Turn 3
        messages.Add(new ChatMessage(ChatRole.User, "What's the weather like in Tokyo?"));
        PrintUserMessage(messages[^1]);
        await ProcessTurnWithTools(chatClient, messages, options, tools);
    }

    /// <summary>
    /// Example 4: Different tool choice modes
    /// </summary>
    static async Task RunToolChoiceModes(IChatClient chatClient, List<AITool> tools)
    {
        Console.WriteLine("--- Example 4: Tool Choice Modes ---\n");

        // Mode 1: Auto (default) - Model decides whether to use tools
        Console.WriteLine("Mode: Auto (model decides)");
        var autoOptions = new ChatOptions
        {
            Tools = tools,
            ToolMode = ChatToolMode.Auto
        };

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello! How are you?")
        };
        PrintUserMessage(messages[0]);
        var response = await chatClient.CompleteAsync(messages, autoOptions);
        PrintAssistantResponse(response);
        Console.WriteLine($"Tools used: {response.Message.Contents.OfType<FunctionCallContent>().Any()}\n");

        // Mode 2: RequireAny - Force model to use at least one tool
        Console.WriteLine("Mode: RequireAny (force tool use)");
        var requireOptions = new ChatOptions
        {
            Tools = tools,
            ToolMode = ChatToolMode.RequireAny
        };

        messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Tell me something")
        };
        PrintUserMessage(messages[0]);
        response = await chatClient.CompleteAsync(messages, requireOptions);
        PrintAssistantResponse(response);
        Console.WriteLine($"Tools used: {response.Message.Contents.OfType<FunctionCallContent>().Any()}\n");

        // Mode 3: RequireSpecific - Force specific tool
        Console.WriteLine("Mode: RequireSpecific (force specific tool)");
        var specificTool = tools.OfType<AIFunction>().First(t => t.Metadata.Name == "get_current_time");
        var specificOptions = new ChatOptions
        {
            Tools = tools,
            ToolMode = ChatToolMode.RequireSpecific(specificTool.Metadata.Name)
        };

        messages = new List<ChatMessage>
        {
            new(ChatRole.User, "What's happening?")
        };
        PrintUserMessage(messages[0]);
        response = await chatClient.CompleteAsync(messages, specificOptions);
        PrintAssistantResponse(response);

        var toolCall = response.Message.Contents.OfType<FunctionCallContent>().FirstOrDefault();
        if (toolCall != null)
        {
            Console.WriteLine($"Forced tool: {toolCall.Name}\n");
            var tool = tools.OfType<AIFunction>().First(t => t.Metadata.Name == toolCall.Name);
            var result = await tool.InvokeAsync(toolCall.Arguments);
            Console.WriteLine($"Result: {result}");
        }
    }

    /// <summary>
    /// Helper method to process a conversation turn with tool calls
    /// </summary>
    static async Task ProcessTurnWithTools(IChatClient chatClient, List<ChatMessage> messages,
        ChatOptions options, List<AITool> tools)
    {
        var response = await chatClient.CompleteAsync(messages, options);
        PrintAssistantResponse(response);

        var toolCalls = response.Message.Contents.OfType<FunctionCallContent>().ToList();
        if (toolCalls.Any())
        {
            messages.Add(response.Message);

            foreach (var toolCall in toolCalls)
            {
                Console.WriteLine($"[Executing: {toolCall.Name}]");
                var tool = tools.OfType<AIFunction>().FirstOrDefault(t => t.Metadata.Name == toolCall.Name);
                if (tool != null)
                {
                    var result = await tool.InvokeAsync(toolCall.Arguments);
                    messages.Add(new ChatMessage(ChatRole.Tool,
                        [new FunctionResultContent(toolCall.CallId, toolCall.Name, result)]));
                }
            }

            var finalResponse = await chatClient.CompleteAsync(messages, options);
            PrintAssistantResponse(finalResponse);
        }
    }

    static void PrintUserMessage(ChatMessage message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"User: {message.Text}");
        Console.ResetColor();
    }

    static void PrintAssistantResponse(ChatCompletion response)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;

        // Print text content
        var textContent = response.Message.Text;
        if (!string.IsNullOrEmpty(textContent))
        {
            Console.WriteLine($"Assistant: {textContent}");
        }

        // Print tool calls
        var toolCalls = response.Message.Contents.OfType<FunctionCallContent>();
        foreach (var toolCall in toolCalls)
        {
            Console.WriteLine($"[Tool Call: {toolCall.Name}({JsonSerializer.Serialize(toolCall.Arguments)})]");
        }

        // Print usage if available
        if (response.Usage != null)
        {
            Console.WriteLine($"[Tokens: {response.Usage.InputTokenCount} in, {response.Usage.OutputTokenCount} out]");
        }

        Console.ResetColor();
    }
}

// ============================================================================
// Tool Implementations
// ============================================================================

/// <summary>
/// Weather tool - simulates weather data retrieval
/// </summary>
public class WeatherTool
{
    [Description("Get the current weather for a specified location")]
    public async Task<string> GetWeather(
        [Description("The city name (e.g., 'Seattle', 'London')")] string location,
        [Description("Temperature unit: 'celsius' or 'fahrenheit'")] string unit = "celsius")
    {
        // Simulate API delay
        await Task.Delay(100);

        // Simulate weather data (in real scenario, call weather API)
        var temperature = Random.Shared.Next(-10, 35);
        var conditions = new[] { "sunny", "cloudy", "rainy", "partly cloudy", "overcast" };
        var condition = conditions[Random.Shared.Next(conditions.Length)];

        var tempUnit = unit.ToLower() == "fahrenheit" ? "°F" : "°C";
        if (unit.ToLower() == "fahrenheit")
        {
            temperature = (int)(temperature * 9.0 / 5.0 + 32);
        }

        var weatherData = new
        {
            location = location,
            temperature = $"{temperature}{tempUnit}",
            condition = condition,
            humidity = $"{Random.Shared.Next(30, 90)}%",
            wind_speed = $"{Random.Shared.Next(5, 30)} km/h"
        };

        return JsonSerializer.Serialize(weatherData);
    }
}

/// <summary>
/// Calculator tool - performs mathematical operations
/// </summary>
public class CalculatorTool
{
    [Description("Perform a mathematical calculation")]
    public Task<string> Calculate(
        [Description("The mathematical operation: 'add', 'subtract', 'multiply', or 'divide'")] string operation,
        [Description("First number")] double a,
        [Description("Second number")] double b)
    {
        try
        {
            double result = operation.ToLower() switch
            {
                "add" or "+" => a + b,
                "subtract" or "-" => a - b,
                "multiply" or "*" => a * b,
                "divide" or "/" when b != 0 => a / b,
                "divide" or "/" when b == 0 => throw new DivideByZeroException("Cannot divide by zero"),
                _ => throw new ArgumentException($"Unknown operation: {operation}")
            };

            var calculationResult = new
            {
                operation = operation,
                operand1 = a,
                operand2 = b,
                result = result
            };

            return Task.FromResult(JsonSerializer.Serialize(calculationResult));
        }
        catch (Exception ex)
        {
            var error = new
            {
                error = ex.Message,
                operation = operation,
                operand1 = a,
                operand2 = b
            };

            return Task.FromResult(JsonSerializer.Serialize(error));
        }
    }
}

/// <summary>
/// Time tool - provides current time information
/// </summary>
public class TimeTool
{
    [Description("Get the current date and time, optionally for a specific timezone")]
    public Task<string> GetCurrentTime(
        [Description("Timezone identifier (e.g., 'UTC', 'America/New_York', 'Europe/London')")] string? timezone = null)
    {
        try
        {
            DateTime currentTime;
            string timezoneDisplay;

            if (string.IsNullOrEmpty(timezone) || timezone.Equals("local", StringComparison.OrdinalIgnoreCase))
            {
                currentTime = DateTime.Now;
                timezoneDisplay = "Local";
            }
            else if (timezone.Equals("UTC", StringComparison.OrdinalIgnoreCase))
            {
                currentTime = DateTime.UtcNow;
                timezoneDisplay = "UTC";
            }
            else
            {
                // For demonstration, we'll just use UTC for other timezones
                // In production, use TimeZoneInfo.FindSystemTimeZoneById()
                currentTime = DateTime.UtcNow;
                timezoneDisplay = timezone;
            }

            var timeInfo = new
            {
                timezone = timezoneDisplay,
                datetime = currentTime.ToString("yyyy-MM-dd HH:mm:ss"),
                iso8601 = currentTime.ToString("o"),
                unix_timestamp = new DateTimeOffset(currentTime).ToUnixTimeSeconds(),
                day_of_week = currentTime.DayOfWeek.ToString()
            };

            return Task.FromResult(JsonSerializer.Serialize(timeInfo));
        }
        catch (Exception ex)
        {
            var error = new
            {
                error = ex.Message,
                timezone = timezone
            };

            return Task.FromResult(JsonSerializer.Serialize(error));
        }
    }
}
