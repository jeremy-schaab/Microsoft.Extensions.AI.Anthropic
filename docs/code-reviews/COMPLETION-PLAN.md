# Completion Plan: Microsoft.Extensions.AI.Anthropic

**Date**: 2025-01-19
**Current Status**: 65% Complete (Phases 1-4 done, Phases 5-6 minimal)
**Target**: Production-Ready Release
**Timeline**: 8 weeks

---

## Executive Summary

This document provides a detailed, actionable plan to complete the Microsoft.Extensions.AI.Anthropic library and bring it to production-ready status.

**Current State**: Core functionality complete (Phases 1-4), minimal testing and examples
**Required Work**: Comprehensive testing, examples, utilities, documentation, and bug fixes
**Estimated Effort**: 8 weeks (320 hours)
**Team Size**: 1-2 developers

---

## Sprint Breakdown (2-Week Sprints)

### Sprint 1 (Weeks 1-2): Critical Fixes & Core Testing

**Goal**: Fix critical issues and establish testing foundation

**Days 1-2: Critical Issue Resolution (16 hours)**
- [x] Target framework configured (net9.0) - COMPLETED
  - Using latest .NET 9 with C# 13 features
  - All projects use net9.0
  - Build verified successfully

- [ ] Fix thread safety violation (30 min)
  - Modify `AnthropicChatClient.cs` lines 43, 102, 217-223
  - Make `_anthropicClient` properly nullable
  - Add `_shouldDisposeClient` flag
  - Update `Dispose()` method with null check

- [ ] Fix blocking async in DI (1 hour)
  - Modify `MicrosoftExtensionsAIAnthropicFoundryExtensions.cs` line 58
  - Remove `GetAwaiter().GetResult()`
  - Implement synchronous credential creation or deferred async initialization

- [ ] Build and verify all fixes (30 min)
  - Run `dotnet build -c Release`
  - Run existing 13 tests
  - Verify no regressions

**Days 3-10: AnthropicChatClient Tests (40 tests, 48 hours)**

Create `tests/Microsoft.Extensions.AI.Anthropic.Tests/AnthropicChatClientTests.cs`:

- [ ] **Constructor Tests (8 tests)**
  - `Constructor_WithAnthropicClient_InitializesCorrectly`
  - `Constructor_WithAnthropicFoundryClient_InitializesCorrectly`
  - `Constructor_WithMessageService_InitializesCorrectly`
  - `Constructor_WithNullClient_ThrowsArgumentNullException`
  - `Constructor_WithNullMessageService_ThrowsArgumentNullException`
  - `Constructor_WithModelId_StoresModelId`
  - `Constructor_DetectsAzureFoundry_SetsCorrectProviderName`
  - `Metadata_ReturnsCorrectMetadata`

- [ ] **GetResponseAsync Tests (12 tests)**
  - `GetResponseAsync_BasicChat_ReturnsResponse`
  - `GetResponseAsync_WithSystemMessage_ExtractsSystemPrompt`
  - `GetResponseAsync_WithMultipleSystemMessages_CombinesSystemPrompts`
  - `GetResponseAsync_WithOptions_PassesOptionsCorrectly`
  - `GetResponseAsync_WithModelIdInOptions_OverridesConstructorModelId`
  - `GetResponseAsync_WithNoModelId_ThrowsInvalidOperationException`
  - `GetResponseAsync_WithNullMessages_ThrowsArgumentNullException`
  - `GetResponseAsync_WithCancellationToken_PropagatesCancellation`
  - `GetResponseAsync_WithToolCalls_ReturnsToolUseBlocks`
  - `GetResponseAsync_WithImageContent_SendsImageBlock`
  - `GetResponseAsync_WithUsageContent_ReturnsUsageInformation`
  - `GetResponseAsync_WithApiError_ThrowsCorrectException`

