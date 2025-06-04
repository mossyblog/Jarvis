# AI Assistant Guide for Confluence Documentation

This guide explains how AI assistants can read and update Confluence documentation for the Jarvis project.

## Prerequisites

To access Confluence via API, you need:
1. **Email address** of the Atlassian account
2. **API token** (Personal Access Token)
3. **Space key** or space ID
4. **Page ID** (for updates) or parent page ID (for new pages)

## Authentication

Confluence uses Basic Authentication with email and API token:

```bash
# Create Base64 encoded credentials
echo -n "email@example.com:API_TOKEN" | base64

# Use in API requests
curl -H "Authorization: Basic <base64_encoded_credentials>"
```

## Common Operations

### 1. Reading a Confluence Page

```bash
# Get page content by ID
curl -X GET "https://<instance>.atlassian.net/wiki/rest/api/content/<page_id>?expand=body.storage,version" \
  -H "Authorization: Basic <credentials>" \
  -H "Accept: application/json"
```

### 2. Creating a New Page

Required JSON structure:
```json
{
  "type": "page",
  "title": "Page Title",
  "space": {"key": "SPACEKEY"},
  "ancestors": [{"id": "parent_page_id"}],  // Optional - for sub-pages
  "body": {
    "storage": {
      "value": "<p>HTML content</p>",
      "representation": "storage"
    }
  }
}
```

API call:
```bash
curl -X POST "https://<instance>.atlassian.net/wiki/rest/api/content" \
  -H "Authorization: Basic <credentials>" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d @page_content.json
```

### 3. Updating an Existing Page

Required JSON structure includes version number:
```json
{
  "version": {
    "number": 5,  // Must be current version + 1
    "message": "Update description"
  },
  "title": "Updated Title",
  "type": "page",
  "body": {
    "storage": {
      "value": "<p>Updated HTML content</p>",
      "representation": "storage"
    }
  }
}
```

API call:
```bash
curl -X PUT "https://<instance>.atlassian.net/wiki/rest/api/content/<page_id>" \
  -H "Authorization: Basic <credentials>" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d @updated_content.json
```

## Content Formatting

### HTML Storage Format

Confluence uses a specific HTML format for content storage:

```html
<!-- Headers -->
<h2>Section Title</h2>
<h3>Subsection</h3>

<!-- Paragraphs and formatting -->
<p>Regular text with <strong>bold</strong> and <code>inline code</code></p>

<!-- Lists -->
<ul>
  <li>Unordered list item</li>
</ul>
<ol>
  <li>Ordered list item</li>
</ol>

<!-- Tables -->
<table data-table-width="760" data-layout="default">
  <tbody>
    <tr>
      <th><p>Header 1</p></th>
      <th><p>Header 2</p></th>
    </tr>
    <tr>
      <td><p>Cell 1</p></td>
      <td><p>Cell 2</p></td>
    </tr>
  </tbody>
</table>
```

### Code Blocks

Use structured macros for code blocks:

```html
<ac:structured-macro ac:name="code" ac:schema-version="1">
  <ac:parameter ac:name="language">csharp</ac:parameter>
  <ac:plain-text-body><![CDATA[
// Your code here
public class Example {
    public string Name { get; set; }
}
  ]]></ac:plain-text-body>
</ac:structured-macro>
```

## Best Practices for AI Assistants

### 1. Always Verify Current State

Before updating, always fetch the current page to:
- Get the correct version number
- Understand existing structure
- Preserve important content

### 2. Use Descriptive Update Messages

```json
{
  "version": {
    "number": 5,
    "message": "Updated DataContext documentation to reflect handler-based architecture"
  }
}
```

### 3. Handle Special Characters

- Escape HTML entities: `&` → `&amp;`, `<` → `&lt;`, `>` → `&gt;`
- Use CDATA blocks for code containing special characters
- Be careful with quotes in JSON payloads

### 4. Error Handling

Common errors and solutions:

| Error | Cause | Solution |
|-------|-------|----------|
| 403 Forbidden | Invalid credentials or permissions | Verify email/token and user has Confluence access |
| 400 Bad Request - No space key | Missing space information | Add `"space": {"key": "KEY"}` to request |
| 409 Conflict | Version mismatch | Fetch current version and increment |

