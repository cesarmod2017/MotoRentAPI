using FluentValidation;
using MotoRent.Application.DTOs.Deliveryman;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Png;
using System.Text.RegularExpressions;

namespace MotoRent.Application.Validators
{
    public class CreateDeliverymanDtoValidator : AbstractValidator<CreateDeliverymanDto>
    {
        public CreateDeliverymanDtoValidator()
        {
            RuleFor(x => x.Identifier)
                .NotEmpty().WithMessage("O identificador é obrigatório.")
                .MaximumLength(50).WithMessage("O identificador não deve exceder 50 caracteres.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("O nome é obrigatório.")
                .MaximumLength(100).WithMessage("O nome não deve exceder 100 caracteres.");

            RuleFor(x => x.CNPJ)
                .NotEmpty().WithMessage("O CNPJ é obrigatório.")
                .Must(BeValidCNPJ).WithMessage("O CNPJ fornecido não é válido.");

            RuleFor(x => x.BirthDate)
                .NotEmpty().WithMessage("A data de nascimento é obrigatória.")
                .LessThan(DateTime.Now.AddYears(-18)).WithMessage("O entregador deve ter pelo menos 18 anos de idade.");

            RuleFor(x => x.LicenseNumber)
                .NotEmpty().WithMessage("O número da licença é obrigatório.")
                .Must(BeValidLicenseNumber).WithMessage("O número da licença fornecido não é válido.");

            RuleFor(x => x.LicenseType)
                .NotEmpty().WithMessage("O tipo de licença é obrigatório.")
                .Must(x => x == "A" || x == "B" || x == "AB")
                .WithMessage("O tipo de licença deve ser A, B ou AB.");

            RuleFor(x => x.LicenseImage)
                .NotEmpty().WithMessage("A imagem da licença é obrigatória.")
                .Must(BeAValidImage).WithMessage("A imagem da licença deve estar em formato PNG ou BMP.");
        }

        private bool BeValidCNPJ(string cnpj)
        {
            cnpj = new string(cnpj.Where(char.IsDigit).ToArray());

            if (cnpj.Length != 14)
                return false;

            int[] multiplier1 = new int[12] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplier2 = new int[13] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

            string tempCnpj = cnpj.Substring(0, 12);
            int sum = 0;

            for (int i = 0; i < 12; i++)
                sum += int.Parse(tempCnpj[i].ToString()) * multiplier1[i];

            int remainder = (sum % 11);
            if (remainder < 2)
                remainder = 0;
            else
                remainder = 11 - remainder;

            string digit = remainder.ToString();
            tempCnpj = tempCnpj + digit;
            sum = 0;
            for (int i = 0; i < 13; i++)
                sum += int.Parse(tempCnpj[i].ToString()) * multiplier2[i];

            remainder = (sum % 11);
            if (remainder < 2)
                remainder = 0;
            else
                remainder = 11 - remainder;

            digit = digit + remainder.ToString();

            return cnpj.EndsWith(digit);
        }

        private bool BeValidLicenseNumber(string licenseNumber)
        {
            return Regex.IsMatch(licenseNumber, @"^\d{11}$");
        }

        private bool BeAValidImage(string licenseImage)
        {
            if (string.IsNullOrEmpty(licenseImage))
                return false;

            var base64Data = licenseImage.Split(',').Last();
            var imageBytes = Convert.FromBase64String(base64Data);

            try
            {
                using (var stream = new MemoryStream(imageBytes))
                {
                    IImageFormat format = Image.DetectFormat(stream);
                    return format is PngFormat || format is BmpFormat;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}