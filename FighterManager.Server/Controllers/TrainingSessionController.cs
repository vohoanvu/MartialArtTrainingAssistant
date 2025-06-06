using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FighterManager.Server.Helpers;
using FighterManager.Server.Models.Dtos;
using System.Net;
using System.Security.Claims;
using SharedEntities.Models;
using FighterManager.Server.Domain.AttendanceService;

namespace FighterManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TrainingSessionController(IUnitOfWork unitOfWork, IMapper objectMapper, IAttendanceService attendanceService) : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMapper _objectMapper = objectMapper;
        private readonly IAttendanceService _attendanceService = attendanceService;

        [HttpGet]
        public async Task<ActionResult<List<TrainingSessionDtoBase>>> GetSessionsAsync()
        {
            var authUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var appUser = await _unitOfWork.AppDbContext.Users.Include(u => u.Fighter).FirstAsync(u => u.Id == authUserId);
            var allSessions = await _unitOfWork.AppDbContext.TrainingSessions.Where(ts => ts.InstructorId == appUser.Fighter!.Id)
                .Include(ts => ts.Students!)
                .ToListAsync();

            return Ok(_objectMapper.Map<List<TrainingSession>, List<TrainingSessionDtoBase>>(allSessions));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GetSessionDetailResponse>> GetSessionAsync(int id)
        {
            var session = await _unitOfWork.AppDbContext.TrainingSessions.Where(ts => ts.Id == id)
                .Include(ts => ts.Students!).ThenInclude(joint => joint.Fighter)
                .Include(ts => ts.Instructor)
                .FirstOrDefaultAsync();
            if (session == null)
                return NotFound();

            var getSessionDetailsResponse = _objectMapper.Map<TrainingSession, GetSessionDetailResponse>(session);
            getSessionDetailsResponse.IsCurriculumGenerated = session.RawCurriculumJson != null && session.RawCurriculumJson.Length > 0;
            getSessionDetailsResponse.RawFighterPairsJson = session.RawFighterPairsJson;

            return Ok(getSessionDetailsResponse);
        }

        [HttpPost]
        public async Task<ActionResult<TrainingSessionDtoBase>> CreateSessionAsync(TrainingSessionDtoBase input)
        {
            if (input.Status != null && !Enum.IsDefined(typeof(SessionStatus), input.Status))
            {
                return BadRequest("Invalid status value");
            }

            var authUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var appUser = await _unitOfWork.AppDbContext.Users
                .Include(u => u.Fighter).FirstOrDefaultAsync(u => u.Id == authUserId);

            if (appUser?.Fighter == null || appUser.Fighter.Role != FighterRole.Instructor)
            {
                return NotFound(new { Message = "Only an Instructor can create new sessions!" });
            }

            input.InstructorId = appUser!.Fighter!.Id;
            var trainingSession = _objectMapper.Map<TrainingSessionDtoBase, TrainingSession>(input);
            trainingSession.Status = Enum.TryParse<SessionStatus>(input.Status, out var status) ? status : SessionStatus.Active;
            await _unitOfWork.Repository<TrainingSession>().AddAsync(trainingSession);
            await _unitOfWork.SaveChangesAsync();

            return StatusCode(201, input);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<GetSessionDetailResponse>> UpdateSessionAsync(int id, UpdateSessionDetailsRequest input)
        {
            if (input.Status != null && !Enum.IsDefined(typeof(SessionStatus), input.Status) ||
                input.TargetLevel != null && !Enum.IsDefined(typeof(TargetLevel), input.TargetLevel))
            {
                return BadRequest(new { Message = "Invalid Enum values" });
            }

            var existingSession = await _unitOfWork.Repository<TrainingSession>().GetByIdAsync(id);
            if (existingSession == null)
                return NotFound(new { Message = "Training session not found!" });

            existingSession.Update(input, _unitOfWork.AppDbContext);

            _unitOfWork.Repository<TrainingSession>().Update(existingSession);
            await _unitOfWork.SaveChangesAsync();

            var updatedSession = await _unitOfWork.AppDbContext.TrainingSessions.Where(ts => ts.Id == id)
                .Include(ts => ts.Students!).ThenInclude(joint => joint.Fighter)
                .Include(ts => ts.Instructor)
                .FirstOrDefaultAsync();
            if (updatedSession != null)
            {
                var sessionDetailResponse = _objectMapper.Map<GetSessionDetailResponse>(updatedSession);

                return Ok(sessionDetailResponse);
            }

            throw new ErrorResponseException().SetStatusCode(HttpStatusCode.UnprocessableEntity)
                .SetMessage("Cannot fetch the newly updated Session with related properties.");
        }

        [HttpPatch("{id}/close")]
        public async Task<ActionResult> CloseSessionAsync(int id)
        {
            try
            {
                var session = await _unitOfWork.Repository<TrainingSession>().GetByIdAsync(id);
                if (session == null)
                    return NotFound();

                session.Status = SessionStatus.Completed;
                _unitOfWork.Repository<TrainingSession>().Update(session);
                await _unitOfWork.SaveChangesAsync();

                return NoContent();
            }
            catch (ErrorResponseException ex)
            {
                return StatusCode((int)ex.StatusCode, new { ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteSessionAsync(int id)
        {
            try
            {
                var session = await _unitOfWork.Repository<TrainingSession>().GetByIdAsync(id);
                if (session == null)
                    return NotFound();

                _unitOfWork.AppDbContext.TrainingSessions.Remove(session);
                await _unitOfWork.SaveChangesAsync();

                return NoContent();
            }
            catch (ErrorResponseException ex)
            {
                return StatusCode((int)ex.StatusCode, new { ex.Message });
            }
        }

        [HttpPost("{id}/attendance")]
        [ProducesResponseType(typeof(TakeAttendanceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TakeAttendanceResponse>> TakeAttendance(
            int id,
            TakeAttendanceRequest request)
        {
            try
            {
                var instructorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(instructorUserId))
                    return Unauthorized();

                var response = await _attendanceService.ProcessAttendanceAsync(
                    id,
                    request.Records,
                    instructorUserId);

                if (!response.Success)
                    return BadRequest(new { response.Message });

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }


        [HttpDelete("{id}/attendance/remove/{fighterId}")]
        public async Task<IActionResult> RemoveStudentAttendanceAsync(int id, int fighterId)
        {
            if (fighterId <= 0)
            {
                return BadRequest("Invalid Fighter ID");
            }

            var session = await _unitOfWork.AppDbContext.TrainingSessions
                .Include(ts => ts.Students)
                .FirstOrDefaultAsync(ts => ts.Id == id);
            if (session == null)
            {
                return NotFound("Training session not found");
            }

            var student = session.Students?.FirstOrDefault(s => s.FighterId == fighterId);
            if (student == null)
            {
                return NotFound("Fighter not found in this session");
            }

            _unitOfWork.AppDbContext.TrainingSessionFighterJoints.Remove(student);
            await _unitOfWork.AppDbContext.SaveChangesAsync();

            return NoContent();
        }
    }

}
