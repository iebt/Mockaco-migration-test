********# Test Plan

## Summary

**Test Project:** Mockaco.AspNetCore.Tests
**Test Framework:** xUnit
**Coverage Tool:** coverlet.collector v6.0.4 (already configured)
**Total Tests:** 70
**Test Status:** ✅ All tests passing

## Current Coverage Status

### Overall Coverage Metrics

- **Line Coverage:** 33.41% (645 of 1,930 lines covered)
- **Branch Coverage:** 26.75% (122 of 456 branches covered)

### Coverage by Assembly

| Assembly | Line Coverage | Branch Coverage |
|----------|---------------|-----------------|
| Mockaco.AspNetCore | 33.41% | 26.75% |

## Command to Run Tests

### Run tests with coverage collection

```bash
dotnet test Mockaco.sln --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

### Alternative: Run tests on specific test project

```bash
dotnet test test/Mockaco.AspNetCore.Tests/Mockaco.AspNetCore.Tests.csproj --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

## Coverage Report Outputs

Coverage results are generated in Cobertura XML format at:
```
./TestResults/{guid}/coverage.cobertura.xml
```

## Test Execution Results

Latest run (2025-11-28):
- ✅ Passed: 70
- ❌ Failed: 0
- ⏭️ Skipped: 0
- ⏱️ Duration: ~6 seconds

## Notes

1. **Coverage is already configured** - The test project includes `coverlet.collector` package, so no additional setup is needed.

2. **Build warnings** - There are 6 nullable reference warnings in the test code:
   - `JsonRequestBodyStrategyTest.cs:38` - Dereference of possibly null reference
   - `JsonRequestBodyStrategyTest.cs:15` - Non-nullable field not initialized
   - `ResponseBodyFactoryTest.cs:14` - Null literal to non-nullable type
   - `ErrorHandlingMiddlewareTest.cs:50,51,73` - Dereference of possibly null references

3. **Coverage analysis** - Current coverage (33.41% line, 26.75% branch) suggests significant portions of the codebase are not yet tested. Key areas with low coverage include:
   - T4 templating infrastructure (Directive, TemplateSegment classes)
   - Tokenizer state machine logic
   - Some directive parsing methods

## Recommendations

To improve coverage:
1. Add tests for uncovered templating components (T4 tokenizer, directive parsing)
2. Increase branch coverage by testing edge cases and error paths
3. Consider using coverage reports to identify specific untested code paths
4. Address nullable reference warnings to improve code quality

---

# Test Generation Plan

## Overview

This plan outlines specific test cases to be generated to improve code coverage from 33.41% to a higher threshold. Tests are prioritized by business criticality and ordered to focus on core domain logic first.

## Testing Conventions

- **Test Framework:** xUnit with Moq for mocking
- **Assertion Library:** FluentAssertions (already in use)
- **Test Patterns:**
  - Use `[Theory]` with `[InlineData]` for data-driven tests
  - Mock dependencies via interfaces using Moq
  - Use `Mock.Of<T>()` for simple mocks, `new Mock<T>()` for complex setups
  - Follow AAA pattern: Arrange, Act, Assert
  - Use descriptive test names: `MethodName_Scenario_ExpectedBehavior`

---

## CRITICAL Priority Tests

### 1. RouteMatcher (Common/RouteMatcher.cs)

**File:** `src/Mockaco.AspNetCore/Common/RouteMatcher.cs`
**Current Coverage:** 0%
**Priority:** Critical
**Reason:** Core routing logic - critical for matching incoming requests to mock templates

**Suggested Test Cases:**

1. **Match_WithValidRouteTemplateAndPath_ReturnsRouteValues**
   - Given: route template `/users/{id}` and request path `/users/123`
   - When: Match is called
   - Then: Returns RouteValueDictionary with `id=123`

2. **Match_WithMultipleParameters_ExtractsAllParameters**
   - Given: route template `/api/{version}/users/{id}` and path `/api/v1/users/456`
   - When: Match is called
   - Then: Returns dictionary with `version=v1` and `id=456`

3. **Match_WithNonMatchingPath_ReturnsNull**
   - Given: route template `/users/{id}` and path `/products/123`
   - When: Match is called
   - Then: Returns null

4. **Match_WithEmptyOrNullRouteTemplate_ReturnsNull**
   - Given: null or empty route template
   - When: Match is called
   - Then: Returns null

