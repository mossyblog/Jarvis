# Component Mirror Generator

## Overview

The Component Mirror Generator is a sophisticated .NET tool that uses Roslyn to analyze C# component classes in the Jarvis ECS framework and automatically generate TypeScript interfaces with full metadata. This tool bridges the gap between the backend C# component definitions and the frontend TypeScript types, ensuring type safety and consistency across the full stack.

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     Component Mirror Generator                          │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐      │
│  │   C# Source     │──▶│  Roslyn Parser  │──▶│  TS Generator   │      │
│  │   Analysis      │   │                 │   │                 │      │
│  └─────────────────┘   └─────────────────┘   └─────────────────┘      │
│           │                        │                        │         │
│           ▼                        ▼                        ▼         │
│  ┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐      │
│  │   Component     │   │   Metadata      │   │  TypeScript     │      │
│  │   Discovery     │   │   Extraction    │   │  Interface      │      │
│  └─────────────────┘   └─────────────────┘   └─────────────────┘      │
│                                                         │               │
│                                                         ▼               │
│                                               ┌─────────────────┐      │
│                                               │   API Endpoint  │      │
│                                               │   Discovery     │      │
│                                               └─────────────────┘      │
└─────────────────────────────────────────────────────────────────────────┘
```

## Core Implementation

### 1. Component Discovery Engine

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.Json;

namespace core.jarvis.tools.ComponentMirrorGenerator;

/// <summary>
/// Discovers and analyzes C# components that implement IComponent interface
/// </summary>
public class ComponentDiscoveryEngine
{
    private readonly ILogger<ComponentDiscoveryEngine> _logger;
    private readonly ComponentAnalyzer _analyzer;
    
    public ComponentDiscoveryEngine(ILogger<ComponentDiscoveryEngine> logger)
    {
        _logger = logger;
        _analyzer = new ComponentAnalyzer();
    }

    /// <summary>
    /// Discovers all components in the specified projects
    /// </summary>
    public async Task<List<ComponentMetadata>> DiscoverComponents(params string[] projectPaths)
    {
        var components = new List<ComponentMetadata>();
        
        foreach (var projectPath in projectPaths)
        {
            _logger.LogInformation("Analyzing project: {ProjectPath}", projectPath);
            
            var workspace = MSBuildWorkspace.Create();
            var project = await workspace.OpenProjectAsync(projectPath);
            var compilation = await project.GetCompilationAsync();
            
            if (compilation == null)
            {
                _logger.LogWarning("Failed to compile project: {ProjectPath}", projectPath);
                continue;
            }
            
            var componentSymbols = FindComponentTypes(compilation);
            
            foreach (var symbol in componentSymbols)
            {
                var metadata = await _analyzer.AnalyzeComponent(symbol, compilation);
                if (metadata != null)
                {
                    components.Add(metadata);
                    _logger.LogDebug("Discovered component: {ComponentName}", metadata.Name);
                }
            }
        }
        
        return components;
    }

    /// <summary>
    /// Finds all types that implement IComponent interface
    /// </summary>
    private IEnumerable<INamedTypeSymbol> FindComponentTypes(Compilation compilation)
    {
        var componentInterface = compilation.GetTypeByMetadataName("core.jarvis.Data.IComponent");
        if (componentInterface == null)
        {
            _logger.LogWarning("IComponent interface not found in compilation");
            yield break;
        }

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = await syntaxTree.GetRootAsync();
            
            var classDeclarations = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Concat(root.DescendantNodes().OfType<RecordDeclarationSyntax>());

            foreach (var declaration in classDeclarations)
            {
                var symbol = semanticModel.GetDeclaredSymbol(declaration) as INamedTypeSymbol;
                if (symbol?.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, componentInterface)) == true)
                {
                    yield return symbol;
                }
            }
        }
    }
}
```

### 2. Component Analyzer

