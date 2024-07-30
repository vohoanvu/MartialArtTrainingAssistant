using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SampleAspNetReactDockerApp.Server.Helpers;
using SampleAspNetReactDockerApp.Server.Models;
using SampleAspNetReactDockerApp.Server.Models.Dtos;
using SampleAspNetReactDockerApp.Server.Repository;

namespace SampleAspNetReactDockerApp.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FighterController(IFighterRepository fighterRepository,
        IMapper objectMapper) : ControllerBase
    {
        private readonly IFighterRepository _fighterRepository = fighterRepository;
        private readonly IMapper _objectMapper = objectMapper;

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<List<ViewFighterDto>>> GetListAsync()
        {
            var allFighters = await _fighterRepository.GetFighters();
            return Ok(_objectMapper.Map<List<Fighter>, List<ViewFighterDto>>(allFighters));
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<ViewFighterDto>> GetAsync(int id)
        {
            var fighter = await _fighterRepository.GetFighter(id);
            if (fighter == null)
            {
                return NotFound(new { Message = "Fighter Not Found!" });
            }

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
            var createdFighter = await _fighterRepository.AddFighter(newFighter);
            return CreatedAtAction(nameof(GetAsync), new { id = createdFighter.Id }, _objectMapper.Map<Fighter, ViewFighterDto>(createdFighter));
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

            var existingFighter = await _fighterRepository.GetFighter(id);
            if (existingFighter == null)
            {
                return NotFound(new { Message = "Fighter Not Found!" });
            }

            existingFighter.Update(input);
            var updatedFighter = await _fighterRepository.UpdateFighter(existingFighter);
            return Ok(_objectMapper.Map<Fighter, ViewFighterDto>(updatedFighter));
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteAsync(int id)
        {
            try
            {
                await _fighterRepository.DeleteFighter(id);
                return NoContent();
            }
            catch (ErrorResponseException ex)
            {
                return StatusCode((int)ex.StatusCode, new { ex.Message });
            }
        }
    }
}