5. **IsMatch_WithMatchingRoute_ReturnsTrue**
   - Given: matching route template and path
   - When: IsMatch is called
   - Then: Returns true

6. **IsMatch_WithNonMatchingRoute_ReturnsFalse**
   - Given: non-matching route template and path
   - When: IsMatch is called
   - Then: Returns false

7. **Match_WithDefaultValueParameters_IncludesDefaults**
   - Given: route template with default values `/users/{id=1}`
   - When: Match is called without providing the parameter
   - Then: Returns dictionary with default value

**Dependencies to Mock:** None (uses ASP.NET Core routing directly)
**Estimated Complexity:** Simple

---

### 2. MockProvider (MockProvider.cs)

**File:** `src/Mockaco.AspNetCore/MockProvider.cs`
**Current Coverage:** 0%
**Priority:** Critical
**Reason:** Core provider that manages template caching, warm-up, and error tracking

**Suggested Test Cases:**

1. **WarmUp_WithValidTemplates_CachesTransformedMocks**
   - Given: Valid raw templates from ITemplateProvider
   - When: WarmUp is called
   - Then: Mocks are transformed and cached in _cache list

2. **WarmUp_WithUnchangedTemplateHash_ReusesCachedMock**
   - Given: A template already cached with same hash
   - When: WarmUp is called again
   - Then: Uses cached mock instead of re-transforming

3. **WarmUp_WithJsonReaderException_AddsErrorAndSkipsTemplate**
   - Given: Template that throws JsonReaderException during transformation
   - When: WarmUp is called
   - Then: Error is added to _errors list, template is skipped

4. **WarmUp_WithParserException_AddsErrorWithLocation**
   - Given: Template that throws ParserException
   - When: WarmUp is called
   - Then: Error includes parser location information

5. **WarmUp_OrdersMocksByCondition_ConditionsLast**
   - Given: Mix of mocks with and without conditions
   - When: WarmUp completes
   - Then: Mocks with conditions are ordered last in cache

6. **WarmUp_SetsStartupHealthCheckCompleted**
   - Given: WarmUp process completes successfully
   - When: WarmUp finishes
   - Then: StartupHealthCheck.StartupCompleted is set to true

7. **GetMocks_ReturnsCurrentCache**
   - Given: Cached mocks exist
   - When: GetMocks is called
   - Then: Returns the current _cache list

8. **GetErrors_ReturnsAllErrors**
   - Given: Multiple errors during warm-up
   - When: GetErrors is called
   - Then: Returns all accumulated errors with template names

9. **TemplateProviderChange_TriggersWarmUp**
   - Given: ITemplateProvider raises OnChange event
   - When: Event is triggered
   - Then: WarmUp is called automatically

**Dependencies to Mock:**
- `ITemplateProvider`
- `ITemplateTransformer`
- `IFakerFactory`
- `IRequestBodyFactory`
- `IGlobalVariableStorage`
- `StartupHealthCheck`
- `ILogger<MockProvider>`

**Estimated Complexity:** Complex

---

### 3. ChaosMiddleware (Middlewares/ChaosMiddleware.cs)

**File:** `src/Mockaco.AspNetCore/Middlewares/ChaosMiddleware.cs`
**Current Coverage:** 0%
**Priority:** Critical
**Reason:** Chaos engineering feature that must reliably introduce controlled failures

**Suggested Test Cases:**

1. **Invoke_WhenDisabled_CallsNextMiddleware**
   - Given: ChaosOptions.Enabled = false
   - When: Invoke is called
   - Then: Calls _next(httpContext) without chaos

2. **Invoke_CounterIncrementsOnEachRequest**
   - Given: Multiple requests
   - When: Invoke is called repeatedly
   - Then: Counter increments from 1 to 100, then resets

3. **Invoke_CounterResetsAfter100**
   - Given: Counter is at 100
   - When: Invoke is called
   - Then: Counter resets to 1

4. **Invoke_GeneratesErrorListAtCounterOne**
   - Given: Counter is 1
   - When: Invoke is called
   - Then: ErrorList is regenerated based on ChaosRate

5. **Invoke_WhenCounterInErrorList_AppliesRandomStrategy**
   - Given: Counter matches value in ErrorList
   - When: Invoke is called
   - Then: Random chaos strategy is selected and applied