```csharp
/// <summary>
/// Analyzes individual component types and extracts metadata
/// </summary>
public class ComponentAnalyzer
{
    /// <summary>
    /// Analyzes a component type and extracts comprehensive metadata
    /// </summary>
    public async Task<ComponentMetadata?> AnalyzeComponent(INamedTypeSymbol componentSymbol, Compilation compilation)
    {
        var metadata = new ComponentMetadata
        {
            Name = componentSymbol.Name,
            FullName = componentSymbol.ToDisplayString(),
            Namespace = componentSymbol.ContainingNamespace.ToDisplayString(),
            IsVersioned = ImplementsInterface(componentSymbol, "core.jarvis.Data.IVersionedComponent"),
            Documentation = ExtractDocumentation(componentSymbol),
            Properties = await AnalyzeProperties(componentSymbol),
            Attributes = ExtractAttributes(componentSymbol),
            TableName = GenerateTableName(componentSymbol.Name),
            ApiEndpoints = GenerateApiEndpoints(componentSymbol.Name)
        };

        return metadata;
    }

    /// <summary>
    /// Analyzes all properties of a component
    /// </summary>
    private async Task<List<PropertyMetadata>> AnalyzeProperties(INamedTypeSymbol componentSymbol)
    {
        var properties = new List<PropertyMetadata>();
        
        foreach (var member in componentSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            // Skip inherited IComponent properties unless we want to include them
            if (IsInheritedProperty(member))
                continue;

            var propertyMetadata = new PropertyMetadata
            {
                Name = member.Name,
                TypeName = member.Type.ToDisplayString(),
                TypeScriptType = MapToTypeScriptType(member.Type),
                DatabaseColumnName = ConvertToSnakeCase(member.Name),
                IsNullable = member.Type.CanBeReferencedByName && member.Type.NullableAnnotation == NullableAnnotation.Annotated,
                IsRequired = HasRequiredAttribute(member),
                DefaultValue = ExtractDefaultValue(member),
                Documentation = ExtractDocumentation(member),
                ValidationRules = ExtractValidationRules(member),
                Attributes = ExtractAttributes(member)
            };

            properties.Add(propertyMetadata);
        }

        return properties;
    }

    /// <summary>
    /// Maps C# types to TypeScript types
    /// </summary>
    private string MapToTypeScriptType(ITypeSymbol type)
    {
        var typeString = type.ToDisplayString();
        
        return typeString switch
        {
            "string" => "string",
            "int" => "number",
            "long" => "number",
            "decimal" => "number",
            "double" => "number",
            "float" => "number",
            "bool" => "boolean",
            "System.DateTime" => "string", // ISO 8601
            "System.Guid" => "string",
            "byte[]" => "string", // Base64
            _ when type.TypeKind == TypeKind.Enum => ExtractEnumValues(type),
            _ when typeString.StartsWith("System.Collections.Generic.List<") => 
                $"{MapToTypeScriptType(((INamedTypeSymbol)type).TypeArguments[0])}[]",
            _ when typeString.StartsWith("System.Collections.Generic.Dictionary<") =>
                $"Record<{MapToTypeScriptType(((INamedTypeSymbol)type).TypeArguments[0])}, {MapToTypeScriptType(((INamedTypeSymbol)type).TypeArguments[1])}>",
            _ when type.NullableAnnotation == NullableAnnotation.Annotated => 
                $"{MapToTypeScriptType(((INamedTypeSymbol)type).TypeArguments[0])} | null",
            _ => "any" // Fallback for complex types
        };
    }

    /// <summary>
    /// Extracts validation rules from attributes
    /// </summary>
    private List<ValidationRule> ExtractValidationRules(IPropertySymbol property)
    {
        var rules = new List<ValidationRule>();
        
        foreach (var attribute in property.GetAttributes())
        {
            var rule = attribute.AttributeClass?.Name switch
            {
                "RequiredAttribute" => new ValidationRule { Type = "required", Message = GetAttributeMessage(attribute) },
                "MinLengthAttribute" => new ValidationRule { Type = "minLength", Value = GetAttributeValue(attribute, 0), Message = GetAttributeMessage(attribute) },
                "MaxLengthAttribute" => new ValidationRule { Type = "maxLength", Value = GetAttributeValue(attribute, 0), Message = GetAttributeMessage(attribute) },
                "RangeAttribute" => new ValidationRule { Type = "range", Value = new { Min = GetAttributeValue(attribute, 0), Max = GetAttributeValue(attribute, 1) }, Message = GetAttributeMessage(attribute) },
                "EmailAddressAttribute" => new ValidationRule { Type = "email", Message = GetAttributeMessage(attribute) },
                "UrlAttribute" => new ValidationRule { Type = "url", Message = GetAttributeMessage(attribute) },
                "RegularExpressionAttribute" => new ValidationRule { Type = "pattern", Value = GetAttributeValue(attribute, 0), Message = GetAttributeMessage(attribute) },
                _ => null
            };
            
            if (rule != null)
                rules.Add(rule);
        }
        
        return rules;
    }

    /// <summary>
    /// Generates API endpoints based on component naming conventions
    /// </summary>
    private List<ApiEndpoint> GenerateApiEndpoints(string componentName)
    {
        var baseName = componentName.Replace("Component", "").ToLowerInvariant();
        var pluralName = MakePlural(baseName);
        
        return new List<ApiEndpoint>
        {
            new() { Method = "GET", Path = $"/api/{pluralName}", Description = $"Get all {pluralName}" },
            new() { Method = "GET", Path = $"/api/{pluralName}/{{id}}", Description = $"Get {baseName} by ID" },
            new() { Method = "POST", Path = $"/api/{pluralName}", Description = $"Create new {baseName}" },
            new() { Method = "PUT", Path = $"/api/{pluralName}/{{id}}", Description = $"Update {baseName}" },
            new() { Method = "DELETE", Path = $"/api/{pluralName}/{{id}}", Description = $"Delete {baseName}" },
            new() { Method = "GET", Path = $"/api/{pluralName}/entity/{{entityId}}", Description = $"Get {baseName} by entity ID" }
        };
    }
}
```

