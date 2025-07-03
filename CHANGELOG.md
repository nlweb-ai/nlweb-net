# Changelog

All notable changes to the NLWebNet project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0-alpha.5] - 2025-06-24

### Added

- Comprehensive markdown formatting tools and documentation system
- Advanced tool handler testing framework with comprehensive unit tests
- Performance benchmarking framework for tool selection and backend operations
- Enhanced Docker build optimizations and SSL certificate handling
- Semantic vector search implementation for AspireDemo application

### Changed

- Improved demo setup guide and enhanced mock data backend tests
- Migrated from controllers to minimal APIs for better type safety
- Enhanced GitHub Models client integration with proper service lifetime management

### Fixed

- Docker build timeout issues and SSL certificate validation
- GitHub Models client registration and dependency injection
- Formatting issues across multiple files for improved readability

## [0.1.0-alpha.4] - 2025-06-21

### Added

- .NET SDK container build support for modern containerization
- Comprehensive deployment infrastructure grouped into single deployment/ directory
- Enhanced documentation and API reference
- MCP SDK updated to version 0.3.0-preview.1

### Changed

- Organized deployment infrastructure into consolidated deployment/ directory
- Updated Docker build paths in GitHub Actions workflow
- Enhanced demo application with improved configuration management

### Fixed

- Docker build path issues in CI/CD pipeline
- README.md packaging path to support multiple locations

## [0.1.0-alpha.3] - 2025-06-21

### Added

- Multi-backend retrieval architecture with concurrent querying and deduplication
- Advanced tool system with Search, Details, Compare, Ensemble, and Recipe tools
- Tool selection framework for intelligent query routing
- YAML and XML configuration format support
- Comprehensive testing framework with performance benchmarking
- OpenTelemetry integration for monitoring and observability
- Aspire demo application with vector search capabilities

### Changed

- Enhanced backend manager with multi-backend support
- Updated configuration system to support multiple formats
- Improved streaming implementation with multi-stage responses

## [0.1.0-alpha.2] - 2025-06-20

### Added

- GitHub Models integration for demo application
- AI provider detection and configuration service
- Secure session-based token storage for GitHub Models
- Enhanced demo UI with model selection and validation
- Comprehensive deployment infrastructure (Docker, Kubernetes, Azure)

### Changed

- Improved demo application user experience
- Enhanced AI provider integration flow

### Fixed

- Package versioning in CI/CD pipeline
- Build permissions for GitHub Actions
- Package validation and symbols generation

## [0.1.0-alpha.1] - 2025-06-20

### Added

- **Core Library Implementation**

  - Complete NLWeb protocol implementation with `/ask` and `/mcp` endpoints
  - Three query modes: List, Summarize, Generate
  - Streaming response support using Server-Sent Events (SSE)
  - Model Context Protocol (MCP) integration with tools and prompts
  - Microsoft.Extensions.AI integration for AI client abstraction
  - Extensible `IDataBackend` interface with `MockDataBackend` implementation

- **API and Middleware**

  - Minimal API endpoints replacing traditional controllers
  - Centralized request processing with `NLWebMiddleware`
  - CORS support for cross-origin requests
  - Global error handling and logging with correlation IDs
  - Request validation and sanitization
  - Query ID generation for request tracking

- **Dependency Injection and Configuration**

  - `AddNLWebNet()` extension method for service registration
  - `MapNLWebNet()` extension method for endpoint mapping
  - Strongly-typed `NLWebOptions` configuration class
  - Support for multiple AI providers (Azure OpenAI, OpenAI API)
  - Environment-specific configurations (Development, Production)

- **Demo Application**

  - .NET 9 Blazor Web App with modern component structure
  - Interactive query interface with all NLWeb options
  - Real-time streaming demonstration
  - Comprehensive API testing interface
  - MCP demonstration page with tool and prompt testing
  - Bootstrap-based responsive UI with FontAwesome icons

- **Testing and Quality**

  - Comprehensive unit test suite (39 tests) using MSTest
  - API endpoint testing with validation scenarios
  - MCP functionality testing with NSubstitute mocks
  - Manual testing guides with curl examples
  - Sample HTTP requests for IDE testing
  - CI/CD pipeline with automated build, test, and validation

- **Documentation and Packaging**

  - XML documentation for all public APIs
  - Comprehensive README with usage examples
  - NuGet package with proper metadata and symbols
  - Complete setup guide for AI integration
  - Manual testing documentation
  - Deployment configuration examples

### Technical Implementation Details

- **Architecture**: Clean interfaces with async/await patterns and cancellation token support
- **Streaming**: Proper SSE implementation avoiding C# yield-return-in-try-catch limitations
- **Testing**: 100% pass rate across all unit and integration tests
- **Package**: Follows Microsoft.Extensions.* best practices with discoverable extension methods
- **CI/CD**: GitHub Actions workflow with security scanning and package validation

### Fixed

- Extension method accessibility in NuGet package (resolved namespace issues)
- Package API surface area exposure
- Minimal API logger dependency injection
- Parameter binding and routing for minimal APIs
- Build configurations and deterministic builds

## Project Milestones

### Phase 1-2: Foundation (June 2025)

- Library project setup with .NET 9 and dependency management
- Core data models and request/response types
- CI/CD pipeline with GitHub Actions

### Phase 3-4: Core Services (June 2025)

- Business logic layer with service interfaces
- Model Context Protocol (MCP) integration
- Microsoft.Extensions.AI client abstraction

### Phase 5-6: API Layer (June 2025)

- Controller-based API implementation (later migrated to Minimal APIs)
- Dependency injection extensions and configuration
- Streaming support with Server-Sent Events

### Phase 7-8: Demo and Configuration (June 2025)

- Enhanced Blazor demo application with modern UI
- Comprehensive configuration system
- Multi-environment support

### Phase 9-10: Testing and Packaging (June 2025)

- Complete unit test coverage with MSTest
- NuGet package creation and publication
- Documentation and API reference

### Phase 11: Production Deployment (June 2025)

- Live NuGet package publication
- Deployment infrastructure (Docker, Kubernetes)
- Production-ready CI/CD pipeline

## References

- [NLWeb Protocol Specification](https://github.com/microsoft/NLWeb)
- [NuGet Package](https://www.nuget.org/packages/NLWebNet/)
- [GitHub Repository](https://github.com/jongalloway/NLWebNet)
