using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SampleAspNetReactDockerApp.Server.Helpers;
using SampleAspNetReactDockerApp.Server.Models;
using SampleAspNetReactDockerApp.Server.Models.Dtos;
using System.Net;
using System.Security.Claims;

namespace SampleAspNetReactDockerApp.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TrainingSessionController(IUnitOfWork unitOfWork, IMapper objectMapper) : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMapper _objectMapper = objectMapper;

        [HttpGet]
        public async Task<ActionResult<List<GetSessionDetailResponse>>> GetSessionsAsync()
        {
            var allSessions = await _unitOfWork.AppDbContext.TrainingSessions
                .Include(ts => ts.Students!).ToListAsync();
            
            return Ok(_objectMapper.Map<List<TrainingSession>, List<GetSessionDetailResponse>>(allSessions));
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
        
            return Ok(_objectMapper.Map<TrainingSession, GetSessionDetailResponse>(session));
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
            await _unitOfWork.Repository<TrainingSession>().AddAsync(trainingSession);
            await _unitOfWork.SaveChangesAsync();

            return StatusCode(201, input);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<GetSessionDetailResponse>> UpdateSessionAsync(int id, UpdateSessionDetailsRequest input)
        {
            if (input.Status != null && !Enum.IsDefined(typeof(SessionStatus), input.Status))
            {
                return BadRequest(new { Message = "Invalid Enum values" });
            }

            var existingSession = await _unitOfWork.Repository<TrainingSession>().GetByIdAsync(id);
            if (existingSession == null)
                return NotFound(new { Message = "Training session not found!" });

            existingSession.Update(input,_unitOfWork.AppDbContext);

            _unitOfWork.Repository<TrainingSession>().Update(existingSession);
            await _unitOfWork.SaveChangesAsync();

            var updatedSession = await _unitOfWork.AppDbContext.TrainingSessions.Where(ts => ts.Id == id)
                .Include(ts => ts.Students!).ThenInclude(joint => joint.Fighter)
                .FirstOrDefaultAsync();
            if (updatedSession != null)
            {
                 var sessionDetailResponse = _objectMapper.Map<GetSessionDetailResponse>(updatedSession);

                return Ok(sessionDetailResponse);
            }

            throw new ErrorResponseException().SetStatusCode(HttpStatusCode.UnprocessableEntity)
                .SetMessage("Cannot fetch the newly updated Session with related properties.");
        }

        [HttpDelete("{id}")]
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
    }

}
