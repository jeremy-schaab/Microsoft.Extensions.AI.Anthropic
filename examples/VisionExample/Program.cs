using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Anthropic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;

namespace VisionExample;

/// <summary>
/// Demonstrates multi-modal capabilities of Anthropic Claude using Microsoft.Extensions.AI
/// Includes: image analysis, PDF processing, and combined text+vision prompts
/// </summary>
internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Anthropic Vision & Multi-Modal Example ===\n");

        // Check environment variables
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        var azureResource = Environment.GetEnvironmentVariable("ANTHROPIC_FOUNDRY_RESOURCE");

        if (string.IsNullOrEmpty(apiKey) && string.IsNullOrEmpty(azureResource))
        {
            Console.WriteLine("ERROR: Neither ANTHROPIC_API_KEY nor ANTHROPIC_FOUNDRY_RESOURCE is set.");
            Console.WriteLine("\nFor Standard API:");
            Console.WriteLine("  set ANTHROPIC_API_KEY=your-api-key-here");
            Console.WriteLine("\nFor Azure Foundry:");
            Console.WriteLine("  set ANTHROPIC_FOUNDRY_RESOURCE=your-resource-name");
            return;
        }

        // Build host with DI
        var builder = Host.CreateApplicationBuilder(args);

        // Register IChatClient based on available credentials
        if (!string.IsNullOrEmpty(azureResource))
        {
            Console.WriteLine($"Using Azure Anthropic Foundry: {azureResource}\n");
            builder.Services.AddAnthropicFoundryChatClientFromEnvironment(
                resourceName: null,
                modelId: "claude-3-5-sonnet-20241022");
        }
        else
        {
            Console.WriteLine("Using Standard Anthropic API\n");
            builder.Services.AddAnthropicChatClient(
                apiKey: apiKey,
                modelId: "claude-3-5-sonnet-20241022");
        }

        var host = builder.Build();
        var chatClient = host.Services.GetRequiredService<IChatClient>();

        // Run examples
        try
        {
            await RunImageAnalysisFromFileExample(chatClient);
            await RunImageAnalysisFromUrlExample(chatClient);
            await RunMultipleImagesExample(chatClient);
            await RunTextPlusImageExample(chatClient);
            await RunPdfAnalysisExample(chatClient);
            await RunImageComparisonExample(chatClient);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nERROR: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner: {ex.InnerException.Message}");
            }
        }
    }

    /// <summary>
    /// Analyzes an image loaded from a local file
    /// </summary>
    static async Task RunImageAnalysisFromFileExample(IChatClient chatClient)
    {
        Console.WriteLine("--- Example 1: Image Analysis from File ---\n");

        // Check if sample image exists
        var imagePath = Path.Combine(AppContext.BaseDirectory, "images", "sample.jpg");
        if (!File.Exists(imagePath))
        {
            Console.WriteLine($"Sample image not found at: {imagePath}");
            Console.WriteLine("Please add a sample.jpg to the images/ directory.\n");
            return;
        }

        try
        {
            // Load image and create DataContent
            var imageData = await LoadImageAsync(imagePath);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, new AIContent[]
                {
                    new TextContent("What do you see in this image? Describe it in detail."),
                    imageData
                })
            };

            Console.WriteLine("Analyzing image...\n");
            var response = await chatClient.GetResponseAsync(messages);

            Console.WriteLine($"Claude's Response:\n{response.Text}\n");

            // Extract usage information
            var usageContent = response.Messages[0].Contents.OfType<UsageContent>().FirstOrDefault();
            if (usageContent is not null)
            {
                Console.WriteLine($"Tokens Used: {usageContent.Details.TotalTokenCount}\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}\n");
        }
    }

    /// <summary>
    /// Analyzes an image from a public URL
    /// </summary>
    static async Task RunImageAnalysisFromUrlExample(IChatClient chatClient)
    {
        Console.WriteLine("--- Example 2: Image Analysis from URL ---\n");

        // Use a publicly accessible image URL
        var imageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/3/3a/Cat03.jpg/480px-Cat03.jpg";

        try
        {
            // Download image and create DataContent
            var imageData = await LoadImageFromUrlAsync(imageUrl);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, new AIContent[]
                {
                    new TextContent("Identify the animal in this image and describe its characteristics."),
                    imageData
                })
            };

            Console.WriteLine($"Analyzing image from URL: {imageUrl}\n");
            var response = await chatClient.GetResponseAsync(messages);

            Console.WriteLine($"Claude's Response:\n{response.Text}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}\n");
        }
    }

    /// <summary>
    /// Analyzes multiple images in a single prompt
    /// </summary>
    static async Task RunMultipleImagesExample(IChatClient chatClient)
    {
        Console.WriteLine("--- Example 3: Multiple Images Analysis ---\n");

        var image1Path = Path.Combine(AppContext.BaseDirectory, "images", "image1.jpg");
        var image2Path = Path.Combine(AppContext.BaseDirectory, "images", "image2.jpg");

        if (!File.Exists(image1Path) || !File.Exists(image2Path))
        {
            Console.WriteLine("Sample images not found. Add image1.jpg and image2.jpg to images/ directory.\n");
            return;
        }

        try
        {
            var image1 = await LoadImageAsync(image1Path);
            var image2 = await LoadImageAsync(image2Path);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, new AIContent[]
                {
                    new TextContent("Compare these two images. What are the similarities and differences?"),
                    image1,
                    image2
                })
            };

            Console.WriteLine("Analyzing multiple images...\n");
            var response = await chatClient.GetResponseAsync(messages);

            Console.WriteLine($"Claude's Response:\n{response.Text}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}\n");
        }
    }

    /// <summary>
    /// Combines text context with image analysis
    /// </summary>
    static async Task RunTextPlusImageExample(IChatClient chatClient)
    {
        Console.WriteLine("--- Example 4: Text + Image Combined Prompt ---\n");

        var imagePath = Path.Combine(AppContext.BaseDirectory, "images", "diagram.png");
        if (!File.Exists(imagePath))
        {
            Console.WriteLine("Sample diagram not found. Add diagram.png to images/ directory.\n");
            return;
        }

        try
        {
            var imageData = await LoadImageAsync(imagePath);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, new AIContent[]
                {
                    new TextContent("I'm working on a software architecture project. " +
                                   "Can you analyze this diagram and explain the components and their relationships? " +
                                   "Also suggest potential improvements."),
                    imageData
                })
            };

            Console.WriteLine("Analyzing diagram with context...\n");
            var response = await chatClient.GetResponseAsync(messages);

            Console.WriteLine($"Claude's Response:\n{response.Text}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}\n");
        }
    }

    /// <summary>
    /// Analyzes a PDF document using Beta API
    /// </summary>
    static async Task RunPdfAnalysisExample(IChatClient chatClient)
    {
        Console.WriteLine("--- Example 5: PDF Document Analysis ---\n");

        var pdfPath = Path.Combine(AppContext.BaseDirectory, "documents", "sample.pdf");
        if (!File.Exists(pdfPath))
        {
            Console.WriteLine("Sample PDF not found. Add sample.pdf to documents/ directory.\n");
            return;
        }

        try
        {
            // Load PDF and create DataContent
            var pdfData = await LoadPdfAsync(pdfPath);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, new AIContent[]
                {
                    new TextContent("Please summarize this document and extract the key points."),
                    pdfData
                })
            };

            Console.WriteLine("Analyzing PDF document...\n");

            // Note: PDF analysis requires Beta API headers
            var options = new ChatOptions
            {
                AdditionalProperties = new AdditionalPropertiesDictionary
                {
                    { "anthropic-beta", "pdfs-2024-09-25" }
                }
            };

            var response = await chatClient.GetResponseAsync(messages, options);

            Console.WriteLine($"Claude's Response:\n{response.Text}\n");

            // Extract usage information
            var usageContent = response.Messages[0].Contents.OfType<UsageContent>().FirstOrDefault();
            if (usageContent is not null)
            {
                Console.WriteLine($"Tokens Used: {usageContent.Details.TotalTokenCount}\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}\n");
            Console.WriteLine("Note: PDF analysis requires Beta API access.\n");
        }
    }

    /// <summary>
    /// Demonstrates streaming with vision capabilities
    /// </summary>
    static async Task RunImageComparisonExample(IChatClient chatClient)
    {
        Console.WriteLine("--- Example 6: Streaming Image Comparison ---\n");

        var before = Path.Combine(AppContext.BaseDirectory, "images", "before.jpg");
        var after = Path.Combine(AppContext.BaseDirectory, "images", "after.jpg");

        if (!File.Exists(before) || !File.Exists(after))
        {
            Console.WriteLine("Comparison images not found. Add before.jpg and after.jpg to images/ directory.\n");
            return;
        }

        try
        {
            var beforeImage = await LoadImageAsync(before);
            var afterImage = await LoadImageAsync(after);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, new AIContent[]
                {
                    new TextContent("These are 'before' and 'after' images. Describe what changed."),
                    beforeImage,
                    afterImage
                })
            };

            Console.WriteLine("Streaming response:\n");

            await foreach (var update in chatClient.GetStreamingResponseAsync(messages))
            {
                if (update.Text is not null)
                {
                    Console.Write(update.Text);
                }
            }

            Console.WriteLine("\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}\n");
        }
    }

    #region Helper Methods

    /// <summary>
    /// Loads an image file and creates DataContent with appropriate MIME type
    /// </summary>
    static async Task<DataContent> LoadImageAsync(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var mimeType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => throw new NotSupportedException($"Unsupported image format: {extension}")
        };

        var imageBytes = await File.ReadAllBytesAsync(filePath);
        return new DataContent(imageBytes, mimeType);
    }

    /// <summary>
    /// Downloads an image from a URL and creates DataContent
    /// </summary>
    static async Task<DataContent> LoadImageFromUrlAsync(string url)
    {
        using var httpClient = new HttpClient();
        var imageBytes = await httpClient.GetByteArrayAsync(url);

        // Detect MIME type from URL or content
        var mimeType = url.ToLowerInvariant() switch
        {
            var u when u.Contains(".jpg") || u.Contains(".jpeg") => "image/jpeg",
            var u when u.Contains(".png") => "image/png",
            var u when u.Contains(".gif") => "image/gif",
            var u when u.Contains(".webp") => "image/webp",
            _ => "image/jpeg" // Default
        };

        return new DataContent(imageBytes, mimeType);
    }

    /// <summary>
    /// Loads a PDF file and creates DataContent
    /// </summary>
    static async Task<DataContent> LoadPdfAsync(string filePath)
    {
        if (!filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("File must be a PDF", nameof(filePath));
        }

        var pdfBytes = await File.ReadAllBytesAsync(filePath);
        return new DataContent(pdfBytes, "application/pdf");
    }

    /// <summary>
    /// Gets the base64 representation of image data (for debugging)
    /// </summary>
    static string GetBase64String(byte[] data)
    {
        return Convert.ToBase64String(data);
    }

    /// <summary>
    /// Validates image format support
    /// </summary>
    static bool IsSupportedImageFormat(string extension)
    {
        var supported = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        return supported.Contains(extension.ToLowerInvariant());
    }

    #endregion
}
