#!/usr/bin/env dotnet-script
#r "nuget: Markdig, 0.33.0"
#r "nuget: Newtonsoft.Json, 13.0.3"
#r "nuget: RestSharp, 110.2.0"

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Http;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Markdig.Renderers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

// Configuration
var config = new
{
    Email = Environment.GetEnvironmentVariable("CONFLUENCE_EMAIL") ?? "",
    ApiToken = Environment.GetEnvironmentVariable("CONFLUENCE_API_TOKEN") ?? "",
    BaseUrl = "https://risksec.atlassian.net/wiki",
    DebugMode = false
};

// Check arguments
if (Args.Count < 2)
{
    Console.WriteLine("Usage: dotnet script markdown-to-confluence.csx <markdown-file> <page-id> [--create-child <parent-id>] [--title \"Page Title\"]");
    Console.WriteLine("\nOptions:");
    Console.WriteLine("  <markdown-file>              Path to the markdown file to convert");
    Console.WriteLine("  <page-id>                    Confluence page ID to update (or 'new' for creation)");
    Console.WriteLine("  --create-child <parent-id>   Create as child page under specified parent");
    Console.WriteLine("  --title \"Page Title\"         Specify page title (required for new pages)");
    Console.WriteLine("  --space <space-key>          Space key (required for new pages without parent)");
    Console.WriteLine("\nEnvironment variables:");
    Console.WriteLine("  CONFLUENCE_EMAIL             Your Atlassian email");
    Console.WriteLine("  CONFLUENCE_API_TOKEN         Your Atlassian API token");
    Console.WriteLine("\nExamples:");
    Console.WriteLine("  # Update existing page");
    Console.WriteLine("  dotnet script markdown-to-confluence.csx ./docs/README.md 12345678");
    Console.WriteLine("\n  # Create new child page");
    Console.WriteLine("  dotnet script markdown-to-confluence.csx ./docs/guide.md new --create-child 12345678 --title \"User Guide\"");
    Console.WriteLine("\n  # Create new page in space");
    Console.WriteLine("  dotnet script markdown-to-confluence.csx ./docs/guide.md new --space MYSPACE --title \"User Guide\"");
    Environment.Exit(1);
}

// Parse arguments
var markdownFile = Args[0];
var pageId = Args[1];
var isNewPage = pageId.ToLower() == "new";
var parentId = "";
var pageTitle = "";
var spaceKey = "";

for (int i = 2; i < Args.Count; i++)
{
    if (Args[i] == "--create-child" && i + 1 < Args.Count)
    {
        parentId = Args[++i];
    }
    else if (Args[i] == "--title" && i + 1 < Args.Count)
    {
        pageTitle = Args[++i];
    }
    else if (Args[i] == "--space" && i + 1 < Args.Count)
    {
        spaceKey = Args[++i];
    }
}

// Validate inputs
if (!File.Exists(markdownFile))
{
    Console.WriteLine($"Error: Markdown file not found: {markdownFile}");
    Environment.Exit(1);
}

if (string.IsNullOrEmpty(config.Email) || string.IsNullOrEmpty(config.ApiToken))
{
    Console.WriteLine("Error: Please set CONFLUENCE_EMAIL and CONFLUENCE_API_TOKEN environment variables");
    Console.WriteLine("You can create an API token at: https://id.atlassian.com/manage-profile/security/api-tokens");
    Environment.Exit(1);
}

if (isNewPage && string.IsNullOrEmpty(pageTitle))
{
    Console.WriteLine("Error: --title is required when creating a new page");
    Environment.Exit(1);
}

if (isNewPage && string.IsNullOrEmpty(parentId) && string.IsNullOrEmpty(spaceKey))
{
    Console.WriteLine("Error: Either --create-child or --space is required when creating a new page");
    Environment.Exit(1);
}

// Custom Confluence HTML renderer
public class ConfluenceHtmlRenderer : TextRendererBase<HtmlRenderer>
{
    private int _listLevel = 0;
    private bool _inTable = false;

    public ConfluenceHtmlRenderer(TextWriter writer) : base(writer)
    {
        ObjectRenderers.Add(new CodeBlockRenderer());
        ObjectRenderers.Add(new TableRenderer());
    }

    private class CodeBlockRenderer : HtmlObjectRenderer<CodeBlock>
    {
        protected override void Write(HtmlRenderer renderer, CodeBlock obj)
        {
            var writer = renderer.Writer;
            var language = obj.Info ?? "text";
            
            // Map common language aliases
            language = language.ToLower() switch
            {
                "cs" => "csharp",
                "js" => "javascript",
                "ts" => "typescript",
                "yml" => "yaml",
                "sh" => "bash",
                _ => language
            };

            writer.WriteLine($"<ac:structured-macro ac:name=\"code\" ac:schema-version=\"1\">");
            writer.WriteLine($"  <ac:parameter ac:name=\"language\">{language}</ac:parameter>");
            writer.WriteLine("  <ac:plain-text-body><![CDATA[");
            
            var lines = obj.Lines.Lines;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                writer.WriteLine(line.Slice.ToString());
            }
            
