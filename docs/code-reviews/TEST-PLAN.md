# Microsoft.Extensions.AI.Anthropic - Comprehensive Test Plan

**Date:** 2025-11-19
**QA Engineer:** Parker
**Target Coverage:** 90%+
**Current Coverage:** ~15% (only AnthropicContentConverter partially tested)

---

## Executive Summary

The Microsoft.Extensions.AI.Anthropic library currently has **only 13 tests** covering basic scenarios in `AnthropicContentConverter`. To reach the 90% coverage target for Phase 5 release, we need approximately **180-220 additional tests** across unit, integration, and performance categories.

### Current State Analysis

**Existing Tests (13 total):**
- ✅ AnthropicContentConverter: 13 tests (basic ToAnthropicContent/FromAnthropicContent)
  - Text content conversion
  - Image content conversion
  - Tool use/result content conversion
  - Empty/null handling
  - Unsupported media types

**Missing Test Coverage:**
- ❌ AnthropicChatClient: 0 tests (0% coverage)
- ❌ AnthropicMessageConverter: 0 tests (0% coverage)
- ❌ AnthropicToolConverter: 0 tests (0% coverage)
- ❌ AnthropicOptionsConverter: 0 tests (0% coverage)
- ❌ AnthropicStreamingConverter: 0 tests (0% coverage)
- ❌ DI Extensions: 0 tests (0% coverage)
- ❌ Integration tests: 0 tests
- ❌ Performance tests: 0 tests

**Critical Gaps:**
1. No tests for the main `AnthropicChatClient` entry point
2. No streaming tests (critical for production use)
3. No Azure Foundry authentication tests
4. No error handling/exception mapping tests
5. No concurrent request tests
6. No performance baseline tests

---

## Test Infrastructure Requirements

### Test Framework Setup

```xml
<!-- Already configured in .csproj -->
<PackageReference Include="xunit" Version="2.9.2" />
<PackageReference Include="FluentAssertions" Version="7.0.0" />
<PackageReference Include="Moq" Version="4.20.72" />
<PackageReference Include="coverlet.collector" Version="6.0.0" />
```

### Additional Dependencies Needed

```xml
<!-- For integration tests -->
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
<PackageReference Include="Testcontainers" Version="3.10.0" /> <!-- If needed for mocking Azure -->

<!-- For performance tests -->
<PackageReference Include="BenchmarkDotNet" Version="0.13.12" />
<PackageReference Include="NBomber" Version="5.8.2" /> <!-- Load testing -->

<!-- For async testing -->
<PackageReference Include="AsyncFixer" Version="1.6.0" /> <!-- Analyzer for async issues -->
```

### Mock Strategy

**IAnthropicClient Mocking:**
- Use Moq to mock `IAnthropicClient` interface
- Use Moq to mock `IMessageService` interface
- Create test builders for Anthropic SDK types (Message, MessageParam, ContentBlock, etc.)

**Test Data Builders:**
```csharp
// Create builders for complex Anthropic types
public class MessageBuilder { ... }
public class MessageParamBuilder { ... }
public class ContentBlockBuilder { ... }
public class RawMessageStreamEventBuilder { ... }
```

**Fake Implementations:**
```csharp
// For integration tests
public class FakeAnthropicClient : IAnthropicClient { ... }
public class FakeMessageService : IMessageService { ... }
```

---

## Phase 1: Unit Tests (Priority 1)

### 1.1 AnthropicContentConverter Tests (Expand Existing)

**Current:** 13 tests ✅
**Target:** 35 tests
**Additional Tests Needed:** 22

#### 1.1.1 ToAnthropicContent - Additional Scenarios (12 tests)

| Test # | Test Name | Scenario | Priority |
|--------|-----------|----------|----------|
| 14 | `ToAnthropicContent_MixedContentTypes_CreatesCorrectBlocks` | Text + Image + Tool call | High |
| 15 | `ToAnthropicContent_MultipleImages_AllConverted` | Multiple images in sequence | High |
| 16 | `ToAnthropicContent_ImageJpeg_CreatesCorrectMediaType` | JPEG image handling | Medium |
| 17 | `ToAnthropicContent_ImageWebp_CreatesCorrectMediaType` | WebP image handling | Medium |
| 18 | `ToAnthropicContent_ImageGif_CreatesCorrectMediaType` | GIF image handling | Low |
| 19 | `ToAnthropicContent_LargeBase64Image_HandlesCorrectly` | Large image data (>1MB) | Medium |
| 20 | `ToAnthropicContent_ToolResultWithJson_ParsesCorrectly` | JSON result content | High |
| 21 | `ToAnthropicContent_ToolResultWithPlainText_HandlesCorrectly` | Plain text result | High |
| 22 | `ToAnthropicContent_ToolResultWithEmptyContent_CreatesBlock` | Empty result handling | Medium |
| 23 | `ToAnthropicContent_FunctionCallWithComplexArgs_SerializesCorrectly` | Nested arguments | High |
| 24 | `ToAnthropicContent_FunctionCallWithNullArgs_HandlesGracefully` | Null arguments | Medium |
| 25 | `ToAnthropicContent_WhitespaceOnlyText_IsSkipped` | Whitespace filtering | Low |

#### 1.1.2 FromAnthropicContent - Additional Scenarios (10 tests)

| Test # | Test Name | Scenario | Priority |
|--------|-----------|----------|----------|
| 26 | `FromAnthropicContent_TextBlockWithCitations_ExtractsText` | Text with citations | Medium |
| 27 | `FromAnthropicContent_ToolUseWithComplexInput_DeserializesCorrectly` | Complex tool input | High |
| 28 | `FromAnthropicContent_ToolUseWithEmptyInput_HandlesGracefully` | Empty tool input | Medium |
| 29 | `FromAnthropicContent_UnknownBlockType_ReturnsNull` | Future block types | High |
| 30 | `FromAnthropicContent_MultipleBlockTypes_ConvertsAll` | Mixed content | Medium |
| 31 | `FromAnthropicContent_TextBlock_PreservesNewlines` | Text formatting | Low |
| 32 | `FromAnthropicContent_ToolUseBlock_PreservesAllProperties` | Complete property mapping | Medium |
| 33 | `FromAnthropicContent_ToolUseWithJsonElementArgs_ConvertsToObject` | JsonElement handling | High |
| 34 | `FromAnthropicContent_LargeTextBlock_HandlesEfficiently` | Performance check | Low |
| 35 | `FromAnthropicContent_InvalidJsonInToolUse_HandlesGracefully` | Error handling | High |

**Estimated Tests for AnthropicContentConverter:** 35 total

---

### 1.2 AnthropicMessageConverter Tests (NEW)

**Current:** 0 tests ❌
**Target:** 45 tests
**Priority:** CRITICAL (core message handling)

#### 1.2.1 ToAnthropicMessages - Basic Conversion (15 tests)

