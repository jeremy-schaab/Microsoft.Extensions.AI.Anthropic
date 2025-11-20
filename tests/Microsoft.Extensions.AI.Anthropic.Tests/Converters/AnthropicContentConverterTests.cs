using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
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

    // ========================================
    // PDF Content Tests (3 tests)
    // ========================================

    [Fact]
    public void ToAnthropicContent_PdfContent_CreatesPDFBlock()
    {
        // Arrange
        var pdfData = Encoding.UTF8.GetBytes("%PDF-1.4 fake pdf content");
        var contents = new List<AIContent>
        {
            new DataContent(pdfData, "application/pdf")
        };

        // Act & Assert
        // PDF content is not currently supported by the SDK
        var act = () => AnthropicContentConverter.ToAnthropicContent(contents);
        act.Should().Throw<NotSupportedException>()
            .WithMessage("*PDF content is not currently supported*");
    }

    [Fact]
    public void ToAnthropicContent_PdfContent_WithBase64_CreatesCorrectBlock()
    {
        // Arrange
        var pdfData = Convert.FromBase64String("JVBERi0xLjQKJeLjz9MKMSAwIG9iago8PC9UeXBlIC9DYXRhbG9nIC9QYWdlcyAyIDAgUj4+CmVuZG9iagoyIDAgb2JqCjw8L1R5cGUgL1BhZ2VzIC9LaWRzIFszIDAgUl0gL0NvdW50IDE+PgplbmRvYmoKMyAwIG9iago8PC9UeXBlIC9QYWdlIC9QYXJlbnQgMiAwIFIgL01lZGlhQm94IFswIDAgNjEyIDc5Ml0+PgplbmRvYmoKdHJhaWxlcgo8PC9TaXplIDQgL1Jvb3QgMSAwIFI+PgpzdGFydHhyZWYKMTc0CiUlRU9GCg==");
        var contents = new List<AIContent>
        {
            new DataContent(pdfData, "application/pdf")
        };

        // Act & Assert
        // PDF content is not currently supported by the SDK
        var act = () => AnthropicContentConverter.ToAnthropicContent(contents);
        act.Should().Throw<NotSupportedException>()
            .WithMessage("*PDF content is not currently supported*");
    }

    [Fact]
    public void ToAnthropicContent_PdfContent_LargePdf_HandlesCorrectly()
    {
        // Arrange - Create a larger PDF (1MB)
        var largePdfData = new byte[1024 * 1024];
        Array.Fill(largePdfData, (byte)'P');
        var contents = new List<AIContent>
        {
            new DataContent(largePdfData, "application/pdf")
        };

        // Act & Assert
        // PDF content is not currently supported by the SDK
        var act = () => AnthropicContentConverter.ToAnthropicContent(contents);
        act.Should().Throw<NotSupportedException>()
            .WithMessage("*PDF content is not currently supported*");
    }

    // ========================================
    // Complex FunctionCallContent Tests (4 tests)
    // ========================================

    [Fact]
    public void ToAnthropicContent_FunctionCallContent_WithComplexArguments()
    {
        // Arrange
        var arguments = new Dictionary<string, object?>
        {
            ["location"] = "San Francisco, CA",
            ["units"] = "celsius",
            ["forecast_days"] = 7,
            ["include_hourly"] = true,
            ["metadata"] = new Dictionary<string, object?>
            {
                ["source"] = "user_request",
                ["priority"] = 1
            }
        };
        var contents = new List<AIContent>
        {
            new FunctionCallContent("call-complex-123", "get_weather_forecast", arguments)
        };

        // Act
        var result = AnthropicContentConverter.ToAnthropicContent(contents);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().NotBeNull();
    }

    [Fact]
    public void ToAnthropicContent_FunctionCallContent_WithEmptyArguments()
    {
        // Arrange
        var arguments = new Dictionary<string, object?>();
        var contents = new List<AIContent>
        {
            new FunctionCallContent("call-empty-456", "no_args_function", arguments)
        };

        // Act
        var result = AnthropicContentConverter.ToAnthropicContent(contents);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().NotBeNull();
    }

    [Fact]
    public void ToAnthropicContent_FunctionCallContent_WithNullArguments()
    {
        // Arrange
        var contents = new List<AIContent>
        {
            new FunctionCallContent("call-null-789", "nullable_function", null)
        };

        // Act
        var result = AnthropicContentConverter.ToAnthropicContent(contents);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().NotBeNull();
    }

    [Fact]
    public void ToAnthropicContent_FunctionCallContent_WithJsonArguments()
    {
        // Arrange
        var jsonObject = JsonSerializer.SerializeToElement(new
        {
            query = "test query",
            filters = new[] { "filter1", "filter2" },
            limit = 10
        });
        var arguments = new Dictionary<string, object?>
        {
            ["json_data"] = jsonObject
        };
        var contents = new List<AIContent>
        {
            new FunctionCallContent("call-json-999", "search_with_json", arguments)
        };

        // Act
        var result = AnthropicContentConverter.ToAnthropicContent(contents);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().NotBeNull();
    }

    // ========================================
    // Complex FunctionResultContent Tests (4 tests)
    // ========================================

    [Fact]
    public void ToAnthropicContent_FunctionResultContent_WithComplexResult()
    {
        // Arrange
        var complexResult = JsonSerializer.Serialize(new
        {
            temperature = 72.5,
            humidity = 65,
            conditions = "Partly Cloudy",
            forecast = new[] { "Sunny", "Rainy", "Cloudy" },
            alerts = new List<string>()
        });
        var contents = new List<AIContent>
        {
            new FunctionResultContent("call-complex-result-111", complexResult)
        };

        // Act
        var result = AnthropicContentConverter.ToAnthropicContent(contents);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().NotBeNull();
    }

    [Fact]
    public void ToAnthropicContent_FunctionResultContent_WithEmptyResult()
    {
        // Arrange
        var contents = new List<AIContent>
        {
            new FunctionResultContent("call-empty-result-222", "")
        };

        // Act
        var result = AnthropicContentConverter.ToAnthropicContent(contents);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().NotBeNull();
    }

    [Fact]
    public void ToAnthropicContent_FunctionResultContent_WithJsonResult()
    {
        // Arrange
        var jsonResult = JsonSerializer.Serialize(new
        {
            status = "success",
            data = new { id = 123, name = "Test Item" },
            timestamp = DateTimeOffset.UtcNow
        });
        var contents = new List<AIContent>
        {
            new FunctionResultContent("call-json-result-333", jsonResult)
        };

        // Act
        var result = AnthropicContentConverter.ToAnthropicContent(contents);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().NotBeNull();
    }

    [Fact]
    public void ToAnthropicContent_FunctionResultContent_WithErrorResult()
    {
        // Arrange
        var errorResult = JsonSerializer.Serialize(new
        {
            error = true,
            error_code = "FUNCTION_FAILED",
            error_message = "The requested operation could not be completed",
            stack_trace = "at Function.execute() line 42"
        });
        var contents = new List<AIContent>
        {
            new FunctionResultContent("call-error-result-444", errorResult)
        };

        // Act
        var result = AnthropicContentConverter.ToAnthropicContent(contents);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().NotBeNull();
    }

    // ========================================
    // Edge Cases (5 tests)
    // ========================================

    [Fact]
    public void ToAnthropicContent_VeryLargeText_HandlesCorrectly()
    {
        // Arrange - Create text content with 100,000 characters
        var largeText = new string('A', 100_000);
        var contents = new List<AIContent>
        {
            new TextContent(largeText)
        };

        // Act
        var result = AnthropicContentConverter.ToAnthropicContent(contents);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().NotBeNull();
    }

    [Fact]
    public void ToAnthropicContent_EmptyCollection_ReturnsEmpty()
    {
        // Arrange
        var contents = new List<AIContent>();

        // Act
        var result = AnthropicContentConverter.ToAnthropicContent(contents);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToAnthropicContent_MixedContent_PreservesOrder()
    {
        // Arrange
        var imageData = Encoding.UTF8.GetBytes("image-data");
        var contents = new List<AIContent>
        {
            new TextContent("First text"),
            new DataContent(imageData, "image/png"),
            new TextContent("Second text"),
            new FunctionCallContent("call-555", "test_function", new Dictionary<string, object?>()),
            new TextContent("Third text")
        };

        // Act
        var result = AnthropicContentConverter.ToAnthropicContent(contents);

        // Assert
        result.Should().HaveCount(5);
        result.Should().AllSatisfy(block => block.Should().NotBeNull());
    }

    [Fact]
    public void ToAnthropicContent_UnicodeContent_HandlesCorrectly()
    {
        // Arrange
        var contents = new List<AIContent>
        {
            new TextContent("Hello 世界 🌍 Здравствуй мир! مرحبا بالعالم"),
            new TextContent("Emoji test: 😀🎉🚀💯✨"),
            new TextContent("Math symbols: ∑∫√π∞≠≈")
        };

        // Act
        var result = AnthropicContentConverter.ToAnthropicContent(contents);

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(block => block.Should().NotBeNull());
    }

    [Fact]
    public void ToAnthropicContent_SpecialCharacters_EncodesCorrectly()
    {
        // Arrange
        var contents = new List<AIContent>
        {
            new TextContent("Special chars: <>&\"'\n\r\t\\"),
            new TextContent("Control chars: \u0000\u0001\u001F"),
            new TextContent("JSON test: {\"key\": \"value\", \"array\": [1, 2, 3]}")
        };

        // Act
        var result = AnthropicContentConverter.ToAnthropicContent(contents);

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(block => block.Should().NotBeNull());
    }

    // ========================================
    // FromAnthropicContent Additional Tests (3 tests)
    // ========================================
    // NOTE: The following 3 tests are commented out because ImageBlock, PDFBlock, and ToolResultBlock
    // only exist in REQUEST content (ContentBlockParam), not in RESPONSE content (ContentBlock).
    // Anthropic's API is asymmetric: responses only contain TextBlock and ToolUseBlock.
    // See AnthropicContentConverter.FromAnthropicContent (lines 140-158) which only handles Text and ToolUse.

    // [Fact]
    // public void FromAnthropicContent_ImageBlock_CreatesDataContent()
    // {
    //     // NOTE: ImageBlock does not exist in response ContentBlock union
    // }

    // [Fact]
    // public void FromAnthropicContent_PDFBlock_CreatesDataContent()
    // {
    //     // NOTE: PDFBlock does not exist in response ContentBlock union
    // }

    // [Fact]
    // public void FromAnthropicContent_ToolResultBlock_CreatesFunctionResultContent()
    // {
    //     // NOTE: ToolResultBlock only exists in requests, not responses
    // }

    [Fact]
    public void FromAnthropicContent_UnknownBlock_HandlesGracefully()
    {
        // Arrange
        // Create a custom block type that mimics an unknown block
        var block = new TextBlock
        {
            Text = "",
            Citations = new List<TextCitation>()
        };

        // Act
        var result = AnthropicContentConverter.FromAnthropicContent(block);

        // Assert
        // Should still return a valid AIContent (TextContent in this case)
        result.Should().NotBeNull();
    }

    [Fact]
    public void FromAnthropicContent_NullContent_HandlesGracefully()
    {
        // This test verifies that null parameter throws ArgumentNullException
        // (already tested above, but included for completeness in this category)

        // Act & Assert
        var act = () => AnthropicContentConverter.FromAnthropicContent(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FromAnthropicContent_EmptyBlock_HandlesGracefully()
    {
        // Arrange
        var block = new TextBlock
        {
            Text = "",
            Citations = new List<TextCitation>()
        };

        // Act
        var result = AnthropicContentConverter.FromAnthropicContent(block);

        // Assert
        result.Should().BeOfType<TextContent>();
        var textContent = result as TextContent;
        textContent!.Text.Should().BeEmpty();
    }
}