            writer.WriteLine("  ]]></ac:plain-text-body>");
            writer.WriteLine("</ac:structured-macro>");
        }
    }

    private class TableRenderer : HtmlObjectRenderer<Table>
    {
        protected override void Write(HtmlRenderer renderer, Table table)
        {
            var writer = renderer.Writer;
            writer.WriteLine("<table data-layout=\"default\">");
            writer.WriteLine("  <tbody>");
            
            var isFirstRow = true;
            foreach (var rowObj in table)
            {
                if (rowObj is TableRow row)
                {
                    writer.WriteLine("    <tr>");
                    for (int i = 0; i < row.Count; i++)
                    {
                        if (row[i] is TableCell cell)
                        {
                            var tag = (row.IsHeader || isFirstRow) ? "th" : "td";
                            writer.Write($"      <{tag}><p>");
                            renderer.WriteChildren(cell);
                            writer.WriteLine($"</p></{tag}>");
                        }
                    }
                    writer.WriteLine("    </tr>");
                    isFirstRow = false;
                }
            }
            
            writer.WriteLine("  </tbody>");
            writer.WriteLine("</table>");
        }
    }
}

// Convert Markdown to Confluence HTML
string ConvertMarkdownToConfluence(string markdown)
{
    // Pre-process markdown to handle special cases
    markdown = PreProcessMarkdown(markdown);
    
    var pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();
    
    var document = Markdown.Parse(markdown, pipeline);
    
    using (var writer = new StringWriter())
    {
        var renderer = new HtmlRenderer(writer);
        renderer.Render(document);
        var html = writer.ToString();
        
        // Post-process HTML for Confluence
        html = PostProcessHtml(html);
        
        return html;
    }
}

string PreProcessMarkdown(string markdown)
{
    // Handle inline code blocks that might contain special characters
    markdown = Regex.Replace(markdown, @"`([^`]+)`", m => 
    {
        var code = m.Groups[1].Value;
        // Escape HTML entities in inline code
        code = System.Net.WebUtility.HtmlEncode(code);
        return $"<code>{code}</code>";
    });
    
    return markdown;
}

string PostProcessHtml(string html)
{
    // Convert fenced code blocks to Confluence format
    html = Regex.Replace(html, @"<pre><code class=""language-(\w+)"">(.*?)</code></pre>", m =>
    {
        var language = m.Groups[1].Value;
        var code = m.Groups[2].Value;
        
        // Decode HTML entities in code content
        code = System.Net.WebUtility.HtmlDecode(code);
        
        return $@"<ac:structured-macro ac:name=""code"" ac:schema-version=""1"">
  <ac:parameter ac:name=""language"">{language}</ac:parameter>
  <ac:plain-text-body><![CDATA[{code}]]></ac:plain-text-body>
</ac:structured-macro>";
    }, RegexOptions.Singleline);
    
    // Convert code blocks without language
    html = Regex.Replace(html, @"<pre><code>(.*?)</code></pre>", m =>
    {
        var code = m.Groups[1].Value;
        code = System.Net.WebUtility.HtmlDecode(code);
        
        return $@"<ac:structured-macro ac:name=""code"" ac:schema-version=""1"">
  <ac:parameter ac:name=""language"">text</ac:parameter>
  <ac:plain-text-body><![CDATA[{code}]]></ac:plain-text-body>
</ac:structured-macro>";
    }, RegexOptions.Singleline);
    
    // Fix table headers - Confluence expects headers to be in <th> tags
    html = Regex.Replace(html, @"<thead>\s*<tr>(.*?)</tr>\s*</thead>", m =>
    {
        var headerRow = m.Groups[1].Value;
        headerRow = headerRow.Replace("<td>", "<th><p>").Replace("</td>", "</p></th>");
        return $"<tbody>\n    <tr>{headerRow}</tr>";
    }, RegexOptions.Singleline);
    
    // Wrap table cells in <p> tags as Confluence expects
    html = Regex.Replace(html, @"<td>([^<].*?)</td>", "<td><p>$1</p></td>");
    html = Regex.Replace(html, @"<th>([^<].*?)</th>", "<th><p>$1</p></th>");
    
    // Ensure tables have proper Confluence attributes
    html = html.Replace("<table>", "<table data-layout=\"default\">");
    
    // Remove empty paragraphs
    html = Regex.Replace(html, @"<p>\s*</p>", "");
    
    // Confluence doesn't like <br /> tags, use empty paragraphs instead
    html = html.Replace("<br />", "</p><p>");
    
    return html;
}

// API functions
async Task<JObject> GetPageInfo(string pageId)
{
    var client = new RestClient(config.BaseUrl);
    var request = new RestRequest($"/rest/api/content/{pageId}?expand=version,space", Method.Get);
    
    var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{config.Email}:{config.ApiToken}"));
    request.AddHeader("Authorization", $"Basic {authValue}");
    request.AddHeader("Accept", "application/json");
    
    var response = await client.ExecuteAsync(request);
    
    if (!response.IsSuccessful)
    {
        throw new Exception($"Failed to get page info: {response.StatusCode} - {response.Content}");
    }
    
    return JObject.Parse(response.Content);
}

