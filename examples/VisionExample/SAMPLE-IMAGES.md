# Sample Images Setup Guide

This guide helps you set up sample images and documents for the Vision Example project.

## Quick Setup

### Option 1: Use Your Own Images

1. Create directories:
```bash
mkdir images
mkdir documents
```

2. Add any images you want to analyze:
   - Copy photos from your device
   - Download images from the web
   - Use screenshots or diagrams
   - Export PDFs from documents

### Option 2: Download Sample Images

Here are suggested sample images you can download:

#### General Image Analysis (sample.jpg)
Use any photograph or image. Suggestions:
- A photo from your photo library
- A screenshot of a webpage
- An architectural photo
- A nature scene

#### Multiple Images (image1.jpg, image2.jpg)
Two related images for comparison:
- Two versions of the same scene
- Similar objects in different settings
- Before/after edits
- Two products to compare

#### Technical Diagram (diagram.png)
Any diagram or flowchart:
- Software architecture diagram
- Network topology
- Process flowchart
- UML diagram

#### Before/After Images (before.jpg, after.jpg)
Two images showing change:
- Image editing progression
- Construction progress
- UI design iterations
- Product improvements

#### PDF Document (sample.pdf)
Any PDF document:
- Research paper
- Technical specification
- Invoice or receipt
- Resume or CV

## Directory Structure

Create this structure in the VisionExample project:

```
VisionExample/
├── images/
│   ├── sample.jpg          # General image for analysis
│   ├── image1.jpg          # First comparison image
│   ├── image2.jpg          # Second comparison image
│   ├── diagram.png         # Technical diagram
│   ├── before.jpg          # Before state
│   └── after.jpg           # After state
└── documents/
    └── sample.pdf          # Sample PDF document
```

## Creating Sample Images

### Windows

```powershell
# Create directories
New-Item -ItemType Directory -Force -Path "images"
New-Item -ItemType Directory -Force -Path "documents"

# Copy files (adjust paths as needed)
Copy-Item "C:\Users\YourName\Pictures\photo.jpg" -Destination "images\sample.jpg"
```

### Linux/macOS

```bash
# Create directories
mkdir -p images documents

# Copy files (adjust paths as needed)
cp ~/Pictures/photo.jpg images/sample.jpg
```

## Download Free Sample Images

### Using PowerShell (Windows)

```powershell
# Download a sample image from Lorem Picsum
Invoke-WebRequest -Uri "https://picsum.photos/800/600" -OutFile "images\sample.jpg"

# Download another for comparison
Invoke-WebRequest -Uri "https://picsum.photos/800/600?random=1" -OutFile "images\image1.jpg"
Invoke-WebRequest -Uri "https://picsum.photos/800/600?random=2" -OutFile "images\image2.jpg"
```

### Using curl (Linux/macOS/Windows)

```bash
# Download sample images
curl -o images/sample.jpg https://picsum.photos/800/600
curl -o images/image1.jpg "https://picsum.photos/800/600?random=1"
curl -o images/image2.jpg "https://picsum.photos/800/600?random=2"
```

## Image Requirements

### Supported Formats
- JPEG (.jpg, .jpeg)
- PNG (.png)
- GIF (.gif)
- WebP (.webp)
- PDF (.pdf) - for documents only

### Size Recommendations
- **Minimum**: 200x200 pixels
- **Recommended**: 800x600 to 1920x1080 pixels
- **Maximum**: 5MB file size for optimal performance

### Quality Guidelines
- Use clear, well-lit images
- Avoid excessive compression
- Ensure text is readable (if OCR needed)
- Higher resolution = more detail but more tokens

## Creating Test Diagrams

If you need technical diagrams:

### Online Tools
- **draw.io** - https://draw.io (free diagram tool)
- **Excalidraw** - https://excalidraw.com (hand-drawn style)
- **Mermaid Live** - https://mermaid.live (code-to-diagram)
- **PlantUML** - https://plantuml.com (UML diagrams)

### Quick PowerPoint/Google Slides
1. Create a simple flowchart or diagram
2. Export as PNG or JPEG
3. Save to images/ directory

## Creating Sample PDFs

### From Text Files

**Windows (PowerShell)**
```powershell
# Create a sample text file
@"
Sample Document

This is a sample PDF document for testing the vision capabilities.

Key Points:
- PDF analysis requires Beta API access
- Multi-page documents are supported
- Text and images can be extracted
- Use the anthropic-beta header

For more information, visit the documentation.
"@ | Out-File -FilePath "sample.txt"

# Convert to PDF using Word or print-to-PDF feature
```

### From Web Pages
1. Open any webpage
2. Print to PDF (Ctrl+P, select "Save as PDF")
3. Save to documents/ directory

### Online PDF Creation
- **PDFEscape** - https://pdfescape.com (create PDFs)
- **PDF24** - https://tools.pdf24.org (PDF tools)
- **SmallPDF** - https://smallpdf.com (PDF converter)

## Verifying Setup

Run this PowerShell script to check your setup:

```powershell
# Check images directory
$imageFiles = Get-ChildItem -Path "images" -File
Write-Host "Images found: $($imageFiles.Count)"
$imageFiles | ForEach-Object { Write-Host "  - $($_.Name)" }

# Check documents directory
$docFiles = Get-ChildItem -Path "documents" -File
Write-Host "Documents found: $($docFiles.Count)"
$docFiles | ForEach-Object { Write-Host "  - $($_.Name)" }

# Check file sizes
Write-Host "`nFile sizes:"
Get-ChildItem -Path "images","documents" -File | ForEach-Object {
    $sizeMB = [math]::Round($_.Length / 1MB, 2)
    Write-Host "  $($_.Name): $sizeMB MB"
}
```

## Troubleshooting

### Files Not Found
- Verify directory structure matches example
- Check file names match exactly (case-sensitive on Linux/macOS)
- Ensure files are in the correct location
- Confirm .csproj includes file copy directive

### Format Not Supported
```xml
<!-- Add to .csproj if needed -->
<ItemGroup>
  <None Update="images\*.jpg;images\*.png;images\*.gif;images\*.webp"
        CopyToOutputDirectory="PreserveNewest" />
  <None Update="documents\*.pdf"
        CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

### File Too Large
- Resize images before using
- Use online tools to compress
- Convert to more efficient format (JPEG for photos, PNG for diagrams)

## Testing Individual Examples

You don't need all files to run the example. It will gracefully skip missing files:

- **Example 1**: Needs `images/sample.jpg`
- **Example 2**: Downloads from URL (no local file needed)
- **Example 3**: Needs `images/image1.jpg` and `images/image2.jpg`
- **Example 4**: Needs `images/diagram.png`
- **Example 5**: Needs `documents/sample.pdf`
- **Example 6**: Needs `images/before.jpg` and `images/after.jpg`

## Additional Resources

### Free Stock Photos
- **Unsplash** - https://unsplash.com
- **Pexels** - https://pexels.com
- **Pixabay** - https://pixabay.com

### Public Domain Images
- **Wikimedia Commons** - https://commons.wikimedia.org
- **Library of Congress** - https://loc.gov/pictures
- **NASA Images** - https://images.nasa.gov

### Technical Diagrams
- **Diagram Examples** - Search GitHub for sample architecture diagrams
- **Documentation Templates** - Many open-source projects have diagram examples
- **Wikipedia** - Technical articles often include diagrams (public domain)

## Next Steps

Once you have sample files:

1. Run the example: `dotnet run`
2. Observe the analysis results
3. Try your own images
4. Experiment with different prompts
5. Explore advanced multi-modal scenarios

Happy testing!