6. **Invoke_WhenStatusCodeNotOK_DoesNotCallNext**
   - Given: Chaos strategy sets non-OK status code
   - When: Invoke completes
   - Then: _next is not called

7. **GenerateErrorList_CreatesCorrectNumberOfErrors**
   - Given: ChaosRate = 25
   - When: GenerateErrorList is called
   - Then: Returns list with 25 unique numbers between 1-100

8. **GenerateErrorList_ReturnsUniqueValues**
   - Given: Any chaos rate
   - When: GenerateErrorList is called
   - Then: All values in list are unique

**Dependencies to Mock:**
- `RequestDelegate _next`
- `IEnumerable<IChaosStrategy>`
- `ILogger<ChaosMiddleware>`
- `IOptions<ChaosOptions>`
- `HttpContext`

**Estimated Complexity:** Medium

---

### 4. CallbackMiddleware (Middlewares/CallbackMiddleware.cs)

**File:** `src/Mockaco.AspNetCore/Middlewares/CallbackMiddleware.cs`
**Current Coverage:** 0%
**Priority:** Critical
**Reason:** Webhook/callback functionality - must fire external HTTP requests reliably

**Suggested Test Cases:**

1. **Invoke_WhenNoCallbacks_CompletesImmediately**
   - Given: Template with no callbacks
   - When: Invoke is called
   - Then: Returns completed task without registering OnCompleted

2. **Invoke_WithCallbacks_RegistersOnCompletedHandler**
   - Given: Template with callbacks
   - When: Invoke is called
   - Then: Registers handler on httpContext.Response.OnCompleted

3. **PerformCallbacks_TransformsTemplateWithScriptContext**
   - Given: Template with callbacks
   - When: Callbacks are performed
   - Then: templateTransformer.Transform is called with scriptContext

4. **PerformCallbacks_ExecutesAllCallbacksInParallel**
   - Given: Template with multiple callbacks
   - When: PerformCallbacks is called
   - Then: All callbacks execute via Task.WhenAll

5. **PerformCallback_DelaysRequestByConfiguredAmount**
   - Given: Callback with Delay = 1000ms
   - When: PerformCallback executes
   - Then: Waits for remaining delay time

6. **PerformCallback_SetsHttpMethodFromTemplate**
   - Given: Callback with Method = "POST"
   - When: HTTP request is prepared
   - Then: HttpRequestMessage uses POST method

7. **PerformCallback_SetsContentTypeFromHeaders**
   - Given: Callback with Content-Type header
   - When: HTTP request is prepared
   - Then: Content uses specified Content-Type

8. **PerformCallback_AddsAcceptHeaderIfMissing**
   - Given: Request without Accept header
   - When: PrepareHeaders is called
   - Then: Adds Accept header with default content type

9. **PerformCallback_SetsTimeoutFromTemplate**
   - Given: Callback with Timeout = 5000ms
   - When: HttpClient is prepared
   - Then: HttpClient.Timeout is set to 5000ms

10. **PerformCallback_HandlesOperationCanceledException**
    - Given: Callback that times out
    - When: Request exceeds timeout
    - Then: Logs timeout error without throwing

11. **PerformCallbacks_LogsErrorOnException**
    - Given: Template transformation throws exception
    - When: PerformCallbacks executes
    - Then: Logs error and continues without crashing

**Dependencies to Mock:**
- `RequestDelegate _next`
- `ILogger<CallbackMiddleware>`
- `HttpContext`
- `IMockacoContext`
- `IScriptContext`
- `ITemplateTransformer`
- `IOptionsSnapshot<MockacoOptions>`
- `IHttpClientFactory`

**Estimated Complexity:** Complex

---

### 5. TemplateTransformer (Templating/TemplateTransformer.cs)

**File:** `src/Mockaco.AspNetCore/Templating/TemplateTransformer.cs`
**Current Coverage:** Partial (only basic paths tested)
**Priority:** Critical
**Reason:** Core template processing - transforms templates with C# scripts

**Suggested Test Cases:**

1. **TransformAndSetVariables_EnablesGlobalVariableWriting**
   - Given: ScriptContext with global variables
   - When: TransformAndSetVariables is called
   - Then: scriptContext.Global.EnableWriting() is called

2. **Transform_DisablesGlobalVariableWriting**
   - Given: ScriptContext
   - When: Transform is called
   - Then: scriptContext.Global.DisableWriting() is called

