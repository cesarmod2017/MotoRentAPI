using System.Security.Cryptography;

namespace MotoRent.Application.Helpers
{
    public static class NameGenerator
    {
        private static readonly List<string> FirstNames = new List<string>
        {
            "João", "Maria", "Pedro", "Ana", "Carlos", "Mariana", "José", "Fernanda", "Paulo", "Beatriz",
            "Lucas", "Juliana", "André", "Camila", "Felipe", "Gabriela", "Ricardo", "Isabela", "Daniel", "Larissa"
        };

        private static readonly List<string> LastNames = new List<string>
        {
            "Silva", "Santos", "Oliveira", "Souza", "Rodrigues", "Ferreira", "Alves", "Pereira", "Lima", "Gomes",
            "Costa", "Ribeiro", "Martins", "Carvalho", "Almeida", "Lopes", "Soares", "Fernandes", "Vieira", "Barbosa"
        };

        public static string GenerateFullName()
        {
            string firstName = GetRandomElement(FirstNames);
            string lastName = GetRandomElement(LastNames);

            return $"{firstName} {lastName}";
        }

        private static string GetRandomElement(List<string> list)
        {
            int index = RandomNumberGenerator.GetInt32(0, list.Count);
            return list[index];
        }

        public static void AddFirstName(string firstName)
        {
            if (!string.IsNullOrWhiteSpace(firstName) && !FirstNames.Contains(firstName))
            {
                FirstNames.Add(firstName);
            }
        }

        public static void AddLastName(string lastName)
        {
            if (!string.IsNullOrWhiteSpace(lastName) && !LastNames.Contains(lastName))
            {
                LastNames.Add(lastName);
            }
        }
    }
}