- [ ] **GetStreamingResponseAsync Tests (12 tests)**
  - `GetStreamingResponseAsync_BasicChat_StreamsResponse`
  - `GetStreamingResponseAsync_WithSystemMessage_ExtractsSystemPrompt`
  - `GetStreamingResponseAsync_WithOptions_PassesOptionsCorrectly`
  - `GetStreamingResponseAsync_WithNullMessages_ThrowsArgumentNullException`
  - `GetStreamingResponseAsync_WithCancellationToken_PropagatesCancellation`
  - `GetStreamingResponseAsync_WithTextDelta_StreamsTextChunks`
  - `GetStreamingResponseAsync_WithToolCall_StreamsToolUse`
  - `GetStreamingResponseAsync_WithMessageStop_ReturnsUsage`
  - `GetStreamingResponseAsync_WithPartialContent_AccumulatesCorrectly`
  - `GetStreamingResponseAsync_WithMultipleContentBlocks_HandlesCorrectly`
  - `GetStreamingResponseAsync_WithApiError_ThrowsCorrectException`
  - `GetStreamingResponseAsync_EnumerateTwice_ThrowsException`

- [ ] **GetService Tests (4 tests)**
  - `GetService_WithIChatClientType_ReturnsSelf`
  - `GetService_WithMetadataType_ReturnsMetadata`
  - `GetService_WithUnderlyingClientType_ReturnsClient`
  - `GetService_WithUnregisteredType_ReturnsNull`

- [ ] **Dispose Tests (4 tests)**
  - `Dispose_WithDisposableClient_DisposesClient`
  - `Dispose_WithNonDisposableClient_DoesNotThrow`
  - `Dispose_CalledMultipleTimes_DoesNotThrow`
  - `Dispose_WithNullClient_DoesNotThrow`

**Days 3-10: AnthropicStreamingConverter Tests (40 tests, 48 hours)**

Create `tests/Microsoft.Extensions.AI.Anthropic.Tests/Converters/StreamingConverterTests.cs`:

- [ ] **message_start Event Tests (5 tests)**
  - `ConvertStream_MessageStart_InitializesResponse`
  - `ConvertStream_MessageStart_SetsMessageId`
  - `ConvertStream_MessageStart_SetsModelId`
  - `ConvertStream_MessageStart_SetsRole`
  - `ConvertStream_MessageStart_CreatesEmptyUpdate`

- [ ] **content_block_start Event Tests (6 tests)**
  - `ConvertStream_ContentBlockStart_Text_InitializesTextBlock`
  - `ConvertStream_ContentBlockStart_ToolUse_InitializesToolBlock`
  - `ConvertStream_ContentBlockStart_MultipleBlocks_TracksIndices`
  - `ConvertStream_ContentBlockStart_WithoutMessageStart_ThrowsException`
  - `ConvertStream_ContentBlockStart_InvalidType_HandlesGracefully`
  - `ConvertStream_ContentBlockStart_CreatesNoUpdate`

- [ ] **content_block_delta Event Tests (10 tests)**
  - `ConvertStream_ContentBlockDelta_Text_AccumulatesText`
  - `ConvertStream_ContentBlockDelta_Text_YieldsTextUpdate`
  - `ConvertStream_ContentBlockDelta_ToolUse_AccumulatesJson`
  - `ConvertStream_ContentBlockDelta_MultipleDeltas_AccumulatesCorrectly`
  - `ConvertStream_ContentBlockDelta_LargeText_HandlesEfficiently`
  - `ConvertStream_ContentBlockDelta_InvalidJson_HandlesGracefully`
  - `ConvertStream_ContentBlockDelta_WithoutBlockStart_ThrowsException`
  - `ConvertStream_ContentBlockDelta_EmptyDelta_SkipsUpdate`
  - `ConvertStream_ContentBlockDelta_UnicodeCharacters_HandlesCorrectly`
  - `ConvertStream_ContentBlockDelta_ThousandsOfDeltas_PerformsWell`

- [ ] **content_block_stop Event Tests (5 tests)**
  - `ConvertStream_ContentBlockStop_Text_FinalizesTextBlock`
  - `ConvertStream_ContentBlockStop_ToolUse_ParsesJsonAndCreatesToolCall`
  - `ConvertStream_ContentBlockStop_ToolUse_InvalidJson_ThrowsException`
  - `ConvertStream_ContentBlockStop_MultipleBlocks_HandlesCorrectly`
  - `ConvertStream_ContentBlockStop_YieldsNoUpdate`

- [ ] **message_delta Event Tests (5 tests)**
  - `ConvertStream_MessageDelta_UpdatesStopReason`
  - `ConvertStream_MessageDelta_UpdatesUsage`
  - `ConvertStream_MessageDelta_YieldsUpdate`
  - `ConvertStream_MessageDelta_MultipleDeltas_AccumulatesUsage`
  - `ConvertStream_MessageDelta_WithoutMessageStart_ThrowsException`