3. **Transform_WithEmptyInput_ReturnsEmptyString**
   - Given: Empty or null input string
   - When: Transform is called
   - Then: Returns input as-is

4. **Transform_WithContentTokens_PreservesPlainText**
   - Given: Template with only plain JSON (no scripts)
   - When: Transform is called
   - Then: Returns unchanged content

5. **Transform_WithExpressionTokens_EvaluatesAndInsertsResult**
   - Given: Template with `<#= 1 + 1 #>`
   - When: Transform is called
   - Then: Expression is evaluated and result "2" is inserted

6. **Transform_WithBlockTokens_ExecutesWithoutOutput**
   - Given: Template with `<# var x = 1; #>`
   - When: Transform is called
   - Then: Block executes but produces no output

7. **Transform_WithMixedTokens_ProcessesAllCorrectly**
   - Given: Template mixing content, expressions, and blocks
   - When: Transform is called
   - Then: Each token type is processed appropriately

8. **Transform_WithInvalidJson_ThrowsInvalidMockException**
   - Given: Template that generates invalid JSON
   - When: Transform is called
   - Then: Throws InvalidMockException with generated output in exception data

9. **Transform_WithScriptException_LogsErrorAndRethrows**
   - Given: Template with failing C# script
   - When: Transform is called
   - Then: Logs error and rethrows exception

**Dependencies to Mock:**
- `IScriptRunnerFactory`
- `ILogger<TemplateTransformer>`

**Estimated Complexity:** Medium

---

## HIGH Priority Tests

### 6. RequestRouteMatcher (Templating/Request/RequestRouteMatcher.cs)

**File:** `src/Mockaco.AspNetCore/Templating/Request/RequestRouteMatcher.cs`
**Current Coverage:** 0%
**Priority:** High
**Reason:** Matches incoming HTTP requests to mock route patterns

**Suggested Test Cases:**

1. **IsMatch_WithNullOrEmptyRoute_UsesDefaultRoute**
   - Given: Mock with null or empty route
   - When: IsMatch is called
   - Then: Matches against "/" default route

2. **IsMatch_WithValidRoute_DelegatesToRouteMatcher**
   - Given: Mock with route "/users/{id}"
   - When: IsMatch is called with "/users/123"
   - Then: Returns true

3. **IsMatch_WithNonMatchingRoute_ReturnsFalse**
   - Given: Mock route doesn't match request path
   - When: IsMatch is called
   - Then: Returns false

**Dependencies to Mock:**
- `HttpRequest`
- `Mock`

**Estimated Complexity:** Simple

---

### 7. RequestMatchingMiddleware (Middlewares/RequestMatchingMiddleware.cs)

**File:** `src/Mockaco.AspNetCore/Middlewares/RequestMatchingMiddleware.cs`
**Current Coverage:** ~26% (partially tested)
**Priority:** High
**Reason:** Critical middleware that matches requests to templates

**Suggested Test Cases:**

1. **Invoke_WithNoMatchingMock_ContinuesToNextMiddleware**
   - Given: No mocks match the request
   - When: Invoke is called
   - Then: Calls _next without setting mock in context

2. **Invoke_WithMatchingMock_AttachesToContext**
   - Given: Request matches a mock
   - When: Invoke is called
   - Then: Sets mockacoContext.Mock to matched mock

3. **Invoke_WithAllMatchersReturningTrue_MatchesRequest**
   - Given: All IRequestMatcher implementations return true
   - When: Invoke is called
   - Then: Request is matched

4. **Invoke_WithAnyMatcherReturningFalse_DoesNotMatch**
   - Given: At least one IRequestMatcher returns false
   - When: Invoke is called
   - Then: Request is not matched

5. **AttachRequestToScriptContext_PopulatesScriptContextRequest**
   - Given: Matched request
   - When: AttachRequestToScriptContext is called
   - Then: ScriptContext.Request contains request data

6. **LoadHeaders_FiltersHeadersByAllowedList**
   - Given: HTTP request with multiple headers
   - When: LoadHeaders is called with allowed headers list
   - Then: Only allowed headers are loaded

7. **Invoke_StoresMatchedRouteInCache**
   - Given: Request matched successfully
   - When: Invoke completes
   - Then: Route is stored in IMemoryCache for verification

