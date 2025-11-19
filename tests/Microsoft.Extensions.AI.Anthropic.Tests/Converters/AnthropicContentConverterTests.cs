using System;
using System.Collections.Generic;
using System.Text;
using Anthropic.Models.Messages;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Anthropic;

namespace Microsoft.Extensions.AI.Anthropic.Tests.Converters;

public class AnthropicContentConverterTests
{
    [Fact]
    public void ToAnthropicContent_TextContent_CreatesTextBlock()
    {
        // Arrange
        var contents = new List<AIContent>
        {
            new TextContent("Hello, Claude!")
        };

        // Act
        var result = AnthropicContentConverter.ToAnthropicContent(contents);

        // Assert
        result.Should().HaveCount(1);
        // ContentBlockParam is a union type, so we can't cast directly
        // Instead, verify it was created successfully
        result[0].Should().NotBeNull();
    }

    [Fact]
    public void ToAnthropicContent_EmptyTextContent_IsSkipped()
    {
        // Arrange
        var contents = new List<AIContent>
        {
            new TextContent("")
        };

        // Act
        var result = AnthropicContentConverter.ToAnthropicContent(contents);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToAnthropicContent_MultipleTextContents_CreatesMultipleBlocks()
    {
        // Arrange
        var contents = new List<AIContent>
        {
            new TextContent("First message"),
            new TextContent("Second message")
        };

        // Act
        var result = AnthropicContentConverter.ToAnthropicContent(contents);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(block => block.Should().NotBeNull());
    }

    [Fact]
    public void ToAnthropicContent_ImageContent_CreatesImageBlock()
    {
        // Arrange
        var imageData = Encoding.UTF8.GetBytes("fake-image-data");
        var contents = new List<AIContent>
        {
            new DataContent(imageData, "image/png")
        };

        // Act
        var result = AnthropicContentConverter.ToAnthropicContent(contents);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().NotBeNull();
    }

    [Fact]
    public void ToAnthropicContent_UnsupportedMediaType_ThrowsNotSupportedException()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("some-data");
        var contents = new List<AIContent>
        {
            new DataContent(data, "video/mp4")
        };

        // Act & Assert
        var act = () => AnthropicContentConverter.ToAnthropicContent(contents);
        act.Should().Throw<NotSupportedException>()
            .WithMessage("Media type 'video/mp4' is not supported*");
    }

    [Fact]
    public void ToAnthropicContent_FunctionCallContent_CreatesToolUseBlock()
    {
        // Arrange
        var arguments = new Dictionary<string, object?>
        {
            ["query"] = "weather in San Francisco"
        };
        var contents = new List<AIContent>
        {
            new FunctionCallContent("call-123", "get_weather", arguments)
        };

        // Act
        var result = AnthropicContentConverter.ToAnthropicContent(contents);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().NotBeNull();
    }

    [Fact]
    public void ToAnthropicContent_FunctionResultContent_CreatesToolResultBlock()
    {
        // Arrange
        var contents = new List<AIContent>
        {
            new FunctionResultContent("call-123", "Sunny, 72°F")
        };

        // Act
        var result = AnthropicContentConverter.ToAnthropicContent(contents);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().NotBeNull();
    }

    [Fact]
    public void ToAnthropicContent_UriContent_ThrowsNotSupportedException()
    {
        // Arrange
        var contents = new List<AIContent>
        {
            new UriContent(new Uri("https://example.com/image.png"), "image/png")
        };

        // Act & Assert
        var act = () => AnthropicContentConverter.ToAnthropicContent(contents);
        act.Should().Throw<NotSupportedException>()
            .WithMessage("UriContent is not directly supported by Anthropic*");
    }

    [Fact]
    public void ToAnthropicContent_UsageContent_IsSkipped()
    {
        // Arrange
        var usageDetails = new UsageDetails
        {
            InputTokenCount = 10,
            OutputTokenCount = 20,
            TotalTokenCount = 30
        };
        var contents = new List<AIContent>
        {
            new TextContent("Hello"),
            new UsageContent(usageDetails)
        };

        // Act
        var result = AnthropicContentConverter.ToAnthropicContent(contents);

        // Assert
        // UsageContent should be skipped, only text content should remain
        result.Should().HaveCount(1);
        result[0].Should().NotBeNull();
    }

    [Fact]
    public void ToAnthropicContent_NullContents_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => AnthropicContentConverter.ToAnthropicContent(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("contents");
    }

    [Fact]
    public void FromAnthropicContent_TextBlock_CreatesTextContent()
    {
        // Arrange
        var block = new TextBlock
        {
            Text = "Hello from Claude!",
            Citations = new List<TextCitation>()
        };

        // Act
        var result = AnthropicContentConverter.FromAnthropicContent(block);

        // Assert
        result.Should().BeOfType<TextContent>();
        var textContent = result as TextContent;
        textContent!.Text.Should().Be("Hello from Claude!");
    }

    [Fact]
    public void FromAnthropicContent_ToolUseBlock_CreatesFunctionCallContent()
    {
        // Arrange
        var input = new Dictionary<string, System.Text.Json.JsonElement>
        {
            ["query"] = System.Text.Json.JsonSerializer.SerializeToElement("weather")
        };
        var block = new ToolUseBlock
        {
            ID = "tool-456",
            Name = "search",
            Input = input
        };

        // Act
        var result = AnthropicContentConverter.FromAnthropicContent(block);

        // Assert
        result.Should().BeOfType<FunctionCallContent>();
        var functionCall = result as FunctionCallContent;
        functionCall!.CallId.Should().Be("tool-456");
        functionCall.Name.Should().Be("search");
        functionCall.Arguments.Should().NotBeNull();
    }

    [Fact]
    public void FromAnthropicContent_NullBlock_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => AnthropicContentConverter.FromAnthropicContent(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("block");
    }
}
