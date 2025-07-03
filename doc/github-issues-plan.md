# GitHub Issues Plan for NLWeb June 2025 Updates

This document outlines the specific GitHub issues and labels that should be created based on the [NLWeb June 2025 Analysis](nlweb-june-2025-analysis.md).

## Required Labels

### 1. `Required Update` Label

- **Color**: `#d73a49` (red)
- **Description**: Critical updates needed for NLWeb protocol compatibility

### 2. `Update Opportunity` Label

- **Color**: `#0366d6` (blue)
- **Description**: Enhancement opportunities from NLWeb June 2025 release

## Required Update Issues

### Issue 1: Implement Multi-Backend Retrieval Architecture

**Labels**: `Required Update`, `enhancement`, `architecture`
**Priority**: Critical
**Effort**: Large

**Description:**
The NLWeb June 2025 release requires support for multiple simultaneous backends. This is a breaking architectural change that needs immediate attention.

**Current State:** NLWebNet supports only single backend via `IDataBackend` interface.

**Requirements:**

- [ ] Update configuration system to support multiple simultaneous backends
- [ ] Implement concurrent querying across multiple backends with automatic deduplication
- [ ] Add concept of primary "write endpoint" while reading from multiple sources
- [ ] Create configuration migration utilities

**Code Areas Affected:**

- `Services/IDataBackend.cs` - Interface needs extension or replacement
- `Services/NLWebService.cs` - Core service logic needs multi-backend support
- Configuration system - New YAML-style configuration support
- Demo application - Update for multi-backend scenarios

**Acceptance Criteria:**

- [ ] Multiple backends can be configured simultaneously
- [ ] Queries execute in parallel across all enabled backends
- [ ] Results are deduplicated automatically
- [ ] One backend can be designated as "write endpoint"
- [ ] Existing single-backend configurations still work

---

### Issue 2: Implement Tool Selection Framework

**Labels**: `Required Update`, `enhancement`, `protocol`
**Priority**: High
**Effort**: Medium

**Description:**
The NLWeb June 2025 release introduces a tool selection framework that routes queries to appropriate tools based on intent.

**Current State:** No tool selection system exists.

**Requirements:**

- [ ] Implement query routing to appropriate tools based on intent
- [ ] Add `tool_selection_enabled` configuration option
- [ ] Ensure existing queries work when tool selection is disabled
- [ ] Maintain existing behavior for `generate_mode` queries

**Code Areas Affected:**

- New `Services/IToolSelector.cs` interface
- `Services/QueryProcessor.cs` - Add tool selection logic
- `Models/NLWebRequest.cs` - May need additional parameters
- Configuration system - Add tool selection settings

**Acceptance Criteria:**

- [ ] Tool selection engine routes queries to appropriate handlers
- [ ] Backward compatibility maintained when tool selection is disabled
- [ ] Configuration option controls tool selection behavior
- [ ] Query processing performance impact < 20%

---

### Issue 3: Update Configuration Format Support

**Labels**: `Required Update`, `configuration`, `breaking-change`
**Priority**: Medium
**Effort**: Small

**Description:**
The NLWeb June 2025 release introduces new configuration formats including YAML-style multi-backend configuration and XML-based tool definitions.

**Current State:** Basic configuration through `NLWebOptions`.

**Requirements:**

- [ ] Support new YAML-style configuration with `enabled` flags
- [ ] Support XML-based tool definitions
- [ ] Provide seamless upgrade from current configuration
- [ ] Maintain backward compatibility

**Code Areas Affected:**

- Configuration system
- `Models/NLWebOptions.cs`
- Demo application configuration
- Documentation

**Acceptance Criteria:**

- [ ] YAML configuration format supported
- [ ] XML tool definitions can be loaded
- [ ] Existing JSON configuration still works
- [ ] Migration path documented

## Update Opportunity Issues

### Issue 4: Implement Advanced Tool System

**Labels**: `Update Opportunity`, `enhancement`, `feature`
**Priority**: High
**Effort**: Large

**Description:**
Implement comprehensive tool system including Search, Details, Compare, Ensemble, and Recipe tools for enhanced query capabilities.

**New Capabilities:**

- [ ] **Search Tool**: Enhanced keyword and semantic search
- [ ] **Details Tool**: Retrieve specific information about named items
- [ ] **Compare Tool**: Side-by-side comparison of two items
- [ ] **Ensemble Tool**: Create cohesive sets of related items
- [ ] **Recipe Tools**: Ingredient substitutions and accompaniment suggestions

**Implementation Strategy:**

- [ ] Create base `IToolHandler` interface
- [ ] Implement handlers for each tool type
- [ ] Add XML tool definition support
- [ ] Create tool selection algorithm based on query analysis

**Sample Queries to Support:**

- "Give me an appetizer, main and dessert for an Italian dinner"
- "I'm visiting Seattle for a day - suggest museums and nearby restaurants"
- "Plan a romantic date night with dinner and entertainment"

