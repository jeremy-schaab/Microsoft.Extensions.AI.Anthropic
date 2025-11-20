# Vision & Multi-Modal Example

This example demonstrates the multi-modal capabilities of Anthropic Claude using Microsoft.Extensions.AI, including image analysis, PDF processing, and combined text+vision prompts.

## Features Demonstrated

1. **Image Analysis from File** - Load and analyze local images
2. **Image Analysis from URL** - Download and analyze remote images
3. **Multiple Images** - Compare and analyze multiple images in one prompt
4. **Text + Image Combined** - Provide context alongside images
5. **PDF Document Analysis** - Extract and analyze PDF content (Beta API)
6. **Streaming with Vision** - Stream responses for image-based prompts

## Prerequisites

### Environment Setup

Choose one of the following:

**Option 1: Standard Anthropic API**
```bash
# Windows (PowerShell)
$env:ANTHROPIC_API_KEY = "your-api-key-here"

# Windows (Command Prompt)
set ANTHROPIC_API_KEY=your-api-key-here

# Linux/macOS
export ANTHROPIC_API_KEY=your-api-key-here
```

**Option 2: Azure Anthropic Foundry**
```bash
# Windows (PowerShell)
$env:ANTHROPIC_FOUNDRY_RESOURCE = "your-resource-name"

# Windows (Command Prompt)
set ANTHROPIC_FOUNDRY_RESOURCE=your-resource-name

# Linux/macOS
export ANTHROPIC_FOUNDRY_RESOURCE=your-resource-name
```

### Sample Media Files

Create the following directory structure:

```
VisionExample/
├── images/
│   ├── sample.jpg          # Any general image for analysis
│   ├── image1.jpg          # First image for comparison
│   ├── image2.jpg          # Second image for comparison
│   ├── diagram.png         # Technical diagram or flowchart
│   ├── before.jpg          # Before state
│   └── after.jpg           # After state
└── documents/
    └── sample.pdf          # Sample PDF document
```

**NOTE**: This example will run partial demonstrations if you don't have all sample files. Add whichever files you want to test.

## Supported Formats

### Images
- **JPEG** (.jpg, .jpeg) - Most common format
- **PNG** (.png) - Lossless compression, transparency
- **GIF** (.gif) - Animations and simple graphics
- **WebP** (.webp) - Modern format with good compression

### Documents
- **PDF** (.pdf) - Requires Beta API access
  - Uses `anthropic-beta: pdfs-2024-09-25` header
  - Supports multi-page documents
  - Extracts text and visual elements

## Running the Example

```bash
# From repository root
dotnet run --project examples/VisionExample/

# Or from the example directory
cd examples/VisionExample
dotnet run
```

## Code Walkthrough

### Loading Images from File

```csharp
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
```

### Creating Vision Prompts

```csharp
var imageData = await LoadImageAsync("path/to/image.jpg");

var messages = new List<ChatMessage>
{
    new(ChatRole.User, new AIContent[]
    {
        new TextContent("What do you see in this image?"),
        imageData  // Add image as DataContent
    })
};

var response = await chatClient.CompleteAsync(messages);
```

### Multiple Images in One Prompt

```csharp
var image1 = await LoadImageAsync("image1.jpg");
var image2 = await LoadImageAsync("image2.jpg");

var messages = new List<ChatMessage>
{
    new(ChatRole.User, new AIContent[]
    {
        new TextContent("Compare these two images."),
        image1,
        image2
    })
};
```

### PDF Analysis (Beta API)

```csharp
var pdfData = await LoadPdfAsync("document.pdf");

var options = new ChatOptions
{
    AdditionalProperties = new Dictionary<string, object>
    {
        { "anthropic-beta", "pdfs-2024-09-25" }
    }
};

var messages = new List<ChatMessage>
{
    new(ChatRole.User, new AIContent[]
    {
        new TextContent("Summarize this document."),
        pdfData
    })
};

var response = await chatClient.CompleteAsync(messages, options);
```

### Downloading Images from URLs