**Dependencies to Mock:**
- `RequestDelegate _next`
- `IEnumerable<IRequestMatcher>`
- `IMockProvider`
- `IMockacoContext`
- `IScriptContext`
- `IRequestBodyFactory`
- `IMemoryCache`
- `ILogger<RequestMatchingMiddleware>`
- `HttpContext`

**Estimated Complexity:** Complex

---

### 8. Chaos Strategies (Chaos/Strategies/*.cs)

**Files:**
- `src/Mockaco.AspNetCore/Chaos/Strategies/ChaosStrategyBehavior.cs`
- `src/Mockaco.AspNetCore/Chaos/Strategies/ChaosStrategyException.cs`
- `src/Mockaco.AspNetCore/Chaos/Strategies/ChaosStrategyLatency.cs`
- `src/Mockaco.AspNetCore/Chaos/Strategies/ChaosStrategyResult.cs`
- `src/Mockaco.AspNetCore/Chaos/Strategies/ChaosStrategyTimeout.cs`

**Current Coverage:** 0%
**Priority:** High
**Reason:** Different chaos behaviors need individual testing

**Suggested Test Cases:**

**ChaosStrategyBehavior:**
1. **Response_ReturnsRandomHttpStatusCode**
   - When: Response is called
   - Then: Sets response status code to random error code

**ChaosStrategyException:**
1. **Response_ThrowsException**
   - When: Response is called
   - Then: Throws exception to simulate server crash

**ChaosStrategyLatency:**
1. **Response_DelaysResponse**
   - When: Response is called
   - Then: Introduces random delay before completing

**ChaosStrategyResult:**
1. **Response_ReturnsDifferentStatusCode**
   - When: Response is called
   - Then: Returns non-standard HTTP status code

**ChaosStrategyTimeout:**
1. **Response_SimulatesTimeout**
   - When: Response is called
   - Then: Delays indefinitely to simulate timeout

**Dependencies to Mock:**
- `HttpResponse`

**Estimated Complexity:** Simple (each strategy is simple individually)

---

### 9. TemplateFileProvider (Templating/Providers/TemplateFileProvider.cs)

**File:** `src/Mockaco.AspNetCore/Templating/Providers/TemplateFileProvider.cs`
**Current Coverage:** 0%
**Priority:** High
**Reason:** Loads templates from file system with caching and change detection

**Suggested Test Cases:**

1. **GetTemplates_LoadsAllJsonFilesFromDirectory**
   - Given: Directory with multiple .json files
   - When: GetTemplates is called
   - Then: Returns all templates

2. **GetTemplates_RespectsSubdirectories**
   - Given: Templates in nested subdirectories
   - When: GetTemplates is called
   - Then: Recursively loads all templates

3. **GetTemplates_CachesTemplatesInMemory**
   - Given: Templates loaded once
   - When: GetTemplates is called again
   - Then: Returns cached templates without file I/O

4. **GetTemplates_InvalidatesCacheOnFileChange**
   - Given: Template file is modified
   - When: File change event triggers
   - Then: Cache is invalidated, templates reloaded

5. **GetTemplates_RespectsTemplateIgnoreFile**
   - Given: .mockignore file excludes certain templates
   - When: GetTemplates is called
   - Then: Excluded templates are not loaded

6. **GetTemplates_HandlesFileLocksWithRetry**
   - Given: File is temporarily locked
   - When: GetTemplates is called
   - Then: Retries using Polly policy

7. **OnChange_RaisesEventOnFileSystemChange**
   - Given: File system watcher detects changes
   - When: Template file changes
   - Then: OnChange event is raised

**Dependencies to Mock:**
- `IMemoryCache`
- `IOptionsMonitor<TemplateFileProviderOptions>`
- `ILogger<TemplateFileProvider>`
- File system (use temporary test directory)

**Estimated Complexity:** Complex

---

### 10. ScriptRunnerFactory (Templating/Scripting/ScriptRunnerFactory.cs)

**File:** `src/Mockaco.AspNetCore/Templating/Scripting/ScriptRunnerFactory.cs`
**Current Coverage:** Partial (basic tests exist)
**Priority:** High
**Reason:** Compiles and caches C# script runners using Roslyn

**Suggested Test Cases:**

1. **Invoke_CompilesCSharpCode**
   - Given: Valid C# expression
   - When: Invoke is called
   - Then: Compiles and executes code

