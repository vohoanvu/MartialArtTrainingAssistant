using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SampleAspNetReactDockerApp.Server.Data;
using SampleAspNetReactDockerApp.Server.Helpers;
using SampleAspNetReactDockerApp.Server.Models;
using SampleAspNetReactDockerApp.Server.Models.Dtos;
using System.Net;

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
        public async Task<List<ViewFighterDto>> GetListAsync()
        {
            var allFighters = await _fighterRepository.GetFighters();
            return _objectMapper.Map<List<Fighter>, List<ViewFighterDto>>(allFighters);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ViewFighterDto> GetAsync(int id)
        {
            var fighter = await _fighterRepository.GetFighter(id) 
                ?? throw new ErrorResponseException().SetMessage("Fighter Not Found!").SetStatusCode(HttpStatusCode.NotFound);

            return _objectMapper.Map<Fighter, ViewFighterDto>(fighter);
        }

        [HttpPost]
        public async Task<ViewFighterDto> CreateAsync(CreateFighterDto input)
        {
            if (!Enum.IsDefined(typeof(Gender), input.Gender) || 
                !Enum.IsDefined(typeof(FighterRole), input.FighterRole) ||
                !Enum.IsDefined(typeof(BeltColor), input.BeltColor))
            {
                throw new ErrorResponseException().SetMessage("Invalid Enum values").SetStatusCode(HttpStatusCode.BadRequest);
            }

            var newFighter = _objectMapper.Map<CreateFighterDto, Fighter>(input);

            var createdFighter = await _fighterRepository.AddFighter(newFighter);
            return _objectMapper.Map<Fighter, ViewFighterDto>(createdFighter);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ViewFighterDto> UpdateAsync(int id, UpdateFighterDto input)
        {
            if (!Enum.IsDefined(typeof(Gender), input.Gender) ||
                !Enum.IsDefined(typeof(FighterRole), input.FighterRole) ||
                !Enum.IsDefined(typeof(BeltColor), input.BeltColor))
            {
                throw new ErrorResponseException().SetMessage("Invalid Enum values").SetStatusCode(HttpStatusCode.BadRequest);
            }

            var existingFighter = await _fighterRepository.GetFighter(id) 
                ?? throw new ErrorResponseException().SetMessage("Fighter Not Found!").SetStatusCode(HttpStatusCode.NotFound);
            existingFighter.Update(input);

            var updatedFighter = await _fighterRepository.UpdateFighter(existingFighter);
            return _objectMapper.Map<Fighter, ViewFighterDto>(updatedFighter);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                await _fighterRepository.DeleteFighter(id);
                return true;
            }
            catch (ErrorResponseException)
            {
                return false;
            }
        }
    }
}