| Test # | Test Name | Scenario | Priority |
|--------|-----------|----------|----------|
| 1 | `ToAnthropicMessages_SingleUserMessage_CreatesUserMessage` | Basic user message | Critical |
| 2 | `ToAnthropicMessages_UserAssistantPair_CreatesAlternatingMessages` | Basic conversation | Critical |
| 3 | `ToAnthropicMessages_SystemMessage_ExtractsToSystemPrompt` | System message extraction | Critical |
| 4 | `ToAnthropicMessages_MultipleSystemMessages_CombinesWithNewlines` | Multiple system messages | High |
| 5 | `ToAnthropicMessages_SystemAtEnd_StillExtracted` | System position independence | High |
| 6 | `ToAnthropicMessages_MixedSystemAndUser_ExtractsAndFilters` | System + user messages | High |
| 7 | `ToAnthropicMessages_ToolRole_ConvertsToUserMessage` | Tool role mapping | High |
| 8 | `ToAnthropicMessages_EmptyMessageList_ThrowsArgumentException` | Empty validation | Critical |
| 9 | `ToAnthropicMessages_OnlySystemMessages_ThrowsArgumentException` | System-only validation | Critical |
| 10 | `ToAnthropicMessages_NullMessages_ThrowsArgumentNullException` | Null validation | Critical |
| 11 | `ToAnthropicMessages_FirstMessageAssistant_ThrowsArgumentException` | First message validation | Critical |
| 12 | `ToAnthropicMessages_ConsecutiveUserMessages_ThrowsArgumentException` | Alternation validation | Critical |
| 13 | `ToAnthropicMessages_ConsecutiveAssistantMessages_ThrowsArgumentException` | Alternation validation | Critical |
| 14 | `ToAnthropicMessages_SystemMessageWithEmptyText_SkipsInPrompt` | Empty system handling | Medium |
| 15 | `ToAnthropicMessages_LongConversation_HandlesCorrectly` | 50+ message conversation | Medium |

#### 1.2.2 ToAnthropicMessages - Complex Content (10 tests)

| Test # | Test Name | Scenario | Priority |
|--------|-----------|----------|----------|
| 16 | `ToAnthropicMessages_MessageWithMultipleContents_ConvertsAll` | Multi-content message | High |
| 17 | `ToAnthropicMessages_MessageWithImageContent_IncludesImage` | Image in message | High |
| 18 | `ToAnthropicMessages_MessageWithToolCall_CreatesToolUseBlock` | Tool call message | High |
| 19 | `ToAnthropicMessages_MessageWithToolResult_CreatesToolResultBlock` | Tool result message | High |
| 20 | `ToAnthropicMessages_MessageWithMixedContent_PreservesOrder` | Content ordering | Medium |
| 21 | `ToAnthropicMessages_EmptyContentList_SkipsMessage` | Empty content handling | Low |
| 22 | `ToAnthropicMessages_MessageWithUsageContent_ExcludesUsage` | Usage filtering | Medium |
| 23 | `ToAnthropicMessages_UnsupportedRole_ThrowsArgumentException` | Role validation | High |
| 24 | `ToAnthropicMessages_MessageWithAdditionalProperties_Preserves` | Metadata preservation | Low |
| 25 | `ToAnthropicMessages_LargeMessageContent_HandlesEfficiently` | Performance | Low |

#### 1.2.3 FromAnthropicMessage - Response Conversion (20 tests)

| Test # | Test Name | Scenario | Priority |
|--------|-----------|----------|----------|
| 26 | `FromAnthropicMessage_SimpleTextResponse_CreatesTextContent` | Basic text response | Critical |
| 27 | `FromAnthropicMessage_MultipleContentBlocks_ConvertsAll` | Multi-block response | High |
| 28 | `FromAnthropicMessage_ToolUseResponse_CreatesFunctionCallContent` | Tool use response | High |
| 29 | `FromAnthropicMessage_WithUsage_CreatesUsageContent` | Usage tracking | High |
| 30 | `FromAnthropicMessage_WithMessageId_SetsAdditionalProperty` | ID preservation | Medium |
| 31 | `FromAnthropicMessage_WithModel_SetsModelId` | Model tracking | Medium |
| 32 | `FromAnthropicMessage_StopReasonEndTurn_MapsToStop` | Stop reason mapping | High |
| 33 | `FromAnthropicMessage_StopReasonMaxTokens_MapsToLength` | Length stop reason | High |
| 34 | `FromAnthropicMessage_StopReasonToolUse_MapsToToolCalls` | Tool stop reason | High |
| 35 | `FromAnthropicMessage_StopReasonStopSequence_MapsToStop` | Sequence stop reason | Medium |
| 36 | `FromAnthropicMessage_NullStopReason_SetsNullFinishReason` | Null stop handling | Medium |
| 37 | `FromAnthropicMessage_EmptyContentList_CreatesEmptyContents` | Empty response | Medium |
| 38 | `FromAnthropicMessage_NullMessage_ThrowsArgumentNullException` | Null validation | Critical |
| 39 | `FromAnthropicMessage_AssistantRole_SetsAssistantRole` | Role mapping | High |
| 40 | `FromAnthropicMessage_WithMetadata_PopulatesResponse` | Metadata handling | Low |
| 41 | `FromAnthropicMessage_LargeResponse_HandlesEfficiently` | Performance | Low |
| 42 | `FromAnthropicMessage_MultipleToolUses_CreatesMultipleFunctionCalls` | Multiple tools | High |
| 43 | `FromAnthropicMessage_MixedTextAndToolUse_PreservesOrder` | Content ordering | Medium |
| 44 | `FromAnthropicMessage_UnknownRole_DefaultsToAssistant` | Role fallback | Medium |
| 45 | `FromAnthropicMessage_WithCitations_IncludesInTextContent` | Citation handling | Low |

**Estimated Tests for AnthropicMessageConverter:** 45 total

---

### 1.3 AnthropicToolConverter Tests (NEW)

**Current:** 0 tests ❌
**Target:** 25 tests
**Priority:** HIGH (tool calling is core feature)

#### 1.3.1 ToAnthropicTools - Tool Definition Conversion (15 tests)