2. **Invoke_CachesCompiledRunner**
   - Given: Same code executed twice
   - When: Invoke is called second time
   - Then: Uses cached compiled runner

3. **Invoke_SupportsConfiguredImports**
   - Given: MockacoOptions with custom imports
   - When: Code uses imported namespace
   - Then: Executes successfully

4. **Invoke_SupportsConfiguredReferences**
   - Given: MockacoOptions with custom assembly references
   - When: Code uses referenced assembly
   - Then: Executes successfully

5. **Invoke_PassesScriptContextAsGlobals**
   - Given: Code that accesses ScriptContext properties
   - When: Invoke is called
   - Then: ScriptContext is available as global

6. **Invoke_WithInvalidCode_ThrowsCompilationException**
   - Given: Invalid C# syntax
   - When: Invoke is called
   - Then: Throws compilation exception

**Dependencies to Mock:**
- `IMemoryCache`
- `IOptionsMonitor<MockacoOptions>`
- `ILogger<ScriptRunnerFactory>`

**Estimated Complexity:** Medium

---

## MEDIUM Priority Tests

### 11. Request Body Strategies

**Files:**
- `FormRequestBodyStrategy.cs` (0% coverage)
- `XmlRequestBodyStrategy.cs` (partial coverage)

**Priority:** Medium
**Reason:** Parse different content types (form and XML not fully tested)

**FormRequestBodyStrategy Test Cases:**

1. **CanHandle_WithFormContentType_ReturnsTrue**
   - Given: Content-Type is application/x-www-form-urlencoded
   - When: CanHandle is called
   - Then: Returns true

2. **ReadBody_ParsesFormData_ReturnsJObject**
   - Given: Form-encoded body
   - When: ReadBody is called
   - Then: Returns JObject with form fields

3. **ReadBody_WithEmptyForm_ReturnsEmptyJObject**
   - Given: Empty form body
   - When: ReadBody is called
   - Then: Returns empty JObject

**XmlRequestBodyStrategy Test Cases:**

1. **ReadBody_WithComplexXml_ConvertsToJson**
   - Given: Complex XML with nested elements
   - When: ReadBody is called
   - Then: Returns equivalent JSON structure

2. **ReadBody_WithXmlAttributes_PreservesAttributes**
   - Given: XML with attributes
   - When: ReadBody is called
   - Then: Attributes are preserved in JSON

**Dependencies to Mock:**
- `HttpRequest`

**Estimated Complexity:** Simple

---

### 12. Response Body Strategies (not yet tested)

**Files:**
- `StringResponseBodyStrategy.cs` (0% coverage)

**Priority:** Medium

**Test Cases:**

1. **CanHandle_WithStringBody_ReturnsTrue**
   - Given: Body is string type
   - When: CanHandle is called
   - Then: Returns true

2. **Write_WritesStringToResponse**
   - Given: String body
   - When: Write is called
   - Then: Writes string to response stream

**Dependencies to Mock:**
- `HttpResponse`

**Estimated Complexity:** Simple

---

### 13. RequestMethodMatcher (Templating/Request/RequestMethodMatcher.cs)

**File:** `src/Mockaco.AspNetCore/Templating/Request/RequestMethodMatcher.cs`
**Current Coverage:** 0%
**Priority:** Medium
**Reason:** Simple but essential HTTP method matching

**Suggested Test Cases:**

1. **IsMatch_WithMatchingMethod_ReturnsTrue**
   - Given: Mock method GET, request method GET
   - When: IsMatch is called
   - Then: Returns true

2. **IsMatch_WithNonMatchingMethod_ReturnsFalse**
   - Given: Mock method POST, request method GET
   - When: IsMatch is called
   - Then: Returns false

3. **IsMatch_WithNullMockMethod_DefaultsToGET**
   - Given: Mock with null method
   - When: IsMatch is called
   - Then: Defaults to GET method

4. **IsMatch_IsCaseInsensitive**
   - Given: Mock method "get", request method "GET"
   - When: IsMatch is called
   - Then: Returns true

**Dependencies to Mock:**
- `HttpRequest`
- `Mock`

**Estimated Complexity:** Simple

---

### 14. ScriptContext Models (Templating/Scripting/ScriptContext*.cs)

**Files:**
- `ScriptContext.cs`
- `ScriptContextRequest.cs`
- `ScriptContextResponse.cs`
- `ScriptContextGlobalVariableStorage.cs`

