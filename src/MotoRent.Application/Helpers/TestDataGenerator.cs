using System.Security.Cryptography;

namespace MotoRent.Application.Helpers
{
    public static class TestDataGenerator
    {
        private static readonly List<int> AllowedPlans = new List<int> { 7, 15, 30, 45, 50 };

        public static int GetRandomPlan()
        {
            int index = RandomNumberGenerator.GetInt32(0, AllowedPlans.Count);
            return AllowedPlans[index];
        }

        public static string GenerateUniqueIdentifier(string prefix)
        {
            return $"{prefix}{DateTime.Now:yyyyMMddHHmmssfff}";
        }

    }
}
