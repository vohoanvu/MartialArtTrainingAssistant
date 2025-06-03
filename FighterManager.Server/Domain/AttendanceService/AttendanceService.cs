using AutoMapper;
using FighterManager.Server.Models.Dtos;
using FighterManager.Server.Repository;
using Microsoft.AspNetCore.Identity;
using SharedEntities.Models;

namespace FighterManager.Server.Domain.AttendanceService
{
    public interface IAttendanceService
    {
        Task<TakeAttendanceResponse> ProcessAttendanceAsync(
            int sessionId, 
            List<AttendanceRecordDto> records,
            string instructorUserId);
    }

    public class AttendanceService : IAttendanceService
    {
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IMapper _mapper;
        private readonly UserManager<AppUserEntity> _userManager;
        private readonly ILogger<AttendanceService> _logger;

        public AttendanceService(
            IAttendanceRepository attendanceRepository,
            IMapper mapper,
            UserManager<AppUserEntity> userManager,
            ILogger<AttendanceService> logger)
        {
            _attendanceRepository = attendanceRepository;
            _mapper = mapper;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<TakeAttendanceResponse> ProcessAttendanceAsync(
            int sessionId,
            List<AttendanceRecordDto> records,
            string instructorUserId)
        {
            try
            {
                _logger.LogInformation("Processing attendance for session {SessionId}", sessionId);
                // 1. Validate input records, session and instructor
                if (!records.Any())
                {
                    return new TakeAttendanceResponse
                    {
                        Success = false,
                        Message = "No attendance records provided"
                    };
                }

                var duplicateNames = records
                    .GroupBy(r => r.FighterName.ToLower())
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);

                if (duplicateNames.Any())
                {
                    return new TakeAttendanceResponse
                    {
                        Success = false,
                        Message = $"Duplicate fighter names found: {string.Join(", ", duplicateNames)}"
                    };
                }

                var session = await _attendanceRepository.GetSessionWithDetailsAsync(sessionId);
                if (session == null)
                {
                    return new TakeAttendanceResponse
                    {
                        Success = false,
                        Message = "Training Session not found"
                    };
                }
                var instructorUser = await _attendanceRepository.GetAppUserByFighterIdAsync(session.InstructorId);
                if (instructorUser?.Id != instructorUserId)
                {
                    return new TakeAttendanceResponse
                    {
                        Success = false,
                        Message = "Unauthorized User to access this session"
                    };
                }

                // 2. Process each attendance record
                var processedFighters = new List<Fighter>();
                foreach (var record in records)
                {
                    var fighter = await ProcessAttendanceRecord(record);
                    processedFighters.Add(fighter);
                }

                // 3. Create joint records (with duplicate check)
                foreach (var fighter in processedFighters)
                {
                    // Check if fighter is already associated with this session
                    var existingJoint = await _attendanceRepository.GetSessionFighterJointAsync(sessionId, fighter.Id);
                    if (existingJoint != null)
                    {
                        return new TakeAttendanceResponse
                        {
                            Success = false,
                            Message = $"Fighter '{fighter.FighterName}' is already registered for this session"
                        };
                    }

                    var joint = new TrainingSessionFighterJoint
                    {
                        TrainingSessionId = sessionId,
                        FighterId = fighter.Id
                    };
                    await _attendanceRepository.AddSessionFighterJointAsync(joint);
                }

                // 4. Save changes
                await _attendanceRepository.SaveChangesAsync();

                // 5. Return updated session
                var updatedSession = await _attendanceRepository.GetSessionWithDetailsAsync(sessionId);
                return new TakeAttendanceResponse
                {
                    Success = true,
                    Message = "Attendance recorded successfully",
                    UpdatedSession = _mapper.Map<GetSessionDetailResponse>(updatedSession)
                };
            }
            catch (Exception ex)
            {
                return new TakeAttendanceResponse
                {
                    Success = false,
                    Message = $"Error processing attendance: {ex.Message}"
                };
            }
        }

        private async Task<Fighter> ProcessAttendanceRecord(AttendanceRecordDto record)
        {
            var existingFighter = await _attendanceRepository.GetFighterByNameAsync(record.FighterName);
            if (existingFighter != null)
                return existingFighter;

            var fighter = _mapper.Map<Fighter>(record);
            fighter.Role = FighterRole.Student;
            fighter.Experience = DetermineExperienceLevel(Enum.Parse<BeltColor>(record.BeltColor));
            fighter.MaxWorkoutDuration = 5;
            fighter.IsWalkIn = true;

            return await _attendanceRepository.AddFighterAsync(fighter);
        }
        
        private static TrainingExperience DetermineExperienceLevel(BeltColor beltColor)
        {
            return beltColor switch
            {
                BeltColor.White => TrainingExperience.LessThanTwoYears,
                BeltColor.Blue => TrainingExperience.FromTwoToFiveYears,
                BeltColor.Purple => TrainingExperience.FromTwoToFiveYears,
                BeltColor.Brown => TrainingExperience.MoreThanFiveYears,
                BeltColor.Black => TrainingExperience.MoreThanFiveYears,
                _ => TrainingExperience.LessThanTwoYears
            };
        }
    }
}