- [ ] **message_stop Event Tests (4 tests)**
  - `ConvertStream_MessageStop_YieldsFinalUpdate`
  - `ConvertStream_MessageStop_IncludesUsage`
  - `ConvertStream_MessageStop_IncludesFinishReason`
  - `ConvertStream_MessageStop_ClearsState`

- [ ] **Integration Tests (5 tests)**
  - `ConvertStream_CompleteTextResponse_WorksEndToEnd`
  - `ConvertStream_CompleteToolCallResponse_WorksEndToEnd`
  - `ConvertStream_MixedContentResponse_WorksEndToEnd`
  - `ConvertStream_CancellationToken_StopsStreaming`
  - `ConvertStream_ExceptionInMiddle_PropagatesException`

**Sprint 1 Deliverables:**
- ✅ All 3 critical issues fixed
- ✅ Target frameworks corrected
- ✅ 80 new tests (ChatClient: 40, Streaming: 40)
- ✅ Test coverage: ~35%
- ✅ CI/CD pipeline passing

---

### Sprint 2 (Weeks 3-4): Converter Testing & Major Fixes

**Goal**: Complete converter test suite and fix major issues

**Days 11-14: AnthropicMessageConverter Tests (45 tests, 32 hours)**

Create `tests/Microsoft.Extensions.AI.Anthropic.Tests/Converters/MessageConverterTests.cs`:

- [ ] **ToAnthropicMessages Tests (25 tests)**
  - System message extraction (8 tests)
  - Role mapping (5 tests)
  - Content conversion (6 tests)
  - Message validation (6 tests)

- [ ] **FromAnthropicMessage Tests (20 tests)**
  - Role mapping (5 tests)
  - Content extraction (8 tests)
  - Usage tracking (3 tests)
  - Metadata handling (4 tests)

**Days 15-16: AnthropicToolConverter Tests (25 tests, 16 hours)**

Create `tests/Microsoft.Extensions.AI.Anthropic.Tests/Converters/ToolConverterTests.cs`:

- [ ] **ToAnthropicToolDefinition Tests (15 tests)**
  - Schema generation (6 tests)
  - Type inference (5 tests)
  - Required parameters (2 tests)
  - Enum support (2 tests)

- [ ] **Tool choice mode Tests (10 tests)**
  - Auto mode (3 tests)
  - Required mode (3 tests)
  - Specific function (4 tests)

**Days 17-18: AnthropicOptionsConverter Tests (30 tests, 16 hours)**

Create `tests/Microsoft.Extensions.AI.Anthropic.Tests/Converters/OptionsConverterTests.cs`:

- [ ] **ToMessageCreateParams Tests (30 tests)**
  - Parameter mapping (12 tests)
  - Validation (8 tests)
  - Tool handling (6 tests)
  - Extended features (4 tests)

**Days 19-20: Major Issue Fixes (16 hours)**

- [ ] Fix string-based type detection (1 hour)
  - Replace `Contains("Foundry")` with proper type checking
  - Test with both client types

- [ ] Fix reflection-based endpoint detection (1 hour)
  - Use pattern matching on concrete types
  - Add proper null handling

- [ ] Fix swallowed exceptions in streaming (30 min)
  - Replace generic `catch` with `catch (JsonException)`
  - Add logging for parse errors

- [ ] Fix input validation issues (2 hours)
  - Add CallId generation validation
  - Add MaxTokens bounds checking
  - Add StopSequences limit
  - Add proper error messages

- [ ] Fix tool schema conversion (2 hours)
  - Correct schema property extraction
  - Test with nested objects
  - Verify against Anthropic API

- [ ] Fix memory leak in streaming (2 hours)
  - Add size limits to StringBuilder
  - Implement IDisposable on StreamingState
  - Add memory tests

- [ ] Fix inefficient JSON conversion (1 hour)
  - Implement direct ValueKind switch
  - Benchmark performance improvement
  - Add performance tests

**Sprint 2 Deliverables:**
- ✅ 100 new tests (Message: 45, Tool: 25, Options: 30)
- ✅ All 7 major issues fixed
- ✅ Test coverage: ~65%
- ✅ Performance benchmarks established

---

### Sprint 3 (Weeks 5-6): DI Testing, Integration, Performance & Examples

