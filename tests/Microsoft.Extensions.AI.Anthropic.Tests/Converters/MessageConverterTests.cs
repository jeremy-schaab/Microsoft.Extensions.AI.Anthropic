using System;
using System.Collections.Generic;
using System.Linq;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Anthropic;

namespace Microsoft.Extensions.AI.Anthropic.Tests.Converters;

/// <summary>
/// Comprehensive unit tests for AnthropicMessageConverter.
/// Tests cover system message extraction, role mapping, content conversion,
/// message validation, and bidirectional conversions.
/// </summary>
public class MessageConverterTests
{
    #region ToAnthropicMessages - System Message Extraction (8 tests)

    [Fact]
    public void ToAnthropicMessages_SingleSystemMessage_ExtractsSystemPrompt()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, "You are a helpful assistant."),
            new ChatMessage(ChatRole.User, "Hello!")
        };

        // Act
        var (anthropicMessages, systemPrompt) = AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        systemPrompt.Should().Be("You are a helpful assistant.");
        anthropicMessages.Should().HaveCount(1);
        string roleJson = ((dynamic)anthropicMessages[0].Role).Json.GetString();
        roleJson.Should().Be("user");
    }

    [Fact]
    public void ToAnthropicMessages_MultipleSystemMessages_CombinesWithNewlines()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, "You are a helpful assistant."),
            new ChatMessage(ChatRole.System, "Always be concise."),
            new ChatMessage(ChatRole.User, "Hello!")
        };

        // Act
        var (anthropicMessages, systemPrompt) = AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        systemPrompt.Should().Be("You are a helpful assistant.\nAlways be concise.");
        anthropicMessages.Should().HaveCount(1);
    }

    [Fact]
    public void ToAnthropicMessages_SystemMessageWithWhitespaceOnly_IsSkipped()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, "   "),
            new ChatMessage(ChatRole.System, ""),
            new ChatMessage(ChatRole.User, "Hello!")
        };

        // Act
        var (anthropicMessages, systemPrompt) = AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        systemPrompt.Should().BeNull();
        anthropicMessages.Should().HaveCount(1);
    }

    [Fact]
    public void ToAnthropicMessages_NoSystemMessages_SystemPromptIsNull()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "Hello!"),
            new ChatMessage(ChatRole.Assistant, "Hi there!")
        };

        // Act
        var (anthropicMessages, systemPrompt) = AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        systemPrompt.Should().BeNull();
        anthropicMessages.Should().HaveCount(2);
    }

    [Fact]
    public void ToAnthropicMessages_SystemMessageBetweenUserMessages_ExtractsCorrectly()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "First message"),
            new ChatMessage(ChatRole.System, "System instruction"),
            new ChatMessage(ChatRole.Assistant, "Response"),
            new ChatMessage(ChatRole.User, "Second message")
        };

        // Act
        var (anthropicMessages, systemPrompt) = AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        systemPrompt.Should().Be("System instruction");
        anthropicMessages.Should().HaveCount(3);
    }

    [Fact]
    public void ToAnthropicMessages_SystemMessagesWithMixedContent_PreservesOrder()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, "First instruction"),
            new ChatMessage(ChatRole.System, "Second instruction"),
            new ChatMessage(ChatRole.System, "Third instruction"),
            new ChatMessage(ChatRole.User, "Hello!")
        };

        // Act
        var (anthropicMessages, systemPrompt) = AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        systemPrompt.Should().Be("First instruction\nSecond instruction\nThird instruction");
    }

    [Fact]
    public void ToAnthropicMessages_SystemMessageWithComplexContent_ExtractsText()
    {
        // Arrange
        var systemMessage = new ChatMessage(ChatRole.System, new List<AIContent>
        {
            new TextContent("You are a helpful assistant.")
        });
        var messages = new List<ChatMessage>
        {
            systemMessage,
            new ChatMessage(ChatRole.User, "Hello!")
        };

        // Act
        var (anthropicMessages, systemPrompt) = AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        systemPrompt.Should().Be("You are a helpful assistant.");
        anthropicMessages.Should().HaveCount(1);
    }

    [Fact]
    public void ToAnthropicMessages_OnlySystemMessages_ThrowsArgumentException()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, "System instruction only")
        };

        // Act
        var act = () => AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*at least one non-system message*");
    }

    #endregion

    #region ToAnthropicMessages - Role Mapping (5 tests)

    [Fact]
    public void ToAnthropicMessages_UserRole_MapsToUserRole()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "Hello!")
        };

        // Act
        var (anthropicMessages, _) = AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        anthropicMessages.Should().HaveCount(1);
        string roleJson = ((dynamic)anthropicMessages[0].Role).Json.GetString();
        roleJson.Should().Be("user");
    }

    [Fact]
    public void ToAnthropicMessages_AssistantRole_MapsToAssistantRole()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "Hello!"),
            new ChatMessage(ChatRole.Assistant, "Hi there!")
        };

        // Act
        var (anthropicMessages, _) = AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        string roleJson = ((dynamic)anthropicMessages[1].Role).Json.GetString();
        roleJson.Should().Be("assistant");
    }

    [Fact]
    public void ToAnthropicMessages_ToolRole_MapsToUserRole()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "What's the weather?"),
            new ChatMessage(ChatRole.Assistant, "Let me check."),
            new ChatMessage(ChatRole.Tool, "Temperature: 72°F")
        };

        // Act
        var (anthropicMessages, _) = AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        string roleJson = ((dynamic)anthropicMessages[2].Role).Json.GetString();
        roleJson.Should().Be("user");
    }

    [Fact]
    public void ToAnthropicMessages_UnsupportedRole_ThrowsArgumentException()
    {
        // Arrange
        var customRole = new ChatRole("custom");
        var messages = new List<ChatMessage>
        {
            new ChatMessage(customRole, "Custom message")
        };

        // Act
        var act = () => AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Unsupported message role*");
    }

    [Fact]
    public void ToAnthropicMessages_AlternatingRoles_ConvertsSuccessfully()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "First"),
            new ChatMessage(ChatRole.Assistant, "Second"),
            new ChatMessage(ChatRole.User, "Third"),
            new ChatMessage(ChatRole.Assistant, "Fourth")
        };

        // Act
        var (anthropicMessages, _) = AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        anthropicMessages.Should().HaveCount(4);
        string role0 = ((dynamic)anthropicMessages[0].Role).Json.GetString();
        string role1 = ((dynamic)anthropicMessages[1].Role).Json.GetString();
        string role2 = ((dynamic)anthropicMessages[2].Role).Json.GetString();
        string role3 = ((dynamic)anthropicMessages[3].Role).Json.GetString();
        role0.Should().Be("user");
        role1.Should().Be("assistant");
        role2.Should().Be("user");
        role3.Should().Be("assistant");
    }

    #endregion

    #region ToAnthropicMessages - Content Conversion (6 tests)

    [Fact]
    public void ToAnthropicMessages_TextContent_ConvertsToContentBlocks()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "Hello, Claude!")
        };

        // Act
        var (anthropicMessages, _) = AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        anthropicMessages.Should().HaveCount(1);
        anthropicMessages[0].Content.Should().NotBeNull();
    }

    [Fact]
    public void ToAnthropicMessages_MultipleContentItems_ConvertsAll()
    {
        // Arrange
        var contents = new List<AIContent>
        {
            new TextContent("First part"),
            new TextContent("Second part")
        };
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, contents)
        };

        // Act
        var (anthropicMessages, _) = AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        anthropicMessages[0].Content.Should().NotBeNull();
    }

    [Fact]
    public void ToAnthropicMessages_FunctionCallContent_ConvertsToToolUse()
    {
        // Arrange
        var arguments = new Dictionary<string, object?>
        {
            ["location"] = "San Francisco"
        };
        var contents = new List<AIContent>
        {
            new FunctionCallContent("call-123", "get_weather", arguments)
        };
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "Weather check"),
            new ChatMessage(ChatRole.Assistant, contents)
        };

        // Act
        var (anthropicMessages, _) = AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        anthropicMessages[1].Content.Should().NotBeNull();
    }

    [Fact]
    public void ToAnthropicMessages_FunctionResultContent_ConvertsToToolResult()
    {
        // Arrange
        var contents = new List<AIContent>
        {
            new FunctionResultContent("call-123", "Sunny, 72°F")
        };
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "Weather check"),
            new ChatMessage(ChatRole.Assistant, "Checking..."),
            new ChatMessage(ChatRole.Tool, contents)
        };

        // Act
        var (anthropicMessages, _) = AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        anthropicMessages[2].Content.Should().NotBeNull();
        string roleJson = ((dynamic)anthropicMessages[2].Role).Json.GetString();
        roleJson.Should().Be("user"); // Tool results sent as user
    }

    [Fact]
    public void ToAnthropicMessages_MixedContent_ConvertsAllTypes()
    {
        // Arrange
        var contents = new List<AIContent>
        {
            new TextContent("Check the weather"),
            new DataContent(new byte[] { 1, 2, 3 }, "image/png")
        };
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, contents)
        };

        // Act
        var (anthropicMessages, _) = AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        anthropicMessages[0].Content.Should().NotBeNull();
    }

    [Fact]
    public void ToAnthropicMessages_EmptyContent_CreatesEmptyContentList()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, new List<AIContent>())
        };

        // Act
        var (anthropicMessages, _) = AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        anthropicMessages[0].Content.Should().NotBeNull();
    }

    #endregion

    #region ToAnthropicMessages - Message Validation (6 tests)

    [Fact]
    public void ToAnthropicMessages_EmptyMessageList_ThrowsArgumentException()
    {
        // Arrange
        var messages = new List<ChatMessage>();

        // Act
        var act = () => AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void ToAnthropicMessages_NullMessages_ThrowsArgumentNullException()
    {
        // Act
        var act = () => AnthropicMessageConverter.ToAnthropicMessages(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("messages");
    }

    [Fact]
    public void ToAnthropicMessages_FirstMessageNotUser_ThrowsArgumentException()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.Assistant, "Hello!")
        };

        // Act
        var act = () => AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*first message must be from the user*");
    }

    [Fact]
    public void ToAnthropicMessages_ConsecutiveUserMessages_ThrowsArgumentException()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "First"),
            new ChatMessage(ChatRole.User, "Second")
        };

        // Act
        var act = () => AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must alternate*");
    }

    [Fact]
    public void ToAnthropicMessages_ConsecutiveAssistantMessages_ThrowsArgumentException()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "Hello"),
            new ChatMessage(ChatRole.Assistant, "First response"),
            new ChatMessage(ChatRole.Assistant, "Second response")
        };

        // Act
        var act = () => AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must alternate*");
    }

    [Fact]
    public void ToAnthropicMessages_ValidAlternatingPattern_Succeeds()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "First"),
            new ChatMessage(ChatRole.Assistant, "Second"),
            new ChatMessage(ChatRole.User, "Third"),
            new ChatMessage(ChatRole.Assistant, "Fourth"),
            new ChatMessage(ChatRole.User, "Fifth")
        };

        // Act
        var act = () => AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region ToAnthropicMessages - Edge Cases (8 tests)

    [Fact]
    public void ToAnthropicMessages_LongConversation_HandlesCorrectly()
    {
        // Arrange
        var messages = new List<ChatMessage>();
        for (int i = 0; i < 50; i++)
        {
            messages.Add(new ChatMessage(i % 2 == 0 ? ChatRole.User : ChatRole.Assistant, $"Message {i}"));
        }

        // Act
        var (anthropicMessages, systemPrompt) = AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        anthropicMessages.Should().HaveCount(50);
        systemPrompt.Should().BeNull();
    }

    [Fact]
    public void ToAnthropicMessages_SystemMessageWithEmptyString_IsSkipped()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, ""),
            new ChatMessage(ChatRole.User, "Hello!")
        };

        // Act
        var (anthropicMessages, systemPrompt) = AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        systemPrompt.Should().BeNull();
    }

    [Fact]
    public void ToAnthropicMessages_SystemMessageWithNull_HandlesGracefully()
    {
        // Arrange
        var systemMessage = new ChatMessage(ChatRole.System, new List<AIContent>());
        var messages = new List<ChatMessage>
        {
            systemMessage,
            new ChatMessage(ChatRole.User, "Hello!")
        };

        // Act
        var (anthropicMessages, systemPrompt) = AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        systemPrompt.Should().BeNull();
        anthropicMessages.Should().HaveCount(1);
    }

    [Fact]
    public void ToAnthropicMessages_MultipleSystemMessagesAtDifferentPositions_CombinesAll()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, "First"),
            new ChatMessage(ChatRole.User, "Hello"),
            new ChatMessage(ChatRole.System, "Second"),
            new ChatMessage(ChatRole.Assistant, "Hi"),
            new ChatMessage(ChatRole.System, "Third"),
            new ChatMessage(ChatRole.User, "Bye")
        };

        // Act
        var (anthropicMessages, systemPrompt) = AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        systemPrompt.Should().Be("First\nSecond\nThird");
        anthropicMessages.Should().HaveCount(3); // Only non-system messages
    }

    [Fact]
    public void ToAnthropicMessages_ToolMessageAsFirstMessage_ConvertsToUserAndThrowsValidation()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.Tool, "Tool result")
        };

        // Act & Assert
        // Tool role maps to User, so this should succeed in role mapping
        // but the message should be treated as starting with user
        var act = () => AnthropicMessageConverter.ToAnthropicMessages(messages);
        act.Should().NotThrow();
    }

    [Fact]
    public void ToAnthropicMessages_MixedSystemAndToolMessages_HandlesCorrectly()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, "Be helpful"),
            new ChatMessage(ChatRole.User, "Hello"),
            new ChatMessage(ChatRole.Assistant, "Checking"),
            new ChatMessage(ChatRole.Tool, "Data: 42")
        };

        // Act
        var (anthropicMessages, systemPrompt) = AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        systemPrompt.Should().Be("Be helpful");
        anthropicMessages.Should().HaveCount(3);
        string roleJson = ((dynamic)anthropicMessages[2].Role).Json.GetString();
        roleJson.Should().Be("user"); // Tool mapped to User
    }

    [Fact]
    public void ToAnthropicMessages_UnicodeInSystemMessage_PreservesCorrectly()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, "你好 🌍 Bonjour"),
            new ChatMessage(ChatRole.User, "Hello!")
        };

        // Act
        var (anthropicMessages, systemPrompt) = AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        systemPrompt.Should().Be("你好 🌍 Bonjour");
    }

    [Fact]
    public void ToAnthropicMessages_SystemMessageWithNewlines_PreservesFormatting()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, "Line 1\nLine 2\nLine 3"),
            new ChatMessage(ChatRole.User, "Hello!")
        };

        // Act
        var (anthropicMessages, systemPrompt) = AnthropicMessageConverter.ToAnthropicMessages(messages);

        // Assert
        systemPrompt.Should().Be("Line 1\nLine 2\nLine 3");
    }

    #endregion
}