| Test # | Test Name | Scenario | Priority |
|--------|-----------|----------|----------|
| 1 | `ToAnthropicTools_SingleFunction_CreatesSingleTool` | Basic function | Critical |
| 2 | `ToAnthropicTools_MultipleFunctions_CreatesMultipleTools` | Multiple tools | Critical |
| 3 | `ToAnthropicTools_FunctionWithDescription_IncludesDescription` | Description mapping | High |
| 4 | `ToAnthropicTools_FunctionWithoutDescription_UsesEmptyString` | Missing description | High |
| 5 | `ToAnthropicTools_FunctionWithJsonSchema_CreatesInputSchema` | Schema conversion | Critical |
| 6 | `ToAnthropicTools_FunctionWithRequiredParams_MarksInSchema` | Required parameters | High |
| 7 | `ToAnthropicTools_FunctionWithOptionalParams_CorrectlyMarked` | Optional parameters | High |
| 8 | `ToAnthropicTools_FunctionWithComplexSchema_HandlesCorrectly` | Nested schema | High |
| 9 | `ToAnthropicTools_FunctionWithArrayParams_HandlesCorrectly` | Array parameters | Medium |
| 10 | `ToAnthropicTools_FunctionWithObjectParams_HandlesCorrectly` | Object parameters | Medium |
| 11 | `ToAnthropicTools_FunctionWithoutName_ThrowsArgumentException` | Name validation | Critical |
| 12 | `ToAnthropicTools_NullToolsList_ThrowsArgumentNullException` | Null validation | Critical |
| 13 | `ToAnthropicTools_EmptyToolsList_ReturnsEmptyList` | Empty handling | Medium |
| 14 | `ToAnthropicTools_NonAIFunctionTool_SkipsWithWarning` | Unsupported tool types | Low |
| 15 | `ToAnthropicTools_FunctionWithSpecialCharsInName_HandlesCorrectly` | Name edge cases | Low |

#### 1.3.2 Schema Conversion Tests (10 tests)

| Test # | Test Name | Scenario | Priority |
|--------|-----------|----------|----------|
| 16 | `ConvertJsonSchemaToInputSchema_ObjectSchema_CreatesCorrectDict` | Object schema | High |
| 17 | `ConvertJsonSchemaToInputSchema_WithProperties_IncludesAll` | Property enumeration | High |
| 18 | `ConvertJsonSchemaToInputSchema_WithEnums_PreservesEnumValues` | Enum handling | Medium |
| 19 | `ConvertJsonSchemaToInputSchema_NonObjectSchema_CreatesDefaultObject` | Non-object schema | Medium |
| 20 | `ConvertJsonSchemaToInputSchema_EmptySchema_CreatesValidObject` | Empty schema | Medium |
| 21 | `ConvertJsonSchemaToInputSchema_WithNestedObjects_HandlesRecursion` | Nested objects | Medium |
| 22 | `ConvertJsonSchemaToInputSchema_WithReferences_ResolvesCorrectly` | Schema references | Low |
| 23 | `ConvertJsonSchemaToInputSchema_WithDefaultValues_Preserves` | Default values | Low |
| 24 | `ConvertJsonSchemaToInputSchema_WithConstraints_Includes` | Constraints (min/max) | Low |
| 25 | `ConvertJsonSchemaToInputSchema_LargeSchema_HandlesEfficiently` | Performance | Low |

**Estimated Tests for AnthropicToolConverter:** 25 total

---

### 1.4 AnthropicOptionsConverter Tests (NEW)

**Current:** 0 tests ❌
**Target:** 30 tests
**Priority:** HIGH (options control behavior)

#### 1.4.1 ToMessageCreateParams - Basic Options (12 tests)

| Test # | Test Name | Scenario | Priority |
|--------|-----------|----------|----------|
| 1 | `ToMessageCreateParams_WithModelId_SetsModel` | Model ID setting | Critical |
| 2 | `ToMessageCreateParams_NoModelId_ThrowsInvalidOperationException` | Model validation | Critical |
| 3 | `ToMessageCreateParams_WithTemperature_SetsTemperature` | Temperature parameter | High |
| 4 | `ToMessageCreateParams_TemperatureOutOfRange_ThrowsArgumentOutOfRange` | Temperature validation | High |
| 5 | `ToMessageCreateParams_WithTopP_SetsTopP` | TopP parameter | High |
| 6 | `ToMessageCreateParams_TopPOutOfRange_ThrowsArgumentOutOfRange` | TopP validation | High |
| 7 | `ToMessageCreateParams_WithMaxTokens_SetsMaxTokens` | MaxTokens parameter | High |
| 8 | `ToMessageCreateParams_NoMaxTokens_UsesDefault4096` | Default MaxTokens | High |
| 9 | `ToMessageCreateParams_WithStopSequences_SetsStopSequences` | Stop sequences | Medium |
| 10 | `ToMessageCreateParams_EmptyStopSequences_DoesNotSet` | Empty stop handling | Low |
| 11 | `ToMessageCreateParams_WithSystemPrompt_SetsSystemParameter` | System prompt | Critical |
| 12 | `ToMessageCreateParams_NullSystemPrompt_DoesNotSetSystem` | Null system | Medium |

#### 1.4.2 ToMessageCreateParams - Advanced Options (10 tests)

| Test # | Test Name | Scenario | Priority |
|--------|-----------|----------|----------|
| 13 | `ToMessageCreateParams_WithTopK_SetsTopK` | TopK (Anthropic specific) | High |
| 14 | `ToMessageCreateParams_TopKNegative_ThrowsArgumentOutOfRange` | TopK validation | Medium |
| 15 | `ToMessageCreateParams_WithMetadataUserId_SetsMetadata` | Metadata parameter | Medium |
| 16 | `ToMessageCreateParams_NoMetadata_DoesNotSetMetadata` | Null metadata | Low |
| 17 | `ToMessageCreateParams_WithAllOptions_SetsAllParameters` | Complete options | High |
| 18 | `ToMessageCreateParams_WithNullOptions_UsesDefaults` | Null options handling | High |
| 19 | `ToMessageCreateParams_DefaultModelIdTakesPrecedence_WhenNoOptionModelId` | Model precedence | High |
| 20 | `ToMessageCreateParams_OptionModelIdOverridesDefault` | Model override | High |
| 21 | `ToMessageCreateParams_EmptyMessages_StillCreatesParams` | Empty messages | Medium |
| 22 | `ToMessageCreateParams_WithAdditionalProperties_IgnoresUnknown` | Unknown properties | Low |

#### 1.4.3 Tool Mode Conversion (8 tests)

| Test # | Test Name | Scenario | Priority |
|--------|-----------|----------|----------|
| 23 | `ConvertToolMode_AutoMode_CreatesToolChoiceAuto` | Auto tool mode | Critical |
| 24 | `ConvertToolMode_RequiredMode_CreatesToolChoiceAny` | Required tool mode | Critical |
| 25 | `ConvertToolMode_SpecificFunction_CreatesToolChoiceTool` | Specific function | High |
| 26 | `ConvertToolMode_NullMode_ReturnsNull` | Null tool mode | Medium |
| 27 | `ConvertToolMode_UnknownMode_DefaultsToAuto` | Unknown mode fallback | Medium |
| 28 | `ToMessageCreateParams_WithTools_SetsToolsParameter` | Tools parameter | High |
| 29 | `ToMessageCreateParams_WithToolsAndMode_SetsBoth` | Tools + mode | High |
| 30 | `ToMessageCreateParams_NoTools_DoesNotSetToolChoice` | No tools handling | Medium |

**Estimated Tests for AnthropicOptionsConverter:** 30 total

---

### 1.5 AnthropicStreamingConverter Tests (NEW)

**Current:** 0 tests ❌
**Target:** 40 tests
**Priority:** CRITICAL (streaming is production-critical)

