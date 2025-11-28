using Xunit;
using Mockaco;
using FluentAssertions;

namespace Mockaco.Tests.Common
{
    public class RouteMatcherTest
    {
        private readonly RouteMatcher _routeMatcher;

        public RouteMatcherTest()
        {
            _routeMatcher = new RouteMatcher();
        }

        [Fact]
        public void Match_WithValidRouteTemplateAndPath_ReturnsRouteValues()
        {
            // Arrange
            var routeTemplate = "/users/{id}";
            var requestPath = "/users/123";

            // Act
            var result = _routeMatcher.Match(routeTemplate, requestPath);

            // Assert
            result.Should().NotBeNull();
            result.Should().ContainKey("id");
            result["id"].Should().Be("123");
        }

        [Fact]
        public void Match_WithMultipleParameters_ExtractsAllParameters()
        {
            // Arrange
            var routeTemplate = "/api/{version}/users/{id}";
            var requestPath = "/api/v1/users/456";

            // Act
            var result = _routeMatcher.Match(routeTemplate, requestPath);

            // Assert
            result.Should().NotBeNull();
            result.Should().ContainKey("version");
            result.Should().ContainKey("id");
            result["version"].Should().Be("v1");
            result["id"].Should().Be("456");
        }

        [Fact]
        public void Match_WithNonMatchingPath_ReturnsNull()
        {
            // Arrange
            var routeTemplate = "/users/{id}";
            var requestPath = "/products/123";

            // Act
            var result = _routeMatcher.Match(routeTemplate, requestPath);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Match_WithNullRouteTemplate_ReturnsNull()
        {
            // Arrange
            string? routeTemplate = null;
            var requestPath = "/users/123";

            // Act
            var result = _routeMatcher.Match(routeTemplate, requestPath);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Match_WithEmptyRouteTemplate_ReturnsNull()
        {
            // Arrange
            var routeTemplate = "";
            var requestPath = "/users/123";

            // Act
            var result = _routeMatcher.Match(routeTemplate, requestPath);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Match_WithWhitespaceRouteTemplate_ReturnsNull()
        {
            // Arrange
            var routeTemplate = "   ";
            var requestPath = "/users/123";

            // Act
            var result = _routeMatcher.Match(routeTemplate, requestPath);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Match_WithDefaultValueParameters_IncludesDefaults()
        {
            // Arrange
            var routeTemplate = "/users/{id=1}";
            var requestPath = "/users/1";

            // Act
            var result = _routeMatcher.Match(routeTemplate, requestPath);

            // Assert
            result.Should().NotBeNull();
            result.Should().ContainKey("id");
        }

        [Fact]
        public void IsMatch_WithMatchingRoute_ReturnsTrue()
        {
            // Arrange
            var routeTemplate = "/users/{id}";
            var requestPath = "/users/123";

            // Act
            var result = _routeMatcher.IsMatch(routeTemplate, requestPath);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsMatch_WithNonMatchingRoute_ReturnsFalse()
        {
            // Arrange
            var routeTemplate = "/users/{id}";
            var requestPath = "/products/123";

            // Act
            var result = _routeMatcher.IsMatch(routeTemplate, requestPath);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Match_WithComplexRoutePattern_ExtractsParameters()
        {
            // Arrange
            var routeTemplate = "/api/{version}/users/{userId}/orders/{orderId}";
            var requestPath = "/api/v2/users/42/orders/999";

            // Act
            var result = _routeMatcher.Match(routeTemplate, requestPath);

            // Assert
            result.Should().NotBeNull();
            result.Should().ContainKey("version");
            result.Should().ContainKey("userId");
            result.Should().ContainKey("orderId");
            result["version"].Should().Be("v2");
            result["userId"].Should().Be("42");
            result["orderId"].Should().Be("999");
        }

        [Fact]
        public void Match_WithRootPath_Matches()
        {
            // Arrange
            var routeTemplate = "/";
            var requestPath = "/";

            // Act
            var result = _routeMatcher.Match(routeTemplate, requestPath);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public void Match_WithStaticRoute_Matches()
        {
            // Arrange
            var routeTemplate = "/api/health";
            var requestPath = "/api/health";

            // Act
            var result = _routeMatcher.Match(routeTemplate, requestPath);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public void Match_WithDifferentCasing_Matches()
        {
            // Arrange - ASP.NET Core routing is case-insensitive
            var routeTemplate = "/api/users";
            var requestPath = "/API/USERS";

            // Act
            var result = _routeMatcher.Match(routeTemplate, requestPath);

            // Assert
            result.Should().NotBeNull();
        }
    }
}