### 5. Structure Documentation Consistently

For Jarvis documentation, follow this structure:
1. **Purpose/Overview** - What the component does
2. **Architecture/Design** - How it works
3. **API Reference** - Methods, properties, interfaces
4. **Usage Examples** - Code samples
5. **Best Practices** - Recommendations
6. **Troubleshooting** - Common issues

## Example: Updating Jarvis Documentation

Here's a complete example of updating the DataContext page:

```bash
# 1. Get current page info
curl -X GET "https://risksec.atlassian.net/wiki/rest/api/content/65011716?expand=version" \
  -H "Authorization: Basic <credentials>" \
  -H "Accept: application/json" \
  > current_page.json

# 2. Extract current version number (e.g., 4)
# Create update with version 5

# 3. Create update payload
cat > update.json << 'EOF'
{
  "version": {
    "number": 5,
    "message": "Updated to reflect current handler-based architecture"
  },
  "title": "Application / Component / Jarvis / DataContext",
  "type": "page",
  "body": {
    "storage": {
      "value": "<h2>Purpose</h2><p>The DataContext component...</p>",
      "representation": "storage"
    }
  }
}
EOF

# 4. Send update
curl -X PUT "https://risksec.atlassian.net/wiki/rest/api/content/65011716" \
  -H "Authorization: Basic <credentials>" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d @update.json
```

## Tips for Jarvis Project Documentation

### 1. Reference Code Accurately

When documenting code, always reference the actual implementation:
```markdown
- File paths: `core.jarvis/Data/DataContext.cs`
- Line numbers: `DataContext.cs:58`
- Method signatures: Match exactly from source
```

### 2. Keep Architecture Diagrams Updated

Use Confluence's built-in diagram tools or embed diagrams that show:
- Component relationships
- Data flow
- Handler registration patterns

### 3. Document Breaking Changes

When architecture changes (like WorkingSet → Handler pattern):
- Create migration guides
- Update all related pages
- Add notices to deprecated approaches

### 4. Use Consistent Terminology

- **Handler**: Component-specific business logic class
- **DataContext**: Main entry point for data operations
- **Component**: Data model implementing IComponent
- **Entity**: Aggregate root with unique ID

## Security Considerations

1. **Never commit credentials**: API tokens should be provided at runtime
2. **Use environment variables**: Store sensitive data outside of code
3. **Validate permissions**: Ensure API tokens have appropriate access
4. **Audit changes**: All Confluence changes are logged with user info

## Troubleshooting API Access

### Cannot Access Confluence (403 Error)

1. Verify API token is valid and not expired
2. Check user has Confluence license
3. Ensure space permissions allow access
4. Try with personal space first: `~accountId`

### Page Not Found (404 Error)

1. Verify page ID is correct
2. Check if page was moved or deleted
3. Ensure you're using the right API version

### Version Conflicts (409 Error)

1. Always fetch current version before updating
2. Someone else may have updated the page
3. Merge changes manually if needed

## References

- [Atlassian REST API Documentation](https://developer.atlassian.com/cloud/confluence/rest/v1/intro/)
- [Confluence Storage Format](https://confluence.atlassian.com/doc/confluence-storage-format-790796544.html)
- [API Authentication](https://developer.atlassian.com/cloud/confluence/basic-auth-for-rest-apis/)

## Example Prompts for AI Assistants

Good prompts for Confluence operations:

1. **Reading**: "Using this token [TOKEN] and email [EMAIL], can you read the page at [URL]"
2. **Updating**: "Update the Confluence page to reflect the current implementation in @DataContext.cs"
3. **Creating**: "Create a sub-page under [PARENT_ID] documenting the snapshot versioning from @snapshot-versioning.md"

Poor prompts (missing information):
- "Update the Confluence page" (missing credentials, page ID)
- "Create documentation" (missing space, parent, content)
- "Read the wiki" (missing specific page URL/ID)

## Summary

To successfully work with Confluence as an AI assistant:
1. Always request email + API token from user
2. Fetch current state before updates
3. Use proper HTML storage format
4. Handle errors gracefully
5. Maintain consistent documentation structure
6. Reference actual code implementations
7. Include clear examples and use cases