**Goal**: Complete DI testing, integration tests, performance tests, and primary examples

**Days 21-23: DI Extension Tests (25 tests, 24 hours)**

Create `tests/Microsoft.Extensions.AI.Anthropic.Tests/Extensions/`:

- [ ] **AnthropicExtensionsTests.cs (12 tests)**
  - IServiceCollection extensions (6 tests)
  - IChatClientBuilder extensions (6 tests)

- [ ] **AnthropicFoundryExtensionsTests.cs (13 tests)**
  - IServiceCollection extensions (7 tests)
  - IChatClientBuilder extensions (6 tests)

**Days 24-25: Integration Tests (20 tests, 16 hours)**

Create `tests/Microsoft.Extensions.AI.Anthropic.Tests/Integration/`:

- [ ] **StandardApiIntegrationTests.cs (10 tests)**
  - Requires `ANTHROPIC_API_KEY` environment variable
  - Basic chat, streaming, tools, vision

- [ ] **AzureFoundryIntegrationTests.cs (10 tests)**
  - Requires Azure credentials
  - API key auth, bearer token auth, managed identity
  - Basic chat, streaming, tools

**Days 26-27: Performance Tests (20 tests, 16 hours)**

Create `tests/Microsoft.Extensions.AI.Anthropic.Tests/Performance/`:

- [ ] **StreamingPerformanceTests.cs (15 benchmarks)**
  - BenchmarkDotNet setup
  - Throughput, latency, memory benchmarks

- [ ] **LoadTests.cs (5 scenarios)**
  - NBomber setup
  - Concurrent requests, sustained load

**Days 28-30: Primary Examples (24 hours)**

- [ ] **AzureFoundryBasicExample** (8 hours)
  - Environment variable configuration
  - API key authentication
  - Basic chat completion
  - Error handling
  - README with setup instructions

- [ ] **StreamingChatExample** (8 hours)
  - Real-time streaming output
  - Cancellation handling
  - Progress indicators
  - Works with both APIs

- [ ] **AzureFoundryManagedIdentityExample** (8 hours)
  - DefaultAzureCredential setup
  - Deployment guides (App Service, Azure Functions, AKS)
  - Environment configuration
  - Troubleshooting guide

**Sprint 3 Deliverables:**
- ✅ 65 new tests (DI: 25, Integration: 20, Performance: 20)
- ✅ 3 primary examples working
- ✅ Test coverage: ~90%
- ✅ Integration tests passing with real APIs
- ✅ Performance baseline established

---

### Sprint 4 (Week 7): Additional Examples & Documentation

**Goal**: Complete remaining examples and documentation

**Days 31-32: Additional Examples (16 hours)**

- [ ] **ToolCallingExample** (8 hours)
  - Weather tool example
  - Calculator tool example
  - Multi-turn conversations
  - Error handling

- [ ] **VisionExample** (8 hours)
  - Image analysis
  - PDF document analysis
  - Multi-modal conversations
  - Base64 encoding helpers

**Days 33-35: Documentation (24 hours)**

- [ ] **AZURE-SETUP.md** (8 hours)
  - Azure resource provisioning
  - Authentication configuration
  - Role assignments (RBAC)
  - Networking and security
  - Pricing and quotas
  - Troubleshooting

- [ ] **AUTHENTICATION.md** (8 hours)
  - API Key authentication
  - Bearer Token authentication
  - Azure Identity (DefaultAzureCredential)
  - Managed Identity setup
  - Service Principal setup
  - Environment variable reference
  - Security best practices

- [ ] **MIGRATION.md** (8 hours)
  - From direct Anthropic SDK
  - From OpenAI to Anthropic
  - Breaking changes
  - Feature parity matrix
  - Code examples
  - Common issues

**Sprint 4 Deliverables:**
- ✅ 2 additional examples (Tool Calling, Vision)
- ✅ 3 documentation guides (AZURE-SETUP, AUTHENTICATION, MIGRATION)
- ✅ All examples tested and working
- ✅ Documentation reviewed

---

### Sprint 5 (Week 8): Polish & Release Prep

**Goal**: Final polish and production release preparation

**Days 36-38: Minor Fixes & Polish (24 hours)**

- [ ] **Add structured logging** (8 hours)
  - ILogger integration
  - Log levels (Debug, Information, Warning, Error)
  - Sensitive data filtering
  - Performance impact assessment

