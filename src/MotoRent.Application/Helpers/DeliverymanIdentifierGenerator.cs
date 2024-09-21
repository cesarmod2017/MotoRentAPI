using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace MotoRent.Application.Helpers
{
    public class DeliverymanIdentifierGenerator
    {
        private const string Prefix = "DEL";
        private const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int IdLength = 6; // 3 for prefix + 3 for unique identifier
        private static readonly ConcurrentDictionary<string, byte> UsedIds = new ConcurrentDictionary<string, byte>();

        public static string GenerateUniqueIdentifier()
        {
            string id;
            do
            {
                id = GenerateId();
            } while (!UsedIds.TryAdd(id, 0));

            return id;
        }

        private static string GenerateId()
        {
            var result = new StringBuilder(Prefix);

            for (int i = Prefix.Length; i < IdLength; i++)
            {
                var randomIndex = RandomNumberGenerator.GetInt32(0, Characters.Length);
                result.Append(Characters[randomIndex]);
            }

            return result.ToString();
        }

        // Optional: Method to reset used IDs (for testing purposes)
        public static void ResetUsedIds()
        {
            UsedIds.Clear();
        }
    }
}