#### 1.5.1 Event Processing - Basic Flow (15 tests)

| Test # | Test Name | Scenario | Priority |
|--------|-----------|----------|----------|
| 1 | `ConvertStreamAsync_MessageStart_InitializesState` | Message start event | Critical |
| 2 | `ConvertStreamAsync_ContentBlockStart_Text_CreatesTextAccumulator` | Text block start | Critical |
| 3 | `ConvertStreamAsync_ContentBlockStart_ToolUse_CreatesToolAccumulator` | Tool block start | Critical |
| 4 | `ConvertStreamAsync_ContentBlockDelta_Text_YieldsUpdate` | Text delta | Critical |
| 5 | `ConvertStreamAsync_ContentBlockDelta_ToolInput_Accumulates` | Tool input delta | Critical |
| 6 | `ConvertStreamAsync_ContentBlockStop_Text_FinalizesBlock` | Text block stop | High |
| 7 | `ConvertStreamAsync_ContentBlockStop_ToolUse_YieldsFunctionCall` | Tool block stop | High |
| 8 | `ConvertStreamAsync_MessageDelta_UpdatesStopReason` | Message delta | High |
| 9 | `ConvertStreamAsync_MessageDelta_UpdatesUsage` | Usage update | High |
| 10 | `ConvertStreamAsync_MessageStop_YieldsFinalUpdate` | Message stop | Critical |
| 11 | `ConvertStreamAsync_CompleteFlow_YieldsAllUpdates` | Complete flow | Critical |
| 12 | `ConvertStreamAsync_EmptyStream_YieldsNothing` | Empty stream | Medium |
| 13 | `ConvertStreamAsync_UnknownEvent_LogsAndContinues` | Unknown event | Medium |
| 14 | `ConvertStreamAsync_CancellationToken_CancelsEnumeration` | Cancellation | High |
| 15 | `ConvertStreamAsync_NullMetadata_HandlesGracefully` | Null metadata | Low |

#### 1.5.2 State Machine Tests (15 tests)

| Test # | Test Name | Scenario | Priority |
|--------|-----------|----------|----------|
| 16 | `StreamingState_TextAccumulation_BuildsCorrectString` | Text accumulation | High |
| 17 | `StreamingState_MultipleTextDeltas_ConcatenatesCorrectly` | Multiple deltas | High |
| 18 | `StreamingState_ToolInputAccumulation_BuildsJson` | Tool input accumulation | High |
| 19 | `StreamingState_MultipleContentBlocks_TracksIndependently` | Multiple blocks | High |
| 20 | `StreamingState_BlockTypeTransition_ResetsState` | State reset | High |
| 21 | `StreamingState_HasPendingContent_TrueWhenText` | Pending text check | Medium |
| 22 | `StreamingState_HasPendingContent_TrueWhenToolId` | Pending tool check | Medium |
| 23 | `StreamingState_HasPendingContent_FalseWhenEmpty` | Empty check | Medium |
| 24 | `StreamingState_UsageTracking_AccumulatesCorrectly` | Usage tracking | Medium |
| 25 | `StreamingState_StopReasonTracking_PreservesLast` | Stop reason tracking | Medium |
| 26 | `StreamingState_MessageId_PreservedThroughout` | ID preservation | Low |
| 27 | `StreamingState_Model_PreservedThroughout` | Model preservation | Low |
| 28 | `StreamingState_CurrentBlockIndex_TracksCorrectly` | Index tracking | Low |
| 29 | `StreamingState_MultipleToolCalls_AllCaptured` | Multiple tools | High |
| 30 | `StreamingState_LargeTextAccumulation_HandlesEfficiently` | Performance | Low |

#### 1.5.3 Content Assembly Tests (10 tests)

| Test # | Test Name | Scenario | Priority |
|--------|-----------|----------|----------|
| 31 | `CreateFinalUpdate_WithText_IncludesTextContent` | Final text | High |
| 32 | `CreateFinalUpdate_WithUsage_IncludesUsageContent` | Final usage | High |
| 33 | `CreateFinalUpdate_WithFinishReason_SetsFinishReason` | Finish reason | High |
| 34 | `CreateFinalUpdate_EmptyState_CreatesMinimalUpdate` | Empty final | Medium |
| 35 | `ParseToolInput_ValidJson_DeserializesCorrectly` | JSON parsing | High |
| 36 | `ParseToolInput_InvalidJson_ReturnsEmptyDict` | Invalid JSON | High |
| 37 | `ParseToolInput_NullJson_ReturnsEmptyDict` | Null JSON | Medium |
| 38 | `ParseToolInput_EmptyString_ReturnsEmptyDict` | Empty string | Medium |
| 39 | `ParseToolInput_ComplexNestedJson_DeserializesCorrectly` | Complex JSON | Medium |
| 40 | `ConvertStreamAsync_RealWorldScenario_WorksEndToEnd` | E2E streaming | Critical |

**Estimated Tests for AnthropicStreamingConverter:** 40 total

---

### 1.6 AnthropicChatClient Tests (NEW)

**Current:** 0 tests ❌
**Target:** 40 tests
**Priority:** CRITICAL (main entry point)

#### 1.6.1 Constructor and Initialization (10 tests)

| Test # | Test Name | Scenario | Priority |
|--------|-----------|----------|----------|
| 1 | `Constructor_WithAnthropicClient_Initializes` | Basic construction | Critical |
| 2 | `Constructor_WithFoundryClient_DetectsAzure` | Azure detection | Critical |
| 3 | `Constructor_WithModelId_SetsDefaultModel` | Model ID setting | High |
| 4 | `Constructor_WithoutModelId_AllowsNullDefault` | No default model | High |
| 5 | `Constructor_NullClient_ThrowsArgumentNullException` | Null validation | Critical |
| 6 | `Constructor_WithMessageService_Initializes` | Service construction | Medium |
| 7 | `Constructor_ExtractsEndpoint_SetsMetadata` | Endpoint extraction | Medium |
| 8 | `Metadata_ReturnsCorrectProviderName_ForStandardClient` | Standard metadata | High |
| 9 | `Metadata_ReturnsCorrectProviderName_ForFoundryClient` | Foundry metadata | High |
| 10 | `Metadata_IncludesDefaultModelId_WhenProvided` | Metadata model | Medium |

#### 1.6.2 GetResponseAsync Tests (15 tests)