### 3. TypeScript Generator

```csharp
/// <summary>
/// Generates TypeScript interfaces and types from component metadata
/// </summary>
public class TypeScriptGenerator
{
    private readonly StringBuilder _output;
    private readonly TypeScriptGeneratorOptions _options;

    public TypeScriptGenerator(TypeScriptGeneratorOptions options)
    {
        _output = new StringBuilder();
        _options = options;
    }

    /// <summary>
    /// Generates TypeScript definitions for all components
    /// </summary>
    public async Task<string> GenerateTypeScript(List<ComponentMetadata> components)
    {
        WriteHeader();
        WriteImports();
        
        foreach (var component in components.OrderBy(c => c.Name))
        {
            WriteComponentInterface(component);
            WriteComponentValidation(component);
            WriteApiClient(component);
            _output.AppendLine();
        }
        
        WriteExports(components);
        WriteUtilityTypes();
        
        return _output.ToString();
    }

    /// <summary>
    /// Writes the TypeScript interface for a component
    /// </summary>
    private void WriteComponentInterface(ComponentMetadata component)
    {
        WriteDocumentation(component.Documentation);
        _output.AppendLine($"export interface {component.Name} {{");
        
        foreach (var property in component.Properties)
        {
            WritePropertyDocumentation(property);
            var optional = property.IsNullable || !property.IsRequired ? "?" : "";
            _output.AppendLine($"  {ToCamelCase(property.Name)}{optional}: {property.TypeScriptType};");
        }
        
        _output.AppendLine("}");
        _output.AppendLine();
        
        // Generate database component interface
        WriteDbComponentInterface(component);
    }

    /// <summary>
    /// Writes the database storage interface (ECS component format)
    /// </summary>
    private void WriteDbComponentInterface(ComponentMetadata component)
    {
        var dbInterfaceName = $"{component.Name}DbComponent";
        
        WriteDocumentation($"Database storage interface for {component.Name}");
        _output.AppendLine($"export interface {dbInterfaceName} extends IComponent {{");
        
        foreach (var property in component.Properties)
        {
            if (IsBaseComponentProperty(property.Name))
                continue;
                
            var dbType = MapToDbType(property);
            var optional = property.IsNullable ? "?" : "";
            _output.AppendLine($"  {property.DatabaseColumnName}{optional}: {dbType};");
        }
        
        _output.AppendLine("}");
        _output.AppendLine();
    }

    /// <summary>
    /// Generates validation schema based on C# attributes
    /// </summary>
    private void WriteComponentValidation(ComponentMetadata component)
    {
        _output.AppendLine($"export const {component.Name}Validation = {{");
        
        foreach (var property in component.Properties.Where(p => p.ValidationRules.Any()))
        {
            _output.AppendLine($"  {ToCamelCase(property.Name)}: [");
            
            foreach (var rule in property.ValidationRules)
            {
                var ruleJson = JsonSerializer.Serialize(rule, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                _output.AppendLine($"    {ruleJson},");
            }
            
            _output.AppendLine("  ],");
        }
        
        _output.AppendLine("};");
        _output.AppendLine();
    }

    /// <summary>
    /// Generates API client functions for the component
    /// </summary>
    private void WriteApiClient(ComponentMetadata component)
    {
        var clientName = $"{component.Name}Api";
        var interfaceName = component.Name;
        
        _output.AppendLine($"export const {clientName} = {{");
        
        foreach (var endpoint in component.ApiEndpoints)
        {
            WriteApiMethod(endpoint, interfaceName);
        }
        
        _output.AppendLine("};");
        _output.AppendLine();
    }

    /// <summary>
    /// Writes an individual API method
    /// </summary>
    private void WriteApiMethod(ApiEndpoint endpoint, string interfaceName)
    {
        var methodName = GenerateMethodName(endpoint);
        var returnType = GenerateReturnType(endpoint, interfaceName);
        var parameters = GenerateParameters(endpoint, interfaceName);
        
        _output.AppendLine($"  /**");
        _output.AppendLine($"   * {endpoint.Description}");
        _output.AppendLine($"   */");
        _output.AppendLine($"  async {methodName}({parameters}): Promise<{returnType}> {{");
        _output.AppendLine($"    const response = await fetch('{endpoint.Path}', {{");
        _output.AppendLine($"      method: '{endpoint.Method}',");
        
        if (endpoint.Method != "GET" && endpoint.Method != "DELETE")
        {
            _output.AppendLine($"      headers: {{ 'Content-Type': 'application/json' }},");
            _output.AppendLine($"      body: JSON.stringify(data),");
        }
        
        _output.AppendLine($"    }});");
        _output.AppendLine($"    ");
        _output.AppendLine($"    if (!response.ok) {{");
        _output.AppendLine($"      throw new Error(`HTTP error! status: ${{response.status}}`);");
        _output.AppendLine($"    }}");
        _output.AppendLine($"    ");
        _output.AppendLine($"    return await response.json();");
        _output.AppendLine($"  }},");
        _output.AppendLine();
    }
}
```

