using AutoMapper;
using MotoRent.Application.DTOs.Deliveryman;
using MotoRent.Application.DTOs.Motorcycle;
using MotoRent.Application.DTOs.Rental;
using MotoRent.Infrastructure.Data.Models;

namespace MotoRent.Application.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<MotorcycleModel, MotorcycleDto>().ReverseMap();
            CreateMap<CreateMotorcycleDto, MotorcycleModel>();

            CreateMap<DeliverymanModel, DeliverymanDto>().ReverseMap();
            CreateMap<CreateDeliverymanDto, DeliverymanModel>();

            CreateMap<RentalModel, RentalDto>().ReverseMap();
            CreateMap<CreateRentalDto, RentalModel>();
        }
    }
}