| Test # | Test Name | Scenario | Priority |
|--------|-----------|----------|----------|
| 11 | `GetResponseAsync_SimpleUserMessage_ReturnsResponse` | Basic request | Critical |
| 12 | `GetResponseAsync_WithModelInOptions_UsesOptionModel` | Model override | High |
| 13 | `GetResponseAsync_WithDefaultModel_UsesDefaultModel` | Default model | High |
| 14 | `GetResponseAsync_NoModel_ThrowsInvalidOperationException` | Model validation | Critical |
| 15 | `GetResponseAsync_NullMessages_ThrowsArgumentNullException` | Null validation | Critical |
| 16 | `GetResponseAsync_WithSystemMessage_ExtractsSystemPrompt` | System handling | High |
| 17 | `GetResponseAsync_WithOptions_AppliesAllOptions` | Options application | High |
| 18 | `GetResponseAsync_WithTools_IncludesInRequest` | Tools handling | High |
| 19 | `GetResponseAsync_WithCancellation_PropagatesCancellation` | Cancellation | High |
| 20 | `GetResponseAsync_ApiError_ThrowsException` | Error handling | High |
| 21 | `GetResponseAsync_ReturnsUsage_InResponse` | Usage tracking | High |
| 22 | `GetResponseAsync_ReturnsFinishReason_InResponse` | Finish reason | High |
| 23 | `GetResponseAsync_WithImage_SendsImageContent` | Image support | Medium |
| 24 | `GetResponseAsync_WithToolCall_ReturnsFunctionCallContent` | Tool response | High |
| 25 | `GetResponseAsync_LongConversation_HandlesCorrectly` | Long conversation | Medium |

#### 1.6.3 GetStreamingResponseAsync Tests (10 tests)

| Test # | Test Name | Scenario | Priority |
|--------|-----------|----------|----------|
| 26 | `GetStreamingResponseAsync_SimpleMessage_YieldsUpdates` | Basic streaming | Critical |
| 27 | `GetStreamingResponseAsync_NullMessages_ThrowsArgumentNullException` | Null validation | Critical |
| 28 | `GetStreamingResponseAsync_NoModel_ThrowsInvalidOperationException` | Model validation | Critical |
| 29 | `GetStreamingResponseAsync_WithCancellation_Cancels` | Cancellation | High |
| 30 | `GetStreamingResponseAsync_TextResponse_YieldsIncrementalUpdates` | Text streaming | Critical |
| 31 | `GetStreamingResponseAsync_ToolUse_YieldsFunctionCall` | Tool streaming | High |
| 32 | `GetStreamingResponseAsync_FinalUpdate_IncludesUsage` | Final usage | High |
| 33 | `GetStreamingResponseAsync_ApiError_ThrowsException` | Error handling | High |
| 34 | `GetStreamingResponseAsync_EmptyResponse_CompletesGracefully` | Empty response | Medium |
| 35 | `GetStreamingResponseAsync_LargeResponse_StreamsEfficiently` | Performance | Medium |

#### 1.6.4 Service Methods (5 tests)

| Test # | Test Name | Scenario | Priority |
|--------|-----------|----------|----------|
| 36 | `GetService_IChatClient_ReturnsSelf` | Service resolution | High |
| 37 | `GetService_AnthropicClient_ReturnsUnderlyingClient` | Client access | High |
| 38 | `GetService_MessageService_ReturnsService` | Service access | Medium |
| 39 | `GetService_UnknownType_ReturnsNull` | Unknown type | Medium |
| 40 | `Dispose_DisposesUnderlyingClient` | Disposal | High |

**Estimated Tests for AnthropicChatClient:** 40 total

---

### 1.7 DI Extensions Tests (NEW)

**Current:** 0 tests ❌
**Target:** 25 tests
**Priority:** HIGH (DI is common use case)

#### 1.7.1 Standard Anthropic Extensions (10 tests)

| Test # | Test Name | Scenario | Priority |
|--------|-----------|----------|----------|
| 1 | `AddAnthropicChatClient_WithApiKey_RegistersClient` | Basic registration | Critical |
| 2 | `AddAnthropicChatClient_FromEnvironment_UsesEnvVar` | Environment var | High |
| 3 | `AddAnthropicChatClient_NoApiKey_ThrowsArgumentException` | API key validation | Critical |
| 4 | `AddAnthropicChatClient_WithClientOptions_RegistersClient` | ClientOptions overload | High |
| 5 | `AddAnthropicChatClient_WithClientInstance_RegistersClient` | Client instance overload | High |
| 6 | `AddAnthropicChatClient_WithFactory_RegistersClient` | Factory overload | High |
| 7 | `AddAnthropicChatClient_ResolvesIChatClient_Successfully` | Resolution test | Critical |
| 8 | `AddAnthropicChatClient_NullServices_ThrowsArgumentNullException` | Null validation | High |
| 9 | `AddAnthropicChatClient_WithModelId_SetsDefaultModel` | Model ID setting | High |
| 10 | `AddAnthropicChatClient_RegisteredAsSingleton_ReturnsSameInstance` | Singleton scope | Medium |

#### 1.7.2 Azure Foundry Extensions (15 tests)

| Test # | Test Name | Scenario | Priority |
|--------|-----------|----------|----------|
| 11 | `AddAnthropicFoundryChatClient_FromEnvironment_RegistersClient` | Environment config | Critical |
| 12 | `AddAnthropicFoundryChatClient_NoEnvironment_ThrowsException` | Environment validation | Critical |
| 13 | `AddAnthropicFoundryChatClient_WithCredentials_RegistersClient` | Credentials overload | Critical |
| 14 | `AddAnthropicFoundryChatClient_WithApiKey_RegistersClient` | API key overload | High |
| 15 | `AddAnthropicFoundryChatClient_WithFactory_RegistersClient` | Factory overload | High |
| 16 | `AddAnthropicFoundryChatClient_ResolvesIChatClient_Successfully` | Resolution test | Critical |
| 17 | `AddAnthropicFoundryChatClient_NullServices_ThrowsArgumentNullException` | Null validation | High |
| 18 | `AddAnthropicFoundryChatClient_NullCredentials_ThrowsArgumentNullException` | Credentials validation | High |
| 19 | `AddAnthropicFoundryChatClient_EmptyResourceName_ThrowsArgumentException` | Resource validation | High |
| 20 | `AddAnthropicFoundryChatClient_EmptyApiKey_ThrowsArgumentException` | API key validation | High |
| 21 | `AddAnthropicFoundryChatClient_WithModelId_SetsDefaultModel` | Model ID setting | High |
| 22 | `AddAnthropicFoundryChatClient_RegisteredAsSingleton_ReturnsSameInstance` | Singleton scope | Medium |
| 23 | `AddAnthropicFoundryChatClient_AzureIdentity_WorksCorrectly` | Azure Identity auth | High |
| 24 | `AddAnthropicFoundryChatClient_BearerToken_WorksCorrectly` | Bearer token auth | Medium |
| 25 | `AddAnthropicFoundryChatClient_ResourceName_ConfiguredCorrectly` | Resource configuration | High |

**Estimated Tests for DI Extensions:** 25 total

---

## Phase 2: Integration Tests (Priority 2)

### 2.1 Live API Integration Tests

**Target:** 20 tests
**Priority:** HIGH
**Requirements:** Real Anthropic API keys and Azure credentials

**Test Categories:**

#### 2.1.1 Standard Anthropic API Tests (10 tests)