**Current Coverage:** Low
**Priority:** Medium
**Reason:** API exposed to template scripts - needs thorough testing

**ScriptContext Test Cases:**

1. **Constructor_InitializesFaker**
   - When: ScriptContext is created
   - Then: Faker property is initialized

2. **Constructor_InitializesRequest**
   - When: ScriptContext is created
   - Then: Request property is initialized

3. **Constructor_InitializesResponse**
   - When: ScriptContext is created
   - Then: Response property is initialized

**ScriptContextGlobalVariableStorage Test Cases:**

1. **EnableWriting_AllowsSettingValues**
   - Given: Writing is enabled
   - When: Variable is set
   - Then: Value is stored

2. **DisableWriting_PreventsSetting Values**
   - Given: Writing is disabled
   - When: Variable set is attempted
   - Then: Throws or ignores (verify actual behavior)

3. **GetItem_WithMissingKey_ReturnsNull**
   - Given: Key doesn't exist
   - When: Item is accessed
   - Then: Returns null or default

4. **SetItem_UpdatesExistingValue**
   - Given: Key already exists
   - When: New value is set
   - Then: Value is updated

**Dependencies to Mock:**
- `IFakerFactory`
- `IRequestBodyFactory`

**Estimated Complexity:** Simple

---

### 15. Extension Methods (Extensions/*.cs)

**Files:**
- `EnumerableExtensions.cs` (AllAsync methods 0% coverage)
- `HttpRequestExtensions.cs` (23% coverage - GetUri partially tested)
- `StringDictionaryExtensions.cs` (0% coverage)
- `StringExtensions.cs` (33% coverage - ToMD5Hash not tested)

**Priority:** Medium

**EnumerableExtensions Test Cases:**

1. **AllAsync_WithAllPredicatesTrue_ReturnsTrue**
   - Given: All async predicates return true
   - When: AllAsync is called
   - Then: Returns true

2. **AllAsync_WithAnyPredicateFalse_ReturnsFalse**
   - Given: At least one predicate returns false
   - When: AllAsync is called
   - Then: Returns false immediately (short-circuit)

3. **Random_ReturnsRandomElement**
   - Given: Enumerable with items
   - When: Random is called
   - Then: Returns one of the items

**HttpRequestExtensions Test Cases:**

1. **GetUri_BuildsFullUriFromRequest**
   - Given: HttpRequest with scheme, host, path, query
   - When: GetUri is called
   - Then: Returns complete URI

2. **GetUri_WithHttps_UsesHttpsScheme**
   - Given: Request with HTTPS
   - When: GetUri is called
   - Then: URI uses https://

3. **ReadBodyStream_ReadsRequestBody**
   - Given: Request with body content
   - When: ReadBodyStream is called
   - Then: Returns body as string

**StringExtensions Test Cases:**

1. **ToMD5Hash_CalculatesCorrectHash**
   - Given: Input string
   - When: ToMD5Hash is called
   - Then: Returns MD5 hash hex string

2. **ToMD5Hash_WithSameInput_ReturnsSameHash**
   - Given: Same input twice
   - When: ToMD5Hash is called both times
   - Then: Both hashes match

**StringDictionaryExtensions Test Cases:**

1. **ToStringDictionary_ConvertsEnumerableToStringDictionary**
   - Given: Enumerable of objects
   - When: ToStringDictionary is called with key and value selectors
   - Then: Returns string dictionary

**Dependencies to Mock:** Varies by extension method
**Estimated Complexity:** Simple

---

### 16. Generating Templates (Templating/Generating/*.cs)

**Files:** Multiple files related to OpenAPI template generation
**Current Coverage:** 0%
**Priority:** Medium
**Reason:** Feature for generating templates from OpenAPI specs

**Note:** These files are feature-specific and can be tested as a group if the feature is actively used.

**Test Cases:**

1. **OpenApiTemplateProvider_LoadsOpenApiSpec**
   - Given: Valid OpenAPI spec
   - When: Provider loads spec
   - Then: Templates are generated for endpoints

2. **HttpContentProvider_FetchesRemoteContent**
   - Given: HTTP URL to OpenAPI spec
   - When: Content is requested
   - Then: Fetches and returns content

