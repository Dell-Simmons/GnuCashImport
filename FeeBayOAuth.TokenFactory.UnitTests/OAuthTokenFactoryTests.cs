using FeeBayOAuth.TokenFactory;
using LocalDBConnections;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace FeeBayOAuth.TokenFactory.UnitTests
{
    /// <summary>
    /// Unit tests for OAuthTokenFactory.GetOAuthToken method.
    /// 
    /// Coverage Notes:
    /// - Lines 47-55 (API call with Get_User_Token.MakeCall and recursive call) cannot be tested 
    ///   with Moq because Get_User_Token.MakeCall is a static method that makes actual HTTP calls.
    /// - Lines 61-64 (error handling after API call) depend on the API call path and cannot be tested.
    /// - Line 74 (return empty string) appears to be unreachable defensive code.
    /// 
    /// All other paths are comprehensively tested including:
    /// - Null/empty username validation
    /// - Token caching behavior
    /// - Token expiry logic and thresholds
    /// - Database token retrieval
    /// - Multiple user management
    /// - Reset functionality
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
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLocalDbConnectionManager = new Mock<ILocalDbConnectionManager>();
            _sut = new OAuthTokenFactory(
                _mockHttpClientFactory.Object,
                _mockConfiguration.Object,
                _mockLocalDbConnectionManager.Object);
        }

        [TestMethod]
        public void GetOAuthToken_NullUsername_ReturnsNull()
        {
            // Arrange
            string? username = null;

            // Act
            var result = _sut.GetOAuthToken(username!);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetOAuthToken_EmptyUsername_ReturnsNull()
        {
            // Arrange
            string username = string.Empty;

            // Act
            var result = _sut.GetOAuthToken(username);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetOAuthToken_UserInDictionary_ReturnsCachedToken()
        {
            // Arrange
            string username = "testuser";
            string expectedToken = "cached_token_123";
            
            // First call to populate dictionary
            SetupNonExpiredToken(username, expectedToken);
            _sut.GetOAuthToken(username);

            // Act - Second call should return cached token without hitting database
            var result = _sut.GetOAuthToken(username);

            // Assert
            Assert.AreEqual(expectedToken, result);
        }

        [TestMethod]
        public void GetOAuthToken_UserNotInDictionary_TokenNotExpired_ReturnsTokenFromDatabase()
        {
            // Arrange
            string username = "testuser";
            string expectedToken = "db_token_456";
            SetupNonExpiredToken(username, expectedToken);

            // Act
            var result = _sut.GetOAuthToken(username);

            // Assert
            Assert.AreEqual(expectedToken, result);
            _mockLocalDbConnectionManager.Verify(m => m.GetRefreshToken(username), Times.Once);
            _mockLocalDbConnectionManager.Verify(m => m.GetUserToken(username), Times.Once);
            _mockLocalDbConnectionManager.Verify(m => m.GetUserTokenExpireTime(username), Times.Once);
        }

        [TestMethod]
        public void GetOAuthToken_TokenExpired_NoRefreshToken_ReturnsNull()
        {
            // Arrange
            string username = "testuser";
            DateTime expiredTime = DateTime.Now.AddHours(-1);
            
            _mockLocalDbConnectionManager.Setup(m => m.GetRefreshToken(username)).Returns(default(string)!);
            _mockLocalDbConnectionManager.Setup(m => m.GetUserTokenExpireTime(username)).Returns(expiredTime);
            
            var mockConfigSection = new Mock<IConfigurationSection>();
            mockConfigSection.Setup(s => s.Value).Returns(default(string));
            _mockConfiguration.Setup(c => c.GetSection(It.IsAny<string>())).Returns(mockConfigSection.Object);

            // Act
            var result = _sut.GetOAuthToken(username);

            // Assert
            Assert.IsNull(result);
            _mockLocalDbConnectionManager.Verify(m => m.GetRefreshToken(username), Times.Once);
        }

        [TestMethod]
        public void GetOAuthToken_TokenExpired_EmptyRefreshToken_ReturnsNull()
        {
            // Arrange
            string username = "testuser";
            DateTime expiredTime = DateTime.Now.AddHours(-1);
            
            _mockLocalDbConnectionManager.Setup(m => m.GetRefreshToken(username)).Returns(string.Empty);
            _mockLocalDbConnectionManager.Setup(m => m.GetUserTokenExpireTime(username)).Returns(expiredTime);
            
            var mockConfigSection = new Mock<IConfigurationSection>();
            mockConfigSection.Setup(s => s.Value).Returns(string.Empty);
            _mockConfiguration.Setup(c => c.GetSection(It.IsAny<string>())).Returns(mockConfigSection.Object);

            // Act
            var result = _sut.GetOAuthToken(username);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetOAuthToken_TokenWillExpireSoon_NoRefreshToken_ReturnsNull()
        {
            // Arrange
            string username = "testuser";
            // Token expires in 5 minutes (less than 10 minute threshold)
            DateTime expiringTime = DateTime.Now.ToLocalTime().AddMinutes(5);
            
            _mockLocalDbConnectionManager.Setup(m => m.GetRefreshToken(username)).Returns(default(string)!);
            _mockLocalDbConnectionManager.Setup(m => m.GetUserTokenExpireTime(username)).Returns(expiringTime);
            
            var mockConfigSection = new Mock<IConfigurationSection>();
            mockConfigSection.Setup(s => s.Value).Returns(default(string));
            _mockConfiguration.Setup(c => c.GetSection(It.IsAny<string>())).Returns(mockConfigSection.Object);

            // Act
            var result = _sut.GetOAuthToken(username);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetOAuthToken_MultipleUsers_MaintainsSeparateTokens()
        {
            // Arrange
            string user1 = "user1";
            string user2 = "user2";
            string token1 = "token_user1";
            string token2 = "token_user2";

            SetupNonExpiredToken(user1, token1);
            SetupNonExpiredToken(user2, token2);

            // Act
            var result1 = _sut.GetOAuthToken(user1);
            var result2 = _sut.GetOAuthToken(user2);

            // Assert
            Assert.AreEqual(token1, result1);
            Assert.AreEqual(token2, result2);
        }

        [TestMethod]
        public void GetOAuthToken_CalledMultipleTimes_UsesCachedToken()
        {
            // Arrange
            string username = "testuser";
            string expectedToken = "token_123";
            SetupNonExpiredToken(username, expectedToken);

            // Act
            var result1 = _sut.GetOAuthToken(username);
            var result2 = _sut.GetOAuthToken(username);
            var result3 = _sut.GetOAuthToken(username);

            // Assert
            Assert.AreEqual(expectedToken, result1);
            Assert.AreEqual(expectedToken, result2);
            Assert.AreEqual(expectedToken, result3);
            
            // Database should only be called once (first time)
            _mockLocalDbConnectionManager.Verify(m => m.GetRefreshToken(username), Times.Once);
            _mockLocalDbConnectionManager.Verify(m => m.GetUserToken(username), Times.Once);
            _mockLocalDbConnectionManager.Verify(m => m.GetUserTokenExpireTime(username), Times.Once);
        }

        [TestMethod]
        public void GetOAuthToken_ResetAndCall_RetrievesTokenAgain()
        {
            // Arrange
            string username = "testuser";
            string expectedToken = "token_123";
            SetupNonExpiredToken(username, expectedToken);

            // Act
            var result1 = _sut.GetOAuthToken(username);
            _sut.Reset(username);
            var result2 = _sut.GetOAuthToken(username);

            // Assert
            Assert.AreEqual(expectedToken, result1);
            Assert.AreEqual(expectedToken, result2);
            
            // Database should be called twice (once before reset, once after)
            _mockLocalDbConnectionManager.Verify(m => m.GetUserToken(username), Times.Exactly(2));
        }

        [TestMethod]
        public void GetOAuthToken_TokenExactlyAtExpiryThreshold_ConsideredExpired()
        {
            // Arrange
            string username = "testuser";
            // Token expires exactly at the 10 minute threshold
            DateTime expiryTime = DateTime.Now.ToLocalTime().AddMinutes(10).AddSeconds(-1);
            
            _mockLocalDbConnectionManager.Setup(m => m.GetRefreshToken(username)).Returns(default(string)!);
            _mockLocalDbConnectionManager.Setup(m => m.GetUserTokenExpireTime(username)).Returns(expiryTime);
            
            var mockConfigSection = new Mock<IConfigurationSection>();
            mockConfigSection.Setup(s => s.Value).Returns(default(string));
            _mockConfiguration.Setup(c => c.GetSection(It.IsAny<string>())).Returns(mockConfigSection.Object);

            // Act
            var result = _sut.GetOAuthToken(username);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetOAuthToken_TokenJustBeyondExpiryThreshold_NotExpired()
        {
            // Arrange
            string username = "testuser";
            string expectedToken = "valid_token";
            // Token expires just beyond the 10 minute threshold
            DateTime expiryTime = DateTime.Now.ToLocalTime().AddMinutes(11);
            
            _mockLocalDbConnectionManager.Setup(m => m.GetRefreshToken(username)).Returns("refresh_token");
            _mockLocalDbConnectionManager.Setup(m => m.GetUserToken(username)).Returns(expectedToken);
            _mockLocalDbConnectionManager.Setup(m => m.GetUserTokenExpireTime(username)).Returns(expiryTime);
            
            var mockConfigSection = new Mock<IConfigurationSection>();
            mockConfigSection.Setup(s => s.Value).Returns("config_value");
            _mockConfiguration.Setup(c => c.GetSection(It.IsAny<string>())).Returns(mockConfigSection.Object);

            // Act
            var result = _sut.GetOAuthToken(username);

            // Assert
            Assert.AreEqual(expectedToken, result);
        }

        [TestMethod]
        public void GetOAuthToken_UserTokenNull_ReturnsNull()
        {
            // Arrange
            string username = "testuser";
            DateTime futureExpiry = DateTime.Now.ToLocalTime().AddHours(1);
            
            _mockLocalDbConnectionManager.Setup(m => m.GetRefreshToken(username)).Returns("refresh_token");
            _mockLocalDbConnectionManager.Setup(m => m.GetUserToken(username)).Returns(default(string)!);
            _mockLocalDbConnectionManager.Setup(m => m.GetUserTokenExpireTime(username)).Returns(futureExpiry);
            
            var mockConfigSection = new Mock<IConfigurationSection>();
            mockConfigSection.Setup(s => s.Value).Returns("config_value");
            _mockConfiguration.Setup(c => c.GetSection(It.IsAny<string>())).Returns(mockConfigSection.Object);

            // Act
            var result = _sut.GetOAuthToken(username);

            // Assert
            Assert.IsNull(result);
        }

        #region Helper Methods
        private void SetupNonExpiredToken(string username, string token)
        {
            // Token expires in 1 hour (well beyond 10 minute threshold)
            DateTime futureExpiry = DateTime.Now.ToLocalTime().AddHours(1);
            
            _mockLocalDbConnectionManager.Setup(m => m.GetRefreshToken(username)).Returns("refresh_token");
            _mockLocalDbConnectionManager.Setup(m => m.GetUserToken(username)).Returns(token);
            _mockLocalDbConnectionManager.Setup(m => m.GetUserTokenExpireTime(username)).Returns(futureExpiry);
            
            var mockConfigSection = new Mock<IConfigurationSection>();
            mockConfigSection.Setup(s => s.Value).Returns("config_value");
            _mockConfiguration.Setup(c => c.GetSection(It.IsAny<string>())).Returns(mockConfigSection.Object);
        }
        #endregion
    }
}