### 4. Metadata Models

```csharp
/// <summary>
/// Comprehensive metadata for a component
/// </summary>
public class ComponentMetadata
{
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public bool IsVersioned { get; set; }
    public string? Documentation { get; set; }
    public string TableName { get; set; } = string.Empty;
    public List<PropertyMetadata> Properties { get; set; } = new();
    public List<AttributeMetadata> Attributes { get; set; } = new();
    public List<ApiEndpoint> ApiEndpoints { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Metadata for component properties
/// </summary>
public class PropertyMetadata
{
    public string Name { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string TypeScriptType { get; set; } = string.Empty;
    public string DatabaseColumnName { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public string? Documentation { get; set; }
    public List<ValidationRule> ValidationRules { get; set; } = new();
    public List<AttributeMetadata> Attributes { get; set; } = new();
}

/// <summary>
/// Validation rule extracted from C# attributes
/// </summary>
public class ValidationRule
{
    public string Type { get; set; } = string.Empty;
    public object? Value { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Attribute metadata
/// </summary>
public class AttributeMetadata
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object?> Arguments { get; set; } = new();
}

/// <summary>
/// API endpoint information
/// </summary>
public class ApiEndpoint
{
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ParameterMetadata> Parameters { get; set; } = new();
}

/// <summary>
/// API parameter metadata
/// </summary>
public class ParameterMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string? Description { get; set; }
}
```

## Example Transformation

### Input: C# Component

```csharp
namespace core.jarvis.api.Models;

/// <summary>
/// Represents an order in the e-commerce system
/// Table: order_component
/// </summary>
public record OrderComponent : IComponent, IVersionedComponent
{
    /// <summary>
    /// Unique identifier for this component instance
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The entity this component belongs to
    /// </summary>
    public Guid OwnerEntityId { get; set; }

    /// <summary>
    /// Unique order number
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Customer who placed the order
    /// </summary>
    [Required]
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>
    /// Current order status
    /// </summary>
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    /// <summary>
    /// Order total in cents
    /// </summary>
    [Range(0, int.MaxValue)]
    public int TotalAmountCents { get; set; }

    /// <summary>
    /// When the order was placed
    /// </summary>
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional order notes
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Version for optimistic concurrency
    /// </summary>
    public int? Version { get; set; }
}

public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}
```

### Output: Generated TypeScript

