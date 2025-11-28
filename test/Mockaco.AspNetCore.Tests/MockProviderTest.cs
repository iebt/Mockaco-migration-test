using FluentAssertions;
using Microsoft.Extensions.Logging;
using Mockaco.HealthChecks;
using Mono.TextTemplating;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Mockaco.Tests
{
    public class MockProviderTest
    {
        private readonly Mock<ITemplateProvider> _mockTemplateProvider;
        private readonly Mock<ITemplateTransformer> _mockTemplateTransformer;
        private readonly Mock<IFakerFactory> _mockFakerFactory;
        private readonly Mock<IRequestBodyFactory> _mockRequestBodyFactory;
        private readonly Mock<IGlobalVariableStorage> _mockGlobalVariableStorage;
        private readonly StartupHealthCheck _healthCheck;
        private readonly Mock<ILogger<MockProvider>> _mockLogger;
        private readonly MockProvider _mockProvider;

        public MockProviderTest()
        {
            _mockTemplateProvider = new Mock<ITemplateProvider>();
            _mockTemplateTransformer = new Mock<ITemplateTransformer>();
            _mockFakerFactory = new Mock<IFakerFactory>();
            _mockRequestBodyFactory = new Mock<IRequestBodyFactory>();
            _mockGlobalVariableStorage = new Mock<IGlobalVariableStorage>();
            _healthCheck = new StartupHealthCheck();
            _mockLogger = new Mock<ILogger<MockProvider>>();

            _mockProvider = new MockProvider(
                _mockFakerFactory.Object,
                _mockRequestBodyFactory.Object,
                _mockTemplateProvider.Object,
                _mockTemplateTransformer.Object,
                _mockGlobalVariableStorage.Object,
                _healthCheck,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task WarmUp_WithValidTemplates_CachesTransformedMocks()
        {
            // Arrange
            var rawTemplate = CreateRawTemplate("test-template", "hash1");
            var template = new Template
            {
                Request = new RequestTemplate
                {
                    Method = "GET",
                    Route = "/test"
                }
            };

            _mockTemplateProvider.Setup(p => p.GetTemplates())
                .Returns(new[] { rawTemplate });

            _mockTemplateTransformer.Setup(t => t.Transform(It.IsAny<IRawTemplate>(), It.IsAny<IScriptContext>()))
                .ReturnsAsync(template);

            // Act
            await _mockProvider.WarmUp();

            // Assert
            var mocks = _mockProvider.GetMocks();
            mocks.Should().HaveCount(1);
            mocks[0].Method.Should().Be("GET");
            mocks[0].Route.Should().Be("/test");
            mocks[0].RawTemplate.Should().Be(rawTemplate);
        }

        [Fact]
        public async Task WarmUp_WithUnchangedTemplateHash_ReusesCachedMock()
        {
            // Arrange
            var rawTemplate = CreateRawTemplate("test-template", "hash1");
            var template = new Template
            {
                Request = new RequestTemplate
                {
                    Method = "GET",
                    Route = "/test"
                }
            };

            _mockTemplateProvider.Setup(p => p.GetTemplates())
                .Returns(new[] { rawTemplate });

            _mockTemplateTransformer.Setup(t => t.Transform(It.IsAny<IRawTemplate>(), It.IsAny<IScriptContext>()))
                .ReturnsAsync(template);

            // Act - First warm-up
            await _mockProvider.WarmUp();

            // Act - Second warm-up with same hash
            await _mockProvider.WarmUp();

            // Assert - Transform should be called only once (first time)
            _mockTemplateTransformer.Verify(
                t => t.Transform(It.IsAny<IRawTemplate>(), It.IsAny<IScriptContext>()),
                Times.Once
            );

            var mocks = _mockProvider.GetMocks();
            mocks.Should().HaveCount(1);
        }

        [Fact]
        public async Task WarmUp_WithJsonReaderException_AddsErrorAndSkipsTemplate()
        {
            // Arrange
            var rawTemplate = CreateRawTemplate("invalid-json-template", "hash1");

            _mockTemplateProvider.Setup(p => p.GetTemplates())
                .Returns(new[] { rawTemplate });

            _mockTemplateTransformer.Setup(t => t.Transform(It.IsAny<IRawTemplate>(), It.IsAny<IScriptContext>()))
                .ThrowsAsync(new JsonReaderException("Invalid JSON syntax"));

            // Act
            await _mockProvider.WarmUp();

            // Assert
            var errors = _mockProvider.GetErrors().ToList();
            errors.Should().HaveCount(1);
            errors[0].TemplateName.Should().Be("invalid-json-template");
            errors[0].ErrorMessage.Should().Contain("Generated JSON is invalid");
            errors[0].ErrorMessage.Should().Contain("Invalid JSON syntax");

            var mocks = _mockProvider.GetMocks();
            mocks.Should().BeEmpty();
        }

        [Fact]
        public async Task WarmUp_WithParserException_AddsErrorWithLocation()
        {
            // Arrange
            var rawTemplate = CreateRawTemplate("parser-error-template", "hash1");
            var location = new Location("template.json", 5, 10);
            var parserException = new ParserException("Script syntax error", location);

            _mockTemplateProvider.Setup(p => p.GetTemplates())
                .Returns(new[] { rawTemplate });

            _mockTemplateTransformer.Setup(t => t.Transform(It.IsAny<IRawTemplate>(), It.IsAny<IScriptContext>()))
                .ThrowsAsync(parserException);

            // Act
            await _mockProvider.WarmUp();

            // Assert
            var errors = _mockProvider.GetErrors().ToList();
            errors.Should().HaveCount(1);
            errors[0].TemplateName.Should().Be("parser-error-template");
            errors[0].ErrorMessage.Should().Contain("Script parser error");
            errors[0].ErrorMessage.Should().Contain("Script syntax error");
        }

        [Fact]
        public async Task WarmUp_OrdersMocksByCondition_ConditionsLast()
        {
            // Arrange
            var template1 = CreateRawTemplate("template-with-condition", "hash1");
            var template2 = CreateRawTemplate("template-without-condition", "hash2");

            var transformedTemplate1 = new Template
            {
                Request = new RequestTemplate
                {
                    Method = "GET",
                    Route = "/conditional",
                    Condition = true // Has condition
                }
            };

            var transformedTemplate2 = new Template
            {
                Request = new RequestTemplate
                {
                    Method = "GET",
                    Route = "/unconditional"
                    // No condition
                }
            };

            _mockTemplateProvider.Setup(p => p.GetTemplates())
                .Returns(new[] { template1, template2 });

            _mockTemplateTransformer.Setup(t => t.Transform(template1, It.IsAny<IScriptContext>()))
                .ReturnsAsync(transformedTemplate1);

            _mockTemplateTransformer.Setup(t => t.Transform(template2, It.IsAny<IScriptContext>()))
                .ReturnsAsync(transformedTemplate2);

            // Act
            await _mockProvider.WarmUp();

            // Assert
            var mocks = _mockProvider.GetMocks();
            mocks.Should().HaveCount(2);

            // Mocks with conditions should come first (OrderByDescending)
            mocks[0].Route.Should().Be("/conditional");
            mocks[0].HasCondition.Should().BeTrue();

            // Mocks without conditions should come last
            mocks[1].Route.Should().Be("/unconditional");
            mocks[1].HasCondition.Should().BeFalse();
        }

        [Fact]
        public async Task WarmUp_SetsStartupHealthCheckCompleted()
        {
            // Arrange
            var rawTemplate = CreateRawTemplate("test-template", "hash1");
            var template = new Template
            {
                Request = new RequestTemplate { Method = "GET", Route = "/test" }
            };

            _mockTemplateProvider.Setup(p => p.GetTemplates())
                .Returns(new[] { rawTemplate });

            _mockTemplateTransformer.Setup(t => t.Transform(It.IsAny<IRawTemplate>(), It.IsAny<IScriptContext>()))
                .ReturnsAsync(template);

            _healthCheck.StartupCompleted = false;

            // Act
            await _mockProvider.WarmUp();

            // Assert
            _healthCheck.StartupCompleted.Should().BeTrue();
        }

        [Fact]
        public void GetMocks_ReturnsCurrentCache()
        {
            // Arrange & Act
            var mocks = _mockProvider.GetMocks();

            // Assert
            mocks.Should().NotBeNull();
            mocks.Should().BeEmpty(); // Initially empty
        }

        [Fact]
        public async Task GetErrors_ReturnsAllErrors()
        {
            // Arrange
            var template1 = CreateRawTemplate("error-template-1", "hash1");
            var template2 = CreateRawTemplate("error-template-2", "hash2");

            _mockTemplateProvider.Setup(p => p.GetTemplates())
                .Returns(new[] { template1, template2 });

            _mockTemplateTransformer.Setup(t => t.Transform(template1, It.IsAny<IScriptContext>()))
                .ThrowsAsync(new JsonReaderException("Error 1"));

            _mockTemplateTransformer.Setup(t => t.Transform(template2, It.IsAny<IScriptContext>()))
                .ThrowsAsync(new JsonReaderException("Error 2"));

            // Act
            await _mockProvider.WarmUp();

            // Assert
            var errors = _mockProvider.GetErrors().ToList();
            errors.Should().HaveCount(2);
            errors[0].TemplateName.Should().Be("error-template-1");
            errors[0].ErrorMessage.Should().Contain("Error 1");
            errors[1].TemplateName.Should().Be("error-template-2");
            errors[1].ErrorMessage.Should().Contain("Error 2");
        }

        [Fact]
        public async Task WarmUp_WithGenericException_AddsErrorAndSkipsTemplate()
        {
            // Arrange
            var rawTemplate = CreateRawTemplate("exception-template", "hash1");

            _mockTemplateProvider.Setup(p => p.GetTemplates())
                .Returns(new[] { rawTemplate });

            _mockTemplateTransformer.Setup(t => t.Transform(It.IsAny<IRawTemplate>(), It.IsAny<IScriptContext>()))
                .ThrowsAsync(new InvalidOperationException("Generic error"));

            // Act
            await _mockProvider.WarmUp();

            // Assert
            var errors = _mockProvider.GetErrors().ToList();
            errors.Should().HaveCount(1);
            errors[0].TemplateName.Should().Be("exception-template");
            errors[0].ErrorMessage.Should().Be("Generic error");

            var mocks = _mockProvider.GetMocks();
            mocks.Should().BeEmpty();
        }

        [Fact]
        public async Task WarmUp_ClearsErrorsBeforeProcessing()
        {
            // Arrange
            var template1 = CreateRawTemplate("error-template", "hash1");
            var template2 = CreateRawTemplate("valid-template", "hash2");

            var validTransformedTemplate = new Template
            {
                Request = new RequestTemplate { Method = "GET", Route = "/valid" }
            };

            // First warm-up with error
            _mockTemplateProvider.Setup(p => p.GetTemplates())
                .Returns(new[] { template1 });

            _mockTemplateTransformer.Setup(t => t.Transform(template1, It.IsAny<IScriptContext>()))
                .ThrowsAsync(new JsonReaderException("Error"));

            await _mockProvider.WarmUp();

            var errorsAfterFirstWarmUp = _mockProvider.GetErrors().ToList();
            errorsAfterFirstWarmUp.Should().HaveCount(1);

            // Second warm-up without errors
            _mockTemplateProvider.Setup(p => p.GetTemplates())
                .Returns(new[] { template2 });

            _mockTemplateTransformer.Setup(t => t.Transform(template2, It.IsAny<IScriptContext>()))
                .ReturnsAsync(validTransformedTemplate);

            // Act
            await _mockProvider.WarmUp();

            // Assert - Errors should be cleared
            var errors = _mockProvider.GetErrors().ToList();
            errors.Should().BeEmpty();
        }

        [Fact]
        public async Task WarmUp_WithMultipleTemplates_LoadsAll()
        {
            // Arrange
            var template1 = CreateRawTemplate("template-1", "hash1");
            var template2 = CreateRawTemplate("template-2", "hash2");
            var template3 = CreateRawTemplate("template-3", "hash3");

            var transformedTemplate1 = new Template
            {
                Request = new RequestTemplate { Method = "GET", Route = "/route1" }
            };

            var transformedTemplate2 = new Template
            {
                Request = new RequestTemplate { Method = "POST", Route = "/route2" }
            };

            var transformedTemplate3 = new Template
            {
                Request = new RequestTemplate { Method = "PUT", Route = "/route3" }
            };

            _mockTemplateProvider.Setup(p => p.GetTemplates())
                .Returns(new[] { template1, template2, template3 });

            _mockTemplateTransformer.Setup(t => t.Transform(template1, It.IsAny<IScriptContext>()))
                .ReturnsAsync(transformedTemplate1);

            _mockTemplateTransformer.Setup(t => t.Transform(template2, It.IsAny<IScriptContext>()))
                .ReturnsAsync(transformedTemplate2);

            _mockTemplateTransformer.Setup(t => t.Transform(template3, It.IsAny<IScriptContext>()))
                .ReturnsAsync(transformedTemplate3);

            // Act
            await _mockProvider.WarmUp();

            // Assert
            var mocks = _mockProvider.GetMocks();
            mocks.Should().HaveCount(3);
            mocks.Select(m => m.Route).Should().Contain(new[] { "/route1", "/route2", "/route3" });
            mocks.Select(m => m.Method).Should().Contain(new[] { "GET", "POST", "PUT" });
        }

        private IRawTemplate CreateRawTemplate(string name, string hash)
        {
            var mock = new Mock<IRawTemplate>();
            mock.Setup(t => t.Name).Returns(name);
            mock.Setup(t => t.Hash).Returns(hash);
            mock.Setup(t => t.Content).Returns("{}");
            return mock.Object;
        }
    }
}
