using FluentValidation;
using MotoRent.Application.DTOs.Motorcycle;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Png;

namespace MotoRent.Application.Validators
{
    public class UpdateLicenseImageDtoValidator : AbstractValidator<UpdateLicenseImageDto>
    {
        public UpdateLicenseImageDtoValidator()
        {
            RuleFor(x => x.LicenseImage)
                .NotEmpty().WithMessage("A imagem da licença é obrigatória.")
                .Must(BeAValidImage).WithMessage("A imagem da licença deve estar em formato PNG ou BMP.");
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