```typescript
// Generated on 2025-08-02T10:30:00.000Z
// DO NOT EDIT: This file is auto-generated from C# component definitions

import type { IComponent } from './base.types';

/**
 * Represents an order in the e-commerce system
 * Table: order_component
 */
export interface OrderComponent {
  /** Unique identifier for this component instance */
  id: string;
  /** The entity this component belongs to */
  ownerEntityId: string;
  /** Unique order number */
  orderNumber: string;
  /** Customer who placed the order */
  customerId: string;
  /** Current order status */
  status: OrderStatus;
  /** Order total in cents */
  totalAmountCents: number;
  /** When the order was placed */
  orderDate: string;
  /** Last update timestamp */
  lastUpdated: string;
  /** Optional order notes */
  notes?: string | null;
  /** Version for optimistic concurrency */
  version?: number | null;
}

/**
 * Database storage interface for OrderComponent
 */
export interface OrderComponentDbComponent extends IComponent {
  order_number: string;
  customer_id: string;
  status: string;
  total_amount_cents: number;
  order_date: string;
  notes?: string | null;
  version?: number | null;
}

/**
 * Order status enumeration
 */
export enum OrderStatus {
  Pending = 'Pending',
  Processing = 'Processing',
  Shipped = 'Shipped',
  Delivered = 'Delivered',
  Cancelled = 'Cancelled'
}

/**
 * Validation schema for OrderComponent
 */
export const OrderComponentValidation = {
  orderNumber: [
    { type: 'required', message: 'Order number is required' },
    { type: 'maxLength', value: 50, message: 'Order number cannot exceed 50 characters' }
  ],
  customerId: [
    { type: 'required', message: 'Customer ID is required' }
  ],
  totalAmountCents: [
    { type: 'range', value: { min: 0, max: 2147483647 }, message: 'Total amount must be non-negative' }
  ],
  notes: [
    { type: 'maxLength', value: 1000, message: 'Notes cannot exceed 1000 characters' }
  ]
};

/**
 * API client for OrderComponent operations
 */
export const OrderComponentApi = {
  /**
   * Get all orders
   */
  async getAll(): Promise<OrderComponent[]> {
    const response = await fetch('/api/orders', {
      method: 'GET',
    });
    
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }
    
    return await response.json();
  },

  /**
   * Get order by ID
   */
  async getById(id: string): Promise<OrderComponent> {
    const response = await fetch(`/api/orders/${id}`, {
      method: 'GET',
    });
    
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }
    
    return await response.json();
  },

  /**
   * Create new order
   */
  async create(data: Omit<OrderComponent, 'id' | 'lastUpdated'>): Promise<OrderComponent> {
    const response = await fetch('/api/orders', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });
    
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }
    
    return await response.json();
  },

  /**
   * Update order
   */
  async update(id: string, data: Partial<OrderComponent>): Promise<OrderComponent> {
    const response = await fetch(`/api/orders/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });
    
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }
    
    return await response.json();
  },

  /**
   * Delete order
   */
  async delete(id: string): Promise<void> {
    const response = await fetch(`/api/orders/${id}`, {
      method: 'DELETE',
    });
    
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }
  },

  /**
   * Get order by entity ID
   */
  async getByEntityId(entityId: string): Promise<OrderComponent> {
    const response = await fetch(`/api/orders/entity/${entityId}`, {
      method: 'GET',
    });
    
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }
    
    return await response.json();
  }
};
```

## Build Integration

### 1. MSBuild Target

```xml
<!-- In core.jarvis.api.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  
  <Target Name="GenerateTypeScriptMirrors" BeforeTargets="Build">
    <Exec Command="dotnet run --project $(SolutionDir)tools/ComponentMirrorGenerator 
                  --projects &quot;$(ProjectDir)$(ProjectFileName)&quot; 
                  --output &quot;$(SolutionDir)core.jarvis.ui.studio/src/types/generated&quot; 
                  --config &quot;$(ProjectDir)mirror-generator.json&quot;"
          ContinueOnError="false" />
  </Target>

</Project>
```

### 2. Configuration File

```json
// mirror-generator.json
{
  "generator": {
    "outputPath": "../core.jarvis.ui.studio/src/types/generated",
    "generateApiClients": true,
    "generateValidation": true,
    "generateDbInterfaces": true,
    "fileHeader": "// Generated on {timestamp}\n// DO NOT EDIT: This file is auto-generated from C# component definitions\n",
    "namingConventions": {
      "interfaces": "PascalCase",
      "properties": "camelCase", 
      "dbColumns": "snake_case",
      "apiEndpoints": "kebab-case"
    }
  },
  "projects": [
    {
      "path": "./core.jarvis.api.csproj",
      "includePatterns": ["**/Models/**/*.cs", "**/Components/**/*.cs"],
      "excludePatterns": ["**/Tests/**/*.cs"]
    }
  ],
  "typeMapping": {
    "System.Guid": "string",
    "System.DateTime": "string",
    "System.DateTimeOffset": "string",
    "decimal": "number",
    "byte[]": "string"
  },
  "apiEndpoints": {
    "baseUrl": "/api",
    "conventionType": "RESTful",
    "generateCrudOperations": true,
    "includePagination": true,
    "includeEntityEndpoints": true
  }
}
```

### 3. CLI Tool Implementation

```csharp
// Program.cs for ComponentMirrorGenerator tool
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace core.jarvis.tools.ComponentMirrorGenerator;

