using AutoMapper;
using FighterManager.Server.Models.Dtos;
using SharedEntities.Models;

namespace FighterManager.Server.Helpers;

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
            .ForMember(x => x.Status, y => y.MapFrom(z => string.IsNullOrEmpty(z.Status) ? SessionStatus.Active : Enum.Parse<SessionStatus>(z.Status) ))
            .ForMember(dest => dest.TrainingDate, opt => opt.MapFrom(src => 
                src.TrainingDate.HasValue ? DateTime.SpecifyKind(src.TrainingDate.Value, DateTimeKind.Utc) 
                : (DateTime?)null)
            )
            .ForMember(dest => dest.Capacity, opt => opt.MapFrom(src => src.Capacity ?? default))
            .ForMember(dest => dest.Duration, opt => opt.MapFrom(src => src.Duration ?? default))
            .ForMember(dest => dest.InstructorId, opt => opt.MapFrom(src => src.InstructorId ?? default))
            .ReverseMap()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<TrainingSession, GetSessionDetailResponse>()
            .ForMember(dest => dest.StudentIds, opt => opt.MapFrom(src => src.Students.Select(s => s.FighterId).ToList()))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.SessionNotes));
        CreateMap<TrainingSession, TrainingSessionDtoBase>()
            .ForMember(dest => dest.StudentIds, opt => opt.MapFrom(src => src.Students.Select(s => s.FighterId).ToList()))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.SessionNotes));

        CreateMap<TrainingSessionFighterJoint, ViewFighterDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Fighter!.Id))
            .ForMember(dest => dest.FighterName, opt => opt.MapFrom(src => src.Fighter!.FighterName))
            .ForMember(dest => dest.BeltColor, opt => opt.MapFrom(src => src.Fighter!.BelkRank.ToString()))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Fighter!.Gender.ToString()))
            .ForMember(dest => dest.FighterRole, opt => opt.MapFrom(src => src.Fighter!.Role.ToString()))
            .ForMember(dest => dest.Weight, opt => opt.MapFrom(src => src.Fighter!.Weight))
            .ForMember(dest => dest.Height, opt => opt.MapFrom(src => src.Fighter!.Height))
            .ForMember(dest => dest.MaxWorkoutDuration, opt => opt.MapFrom(src => src.Fighter!.MaxWorkoutDuration))
            .ForMember(dest => dest.Experience, opt => opt.MapFrom(src => src.Fighter!.Experience.ToString()))
            .ForMember(dest => dest.BMI, opt => opt.MapFrom(src => src.Fighter!.BMI));
    }
}