| Test # | Test Name | Scenario | Environment Var |
|--------|-----------|----------|-----------------|
| 1 | `Integration_RealApi_SimpleChat_ReturnsResponse` | Basic chat | ANTHROPIC_API_KEY |
| 2 | `Integration_RealApi_StreamingChat_ReturnsUpdates` | Streaming | ANTHROPIC_API_KEY |
| 3 | `Integration_RealApi_WithImage_ProcessesImage` | Vision | ANTHROPIC_API_KEY |
| 4 | `Integration_RealApi_WithTools_CallsTools` | Tool use | ANTHROPIC_API_KEY |
| 5 | `Integration_RealApi_LongConversation_Maintains Context` | Multi-turn | ANTHROPIC_API_KEY |
| 6 | `Integration_RealApi_WithTemperature_AppliesParameter` | Parameters | ANTHROPIC_API_KEY |
| 7 | `Integration_RealApi_WithStopSequence_Stops` | Stop sequences | ANTHROPIC_API_KEY |
| 8 | `Integration_RealApi_InvalidModel_ThrowsException` | Error handling | ANTHROPIC_API_KEY |
| 9 | `Integration_RealApi_RateLimiting_HandlesGracefully` | Rate limits | ANTHROPIC_API_KEY |
| 10 | `Integration_RealApi_TokenUsage_ReportsCorrectly` | Usage tracking | ANTHROPIC_API_KEY |

#### 2.1.2 Azure Foundry API Tests (10 tests)

| Test # | Test Name | Scenario | Environment Vars |
|--------|-----------|----------|------------------|
| 11 | `Integration_AzureFoundry_ApiKey_Authenticates` | API key auth | FOUNDRY_RESOURCE, FOUNDRY_KEY |
| 12 | `Integration_AzureFoundry_AzureIdentity_Authenticates` | Azure Identity | FOUNDRY_RESOURCE |
| 13 | `Integration_AzureFoundry_SimpleChat_ReturnsResponse` | Basic chat | FOUNDRY_RESOURCE |
| 14 | `Integration_AzureFoundry_StreamingChat_ReturnsUpdates` | Streaming | FOUNDRY_RESOURCE |
| 15 | `Integration_AzureFoundry_WithTools_CallsTools` | Tool use | FOUNDRY_RESOURCE |
| 16 | `Integration_AzureFoundry_ManagedIdentity_Authenticates` | Managed Identity | FOUNDRY_RESOURCE |
| 17 | `Integration_AzureFoundry_InvalidResource_ThrowsException` | Error handling | Invalid resource |
| 18 | `Integration_AzureFoundry_Endpoint_CorrectlyFormatted` | Endpoint check | FOUNDRY_RESOURCE |
| 19 | `Integration_AzureFoundry_Metadata_IncludesAzureInfo` | Metadata | FOUNDRY_RESOURCE |
| 20 | `Integration_AzureFoundry_TokenUsage_ReportsCorrectly` | Usage tracking | FOUNDRY_RESOURCE |

**Implementation Notes:**
- Use `[Fact(Skip = "Requires API key")]` for tests that need credentials
- Create base class `IntegrationTestBase` with setup/teardown
- Use `IClassFixture` for shared client initialization
- Add retry logic for flaky API calls
- Use small models (haiku) to minimize costs

**Test Infrastructure:**
```csharp
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected IChatClient? Client { get; private set; }
    protected bool ShouldSkip => string.IsNullOrEmpty(GetApiKey());

    protected abstract string GetApiKey();
    protected abstract IChatClient CreateClient();

    public async Task InitializeAsync()
    {
        if (!ShouldSkip)
        {
            Client = CreateClient();
        }
    }

    public Task DisposeAsync()
    {
        (Client as IDisposable)?.Dispose();
        return Task.CompletedTask;
    }
}

public class StandardAnthropicIntegrationTests : IntegrationTestBase
{
    protected override string GetApiKey() =>
        Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ?? string.Empty;

    protected override IChatClient CreateClient() =>
        new AnthropicChatClient(
            new AnthropicClient(new ClientOptions { APIKey = GetApiKey() }),
            "claude-haiku-4");
}
```

---

## Phase 3: Performance Tests (Priority 3)

### 3.1 Benchmarks (BenchmarkDotNet)

**Target:** 15 benchmarks
**Priority:** MEDIUM
**Tool:** BenchmarkDotNet

#### 3.1.1 Conversion Performance (8 benchmarks)

| Benchmark # | Name | Scenario | Target |
|-------------|------|----------|--------|
| 1 | `ToAnthropicContent_TextOnly_Benchmark` | Text conversion | < 1μs |
| 2 | `ToAnthropicContent_WithImage_Benchmark` | Image conversion | < 100μs |
| 3 | `ToAnthropicContent_MixedContent_Benchmark` | Mixed content | < 10μs |
| 4 | `ToAnthropicMessages_Conversation_Benchmark` | Message conversion | < 50μs |
| 5 | `ToAnthropicTools_LargeSchema_Benchmark` | Tool conversion | < 20μs |
| 6 | `FromAnthropicMessage_Response_Benchmark` | Response conversion | < 10μs |
| 7 | `StreamingConverter_EventProcessing_Benchmark` | Event processing | < 10μs/event |
| 8 | `OptionsConverter_AllOptions_Benchmark` | Options conversion | < 5μs |

#### 3.1.2 Memory Allocation (5 benchmarks)

| Benchmark # | Name | Scenario | Target |
|-------------|------|----------|--------|
| 9 | `ToAnthropicContent_AllocationTest` | Content allocation | < 1KB |
| 10 | `StreamingConverter_AllocationTest` | Streaming allocation | < 5KB |
| 11 | `MessageConverter_LargeConvo_AllocationTest` | Large conversation | < 50KB |
| 12 | `StringBuilder_TextAccumulation_Test` | Text accumulation | Minimal GC |
| 13 | `ChatClient_RequestCycle_AllocationTest` | Full request cycle | < 20KB |

#### 3.1.3 Throughput (2 benchmarks)

| Benchmark # | Name | Scenario | Target |
|-------------|------|----------|--------|
| 14 | `ChatClient_RequestsPerSecond_Benchmark` | Request throughput | 100+ req/s |
| 15 | `StreamingConverter_EventsPerSecond_Benchmark` | Event throughput | 10K+ events/s |

**Benchmark Setup:**
```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class ConversionBenchmarks
{
    private List<AIContent> _testContent = null!;
    private List<ChatMessage> _testMessages = null!;

    [GlobalSetup]
    public void Setup()
    {
        _testContent = new List<AIContent>
        {
            new TextContent("Hello"),
            new DataContent(new byte[1024], "image/png")
        };

        _testMessages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "Hello"),
            new ChatMessage(ChatRole.Assistant, "Hi there!")
        };
    }

    [Benchmark]
    public void ToAnthropicContent_Benchmark()
    {
        var result = AnthropicContentConverter.ToAnthropicContent(_testContent);
    }

    [Benchmark]
    public void ToAnthropicMessages_Benchmark()
    {
        var result = AnthropicMessageConverter.ToAnthropicMessages(_testMessages);
    }
}
```