class Program
{
    static async Task<int> Main(string[] args)
    {
        return await Parser.Default.ParseArguments<GeneratorOptions>(args)
            .MapResult(async (GeneratorOptions opts) =>
            {
                using var host = CreateHostBuilder(opts).Build();
                var generator = host.Services.GetRequiredService<MirrorGenerator>();
                
                try
                {
                    await generator.GenerateAsync(opts);
                    Console.WriteLine("TypeScript mirrors generated successfully!");
                    return 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    return 1;
                }
            },
            errs => Task.FromResult(1));
    }

    static IHostBuilder CreateHostBuilder(GeneratorOptions options) =>
        Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddLogging(configure => configure.AddConsole());
                services.AddSingleton<ComponentDiscoveryEngine>();
                services.AddSingleton<ComponentAnalyzer>();
                services.AddSingleton<TypeScriptGenerator>();
                services.AddSingleton<MirrorGenerator>();
            });
}

[Verb("generate", HelpText = "Generate TypeScript mirrors from C# components")]
public class GeneratorOptions
{
    [Option('p', "projects", Required = true, HelpText = "C# project files to analyze")]
    public IEnumerable<string> Projects { get; set; } = new List<string>();

    [Option('o', "output", Required = true, HelpText = "Output directory for TypeScript files")]
    public string OutputPath { get; set; } = string.Empty;

    [Option('c', "config", Required = false, HelpText = "Configuration file path")]
    public string? ConfigFile { get; set; }

    [Option('w', "watch", Required = false, HelpText = "Watch for changes and regenerate")]
    public bool Watch { get; set; }

    [Option('v', "verbose", Required = false, HelpText = "Enable verbose logging")]
    public bool Verbose { get; set; }
}
```

### 4. Main Generator Orchestrator

```csharp
/// <summary>
/// Main orchestrator for the component mirror generation process
/// </summary>
public class MirrorGenerator
{
    private readonly ComponentDiscoveryEngine _discoveryEngine;
    private readonly TypeScriptGenerator _typeScriptGenerator;
    private readonly ILogger<MirrorGenerator> _logger;

    public MirrorGenerator(
        ComponentDiscoveryEngine discoveryEngine,
        TypeScriptGenerator typeScriptGenerator,
        ILogger<MirrorGenerator> logger)
    {
        _discoveryEngine = discoveryEngine;
        _typeScriptGenerator = typeScriptGenerator;
        _logger = logger;
    }

    /// <summary>
    /// Generates TypeScript mirrors for all discovered components
    /// </summary>
    public async Task GenerateAsync(GeneratorOptions options)
    {
        _logger.LogInformation("Starting component mirror generation...");
        
        // Load configuration
        var config = await LoadConfiguration(options.ConfigFile);
        
        // Discover components
        var components = await _discoveryEngine.DiscoverComponents(options.Projects.ToArray());
        _logger.LogInformation("Discovered {Count} components", components.Count);
        
        // Generate TypeScript
        var typeScriptContent = await _typeScriptGenerator.GenerateTypeScript(components);
        
        // Write output files
        await WriteOutputFiles(options.OutputPath, typeScriptContent, components, config);
        
        // Generate index file
        await GenerateIndexFile(options.OutputPath, components);
        
        _logger.LogInformation("Component mirror generation completed successfully!");
    }

    /// <summary>
    /// Writes the generated TypeScript to output files
    /// </summary>
    private async Task WriteOutputFiles(string outputPath, string content, List<ComponentMetadata> components, GeneratorConfig config)
    {
        Directory.CreateDirectory(outputPath);
        
        // Write main components file
        var componentsPath = Path.Combine(outputPath, "components.generated.ts");
        await File.WriteAllTextAsync(componentsPath, content);
        
        // Write individual component files if configured
        if (config.SeparateFiles)
        {
            foreach (var component in components)
            {
                var componentContent = await _typeScriptGenerator.GenerateComponentFile(component);
                var componentPath = Path.Combine(outputPath, $"{component.Name.ToLowerInvariant()}.generated.ts");
                await File.WriteAllTextAsync(componentPath, componentContent);
            }
        }
        
        // Write metadata file
        var metadata = new
        {
            GeneratedAt = DateTime.UtcNow,
            Components = components.Select(c => new { c.Name, c.FullName, c.TableName }).ToList(),
            Generator = new { Version = "1.0.0", Configuration = config }
        };
        
        var metadataPath = Path.Combine(outputPath, "generation-metadata.json");
        await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));
    }
}
```

## Usage Examples

### 1. Command Line Usage

```bash
# Generate from single project
dotnet run --project tools/ComponentMirrorGenerator -- generate \
  --projects "./core.jarvis.api/core.jarvis.api.csproj" \
  --output "./core.jarvis.ui.studio/src/types/generated" \
  --config "./mirror-generator.json"