3. **LocalFileContentProvider_ReadsLocalFile**
   - Given: Local file path
   - When: Content is requested
   - Then: Reads and returns file content

4. **GeneratedTemplateStore_StoresGeneratedTemplates**
   - Given: Generated templates
   - When: Store is called
   - Then: Templates are persisted

**Dependencies to Mock:**
- `IHttpClientFactory`
- `IFileSystem` (or use temp directories)
- `ILogger`

**Estimated Complexity:** Medium

---

## Files to SKIP (Low Value)

The following files should NOT have tests generated:

### Configuration/Options Classes
- `ChaosOptions.cs` - Simple property bag
- `MockacoOptions.cs` - Simple property bag
- `TemplateFileProviderOptions.cs` - Simple property bag
- `GeneratingOptions.cs` - Simple property bag
- `TemplateStoreOptions.cs` - Simple property bag

### Model/DTO Classes
- `CallbackTemplate.cs` - Simple model
- `Error.cs` - Simple model
- `Mock.cs` - Simple model
- `RawTemplate.cs` - Simple model
- `RequestTemplate.cs` - Simple model
- `ResponseTemplate.cs` - Simple model
- `Template.cs` - Simple model
- `GeneratedTemplate.cs` - Simple model

### Constants/Static Classes
- `HttpContentTypes.cs` - Constants only
- `HttpHeaders.cs` - Constants only
- `InternalsVisibleTo.cs` - Assembly attribute

### Simple Extensions/Helpers
- `PhoneNumberExtensions.cs` - Already tested via integration
- `SimpleExceptionConverter.cs` - Trivial serialization
- `ObjectExtensions.cs` (77% coverage) - Mostly tested

### Infrastructure/Setup
- `MockacoServiceCollection.cs` - DI registration (integration test coverage sufficient)
- `MockacoApplicationBuilder.cs` - Middleware registration
- `StartupHealthCheck.cs` - Simple property
- `VerificationRouteValueTransformer.cs` - Simple routing

### Interfaces
- All `I*.cs` interface files (no implementation to test)

---

## Test Generation Order

Execute test generation in this order for maximum impact:

1. **RouteMatcher** - Foundational routing logic
2. **MockProvider** - Core caching and warm-up
3. **TemplateTransformer** - Template processing
4. **ChaosMiddleware** - Chaos engineering
5. **CallbackMiddleware** - Webhook functionality
6. **RequestMatchingMiddleware** - Request matching pipeline
7. **RequestRouteMatcher** - Route matching
8. **RequestMethodMatcher** - HTTP method matching
9. **Chaos Strategies** - Individual chaos behaviors
10. **ScriptRunnerFactory** - Script compilation
11. **TemplateFileProvider** - File loading
12. **Request/Response Body Strategies** - Content type handling
13. **ScriptContext models** - Script API
14. **Extension methods** - Utility functions
15. **Template generation** - OpenAPI feature (if needed)

---

## Expected Coverage Improvement

After implementing all CRITICAL and HIGH priority tests:
- **Estimated Line Coverage:** 65-75% (up from 33.41%)
- **Estimated Branch Coverage:** 55-65% (up from 26.75%)

After implementing all tests (including MEDIUM priority):
- **Estimated Line Coverage:** 75-85%
- **Estimated Branch Coverage:** 65-75%

---

## Test Generation Checklist

For each test file generated, ensure:

- [ ] File follows naming convention: `{ClassName}Test.cs`
- [ ] Tests use xUnit `[Fact]` or `[Theory]` attributes
- [ ] Dependencies are mocked using Moq
- [ ] Assertions use FluentAssertions
- [ ] Test names follow pattern: `MethodName_Scenario_ExpectedBehavior`
- [ ] AAA pattern is followed (Arrange, Act, Assert)
- [ ] Edge cases and error paths are covered
- [ ] Tests are isolated and don't depend on external state
- [ ] Async tests use `async Task` and `await`

---

## Notes for Test-Agent

- **Do not modify existing test files** - only create new ones
- **Follow existing patterns** - examine current tests for style
- **Use proper async/await** - all async methods should be properly awaited
- **Mock external dependencies** - never call real HTTP, file system, or database (except for integration tests using temp directories for file-based tests)
- **Make tests deterministic** - avoid random values in assertions (use specific expected values)
- **Test both success and failure paths** - include error handling scenarios
- **Keep tests focused** - one test should verify one behavior
