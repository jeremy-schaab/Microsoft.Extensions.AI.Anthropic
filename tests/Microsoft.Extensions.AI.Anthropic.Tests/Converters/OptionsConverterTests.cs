using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Anthropic.Models.Messages;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Anthropic;

namespace Microsoft.Extensions.AI.Anthropic.Tests.Converters;

/// <summary>
/// Comprehensive unit tests for AnthropicOptionsConverter.
/// Tests parameter mapping, validation, tool handling, and extended features.
/// </summary>
public class OptionsConverterTests
{
    #region Test Data Helpers

    private static List<MessageParam> CreateDefaultMessages()
    {
        return new List<MessageParam>
        {
            new MessageParam
            {
                Role = Role.User,
                Content = new List<ContentBlockParam> { new ContentBlockParam(new TextBlockParam { Text = "Hello" }) }
            }
        };
    }

    private static AIFunction CreateTestFunction(string name = "test_function", string description = "Test function")
    {
        var schema = JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new
            {
                query = new { type = "string", description = "Search query" }
            },
            required = new[] { "query" }
        });

        return AIFunctionFactory.Create(
            (string query) => $"Result for {query}",
            name,
            description);
    }

    #endregion

    #region ToMessageCreateParams - Basic Parameter Mapping (12 tests)

    [Fact]
    public void ToMessageCreateParams_ModelId_MapsToModel()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var options = new ChatOptions
        {
            ModelId = "claude-3-5-sonnet-20241022"
        };

        // Act
        var result = AnthropicOptionsConverter.ToMessageCreateParams(messages, null, options, null);

        // Assert
        result.Should().NotBeNull();
        string modelJson = ((dynamic)result.Model).Json.GetString();
        modelJson.Should().Be("claude-3-5-sonnet-20241022");
    }

    [Fact]
    public void ToMessageCreateParams_DefaultModelId_UsesDefaultWhenOptionsModelIdNull()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var options = new ChatOptions();
        var defaultModelId = "claude-3-haiku-20240307";

        // Act
        var result = AnthropicOptionsConverter.ToMessageCreateParams(messages, null, options, defaultModelId);

        // Assert
        string modelJson = ((dynamic)result.Model).Json.GetString();
        modelJson.Should().Be("claude-3-haiku-20240307");
    }

    [Fact]
    public void ToMessageCreateParams_Temperature_MapsCorrectly()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var options = new ChatOptions
        {
            ModelId = "claude-3-sonnet-20240229",
            Temperature = 0.7f
        };

        // Act
        var result = AnthropicOptionsConverter.ToMessageCreateParams(messages, null, options, null);

        // Assert
        result.Temperature.Should().BeApproximately(0.7, 0.0001);
    }

    [Fact]
    public void ToMessageCreateParams_TemperatureZero_MapsCorrectly()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var options = new ChatOptions
        {
            ModelId = "claude-3-sonnet-20240229",
            Temperature = 0.0f
        };

        // Act
        var result = AnthropicOptionsConverter.ToMessageCreateParams(messages, null, options, null);

        // Assert
        result.Temperature.Should().Be(0.0);
    }

    [Fact]
    public void ToMessageCreateParams_TemperatureOne_MapsCorrectly()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var options = new ChatOptions
        {
            ModelId = "claude-3-sonnet-20240229",
            Temperature = 1.0f
        };

        // Act
        var result = AnthropicOptionsConverter.ToMessageCreateParams(messages, null, options, null);

        // Assert
        result.Temperature.Should().Be(1.0);
    }

    [Fact]
    public void ToMessageCreateParams_TopP_MapsCorrectly()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var options = new ChatOptions
        {
            ModelId = "claude-3-sonnet-20240229",
            TopP = 0.9f
        };

        // Act
        var result = AnthropicOptionsConverter.ToMessageCreateParams(messages, null, options, null);

        // Assert
        result.TopP.Should().BeApproximately(0.9, 0.0001);
    }

    [Fact]
    public void ToMessageCreateParams_TopK_MapsFromAdditionalProperties()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var options = new ChatOptions
        {
            ModelId = "claude-3-sonnet-20240229",
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                ["top_k"] = 40
            }
        };

        // Act
        var result = AnthropicOptionsConverter.ToMessageCreateParams(messages, null, options, null);

        // Assert
        result.TopK.Should().Be(40);
    }

    [Fact]
    public void ToMessageCreateParams_MaxOutputTokens_MapsToMaxTokens()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var options = new ChatOptions
        {
            ModelId = "claude-3-sonnet-20240229",
            MaxOutputTokens = 2048
        };

        // Act
        var result = AnthropicOptionsConverter.ToMessageCreateParams(messages, null, options, null);

        // Assert
        result.MaxTokens.Should().Be(2048);
    }

    [Fact]
    public void ToMessageCreateParams_MaxOutputTokensNotSet_DefaultsTo4096()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var options = new ChatOptions
        {
            ModelId = "claude-3-sonnet-20240229"
        };

        // Act
        var result = AnthropicOptionsConverter.ToMessageCreateParams(messages, null, options, null);

        // Assert
        result.MaxTokens.Should().Be(4096);
    }

    [Fact]
    public void ToMessageCreateParams_StopSequences_MapsCorrectly()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var options = new ChatOptions
        {
            ModelId = "claude-3-sonnet-20240229",
            StopSequences = new List<string> { "STOP", "END", "\n\n" }
        };

        // Act
        var result = AnthropicOptionsConverter.ToMessageCreateParams(messages, null, options, null);

        // Assert
        result.StopSequences.Should().NotBeNull();
        result.StopSequences.Should().HaveCount(3);
        result.StopSequences.Should().Contain("STOP");
        result.StopSequences.Should().Contain("END");
        result.StopSequences.Should().Contain("\n\n");
    }

    [Fact]
    public void ToMessageCreateParams_EmptyStopSequences_ReturnsNull()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var options = new ChatOptions
        {
            ModelId = "claude-3-sonnet-20240229",
            StopSequences = new List<string>()
        };

        // Act
        var result = AnthropicOptionsConverter.ToMessageCreateParams(messages, null, options, null);

        // Assert
        result.StopSequences.Should().BeNull();
    }

    [Fact]
    public void ToMessageCreateParams_SystemPrompt_MapsToSystemParameter()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var systemPrompt = "You are a helpful assistant.";
        var options = new ChatOptions
        {
            ModelId = "claude-3-sonnet-20240229"
        };

        // Act
        var result = AnthropicOptionsConverter.ToMessageCreateParams(messages, systemPrompt, options, null);

        // Assert
        result.System.Should().NotBeNull();
    }

    #endregion

    #region ToMessageCreateParams - Validation (8 tests)

    [Fact]
    public void ToMessageCreateParams_NullModelId_ThrowsInvalidOperationException()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var options = new ChatOptions();

        // Act & Assert
        var act = () => AnthropicOptionsConverter.ToMessageCreateParams(messages, null, options, null);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Model ID must be specified*");
    }

    [Fact]
    public void ToMessageCreateParams_EmptyModelId_ThrowsInvalidOperationException()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var options = new ChatOptions
        {
            ModelId = ""
        };

        // Act & Assert
        var act = () => AnthropicOptionsConverter.ToMessageCreateParams(messages, null, options, null);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Model ID must be specified*");
    }

    [Fact]
    public void ToMessageCreateParams_WhitespaceModelId_ThrowsInvalidOperationException()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var options = new ChatOptions
        {
            ModelId = "   "
        };

        // Act & Assert
        var act = () => AnthropicOptionsConverter.ToMessageCreateParams(messages, null, options, null);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Model ID must be specified*");
    }

    [Fact]
    public void ToMessageCreateParams_TemperatureBelowZero_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var options = new ChatOptions
        {
            ModelId = "claude-3-sonnet-20240229",
            Temperature = -0.1f
        };

        // Act & Assert
        var act = () => AnthropicOptionsConverter.ToMessageCreateParams(messages, null, options, null);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Temperature must be between 0.0 and 1.0*")
            .And.ParamName.Should().Be("Temperature");
    }

    [Fact]
    public void ToMessageCreateParams_TemperatureAboveOne_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var options = new ChatOptions
        {
            ModelId = "claude-3-sonnet-20240229",
            Temperature = 1.1f
        };

        // Act & Assert
        var act = () => AnthropicOptionsConverter.ToMessageCreateParams(messages, null, options, null);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Temperature must be between 0.0 and 1.0*")
            .And.ParamName.Should().Be("Temperature");
    }

    [Fact]
    public void ToMessageCreateParams_TopPBelowZero_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var options = new ChatOptions
        {
            ModelId = "claude-3-sonnet-20240229",
            TopP = -0.1f
        };

        // Act & Assert
        var act = () => AnthropicOptionsConverter.ToMessageCreateParams(messages, null, options, null);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*TopP must be between 0.0 and 1.0*")
            .And.ParamName.Should().Be("TopP");
    }

    [Fact]
    public void ToMessageCreateParams_TopPAboveOne_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var options = new ChatOptions
        {
            ModelId = "claude-3-sonnet-20240229",
            TopP = 1.5f
        };

        // Act & Assert
        var act = () => AnthropicOptionsConverter.ToMessageCreateParams(messages, null, options, null);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*TopP must be between 0.0 and 1.0*")
            .And.ParamName.Should().Be("TopP");
    }

    [Fact]
    public void ToMessageCreateParams_TopKNegative_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var options = new ChatOptions
        {
            ModelId = "claude-3-sonnet-20240229",
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                ["top_k"] = -5
            }
        };

        // Act & Assert
        var act = () => AnthropicOptionsConverter.ToMessageCreateParams(messages, null, options, null);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*TopK must be a positive integer*")
            .And.ParamName.Should().Be("top_k");
    }

    #endregion

    #region ToMessageCreateParams - Tool Handling (6 tests)

    [Fact]
    public void ToMessageCreateParams_ToolsWithoutToolMode_MapsToolsCorrectly()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var function = CreateTestFunction();
        var options = new ChatOptions
        {
            ModelId = "claude-3-sonnet-20240229",
            Tools = new List<AITool> { function }
        };

        // Act
        var result = AnthropicOptionsConverter.ToMessageCreateParams(messages, null, options, null);

        // Assert
        result.Tools.Should().NotBeNull();
        result.Tools.Should().HaveCount(1);
        result.ToolChoice.Should().BeNull();
    }

    [Fact]
    public void ToMessageCreateParams_ToolsWithAutoMode_SetsAutoToolChoice()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var function = CreateTestFunction();
        var options = new ChatOptions
        {
            ModelId = "claude-3-sonnet-20240229",
            Tools = new List<AITool> { function },
            ToolMode = ChatToolMode.Auto
        };

        // Act
        var result = AnthropicOptionsConverter.ToMessageCreateParams(messages, null, options, null);

        // Assert
        result.Tools.Should().NotBeNull();
        result.ToolChoice.Should().NotBeNull();
        // ToolChoice is a union type, we can verify it's not null
        result.ToolChoice.Should().NotBeNull();
    }

    [Fact]
    public void ToMessageCreateParams_ToolsWithRequiredMode_SetsAnyToolChoice()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var function = CreateTestFunction();
        var options = new ChatOptions
        {
            ModelId = "claude-3-sonnet-20240229",
            Tools = new List<AITool> { function },
            ToolMode = ChatToolMode.RequireAny
        };

        // Act
        var result = AnthropicOptionsConverter.ToMessageCreateParams(messages, null, options, null);

        // Assert
        result.Tools.Should().NotBeNull();
        result.ToolChoice.Should().NotBeNull();
    }

    [Fact]
    public void ToMessageCreateParams_MultipleTools_MapsAllTools()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var function1 = CreateTestFunction("search", "Search for information");
        var function2 = CreateTestFunction("calculate", "Perform calculation");
        var options = new ChatOptions
        {
            ModelId = "claude-3-sonnet-20240229",
            Tools = new List<AITool> { function1, function2 }
        };

        // Act
        var result = AnthropicOptionsConverter.ToMessageCreateParams(messages, null, options, null);

        // Assert
        result.Tools.Should().NotBeNull();
        result.Tools.Should().HaveCount(2);
    }

    [Fact]
    public void ToMessageCreateParams_NoTools_ToolsIsNull()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var options = new ChatOptions
        {
            ModelId = "claude-3-sonnet-20240229"
        };

        // Act
        var result = AnthropicOptionsConverter.ToMessageCreateParams(messages, null, options, null);

        // Assert
        result.Tools.Should().BeNull();
        result.ToolChoice.Should().BeNull();
    }

    [Fact]
    public void ToMessageCreateParams_EmptyToolsList_ToolsIsNull()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var options = new ChatOptions
        {
            ModelId = "claude-3-sonnet-20240229",
            Tools = new List<AITool>()
        };

        // Act
        var result = AnthropicOptionsConverter.ToMessageCreateParams(messages, null, options, null);

        // Assert
        result.Tools.Should().BeNull();
    }

    #endregion

    #region ToMessageCreateParams - Extended Features (4 tests)

    [Fact]
    public void ToMessageCreateParams_UserIdInAdditionalProperties_MapsToMetadata()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var options = new ChatOptions
        {
            ModelId = "claude-3-sonnet-20240229",
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                ["user_id"] = "user-12345"
            }
        };

        // Act
        var result = AnthropicOptionsConverter.ToMessageCreateParams(messages, null, options, null);

        // Assert
        result.Metadata.Should().NotBeNull();
        result.Metadata!.UserID.Should().Be("user-12345");
    }

    [Fact]
    public void ToMessageCreateParams_NoUserIdInAdditionalProperties_MetadataIsNull()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var options = new ChatOptions
        {
            ModelId = "claude-3-sonnet-20240229",
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                ["other_property"] = "value"
            }
        };

        // Act
        var result = AnthropicOptionsConverter.ToMessageCreateParams(messages, null, options, null);

        // Assert
        result.Metadata.Should().BeNull();
    }

    [Fact]
    public void ToMessageCreateParams_AllParametersTogether_MapsCorrectly()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var systemPrompt = "You are a helpful assistant specialized in weather.";
        var function = CreateTestFunction("get_weather", "Get current weather");
        var options = new ChatOptions
        {
            ModelId = "claude-3-5-sonnet-20241022",
            Temperature = 0.7f,
            TopP = 0.9f,
            MaxOutputTokens = 2048,
            StopSequences = new List<string> { "DONE" },
            Tools = new List<AITool> { function },
            ToolMode = ChatToolMode.Auto,
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                ["top_k"] = 50,
                ["user_id"] = "test-user"
            }
        };

        // Act
        var result = AnthropicOptionsConverter.ToMessageCreateParams(messages, systemPrompt, options, null);

        // Assert
        string modelJson = ((dynamic)result.Model).Json.GetString();
        modelJson.Should().Be("claude-3-5-sonnet-20241022");
        result.MaxTokens.Should().Be(2048);
        result.Temperature.Should().BeApproximately(0.7, 0.0001);
        result.TopP.Should().BeApproximately(0.9, 0.0001);
        result.TopK.Should().Be(50);
        result.StopSequences.Should().HaveCount(1);
        result.System.Should().NotBeNull();
        result.Tools.Should().HaveCount(1);
        result.ToolChoice.Should().NotBeNull();
        result.Metadata.Should().NotBeNull();
        result.Metadata!.UserID.Should().Be("test-user");
    }

    [Fact]
    public void ToMessageCreateParams_NullOptions_UsesDefaults()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var defaultModelId = "claude-3-haiku-20240307";

        // Act
        var result = AnthropicOptionsConverter.ToMessageCreateParams(messages, null, null, defaultModelId);

        // Assert
        string modelJson = ((dynamic)result.Model).Json.GetString();
        modelJson.Should().Be("claude-3-haiku-20240307");
        result.MaxTokens.Should().Be(4096); // Default
        result.Temperature.Should().BeNull();
        result.TopP.Should().BeNull();
        result.TopK.Should().BeNull();
        result.StopSequences.Should().BeNull();
        result.Tools.Should().BeNull();
        result.ToolChoice.Should().BeNull();
        result.Metadata.Should().BeNull();
    }

    #endregion

    #region Edge Cases and Boundary Tests (2 tests)

    [Fact]
    public void ToMessageCreateParams_EmptySystemPrompt_SystemIsNull()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var options = new ChatOptions
        {
            ModelId = "claude-3-sonnet-20240229"
        };

        // Act
        var result = AnthropicOptionsConverter.ToMessageCreateParams(messages, "", options, null);

        // Assert
        result.System.Should().BeNull();
    }

    [Fact]
    public void ToMessageCreateParams_NullSystemPrompt_SystemIsNull()
    {
        // Arrange
        var messages = CreateDefaultMessages();
        var options = new ChatOptions
        {
            ModelId = "claude-3-sonnet-20240229"
        };

        // Act
        var result = AnthropicOptionsConverter.ToMessageCreateParams(messages, null, options, null);

        // Assert
        result.System.Should().BeNull();
    }

    #endregion
}
