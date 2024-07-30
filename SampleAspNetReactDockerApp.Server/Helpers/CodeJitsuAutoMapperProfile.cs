using AutoMapper;
using SampleAspNetReactDockerApp.Server.Models;
using SampleAspNetReactDockerApp.Server.Models.Dtos;

namespace SampleAspNetReactDockerApp.Server.Helpers;

public class CodeJitsuAutoMapperProfile : Profile
{
    public CodeJitsuAutoMapperProfile()
    {
        /* Create your AutoMapper object mappings here */
        CreateMap<CreateFighterDto, Fighter>()
            .ForMember(dest => dest.Birthdate, opt => opt.MapFrom(src => DateTime.SpecifyKind(src.Birthdate, DateTimeKind.Utc)))
            .ForMember(dest => dest.BMI, opt => opt.MapFrom(src => src.BMI ?? (src.Weight / (src.Height * src.Height))))
            .ForMember(x => x.Gender, y => y.MapFrom(z => Enum.Parse<Gender>(z.Gender)))
            .ForMember(x => x.Role, y => y.MapFrom(z => Enum.Parse<FighterRole>(z.FighterRole)))
            .ForMember(x => x.BelkRank, y => y.MapFrom(z => Enum.Parse<BeltColor>(z.BeltColor)))
            .ReverseMap();

        CreateMap<UpdateFighterDto, Fighter>()
            .ReverseMap();

        CreateMap<FighterDtoBase, Fighter>().ReverseMap();

        CreateMap<Fighter, ViewFighterDto>()
            .ForMember(x => x.BeltColor, y => y.MapFrom(z => z.BelkRank.ToString()))
            .ForMember(x => x.Gender, y => y.MapFrom(z => z.Gender.ToString()))
            .ForMember(x => x.FighterRole, y => y.MapFrom(z => z.Role.ToString()))
            .ForMember(x => x.Experience, y => y.MapFrom(z => z.Experience.ToString()))
            .ForMember(x => x.Id, y => y.MapFrom(z => z.Id))
            .ReverseMap();

        CreateMap<TrainingSessionDtoBase, TrainingSession>()
            .ForMember(x => x.Status, y => y.MapFrom(z => Enum.Parse<SessionStatus>(z.Status)))
            .ForMember(dest => dest.TrainingDate, opt => opt.MapFrom(src => DateTime.SpecifyKind(src.TrainingDate, DateTimeKind.Utc)))
            .ReverseMap();
    }
}