- [ ] **Add telemetry hooks** (8 hours)
  - ActivitySource for distributed tracing
  - Metrics (requests, errors, latency)
  - OpenTelemetry integration
  - Azure Monitor integration

- [ ] **Improve error messages** (4 hours)
  - Review all exception messages
  - Add actionable guidance
  - Include documentation links

- [ ] **Code cleanup** (4 hours)
  - Remove commented code
  - Fix formatting
  - Update XML documentation
  - Run code analysis

**Days 39-40: Release Preparation (16 hours)**

- [ ] **Final review** (4 hours)
  - Code review checklist
  - Security review
  - Performance review
  - Documentation review

- [ ] **NuGet package preparation** (4 hours)
  - Update package metadata
  - Add package icon
  - Write release notes
  - Update README for NuGet
  - Create package README.md

- [ ] **CI/CD setup** (4 hours)
  - GitHub Actions workflow
  - Automated testing
  - Code coverage reporting
  - Automated NuGet publishing

- [ ] **Release artifacts** (4 hours)
  - Build release packages
  - Generate documentation site
  - Create sample repository
  - Prepare announcement

**Sprint 5 Deliverables:**
- ✅ Structured logging implemented
- ✅ Telemetry integrated
- ✅ All error messages improved
- ✅ NuGet package ready
- ✅ CI/CD pipeline configured
- ✅ Release artifacts prepared
- ✅ Production-ready!

---

## Resource Allocation

### Developer 1 (Lead)
- Weeks 1-2: Critical fixes + ChatClient tests
- Weeks 3-4: Converter tests + major fixes
- Weeks 5-6: Integration tests + examples
- Weeks 7-8: Documentation + release prep

### Developer 2 (Optional - for faster completion)
- Weeks 1-2: Streaming tests
- Weeks 3-4: Options converter tests
- Weeks 5-6: Performance tests + examples
- Weeks 7-8: Polish + CI/CD

**With 2 developers**: 4-5 weeks to completion
**With 1 developer**: 8 weeks to completion

---

## Success Metrics

### Code Quality
- [ ] 0 critical issues
- [ ] 0 major issues
- [ ] <5 minor issues
- [ ] 90%+ test coverage
- [ ] All tests passing

### Functionality
- [ ] All 4 phases complete
- [ ] All converters working
- [ ] Streaming working
- [ ] DI working
- [ ] Integration tests passing

### Documentation
- [ ] All XML docs complete
- [ ] 3 setup guides written
- [ ] 5+ examples working
- [ ] README comprehensive
- [ ] Migration guide complete

### Performance
- [ ] Streaming latency <10ms per event
- [ ] Memory overhead <5%
- [ ] 100+ concurrent requests supported
- [ ] Benchmarks established

---

## Risk Mitigation

### Technical Risks

**Risk**: Integration tests fail with real API
- **Mitigation**: Start integration tests in Week 5 to allow time for fixes
- **Contingency**: Mock API responses if real API unavailable

**Risk**: Performance doesn't meet targets
- **Mitigation**: Benchmark early (Week 3) and optimize incrementally
- **Contingency**: Document known limitations, plan future optimization sprint

**Risk**: Azure authentication issues in production
- **Mitigation**: Test all credential types in Week 5
- **Contingency**: Provide detailed troubleshooting guide

### Schedule Risks

**Risk**: Testing takes longer than estimated
- **Mitigation**: Prioritize critical tests first, defer edge cases
- **Contingency**: Extend sprint 2 by 3-5 days

**Risk**: Documentation takes longer than estimated
- **Mitigation**: Use templates and examples from similar projects
- **Contingency**: Ship with minimal docs, add later in patch release

**Risk**: Blocking bugs discovered late
- **Mitigation**: Fix critical issues first (Week 1), continuous testing
- **Contingency**: Add Week 9 buffer for critical bug fixes

---

## Quality Gates

### Sprint 1 Exit Criteria
- [ ] All 3 critical issues fixed
- [ ] 80 tests passing
- [ ] No regressions in existing tests
- [ ] Code builds for all target frameworks

### Sprint 2 Exit Criteria
- [ ] 180 total tests passing
- [ ] All 7 major issues fixed
- [ ] Code coverage ≥65%
- [ ] Performance benchmarks established

