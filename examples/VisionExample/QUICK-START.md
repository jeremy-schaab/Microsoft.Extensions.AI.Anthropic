# Vision Example - Quick Start Guide

Get started with multi-modal AI in 5 minutes!

## 1. Set Environment Variable

Choose one:

**Option A: Standard Anthropic API**
```bash
# Windows (PowerShell)
$env:ANTHROPIC_API_KEY = "sk-ant-..."

# Windows (Command Prompt)
set ANTHROPIC_API_KEY=sk-ant-...
```

**Option B: Azure Anthropic Foundry**
```bash
# Windows (PowerShell)
$env:ANTHROPIC_FOUNDRY_RESOURCE = "your-resource-name"

# Windows (Command Prompt)
set ANTHROPIC_FOUNDRY_RESOURCE=your-resource-name
```

## 2. Add Sample Images (Optional)

The example works without images, but for best results:

1. Create directories:
   ```bash
   mkdir images
   mkdir documents
   ```

2. Add any image files:
   - `images/sample.jpg` - Any photo
   - `images/diagram.png` - Any diagram
   - `documents/sample.pdf` - Any PDF

**Or use the URL example** - it will download a cat image automatically!

## 3. Run

```bash
dotnet run
```

## What You'll See

The example demonstrates 6 scenarios:

1. **Image Analysis from File** - Analyzes a local image
2. **Image Analysis from URL** - Downloads and analyzes an image from Wikipedia
3. **Multiple Images** - Compares two images
4. **Text + Image Combined** - Provides context with image
5. **PDF Analysis** - Extracts and analyzes PDF content (Beta)
6. **Streaming with Vision** - Streams image analysis response

## Output Example

```
=== Anthropic Vision & Multi-Modal Example ===

Using Standard Anthropic API

--- Example 1: Image Analysis from File ---

Sample image not found at: C:\...\images\sample.jpg
Please add a sample.jpg to the images/ directory.

--- Example 2: Image Analysis from URL ---

Analyzing image from URL: https://upload.wikimedia.org/.../Cat03.jpg

Claude's Response:
This image shows an orange and white tabby cat in a relaxed, resting position...

--- Example 3: Multiple Images Analysis ---
...
```

## Key Code Patterns

### Load and Analyze an Image
```csharp
// Load image
var imageData = await LoadImageAsync("path/to/image.jpg");

// Create message with text + image
var messages = new List<ChatMessage>
{
    new(ChatRole.User, new AIContent[]
    {
        new TextContent("What do you see?"),
        imageData
    })
};

// Get response
var response = await chatClient.GetResponseAsync(messages);
Console.WriteLine(response.Text);
```

### Supported Formats
- JPEG (.jpg, .jpeg)
- PNG (.png)
- GIF (.gif)
- WebP (.webp)
- PDF (.pdf) - requires Beta API

### Download from URL
```csharp
using var httpClient = new HttpClient();
var imageBytes = await httpClient.GetByteArrayAsync(url);
var imageData = new DataContent(imageBytes, "image/jpeg");
```

## Troubleshooting

**"ANTHROPIC_API_KEY not set"**
- Set the environment variable before running
- Restart terminal/IDE after setting

**"Sample image not found"**
- The example gracefully skips missing files
- Only Example 2 (URL) works without local files
- Add images to `images/` directory to test other examples

**"PDF analysis fails"**
- Requires Beta API access on your account
- Uses `anthropic-beta: pdfs-2024-09-25` header

**High token usage**
- Images consume tokens based on size and complexity
- Check usage with `UsageContent` in response
- Consider resizing large images

## Next Steps

1. **Add your own images** - Test with your photos/diagrams
2. **Modify prompts** - Ask different questions about images
3. **Try multi-turn** - Build conversations about images
4. **Combine with tools** - Use function calling with vision
5. **Batch processing** - Analyze multiple images efficiently

## Resources

- Full documentation: [README.md](./README.md)
- Sample setup guide: [SAMPLE-IMAGES.md](./SAMPLE-IMAGES.md)
- Anthropic Vision API: https://docs.anthropic.com/en/docs/vision
- Microsoft.Extensions.AI: https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai

## Help

If you encounter issues:

1. Check environment variables are set
2. Verify API key has necessary permissions
3. Ensure images are in correct directories
4. Check image formats are supported
5. Review error messages for specific issues

Happy coding!