---

### Issue 5: Enhanced Debug and Development Experience

**Labels**: `Update Opportunity`, `developer-experience`, `debugging`
**Priority**: Medium
**Effort**: Medium

**Description:**
Enhance the debugging and development experience with real-time visualization and comprehensive metrics.

**New Features:**

- [ ] **Real-time Tool Selection Visualization**: Show how queries are routed to tools
- [ ] **Multi-Backend Query Visualization**: Display parallel backend queries and results
- [ ] **Performance Metrics Dashboard**: Show query performance across backends
- [ ] **Request/Response Debugging**: Enhanced debugging panel in demo application

**Implementation Areas:**

- [ ] Extend existing Blazor demo with advanced debugging components
- [ ] Add SignalR for real-time updates
- [ ] Implement metrics collection and display
- [ ] Create developer-friendly debugging APIs

---

### Issue 6: Implement Configurable Response Headers

**Labels**: `Update Opportunity`, `configuration`, `compliance`
**Priority**: Medium
**Effort**: Small

**Description:**
Add support for configurable response headers including license specifications, data retention policies, and custom metadata.

**New Capabilities:**

- [ ] **License Specification**: MIT License headers with terms links
- [ ] **Data Retention Policies**: Configurable retention policy headers
- [ ] **UI Component Specifications**: Custom rendering hints for client applications
- [ ] **Custom Metadata**: Deployment-specific headers

**Implementation:**

- [ ] Add `ResponseHeadersOptions` configuration
- [ ] Implement middleware for automatic header injection
- [ ] Update demo to showcase header customization
- [ ] Document header configuration patterns

---

### Issue 7: Advanced Backend Support

**Labels**: `Update Opportunity`, `backend`, `integration`
**Priority**: High
**Effort**: Large

**Description:**
Implement support for additional backend types including vector databases and enterprise search solutions.

**New Backend Types:**

- [ ] **Azure AI Search**: Enhanced implementation (current basic support)
- [ ] **Qdrant**: Vector database integration
- [ ] **Milvus**: Vector similarity search
- [ ] **OpenSearch**: Elasticsearch-compatible search
- [ ] **Snowflake**: Data warehouse integration

**Implementation Strategy:**

- [ ] Create backend-specific NuGet packages
- [ ] Implement each backend as separate `IDataBackend` implementations
- [ ] Add configuration templates for each backend type
- [ ] Create demo scenarios for multi-backend deployments

---

### Issue 8: Comprehensive Testing Framework

**Labels**: `Update Opportunity`, `testing`, `quality`
**Priority**: High
**Effort**: Medium

**Description:**
Implement comprehensive testing framework for end-to-end validation and performance benchmarking.

**New Testing Capabilities:**

- [ ] **End-to-End Query Testing**: Configurable test suites for different scenarios
- [ ] **Multi-Backend Verification**: Test query consistency across backends
- [ ] **Tool Selection Accuracy Tests**: Validate query routing decisions
- [ ] **Performance Benchmarking**: Automated performance regression testing
- [ ] **Database Operation Testing**: Backend-specific integration tests

**Implementation Areas:**

- [ ] Extend existing MSTest framework
- [ ] Add integration testing projects
- [ ] Create test data management system
- [ ] Implement automated benchmarking

---

### Issue 9: Streaming and Performance Enhancements

**Labels**: `Update Opportunity`, `performance`, `streaming`
**Priority**: Medium
**Effort**: Medium

**Description:**
Enhance streaming capabilities and performance with multi-stage streaming and advanced optimizations.

**Current Streaming:** Basic Server-Sent Events support exists.

**Enhancement Opportunities:**

- [ ] **Multi-Stage Streaming**: Stream tool selection process and backend queries
- [ ] **Parallel Backend Results**: Stream results as they arrive from different backends
- [ ] **Optimized Deduplication**: Advanced deduplication algorithms for large result sets
- [ ] **Perceived Performance**: Better progress indicators and streaming UX

## Implementation Timeline

### Phase 1: Critical Updates (Months 1-2)

- Issues #1, #2, #3 (Required Updates)

### Phase 2: Core Enhancements (Months 2-4)

- Issues #4, #8 (Advanced Tool System, Testing Framework)

### Phase 3: Advanced Features (Months 4-6)

- Issues #5, #6, #7, #9 (Debug Experience, Headers, Backends, Streaming)

## Notes for Issue Creation

1. **Reference the Analysis**: Each issue should reference the [NLWeb June 2025 Analysis](nlweb-june-2025-analysis.md) document
1. **Link Related Issues**: Cross-reference related issues (e.g., tool selection framework depends on multi-backend architecture)
1. **Acceptance Criteria**: Include specific, measurable acceptance criteria
1. **Technical Specifications**: Reference specific code areas and interfaces that need changes
1. **Backward Compatibility**: Ensure all breaking changes are clearly documented and have migration paths