### Sprint 3 Exit Criteria
- [ ] 245 total tests passing
- [ ] Integration tests passing
- [ ] Code coverage ≥90%
- [ ] 3 primary examples working

### Sprint 4 Exit Criteria
- [ ] 245 total tests passing
- [ ] All 5 examples working
- [ ] 3 documentation guides complete
- [ ] README comprehensive

### Sprint 5 Exit Criteria (Production Release)
- [ ] 260 total tests passing
- [ ] Code coverage ≥90%
- [ ] 0 critical or major issues
- [ ] All documentation complete
- [ ] NuGet package published
- [ ] CI/CD operational

---

## Post-Release Roadmap

### v0.2.0 (4 weeks post-release)
- Beta features support (extended thinking, prompt caching)
- Additional examples (batch API, advanced streaming)
- Performance optimizations

### v0.3.0 (8 weeks post-release)
- Batch API support
- Advanced error recovery
- Retry policies
- Circuit breakers

### v1.0.0 (12 weeks post-release)
- Production hardening
- Enterprise features
- Comprehensive documentation
- Community feedback integration

---

## Communication Plan

### Weekly Updates
- Progress dashboard
- Test coverage metrics
- Issue tracking
- Risk assessment

### Sprint Reviews
- Demo of new features
- Test results review
- Documentation review
- Next sprint planning

### Release Communication
- Release announcement
- Blog post
- Social media
- Community forums

---

## Conclusion

This completion plan provides a clear, actionable roadmap to bring the Microsoft.Extensions.AI.Anthropic library from 65% complete to production-ready in 8 weeks.

**Key Success Factors**:
1. Fix critical issues immediately (Week 1)
2. Prioritize testing throughout (Weeks 1-6)
3. Create Azure Foundry examples (Week 5-6)
4. Complete documentation (Week 7)
5. Polish and release (Week 8)

With disciplined execution and quality focus, this library will provide an excellent developer experience for integrating Anthropic's Claude models with Microsoft.Extensions.AI.

---

**Document Version**: 1.0
**Last Updated**: 2025-01-19
**Author**: Completion Planning Phase
**Status**: READY FOR EXECUTION

---

## Appendix A: Test Inventory

### Implemented Tests (13 tests)
- AnthropicContentConverterTests.cs: 13 tests ✅

### Required Tests (247 tests)
- AnthropicChatClientTests.cs: 40 tests ❌
- StreamingConverterTests.cs: 40 tests ❌
- MessageConverterTests.cs: 45 tests ❌
- ToolConverterTests.cs: 25 tests ❌
- OptionsConverterTests.cs: 30 tests ❌
- AnthropicExtensionsTests.cs: 12 tests ❌
- AnthropicFoundryExtensionsTests.cs: 13 tests ❌
- StandardApiIntegrationTests.cs: 10 tests ❌
- AzureFoundryIntegrationTests.cs: 10 tests ❌
- StreamingPerformanceTests.cs: 15 tests ❌
- LoadTests.cs: 5 tests ❌
- ContentConverterTests.cs: 22 additional tests ❌

**Total**: 13 implemented + 247 required = **260 tests**

---

## Appendix B: Example Inventory

### Implemented Examples (1 partial)
- BasicChatExample ⚠️ (minimal)

### Required Examples (6 additional)
- AzureFoundryBasicExample ❌ **(PRIMARY)**
- StreamingChatExample ❌ **(HIGH PRIORITY)**
- AzureFoundryManagedIdentityExample ❌ **(PRIMARY)**
- ToolCallingExample ❌
- VisionExample ❌
- AzureFoundryStreamingExample ❌ (optional)

**Total**: 1 partial + 6 required = **7 examples**

---

## Appendix C: Documentation Inventory

### Implemented Documentation (3 complete)
- README.md ✅
- CLAUDE.md ✅
- anthropic-integration-research-plan.md ✅
- IMPLEMENTATION-STATUS.md ✅ (this document)
- GAP-ANALYSIS.md ✅ (new)
- COMPLETION-PLAN.md ✅ (this document)

### Required Documentation (3 missing)
- AZURE-SETUP.md ❌
- AUTHENTICATION.md ❌
- MIGRATION.md ❌

**Total**: 6 complete + 3 required = **9 documentation files**

---

*This completion plan provides a detailed roadmap for finishing the Microsoft.Extensions.AI.Anthropic library.*
