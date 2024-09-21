namespace MotoRent.Application.Helpers
{
    public class CnpjGenerator
    {
        public static string GenerateCnpj()
        {
            Random random = new Random();
            int[] cnpj = new int[14];

            // Gera os 12 primeiros dígitos
            for (int i = 0; i < 12; i++)
            {
                cnpj[i] = random.Next(0, 10);
            }

            // Calcula o primeiro dígito verificador
            cnpj[12] = CalculateCnpjDigit(cnpj, new int[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 });

            // Calcula o segundo dígito verificador
            cnpj[13] = CalculateCnpjDigit(cnpj, new int[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 });

            // Formata o CNPJ em string no formato 00.000.000/0000-00
            return $"{cnpj[0]}{cnpj[1]}{cnpj[2]}{cnpj[3]}{cnpj[4]}{cnpj[5]}{cnpj[6]}{cnpj[7]}{cnpj[8]}{cnpj[9]}{cnpj[10]}{cnpj[11]}{cnpj[12]}{cnpj[13]}";
        }

        private static int CalculateCnpjDigit(int[] cnpj, int[] weights)
        {
            int sum = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                sum += cnpj[i] * weights[i];
            }

            int remainder = sum % 11;
            return remainder < 2 ? 0 : 11 - remainder;
        }



    }
}