async Task<string> CreatePage(string title, string content, string spaceKey, string parentId = null)
{
    var client = new RestClient(config.BaseUrl);
    var request = new RestRequest("/rest/api/content", Method.Post);
    
    var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{config.Email}:{config.ApiToken}"));
    request.AddHeader("Authorization", $"Basic {authValue}");
    request.AddHeader("Content-Type", "application/json");
    request.AddHeader("Accept", "application/json");
    
    var body = new
    {
        type = "page",
        title = title,
        space = new { key = spaceKey },
        ancestors = string.IsNullOrEmpty(parentId) ? null : new[] { new { id = parentId } },
        body = new
        {
            storage = new
            {
                value = content,
                representation = "storage"
            }
        }
    };
    
    request.AddJsonBody(body);
    
    var response = await client.ExecuteAsync(request);
    
    if (!response.IsSuccessful)
    {
        throw new Exception($"Failed to create page: {response.StatusCode} - {response.Content}");
    }
    
    var result = JObject.Parse(response.Content);
    return result["id"]?.ToString();
}

async Task UpdatePage(string pageId, string title, string content, int currentVersion)
{
    var client = new RestClient(config.BaseUrl);
    var request = new RestRequest($"/rest/api/content/{pageId}", Method.Put);
    
    var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{config.Email}:{config.ApiToken}"));
    request.AddHeader("Authorization", $"Basic {authValue}");
    request.AddHeader("Content-Type", "application/json");
    request.AddHeader("Accept", "application/json");
    
    var body = new
    {
        version = new
        {
            number = currentVersion + 1,
            message = $"Updated from markdown file: {Path.GetFileName(markdownFile)}"
        },
        title = title,
        type = "page",
        body = new
        {
            storage = new
            {
                value = content,
                representation = "storage"
            }
        }
    };
    
    request.AddJsonBody(body);
    
    var response = await client.ExecuteAsync(request);
    
    if (!response.IsSuccessful)
    {
        throw new Exception($"Failed to update page: {response.StatusCode} - {response.Content}");
    }
}

// Main execution
try
{
    Console.WriteLine($"Reading markdown file: {markdownFile}");
    var markdown = File.ReadAllText(markdownFile);
    
    // Extract title from markdown if not provided
    if (string.IsNullOrEmpty(pageTitle))
    {
        var titleMatch = Regex.Match(markdown, @"^#\s+(.+)$", RegexOptions.Multiline);
        if (titleMatch.Success)
        {
            pageTitle = titleMatch.Groups[1].Value.Trim();
        }
        else
        {
            pageTitle = Path.GetFileNameWithoutExtension(markdownFile);
        }
    }
    
    Console.WriteLine($"Converting markdown to Confluence format...");
    var confluenceHtml = ConvertMarkdownToConfluence(markdown);
    
    if (config.DebugMode)
    {
        Console.WriteLine("\n--- Converted HTML ---");
        Console.WriteLine(confluenceHtml);
        Console.WriteLine("--- End HTML ---\n");
    }
    
    if (isNewPage)
    {
        // Create new page
        Console.WriteLine($"Creating new page: {pageTitle}");
        
        if (!string.IsNullOrEmpty(parentId))
        {
            // Get parent page info to extract space key
            var parentInfo = await GetPageInfo(parentId);
            spaceKey = parentInfo["space"]["key"].ToString();
            Console.WriteLine($"Creating as child of page {parentId} in space {spaceKey}");
        }
        
        var newPageId = await CreatePage(pageTitle, confluenceHtml, spaceKey, parentId);
        
        Console.WriteLine($"✓ Page created successfully!");
        Console.WriteLine($"  Page ID: {newPageId}");
        Console.WriteLine($"  URL: {config.BaseUrl}/spaces/{spaceKey}/pages/{newPageId}");
    }
    else
    {
        // Update existing page
        Console.WriteLine($"Fetching page info for ID: {pageId}");
        var pageInfo = await GetPageInfo(pageId);
        
        var currentTitle = pageInfo["title"].ToString();
        var currentVersion = int.Parse(pageInfo["version"]["number"].ToString());
        var space = pageInfo["space"]["key"].ToString();
        
        Console.WriteLine($"Current page: {currentTitle} (version {currentVersion})");
        
        // Use provided title or keep existing
        var finalTitle = !string.IsNullOrEmpty(pageTitle) ? pageTitle : currentTitle;
        
        Console.WriteLine($"Updating page...");
        await UpdatePage(pageId, finalTitle, confluenceHtml, currentVersion);
        
        Console.WriteLine($"✓ Page updated successfully!");
        Console.WriteLine($"  URL: {config.BaseUrl}/spaces/{space}/pages/{pageId}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    if (config.DebugMode)
    {
        Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
    }
    Environment.Exit(1);
}