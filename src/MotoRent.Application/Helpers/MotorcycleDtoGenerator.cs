using MotoRent.Application.DTOs.Motorcycle;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace MotoRent.Application.Helpers
{
    public static class MotorcycleDtoGenerator
    {
        private static readonly ConcurrentDictionary<string, byte> UsedIdentifiers = new ConcurrentDictionary<string, byte>();
        private static readonly ConcurrentDictionary<string, byte> UsedLicensePlates = new ConcurrentDictionary<string, byte>();
        private static int LastIdentifierNumber = 0;

        private static readonly List<string> MotorcycleModels = new List<string>
        {
            "Honda CG 160", "Yamaha Factor 150", "Honda Biz 125", "Yamaha MT-03", "Honda CB 500F",
            "Kawasaki Ninja 400", "Suzuki GSX-S750", "BMW G 310 R", "Harley-Davidson Iron 883",
            "Triumph Street Triple", "Ducati Monster", "Honda XRE 300", "Yamaha Fazer 250", "Honda CB 250F Twister"
        };

        private static readonly string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public static CreateMotorcycleDto Generate()
        {
            return new CreateMotorcycleDto
            {
                Identifier = GenerateUniqueIdentifier(),
                Year = GenerateRandomYear(),
                Model = GetRandomModel(),
                LicensePlate = GenerateUniqueLicensePlate()
            };
        }

        private static string GenerateUniqueIdentifier()
        {
            string identifier;
            do
            {
                StringBuilder sb = new StringBuilder("MOTO");
                for (int i = 0; i < 5; i++)
                {
                    sb.Append(RandomNumberGenerator.GetInt32(0, 10));
                }
                identifier = sb.ToString();
            } while (!UsedIdentifiers.TryAdd(identifier, 0));

            return identifier;
        }
        private static int GenerateRandomYear()
        {
            return RandomNumberGenerator.GetInt32(2020, 2025); // 2025 because GetInt32 upper bound is exclusive
        }

        private static string GetRandomModel()
        {
            int index = RandomNumberGenerator.GetInt32(0, MotorcycleModels.Count);
            return MotorcycleModels[index];
        }

        private static string GenerateUniqueLicensePlate()
        {
            string licensePlate;
            do
            {
                StringBuilder plate = new StringBuilder();
                for (int i = 0; i < 3; i++)
                {
                    plate.Append(Letters[RandomNumberGenerator.GetInt32(0, Letters.Length)]);
                }
                plate.Append('-');
                for (int i = 0; i < 4; i++)
                {
                    plate.Append(RandomNumberGenerator.GetInt32(0, 10));
                }
                licensePlate = plate.ToString();
            } while (!UsedLicensePlates.TryAdd(licensePlate, 0));

            return licensePlate;
        }

        public static void ResetUsedIdentifiersAndPlates()
        {
            UsedIdentifiers.Clear();
            UsedLicensePlates.Clear();
            LastIdentifierNumber = 0;
        }
    }
}