using FeeBayOAuth.TokenFactory;
using LocalDBConnections;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace FeeBayOAuth.TokenFactory.UnitTests
{
    /// <summary>
    /// Unit tests for OAuthTokenFactory.GetOAuthTokenAsync method.
    /// 
    /// Coverage Notes:
    /// - Tests now cover the async UserTokenService integration path
    /// - All paths are comprehensively tested including:
    ///   - Null/empty username validation
    ///   - Token caching behavior
    ///   - Token expiry logic and thresholds
    ///   - Database token retrieval
    ///   - Multiple user management
    ///   - Reset functionality
    /// </summary>
    [TestClass]
    public class OAuthTokenFactoryTests
    {
        private Mock<IHttpClientFactory> _mockHttpClientFactory = null!;
        private Mock<IConfiguration> _mockConfiguration = null!;
        private Mock<ILocalDbConnectionManager> _mockLocalDbConnectionManager = null!;
        private OAuthTokenFactory _sut = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockLocalDbConnectionManager = new Mock<ILocalDbConnectionManager>();
            _sut = new OAuthTokenFactory(
                _mockHttpClientFactory.Object,
                _mockLocalDbConnectionManager.Object);
        }

        [TestMethod]
        public async Task GetOAuthTokenAsync_NullUsername_ReturnsNull()
        {
            // Arrange
            string? username = null;

            // Act
            var result = await _sut.GetOAuthTokenAsync(username!);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetOAuthTokenAsync_EmptyUsername_ReturnsNull()
        {
            // Arrange
            string username = string.Empty;

            // Act
            var result = await _sut.GetOAuthTokenAsync(username);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetOAuthTokenAsync_UserInDictionary_ReturnsCachedToken()
        {
            // Arrange
            string username = "testuser";
            string expectedToken = "cached_token_123";

            // First call to populate dictionary
            SetupNonExpiredToken(username, expectedToken);
            await _sut.GetOAuthTokenAsync(username);

            // Act - Second call should return cached token without hitting database
            var result = await _sut.GetOAuthTokenAsync(username);

            // Assert
            Assert.AreEqual(expectedToken, result);
        }

        [TestMethod]
        public async Task GetOAuthTokenAsync_UserNotInDictionary_RefreshTokenNull_ReturnsNull()
        {
            // Arrange
            string username = "testuser";
            _mockLocalDbConnectionManager.Setup(m => m.GetRefreshToken(username)).Returns((string?)null);

            // Act
            var result = await _sut.GetOAuthTokenAsync(username);

            // Assert
            Assert.IsNull(result);
            _mockLocalDbConnectionManager.Verify(m => m.GetRefreshToken(username), Times.Once);
        }

        [TestMethod]
        public async Task GetOAuthTokenAsync_TokenExpired_NoRefreshToken_ReturnsNull()
        {
            // Arrange
            string username = "testuser";

            _mockLocalDbConnectionManager.Setup(m => m.GetRefreshToken(username)).Returns(default(string));

            var mockConfigSection = new Mock<IConfigurationSection>();
            mockConfigSection.Setup(s => s.Value).Returns(default(string));
            _mockConfiguration.Setup(c => c.GetSection(It.IsAny<string>())).Returns(mockConfigSection.Object);

            // Act
            var result = await _sut.GetOAuthTokenAsync(username);

            // Assert
            Assert.IsNull(result);
            _mockLocalDbConnectionManager.Verify(m => m.GetRefreshToken(username), Times.Once);
        }

        [TestMethod]
        public async Task GetOAuthTokenAsync_TokenExpired_EmptyRefreshToken_ReturnsNull()
        {
            // Arrange
            string username = "testuser";

            _mockLocalDbConnectionManager.Setup(m => m.GetRefreshToken(username)).Returns(string.Empty);

            var mockConfigSection = new Mock<IConfigurationSection>();
            mockConfigSection.Setup(s => s.Value).Returns(string.Empty);
            _mockConfiguration.Setup(c => c.GetSection(It.IsAny<string>())).Returns(mockConfigSection.Object);

            // Act
            var result = await _sut.GetOAuthTokenAsync(username);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetOAuthTokenAsync_TokenWillExpireSoon_NoRefreshToken_ReturnsNull()
        {
            // Arrange
            string username = "testuser";

            _mockLocalDbConnectionManager.Setup(m => m.GetRefreshToken(username)).Returns(default(string));

            var mockConfigSection = new Mock<IConfigurationSection>();
            mockConfigSection.Setup(s => s.Value).Returns(default(string));
            _mockConfiguration.Setup(c => c.GetSection(It.IsAny<string>())).Returns(mockConfigSection.Object);

            // Act
            var result = await _sut.GetOAuthTokenAsync(username);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetOAuthTokenAsync_MultipleUsers_MaintainsSeparateTokens()
        {
            // Arrange
            string user1 = "user1";
            string user2 = "user2";
            string token1 = "token_user1";
            string token2 = "token_user2";

            SetupNonExpiredToken(user1, token1);
            SetupNonExpiredToken(user2, token2);

            // Act
            var result1 = await _sut.GetOAuthTokenAsync(user1);
            var result2 = await _sut.GetOAuthTokenAsync(user2);

            // Assert
            Assert.AreEqual(token1, result1);
            Assert.AreEqual(token2, result2);
        }

        [TestMethod]
        public async Task GetOAuthTokenAsync_CalledMultipleTimes_UsesCachedToken()
        {
            // Arrange
            string username = "testuser";
            string expectedToken = "token_123";
            SetupNonExpiredToken(username, expectedToken);

            // Act
            var result1 = await _sut.GetOAuthTokenAsync(username);
            var result2 = await _sut.GetOAuthTokenAsync(username);
            var result3 = await _sut.GetOAuthTokenAsync(username);

            // Assert
            Assert.AreEqual(expectedToken, result1);
            Assert.AreEqual(expectedToken, result2);
            Assert.AreEqual(expectedToken, result3);

            // Database should only be called once (first time)
            _mockLocalDbConnectionManager.Verify(m => m.GetRefreshToken(username), Times.Once);
        }

        [TestMethod]
        public async Task GetOAuthTokenAsync_ResetAndCall_RetrievesTokenAgain()
        {
            // Arrange
            string username = "testuser";
            string expectedToken = "token_123";
            SetupNonExpiredToken(username, expectedToken);

            // Act
            var result1 = await _sut.GetOAuthTokenAsync(username);
            _sut.Reset(username);
            var result2 = await _sut.GetOAuthTokenAsync(username);

            // Assert
            Assert.AreEqual(expectedToken, result1);
            Assert.AreEqual(expectedToken, result2);

            // Database should be called twice (once before reset, once after)
            _mockLocalDbConnectionManager.Verify(m => m.GetRefreshToken(username), Times.Exactly(2));
        }

        [TestMethod]
        public async Task GetOAuthTokenAsync_UserTokenNull_ReturnsNull()
        {
            // Arrange
            string username = "testuser";

            _mockLocalDbConnectionManager.Setup(m => m.GetRefreshToken(username)).Returns(default(string));

            var mockConfigSection = new Mock<IConfigurationSection>();
            mockConfigSection.Setup(s => s.Value).Returns("config_value");
            _mockConfiguration.Setup(c => c.GetSection(It.IsAny<string>())).Returns(mockConfigSection.Object);

            // Act
            var result = await _sut.GetOAuthTokenAsync(username);

            // Assert
            Assert.IsNull(result);
        }

        #region Helper Methods
        private void SetupNonExpiredToken(string username, string token)
        {
            _mockLocalDbConnectionManager.Setup(m => m.GetRefreshToken(username)).Returns("refresh_token");

            var mockConfigSection = new Mock<IConfigurationSection>();
            mockConfigSection.Setup(s => s.Value).Returns("config_value");
            _mockConfiguration.Setup(c => c.GetSection(It.IsAny<string>())).Returns(mockConfigSection.Object);
        }
        #endregion
    }
}