# Generate from multiple projects
dotnet run --project tools/ComponentMirrorGenerator -- generate \
  --projects "./core.jarvis.api/core.jarvis.api.csproj" "./core.jarvis.data/core.jarvis.data.csproj" \
  --output "./core.jarvis.ui.studio/src/types/generated" \
  --verbose

# Watch mode for development
dotnet run --project tools/ComponentMirrorGenerator -- generate \
  --projects "./core.jarvis.api/core.jarvis.api.csproj" \
  --output "./core.jarvis.ui.studio/src/types/generated" \
  --watch
```

### 2. Integration with Package.json

```json
{
  "scripts": {
    "generate:types": "cd ../.. && dotnet run --project tools/ComponentMirrorGenerator -- generate --projects './core.jarvis.api/core.jarvis.api.csproj' --output './core.jarvis.ui.studio/src/types/generated'",
    "dev": "npm run generate:types && vite",
    "build": "npm run generate:types && vite build"
  }
}
```

### 3. VS Code Task Integration

```json
// .vscode/tasks.json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "Generate TypeScript Mirrors",
      "type": "shell",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "tools/ComponentMirrorGenerator",
        "--",
        "generate",
        "--projects",
        "./core.jarvis.api/core.jarvis.api.csproj",
        "--output",
        "./core.jarvis.ui.studio/src/types/generated"
      ],
      "group": "build",
      "presentation": {
        "echo": true,
        "reveal": "silent",
        "focus": false,
        "panel": "shared"
      },
      "problemMatcher": []
    }
  ]
}
```

## Advanced Features

### 1. Incremental Generation

```csharp
/// <summary>
/// Provides incremental generation capabilities to only regenerate changed components
/// </summary>
public class IncrementalGenerator
{
    /// <summary>
    /// Checks if regeneration is needed based on file timestamps and checksums
    /// </summary>
    public async Task<bool> ShouldRegenerateAsync(string projectPath, string outputPath)
    {
        var lastGeneration = await GetLastGenerationMetadata(outputPath);
        if (lastGeneration == null)
            return true;

        var sourceFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);
        
        foreach (var file in sourceFiles)
        {
            var lastWrite = File.GetLastWriteTimeUtc(file);
            if (lastWrite > lastGeneration.Timestamp)
                return true;
        }

        return false;
    }
}
```

### 2. Custom Type Mapping

```csharp
/// <summary>
/// Allows custom type mappings for complex scenarios
/// </summary>
public class CustomTypeMapper
{
    private readonly Dictionary<string, string> _customMappings;

