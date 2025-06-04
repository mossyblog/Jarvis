# Confluence Documentation Sync Scripts

These scripts allow you to sync markdown documentation files to Confluence pages, automatically converting markdown to Confluence's storage format.

## Prerequisites

1. **.NET SDK** - Required for running C# scripts
2. **dotnet-script** - Will be installed automatically if not present
3. **Confluence API Token** - Generate at https://id.atlassian.com/manage-profile/security/api-tokens

## Setup

### 1. Set Environment Variables

```bash
# Linux/Mac
export CONFLUENCE_EMAIL="your.email@company.com"
export CONFLUENCE_API_TOKEN="your-api-token-here"

# Windows PowerShell
$env:CONFLUENCE_EMAIL = "your.email@company.com"
$env:CONFLUENCE_API_TOKEN = "your-api-token-here"
```

### 2. Make Scripts Executable (Linux/Mac)

```bash
chmod +x ./scripts/sync-docs-to-confluence.sh
```

## Usage

### Using PowerShell Script (Windows/Cross-platform)

```powershell
# Update an existing page
./scripts/sync-docs-to-confluence.ps1 -MarkdownFile ./docs/README.md -PageId 12345678

# Create a new child page
./scripts/sync-docs-to-confluence.ps1 -MarkdownFile ./docs/guide.md -PageId new -ParentId 12345678 -Title "User Guide"

# Create a new page in a space
./scripts/sync-docs-to-confluence.ps1 -MarkdownFile ./docs/guide.md -PageId new -Space MYSPACE -Title "User Guide"
```

### Using Bash Script (Linux/Mac)

```bash
# Update an existing page
./scripts/sync-docs-to-confluence.sh ./docs/README.md 12345678

# Create a new child page
./scripts/sync-docs-to-confluence.sh ./docs/guide.md new --parent 12345678 --title "User Guide"

# Create a new page in a space
./scripts/sync-docs-to-confluence.sh ./docs/guide.md new --space MYSPACE --title "User Guide"
```

### Using C# Script Directly

```bash
# Update existing page
dotnet script ./scripts/markdown-to-confluence.csx ./docs/README.md 12345678

# Create new child page
dotnet script ./scripts/markdown-to-confluence.csx ./docs/guide.md new --create-child 12345678 --title "User Guide"

# Create new page in space
dotnet script ./scripts/markdown-to-confluence.csx ./docs/guide.md new --space MYSPACE --title "User Guide"
```

## Features

### Markdown Conversion

The scripts automatically convert:
- **Headers** → Confluence headers
- **Code blocks** → Confluence code macros with syntax highlighting
- **Tables** → Confluence tables with proper formatting
- **Lists** → Properly formatted lists
- **Inline code** → Confluence inline code formatting
- **Links** → Confluence links

### Language Mapping

Code blocks are automatically mapped to Confluence-supported languages:
- `cs` → `csharp`
- `js` → `javascript`
- `ts` → `typescript`
- `yml` → `yaml`
- `sh` → `bash`

### Title Extraction

If no title is provided, the script will:
1. Try to extract from the first `# Header` in the markdown
2. Fall back to the filename (without extension)

## Batch Synchronization

### PowerShell

Edit the `Sync-AllDocs` function in `sync-docs-to-confluence.ps1`:

```powershell
$docMappings = @{
    "./docs/architecture/concurrency-control.md" = "115802116"
    "./docs/architecture/handler-pattern.md" = "12345678"
    "./docs/guides/snapshot-versioning.md" = "87654321"
}
```

Then run:
```powershell
. ./scripts/sync-docs-to-confluence.ps1
Sync-AllDocs
```

### Bash

Edit the `doc_mappings` in `sync-docs-to-confluence.sh`:

```bash
declare -A doc_mappings=(
    ["./docs/architecture/concurrency-control.md"]="115802116"
    ["./docs/architecture/handler-pattern.md"]="12345678"
    ["./docs/guides/snapshot-versioning.md"]="87654321"
)
```

Then run:
```bash
./scripts/sync-docs-to-confluence.sh --sync-all
```

## Finding Page IDs

To find a Confluence page ID:

1. **From URL**: The page ID is in the URL
   - Example: `https://company.atlassian.net/wiki/spaces/SPACE/pages/12345678/Page+Title`
   - Page ID: `12345678`

2. **From Page Info**: Click the "..." menu → "Page Information"
   - The page ID is shown in the information panel

3. **From API**: Use curl to get page info
   ```bash
   curl -u email@company.com:api-token \
     https://company.atlassian.net/wiki/rest/api/content?title=Page%20Title&spaceKey=SPACE
   ```

## Troubleshooting

### Authentication Errors (403)
- Verify your email and API token are correct
- Ensure you have access to the Confluence space
- Check if your account has a Confluence license

### Page Not Found (404)
- Verify the page ID is correct
- Check if the page still exists
- Ensure you're using the correct Confluence instance URL

### Version Conflicts (409)
- The page was modified by someone else
- The script automatically fetches the latest version
- If it still fails, try again

### Markdown Conversion Issues
- Check code blocks are properly fenced with ```
- Ensure special characters in code are properly escaped
- Use the debug mode to see the converted HTML

## Advanced Usage

### Custom Confluence Instance

Edit the `BaseUrl` in `markdown-to-confluence.csx`:

```csharp
var config = new
{
    Email = Environment.GetEnvironmentVariable("CONFLUENCE_EMAIL") ?? "",
    ApiToken = Environment.GetEnvironmentVariable("CONFLUENCE_API_TOKEN") ?? "",
    BaseUrl = "https://your-instance.atlassian.net/wiki", // Change this
    DebugMode = false
};
```

### Debug Mode

Enable debug mode to see the converted HTML:

```csharp
var config = new
{
    // ...
    DebugMode = true // Set to true
};
```

## Security Notes

1. **Never commit API tokens** to version control
2. Use environment variables for credentials
3. Consider using a service account for automation
4. Rotate API tokens regularly
5. Limit token permissions to only what's needed

## Examples

### Sync Architecture Documentation

```bash
# Sync all architecture docs
for file in ./docs/architecture/*.md; do
    echo "Syncing $file..."
    # You'll need to map each file to its page ID
    ./scripts/sync-docs-to-confluence.sh "$file" "PAGE_ID"
done
```

### Create Documentation Structure

```bash
# Create main documentation page
./scripts/sync-docs-to-confluence.sh ./docs/README.md new \
  --space PROJ --title "Project Documentation"

# Get the created page ID from output, then create child pages
PARENT_ID="12345678"

# Create child pages
./scripts/sync-docs-to-confluence.sh ./docs/getting-started.md new \
  --parent $PARENT_ID --title "Getting Started"

./scripts/sync-docs-to-confluence.sh ./docs/architecture.md new \
  --parent $PARENT_ID --title "Architecture"
```

## Limitations

1. **Attachments**: Images and files need to be uploaded separately
2. **Macros**: Complex Confluence macros aren't supported
3. **Cross-references**: Internal links need manual adjustment
4. **Comments**: Markdown comments are removed in conversion

## Contributing

To improve the markdown conversion:

1. Edit the conversion logic in `markdown-to-confluence.csx`
2. Add new regex patterns in `PostProcessHtml()`
3. Extend the custom renderers for specific markdown elements
4. Test with various markdown files before committing