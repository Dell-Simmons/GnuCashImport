using EbaySharp.Entities.Develop.KeyManagement.SigningKey;
using LocalDBConnections.StampDataDB.StampDataEntities;

namespace FeeBayConnectionTester.Extensions
{
    public static class SigningKeyExtensions
    {
        /// <summary>
        /// Convert EbaySharp API entity to database entity
        /// </summary>
        public static FeeBaySigningKeys? ToFeeBaySigningKey(this SigningKey apiKey)
        {
            if (apiKey == null) return null;

            return new FeeBaySigningKeys
            {
                SigningKeyId = apiKey.SigningKeyId,
                SigningKeyCipher = apiKey.SigningKeyCipher,
                PublicKey = apiKey.PublicKey,
                PrivateKey = apiKey.PrivateKey,
                JWE = apiKey.JWE,
                CreationTime = apiKey.CreationTime,
                ExpirationTime = apiKey.ExpirationTime
            };
        }
       // var dt = DateTimeOffset.FromUnixTimeSeconds(1604750712).DateTime;

        /// <summary>
        /// Convert database entity back to EbaySharp API entity
        /// </summary>
        public static SigningKey ToSigningKey(this FeeBaySigningKeys? dbKey)
        {
            if (dbKey == null) throw new ArgumentNullException(nameof(dbKey));

            return new SigningKey
            {
                SigningKeyId = dbKey.SigningKeyId,
                SigningKeyCipher = dbKey.SigningKeyCipher.HasValue ? (SigningKeyCipher)dbKey.SigningKeyCipher.Value : default,
                PublicKey = dbKey.PublicKey ?? string.Empty,
                PrivateKey = dbKey.PrivateKey ?? string.Empty,
                JWE = dbKey.JWE ?? string.Empty,
                CreationTime = (int?)dbKey.CreationTime ?? 0,
                ExpirationTime = (int?)dbKey.ExpirationTime ?? 0
            };
        }
    }
}
