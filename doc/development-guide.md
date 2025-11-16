# Development Guide for NLWebNet

This document provides comprehensive guidance for developers working with the NLWebNet codebase. Please follow these instructions when contributing code, making modifications, or extending functionality.

## Project Overview

**NLWebNet** is a .NET implementation of the [NLWeb protocol](https://github.com/microsoft/NLWeb) for building natural language web interfaces.

### Development Status

**This library is being developed for eventual production use and follows .NET best practices.**

- **Purpose**: To create a production-quality implementation of the NLWeb protocol for .NET applications
- **Current status**: Early alpha prerelease - not yet production ready
- **Development goal**: Production-ready library with high code quality standards
- **Demo application**: Simple reference implementation showcasing library capabilities
- **Target audience**: .NET developers building natural language web interfaces

## Technology Stack

- **.NET 10.0** - Latest .NET version with modern features
- **ASP.NET Core** - Web framework with Minimal APIs
- **Blazor Web App** - For the demo application
- **Microsoft.Extensions.AI** - AI service abstractions
- **Model Context Protocol (MCP)** - For enhanced AI integration
- **MSTest** - Testing framework
- **NSubstitute** - Mocking framework for unit tests

## Architecture Patterns

### 1. Minimal API Approach

- **Primary approach**: Uses **Minimal APIs** for modern, lightweight endpoints
- Endpoints are organized in static classes: `AskEndpoints`, `McpEndpoints`
- Extension methods provide clean endpoint mapping: `app.MapNLWebNet()`
- Modern .NET 10 features including TypedResults for improved type safety

### 2. Dependency Injection

- Follows standard .NET DI patterns
- Extension methods for service registration: `services.AddNLWebNet()`
- Interface-based design with clear service contracts
- Supports custom backend implementations via generics

### 3. Service Layer Architecture


```text
Endpoints → Services → Data Backends
        ↘ MCP Integration

```

Key interfaces:

- `INLWebService` - Main orchestration service
- `IDataBackend` - Data retrieval abstraction
- `IQueryProcessor` - Query processing logic
- `IResultGenerator` - Response generation
- `IMcpService` - Model Context Protocol integration

### 4. Configuration Pattern

- Uses `NLWebOptions` for strongly-typed configuration
- Supports configuration binding from `appsettings.json`
- User secrets for sensitive data (API keys)
- Environment-specific configurations

### 5. Modern Architecture

The project uses **Minimal APIs exclusively** for a modern, lightweight approach.

- **Current**: Uses `Endpoints/` classes with static mapping methods and TypedResults
- **Extension methods**: Clean API surface via `MapNLWebNet()` for minimal APIs
- **Best practices**: .NET 10 features including TypedResults for type safety

## Code Conventions

### Naming and Structure

- **Namespace**: All code under `NLWebNet` namespace
- **Models**: Request/response DTOs with JSON serialization attributes
- **Services**: Interface + implementation pattern (`IService` → `Service`)
- **Extensions**: Static extension classes for framework integration
- **Endpoints**: Static classes with minimal API mapping methods

### C# Style Guidelines

- **Nullable reference types** enabled (`<Nullable>enable</Nullable>`)
- **Implicit usings** enabled for common namespaces
- **XML documentation** for all public APIs
- **Data annotations** for request validation
- **JSON property names** in snake_case for protocol compliance

### File Organization


```text
src/NLWebNet/
├── Models/          # Request/response DTOs
├── Services/        # Business logic interfaces/implementations
├── Endpoints/       # Minimal API endpoint definitions with TypedResults
├── Extensions/      # DI and middleware extensions
├── MCP/            # Model Context Protocol integration
└── Middleware/     # Custom middleware components

```

## NLWeb Protocol Implementation

### Core Concepts

- **Three query modes**: `List`, `Summarize`, `Generate`
- **Streaming support**: Real-time response delivery
- **Query context**: Previous queries and decontextualization
- **Site filtering**: Subset targeting within data backends

### Request/Response Flow

1. Validate incoming `NLWebRequest`
1. Process query through `IQueryProcessor`
1. Retrieve data via `IDataBackend`
1. Generate response using `IResultGenerator`
1. Return `NLWebResponse` with results

### MCP Integration

- Supports core MCP methods: `list_tools`, `list_prompts`, `call_tool`, `get_prompt`
- Parallel endpoint structure: `/ask` and `/mcp` with shared logic
- Tool and prompt template management

## Testing Approach

### Unit Testing

- **39 unit tests** with 100% pass rate (current standard)
- **NSubstitute** for mocking dependencies
- **MSTest** framework with `[TestMethod]` attributes
- Focus on service layer and business logic testing

### Integration Testing

- **Manual testing** preferred over automated integration tests
- **Comprehensive guides** in `/doc/manual-testing-guide.md`
- **Sample requests** in `/doc/sample-requests.http` for IDE testing
- **Demo application** for end-to-end validation

### Testing Conventions

- Test classes named `[ClassUnderTest]Tests`
- Arrange-Act-Assert pattern
- Mock external dependencies (AI services, data backends)
- Test both success and error scenarios

## Development Practices

### CI/CD Pipeline

- **GitHub Actions** for automated builds and testing
- **NuGet package** generation and validation
- **Release automation** with version tagging
- **Security scanning** for vulnerable dependencies

### Build and Packaging

- **Deterministic builds** for reproducible packages
- **Symbol packages** (.snupkg) for debugging support
- **Source Link** integration for GitHub source navigation
- **Package validation** scripts for quality assurance

### Documentation Standards

- **Comprehensive README** with usage examples
- **XML documentation** for all public APIs
- **OpenAPI specification** generated automatically
- **Manual testing guides** for validation procedures

## AI Service Integration

### Microsoft.Extensions.AI Pattern

- Use `Microsoft.Extensions.AI` abstractions for AI service integration
- Support multiple AI providers (Azure OpenAI, OpenAI API)
- Configuration-driven AI service selection
- Async/await patterns for AI service calls

### Error Handling

- **Graceful degradation** when AI services are unavailable
- **Fallback responses** for service failures
- **Proper exception handling** with meaningful error messages
- **Logging** for debugging and monitoring

## Common Patterns and Best Practices

### When Adding New Features

1. **Start with interfaces** - Define contracts before implementations
1. **Add configuration options** to `NLWebOptions` if needed
1. **Include unit tests** - Maintain the 100% pass rate standard
1. **Update documentation** - XML docs and README as appropriate
1. **Consider MCP integration** - How does this fit with MCP protocol?

### When Modifying Endpoints

1. **Use Minimal APIs** - All endpoints use the modern `Endpoints/` classes with TypedResults
1. **Maintain protocol compliance** - Follow NLWeb specification
1. **Add OpenAPI documentation** - Use `.WithSummary()` and `.WithDescription()`
1. **Include error responses** - Proper status codes and problem details
1. **Test with sample requests** - Update manual testing guides

### When Adding Dependencies

1. **Prefer Microsoft.Extensions.*** - Use standard .NET abstractions
1. **Check for existing alternatives** - Avoid duplicate functionality
1. **Use Central Package Management** - Add versions to `Directory.Packages.props`, reference packages without versions in project files
1. **Validate package size** - Keep library lightweight

## Limitations and Current Implementation Status

### Current Implementation Status

- **Early alpha prerelease** - Core functionality implemented, not yet production ready
- **Mock data backend** as default - Real data source integrations can be implemented via `IDataBackend`
- **Basic AI integration** - Extensible via Microsoft.Extensions.AI patterns
- **Authentication framework** - Ready for implementation based on application requirements
- **Code quality standards** - Production-level code quality maintained throughout development

### Performance Considerations

- **Streaming responses** for better perceived performance
- **Async/await** throughout for scalability
- **Minimal allocations** where possible
- **Configuration caching** for frequently accessed settings

### Deployment Considerations

- **Requires .NET 10.0** - Latest framework dependency for modern features
- **Early prerelease status** - Not yet ready for production deployment
- **Production-quality code** - Library being developed with production standards
- **Demo app simplicity** - Reference implementation kept simple for clarity

## When to Seek Clarification

Ask for guidance when:

- **Breaking changes** to public APIs are needed
- **New external dependencies** are required
- **Significant architectural changes** are proposed
- **Protocol compliance** questions arise
- **Production deployment** patterns need to be established

## Summary

This library is being developed with production-quality standards, though it is currently in early prerelease and not yet ready for production use. All code additions and edits should maintain production-level quality as the project works toward its goal of becoming a production-ready NLWeb protocol implementation for .NET applications.

---

**Related Documentation:**

- [Project Overview](../README.md)
- [Demo Setup Guide](demo-setup-guide.md)
- [Manual Testing Guide](manual-testing-guide.md)
- [Implementation Status](todo.md)