```csharp
static async Task<DataContent> LoadImageFromUrlAsync(string url)
{
    using var httpClient = new HttpClient();
    var imageBytes = await httpClient.GetByteArrayAsync(url);

    var mimeType = url.ToLowerInvariant() switch
    {
        var u when u.Contains(".jpg") || u.Contains(".jpeg") => "image/jpeg",
        var u when u.Contains(".png") => "image/png",
        var u when u.Contains(".gif") => "image/gif",
        var u when u.Contains(".webp") => "image/webp",
        _ => "image/jpeg"
    };

    return new DataContent(imageBytes, mimeType);
}
```

## Use Cases

### 1. Image Understanding
- Object detection and identification
- Scene description and analysis
- OCR and text extraction from images
- Logo and brand recognition

### 2. Visual Comparison
- Before/after analysis
- Difference detection
- Quality assessment
- Version comparison

### 3. Document Processing
- PDF summarization
- Form data extraction
- Document classification
- Multi-page analysis

### 4. Technical Analysis
- Diagram interpretation
- Flowchart understanding
- Code screenshot analysis
- UI/UX review

### 5. Content Moderation
- Image safety checking
- Content classification
- Quality assessment
- Policy compliance

## Best Practices

### Image Quality
- **Resolution**: Higher resolution provides more detail but increases token usage
- **File Size**: Keep images under 5MB for optimal performance
- **Format**: Use JPEG for photos, PNG for diagrams/screenshots
- **Compression**: Balance quality vs. file size

### Token Usage
- Images consume significant tokens based on size and complexity
- Monitor `response.Usage.TotalTokenCount` to track costs
- Consider resizing large images before analysis
- Cache results for repeated analysis

### Error Handling
```csharp
try
{
    var imageData = await LoadImageAsync(imagePath);
    var response = await chatClient.CompleteAsync(messages);
}
catch (FileNotFoundException)
{
    Console.WriteLine("Image file not found");
}
catch (NotSupportedException ex)
{
    Console.WriteLine($"Unsupported format: {ex.Message}");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Failed to download image: {ex.Message}");
}
```

### Performance
- Load images asynchronously to avoid blocking
- Use HttpClient with connection pooling for URLs
- Consider parallel processing for multiple images
- Implement caching for frequently accessed images

## Troubleshooting

### Images Not Loading
- Verify file paths are correct (check AppContext.BaseDirectory)
- Ensure files are copied to output directory (see .csproj)
- Check file permissions
- Validate image format is supported

### PDF Analysis Fails
- Ensure Beta API access is enabled on your account
- Verify `anthropic-beta` header is set correctly
- Check PDF file is not corrupted
- Confirm file size is within limits

### High Token Usage
- Images consume tokens based on resolution and complexity
- Consider downsizing large images
- Use appropriate image formats (JPEG for photos, PNG for graphics)
- Monitor usage with `response.Usage` property

### Authentication Errors
- Verify `ANTHROPIC_API_KEY` or `ANTHROPIC_FOUNDRY_RESOURCE` is set
- Check API key has necessary permissions
- For Azure Foundry, ensure managed identity is configured
- Validate environment variables are loaded

## Sample Images Resources

If you need sample images for testing:

### Free Image Sources
- **Unsplash** - https://unsplash.com (high-quality photos)
- **Pexels** - https://pexels.com (free stock photos)
- **Wikimedia Commons** - https://commons.wikimedia.org (public domain)
- **Lorem Picsum** - https://picsum.photos (placeholder images)

### Sample PDFs
- Create simple PDFs from text documents
- Use online PDF generators
- Save web pages as PDF
- Export documents from Office applications

## Additional Resources

- [Anthropic Vision API Documentation](https://docs.anthropic.com/en/docs/vision)
- [Microsoft.Extensions.AI Documentation](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai)
- [Claude Model Capabilities](https://docs.anthropic.com/en/docs/about-claude/models)
- [DataContent API Reference](https://learn.microsoft.com/dotnet/api/microsoft.extensions.ai.datacontent)

## Next Steps

After running this example, explore:

1. **Custom Vision Processing** - Build specialized image analysis pipelines
2. **Multi-Turn Conversations** - Discuss images across multiple turns
3. **Batch Processing** - Analyze multiple images efficiently
4. **Vision + Tools** - Combine image analysis with function calling
5. **RAG with Images** - Implement retrieval-augmented generation with visual data

## License

This example is part of the Microsoft.Extensions.AI.Anthropic project and follows the same MIT license.
