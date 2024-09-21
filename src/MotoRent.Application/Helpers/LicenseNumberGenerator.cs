namespace MotoRent.Application.Helpers
{
    public class LicenseNumberGenerator
    {
        public static string GenerateLicenseNumber()
        {
            Random random = new Random();
            string licenseNumber = string.Empty;

            // Gera 11 dígitos aleatórios
            for (int i = 0; i < 11; i++)
            {
                licenseNumber += random.Next(0, 10).ToString();
            }

            return licenseNumber;
        }


    }

}
