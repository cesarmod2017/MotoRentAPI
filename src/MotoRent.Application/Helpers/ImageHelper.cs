namespace MotoRent.Application.Helpers
{
    public class ImageHelper
    {
        public static string ConvertImageToBase64(string relativePath)
        {
            // Caminho do projeto (trabalhando com o diretorio pai do bin durante a execucao do teste)
            var projectDirectory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;

            // Combina o caminho do projeto com o caminho relativo da imagem
            var absolutePath = Path.Combine(projectDirectory, relativePath);

            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException("Arquivo de imagem não encontrado no caminho especificado", absolutePath);
            }

            // Ler o arquivo como bytes
            byte[] imageBytes = File.ReadAllBytes(absolutePath);

            // Converte os bytes da imagem em uma string base64
            string base64String = Convert.ToBase64String(imageBytes);

            return base64String;
        }
    }


}