    public string MapType(ITypeSymbol type, ComponentMetadata component)
    {
        var fullTypeName = type.ToDisplayString();
        
        // Check for custom mappings first
        if (_customMappings.TryGetValue(fullTypeName, out var customMapping))
            return customMapping;
            
        // Check for component-specific mappings
        if (component.Attributes.Any(a => a.Name == "TypeScriptType"))
        {
            return component.Attributes.First(a => a.Name == "TypeScriptType")
                .Arguments["type"]?.ToString() ?? "any";
        }
        
        // Fall back to default mapping
        return DefaultTypeMapper.Map(type);
    }
}
```

### 3. API Endpoint Discovery

```csharp
/// <summary>
/// Discovers API endpoints by analyzing Azure Functions and Controllers
/// </summary>
public class ApiEndpointDiscovery
{
    /// <summary>
    /// Analyzes Azure Functions to discover actual API endpoints
    /// </summary>
    public async Task<List<ApiEndpoint>> DiscoverEndpoints(string componentName, Compilation compilation)
    {
        var endpoints = new List<ApiEndpoint>();
        
        // Find Azure Functions that work with this component
        var functionTypes = compilation.GlobalNamespace
            .GetNamespaceMembers()
            .SelectMany(ns => ns.GetTypeMembers())
            .Where(t => t.Name.EndsWith("Function"));
            
        foreach (var functionType in functionTypes)
        {
            var methods = functionType.GetMembers().OfType<IMethodSymbol>()
                .Where(m => m.GetAttributes().Any(a => a.AttributeClass?.Name == "FunctionAttribute"));
                
            foreach (var method in methods)
            {
                var endpoint = AnalyzeFunctionMethod(method, componentName);
                if (endpoint != null)
                    endpoints.Add(endpoint);
            }
        }
        
        return endpoints;
    }
}
```

## Testing Strategy

### 1. Unit Tests for Generator

```csharp
[TestClass]
public class ComponentAnalyzerTests
{
    [TestMethod]
    public async Task AnalyzeComponent_WithValidComponent_ReturnsMetadata()
    {
        // Arrange
        var source = @"
            using core.jarvis.Data;
            public record TestComponent : IComponent
            {
                public Guid Id { get; init; }
                public Guid OwnerEntityId { get; set; }
                public DateTime LastUpdated { get; set; }
                
                [Required]
                public string Name { get; set; } = string.Empty;
            }";
            
        var compilation = CreateCompilation(source);
        var componentSymbol = GetComponentSymbol(compilation, "TestComponent");
        var analyzer = new ComponentAnalyzer();
        
        // Act
        var metadata = await analyzer.AnalyzeComponent(componentSymbol, compilation);
        
        // Assert
        metadata.Should().NotBeNull();
        metadata.Name.Should().Be("TestComponent");
        metadata.Properties.Should().HaveCount(4);
        metadata.Properties.First(p => p.Name == "Name").ValidationRules
            .Should().Contain(r => r.Type == "required");
    }
}
```

### 2. Integration Tests

```csharp
[TestClass]
public class EndToEndGenerationTests
{
    [TestMethod]
    public async Task GenerateTypeScript_WithRealProject_CreatesValidTypeScript()
    {
        // Arrange
        var projectPath = Path.Combine(TestContext.TestRunDirectory, "TestProject.csproj");
        var outputPath = Path.Combine(TestContext.TestRunDirectory, "generated");
        var generator = new MirrorGenerator(/* dependencies */);
        
        // Act
        await generator.GenerateAsync(new GeneratorOptions
        {
            Projects = new[] { projectPath },
            OutputPath = outputPath
        });
        
        // Assert
        var generatedFile = Path.Combine(outputPath, "components.generated.ts");
        File.Exists(generatedFile).Should().BeTrue();
        
        var content = await File.ReadAllTextAsync(generatedFile);
        content.Should().Contain("export interface");
        content.Should().Contain("export const");
    }
}
```

## Configuration Schema

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "generator": {
      "type": "object",
      "properties": {
        "outputPath": { "type": "string" },
        "generateApiClients": { "type": "boolean", "default": true },
        "generateValidation": { "type": "boolean", "default": true },
        "generateDbInterfaces": { "type": "boolean", "default": true },
        "separateFiles": { "type": "boolean", "default": false },
        "fileHeader": { "type": "string" },
        "namingConventions": {
          "type": "object",
          "properties": {
            "interfaces": { "enum": ["PascalCase", "camelCase"] },
            "properties": { "enum": ["PascalCase", "camelCase"] },
            "dbColumns": { "enum": ["snake_case", "camelCase"] },
            "apiEndpoints": { "enum": ["kebab-case", "camelCase"] }
          }
        }
      }
    },
    "projects": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "path": { "type": "string" },
          "includePatterns": { "type": "array", "items": { "type": "string" } },
          "excludePatterns": { "type": "array", "items": { "type": "string" } }
        },
        "required": ["path"]
      }
    }
  },
  "required": ["generator", "projects"]
}
```

## Benefits

1. **Type Safety**: Ensures perfect synchronization between backend and frontend types
2. **Developer Experience**: Provides IntelliSense and auto-completion for all component properties
3. **Reduced Errors**: Eliminates manual type definition maintenance
4. **API Discovery**: Automatically generates API client code based on naming conventions
5. **Validation Consistency**: Transfers validation rules from C# attributes to TypeScript
6. **Documentation**: Preserves XML documentation comments in generated TypeScript
7. **Build Integration**: Seamlessly integrates with existing build processes
8. **Incremental**: Only regenerates when source files change

This comprehensive Component Mirror Generator ensures that the Jarvis framework maintains perfect type consistency between the C# backend components and the TypeScript frontend, while providing a rich developer experience with automatic API client generation and validation rule preservation.