---

### 3.2 Load Tests (NBomber)

**Target:** 5 load test scenarios
**Priority:** LOW (for production readiness)
**Tool:** NBomber

#### 3.2.1 Load Test Scenarios

| Test # | Name | Scenario | Target |
|--------|------|----------|--------|
| 1 | `LoadTest_ConcurrentRequests_100Users` | 100 concurrent users | No failures |
| 2 | `LoadTest_ConcurrentStreaming_50Users` | 50 concurrent streams | No deadlocks |
| 3 | `LoadTest_SustainedLoad_1Hour` | 1 hour sustained load | Stable memory |
| 4 | `LoadTest_BurstTraffic_SpikeTo500` | Spike to 500 users | Graceful degradation |
| 5 | `LoadTest_ToolCalling_MixedWorkload` | Mixed tool/text requests | Stable throughput |

**NBomber Setup:**
```csharp
var scenario = Scenario.Create("chat_load_test", async context =>
{
    var client = context.GlobalData["client"] as IChatClient;
    var messages = new[] { new ChatMessage(ChatRole.User, "Test message") };

    var response = await client.GetResponseAsync(messages);

    return response != null ? Response.Ok() : Response.Fail();
})
.WithLoadSimulations(
    Simulation.RampingConstant(100, TimeSpan.FromMinutes(5)),
    Simulation.KeepConstant(100, TimeSpan.FromMinutes(10))
);
```

---

## Test Execution Plan

### Priority 1: Unit Tests (Weeks 1-3)

**Week 1:**
- ✅ Complete AnthropicContentConverter tests (22 additional tests)
- ✅ Complete AnthropicMessageConverter tests (45 tests)
- Total: 67 tests

**Week 2:**
- ✅ Complete AnthropicToolConverter tests (25 tests)
- ✅ Complete AnthropicOptionsConverter tests (30 tests)
- Total: 55 tests

**Week 3:**
- ✅ Complete AnthropicStreamingConverter tests (40 tests)
- ✅ Complete AnthropicChatClient tests (40 tests)
- Total: 80 tests

**Week 4:**
- ✅ Complete DI Extensions tests (25 tests)
- ✅ Cleanup and refactoring
- Total: 25 tests

**Unit Test Total: 240 tests**

---

### Priority 2: Integration Tests (Week 5)

**Week 5:**
- ✅ Set up integration test infrastructure
- ✅ Implement Standard API integration tests (10 tests)
- ✅ Implement Azure Foundry integration tests (10 tests)
- Total: 20 tests

**Integration Test Total: 20 tests**

---

### Priority 3: Performance Tests (Week 6)

**Week 6:**
- ✅ Set up BenchmarkDotNet project
- ✅ Implement conversion benchmarks (8 benchmarks)
- ✅ Implement memory benchmarks (5 benchmarks)
- ✅ Implement throughput benchmarks (2 benchmarks)
- ✅ Set up NBomber load tests (5 scenarios)
- Total: 15 benchmarks + 5 load tests

**Performance Test Total: 20 tests**

---

## Coverage Analysis and Targets

### Current Coverage Estimate

Based on existing code structure:

| Component | Lines of Code | Current Tests | Current Coverage | Target Coverage | Tests Needed |
|-----------|---------------|---------------|------------------|-----------------|--------------|
| AnthropicContentConverter | ~280 | 13 | ~30% | 90% | +22 |
| AnthropicMessageConverter | ~260 | 0 | 0% | 90% | +45 |
| AnthropicToolConverter | ~115 | 0 | 0% | 90% | +25 |
| AnthropicOptionsConverter | ~220 | 0 | 0% | 90% | +30 |
| AnthropicStreamingConverter | ~410 | 0 | 0% | 90% | +40 |
| AnthropicChatClient | ~250 | 0 | 0% | 90% | +40 |
| DI Extensions (Standard) | ~160 | 0 | 0% | 85% | +10 |
| DI Extensions (Foundry) | ~175 | 0 | 0% | 85% | +15 |
| **TOTAL** | **~1,870** | **13** | **~5%** | **90%** | **+227** |

### Coverage Measurement Commands

```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Generate HTML report
dotnet tool install -g dotnet-reportgenerator-globaltool

reportgenerator \
  -reports:"coverage/**/coverage.cobertura.xml" \
  -targetdir:"coverage/report" \
  -reporttypes:"Html;Badges;TextSummary"

# View coverage
start coverage/report/index.html
```

### Coverage Quality Gates

```yaml
# CI/CD Pipeline Quality Gate
- script: |
    COVERAGE=$(grep -oP 'line-rate="\K[0-9.]+' coverage/coverage.cobertura.xml | head -1)
    COVERAGE_PERCENT=$(echo "$COVERAGE * 100" | bc)
    if (( $(echo "$COVERAGE_PERCENT < 90" | bc -l) )); then
      echo "##vso[task.logissue type=error]Coverage is below 90% ($COVERAGE_PERCENT%)"
      exit 1
    fi
    echo "Coverage: $COVERAGE_PERCENT%"
  displayName: 'Enforce 90% Coverage Threshold'
```

---

## Test Quality Standards

### Test Naming Convention

```
MethodName_Scenario_ExpectedBehavior

Examples:
✅ ToAnthropicContent_TextContent_CreatesTextBlock
✅ GetResponseAsync_WithModelInOptions_UsesOptionModel
✅ ConvertStreamAsync_MessageStart_InitializesState

❌ TestContentConversion (too vague)
❌ Test1 (meaningless)
❌ ItShouldWork (not descriptive)
```

### AAA Pattern (Arrange-Act-Assert)

```csharp
[Fact]
public void ToAnthropicContent_TextContent_CreatesTextBlock()
{
    // Arrange
    var contents = new List<AIContent>
    {
        new TextContent("Hello, Claude!")
    };

    // Act
    var result = AnthropicContentConverter.ToAnthropicContent(contents);

    // Assert
    result.Should().HaveCount(1);
    result[0].Should().NotBeNull();
}
```

### Fluent Assertions

```csharp
// Use FluentAssertions for better error messages
result.Should().NotBeNull();
result.Should().BeOfType<TextContent>();
result.Should().HaveCount(3);
result.Should().Contain(x => x.Text == "expected");
result.Should().AllSatisfy(x => x.Should().NotBeNull());

// Exceptions
var act = () => converter.Convert(null!);
act.Should().Throw<ArgumentNullException>()
   .WithParameterName("content");
```

### Async Testing

```csharp
[Fact]
public async Task GetResponseAsync_SimpleMessage_ReturnsResponse()
{
    // Arrange
    var mockService = new Mock<IMessageService>();
    mockService.Setup(s => s.Create(It.IsAny<MessageCreateParams>(), default))
               .ReturnsAsync(new Message { /* ... */ });

    var client = new AnthropicChatClient(mockService.Object, "test-model");

    // Act
    var response = await client.GetResponseAsync(
        new[] { new ChatMessage(ChatRole.User, "Hello") });

    // Assert
    response.Should().NotBeNull();
    mockService.Verify(s => s.Create(It.IsAny<MessageCreateParams>(), default), Times.Once);
}
```

