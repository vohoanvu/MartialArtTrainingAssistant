using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SampleAspNetReactDockerApp.Server.Helpers;
using SampleAspNetReactDockerApp.Server.Models;
using SampleAspNetReactDockerApp.Server.Models.Dtos;

namespace SampleAspNetReactDockerApp.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FighterController(IUnitOfWork unitOfWork,
        IMapper objectMapper) : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMapper _objectMapper = objectMapper;

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<List<ViewFighterDto>>> GetListAsync()
        {
            var allFighters = await _unitOfWork.Repository<Fighter>().GetAllAsync();
            return Ok(_objectMapper.Map<List<Fighter>, List<ViewFighterDto>>(allFighters.ToList()));
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<ViewFighterDto>> GetAsync(int id)
        {
            var fighter = await _unitOfWork.Repository<Fighter>().GetByIdAsync(id);
            if (fighter == null)
                return NotFound();

            return Ok(_objectMapper.Map<Fighter, ViewFighterDto>(fighter));
        }

        [HttpPost]
        public async Task<ActionResult<ViewFighterDto>> CreateAsync(CreateFighterDto input)
        {
            if (!Enum.IsDefined(typeof(Gender), input.Gender) || 
                !Enum.IsDefined(typeof(FighterRole), input.FighterRole) ||
                !Enum.IsDefined(typeof(BeltColor), input.BeltColor))
            {
                return BadRequest(new { Message = "Invalid Enum values" });
            }

            var newFighter = _objectMapper.Map<CreateFighterDto, Fighter>(input);
            await _unitOfWork.Repository<Fighter>().AddAsync(newFighter);
            return CreatedAtAction(
                nameof(CreateAsync), 
                new { id = newFighter.Id }, 
                _objectMapper.Map<Fighter, ViewFighterDto>(newFighter)
            );
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<ViewFighterDto>> UpdateAsync(int id, UpdateFighterDto input)
        {
            if (!Enum.IsDefined(typeof(Gender), input.Gender) ||
                !Enum.IsDefined(typeof(FighterRole), input.FighterRole) ||
                !Enum.IsDefined(typeof(BeltColor), input.BeltColor))
            {
                return BadRequest(new { Message = "Invalid Enum values" });
            }

            var existingFighter = await _unitOfWork.Repository<Fighter>().GetByIdAsync(id);
            if (existingFighter == null)
            {
                return NotFound(new { Message = "Fighter Not Found!" });
            }

            existingFighter.Update(input);
            _unitOfWork.Repository<Fighter>().Update(existingFighter);
            await _unitOfWork.SaveChangesAsync();
            return Ok(_objectMapper.Map<Fighter, ViewFighterDto>(existingFighter));
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteAsync(int id)
        {
            try
            {
                var fighter = await _unitOfWork.Repository<Fighter>().GetByIdAsync(id);
                if (fighter == null)
                    return NotFound();

                _unitOfWork.Repository<Fighter>().Delete(fighter);
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