### Test Data Builders

```csharp
public class MessageBuilder
{
    private string _id = "msg_123";
    private string _model = "claude-sonnet-4-5";
    private Role _role = Role.Assistant;
    private List<ContentBlock> _content = new();
    private MessageUsage? _usage = null;
    private StopReason? _stopReason = null;

    public MessageBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public MessageBuilder WithTextContent(string text)
    {
        _content.Add(new ContentBlock(new TextBlock { Text = text }));
        return this;
    }

    public MessageBuilder WithUsage(int inputTokens, int outputTokens)
    {
        _usage = new MessageUsage
        {
            InputTokens = inputTokens,
            OutputTokens = outputTokens
        };
        return this;
    }

    public Message Build() => new()
    {
        ID = _id,
        Model = _model,
        Role = _role,
        Content = _content,
        Usage = _usage,
        StopReason = _stopReason
    };
}

// Usage
var message = new MessageBuilder()
    .WithId("test_123")
    .WithTextContent("Hello!")
    .WithUsage(10, 20)
    .Build();
```

---

## Test Execution Strategy

### Local Development

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~AnthropicContentConverterTests"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run integration tests only
dotnet test --filter "Category=Integration"

# Run fast tests only (exclude integration/performance)
dotnet test --filter "Category!=Integration&Category!=Performance"
```

### CI/CD Pipeline

```yaml
# Azure DevOps Pipeline
stages:
  - stage: Test
    jobs:
      - job: UnitTests
        displayName: 'Unit Tests (Fast)'
        steps:
          - script: dotnet test --filter "Category!=Integration&Category!=Performance" --collect:"XPlat Code Coverage"
            displayName: 'Run Unit Tests'

          - script: |
              # Enforce 90% coverage
              COVERAGE=$(grep -oP 'line-rate="\K[0-9.]+' coverage/coverage.cobertura.xml)
              if (( $(echo "$COVERAGE < 0.90" | bc -l) )); then
                echo "Coverage below 90%"
                exit 1
              fi
            displayName: 'Coverage Gate'

      - job: IntegrationTests
        displayName: 'Integration Tests (Slow)'
        dependsOn: UnitTests
        condition: eq(variables['Build.SourceBranch'], 'refs/heads/main')
        steps:
          - script: dotnet test --filter "Category=Integration"
            displayName: 'Run Integration Tests'
            env:
              ANTHROPIC_API_KEY: $(AnthropicApiKey)
              ANTHROPIC_FOUNDRY_RESOURCE: $(FoundryResource)

      - job: PerformanceTests
        displayName: 'Performance Benchmarks'
        dependsOn: UnitTests
        condition: eq(variables['Build.SourceBranch'], 'refs/heads/main')
        steps:
          - script: dotnet run --project tests/Benchmarks --configuration Release
            displayName: 'Run Benchmarks'
```

---

## Risk Assessment

### High-Risk Areas (Need Extra Testing)

1. **Streaming State Machine (AnthropicStreamingConverter)**
   - Complex state transitions
   - Event ordering dependencies
   - Concurrent access potential
   - **Mitigation:** 40 dedicated tests, stress testing

2. **System Message Extraction (AnthropicMessageConverter)**
   - Critical for correct API behavior
   - Edge cases (empty, whitespace, multiple)
   - **Mitigation:** 15 dedicated tests covering all edge cases

3. **Tool Calling (AnthropicToolConverter + Content)**
   - JSON schema generation/parsing
   - Type conversions
   - **Mitigation:** 25 tool tests + 10 content tests

4. **Azure Authentication (Foundry Extensions)**
   - Multiple credential types
   - Environment variable handling
   - **Mitigation:** 15 DI tests + 10 integration tests

5. **Error Handling**
   - API errors
   - Network failures
   - Invalid inputs
   - **Mitigation:** Dedicated error tests in each component

### Low-Risk Areas (Standard Coverage)

1. **Simple Type Conversions**
   - Text content
   - Basic options
   - **Coverage:** Standard unit tests sufficient

2. **Metadata Handling**
   - Non-critical properties
   - **Coverage:** Basic validation tests

---

## Success Metrics

### Code Coverage Targets

| Metric | Current | Target | Status |
|--------|---------|--------|--------|
| Line Coverage | ~5% | 90% | ❌ |
| Branch Coverage | ~3% | 85% | ❌ |
| Method Coverage | ~8% | 95% | ❌ |

### Test Metrics

| Metric | Current | Target | Status |
|--------|---------|--------|--------|
| Total Tests | 13 | 260 | ❌ |
| Unit Tests | 13 | 240 | ❌ |
| Integration Tests | 0 | 20 | ❌ |
| Performance Tests | 0 | 20 | ❌ |
| Test Pass Rate | 100% | 100% | ✅ |
| Avg Test Duration | <50ms | <100ms | ✅ |

### Quality Gates

| Gate | Threshold | Enforcement |
|------|-----------|-------------|
| Code Coverage | ≥90% | CI/CD Pipeline |
| Test Pass Rate | 100% | CI/CD Pipeline |
| Integration Tests | Pass on main branch | CI/CD Pipeline |
| Performance Regression | <10% degradation | Manual review |
| No Critical Bugs | 0 critical bugs | Manual review |

---

## Timeline Summary

**Total Estimated Effort:** 6 weeks

| Week | Focus | Tests | Coverage Goal |
|------|-------|-------|---------------|
| 1 | Content + Message Converters | 67 | 30% |
| 2 | Tool + Options Converters | 55 | 50% |
| 3 | Streaming + ChatClient | 80 | 75% |
| 4 | DI Extensions | 25 | 85% |
| 5 | Integration Tests | 20 | 88% |
| 6 | Performance Tests | 20 | 90%+ |

---

## Conclusion

This comprehensive test plan provides a roadmap to achieve **90%+ code coverage** with **260 total tests** across unit, integration, and performance categories. The current state (13 tests, ~5% coverage) requires significant expansion, particularly in critical areas like:

1. **AnthropicChatClient** (main entry point)
2. **AnthropicStreamingConverter** (production-critical)
3. **Azure Foundry authentication** (enterprise scenarios)
4. **Integration tests** (real-world validation)

By following this plan systematically over 6 weeks, the library will achieve production-grade quality with comprehensive test coverage, enabling confident releases and maintainability.

**Next Steps:**
1. Review and approve test plan
2. Set up test infrastructure (builders, mocks, fixtures)
3. Begin Week 1 implementation (Content + Message Converters)
4. Track progress against coverage metrics
5. Adjust plan based on actual implementation findings

---

**Test Plan Author:** Parker (QA Engineer)
**Date:** 2025-11-19
**Status:** Ready for Implementation
