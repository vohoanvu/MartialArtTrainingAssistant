using AutoMapper;
using Microsoft.AspNetCore.Identity;
using FighterManager.Server.Helpers;
using FighterManager.Server.Models.Dtos;
using SharedEntities.Models;

namespace FighterManager.Server.Domain.FighterService
{
    public class FighterRegistrationService
    {
        private readonly UserManager<AppUserEntity> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _objectMapper;

        public FighterRegistrationService(UserManager<AppUserEntity> userManager, 
            IUnitOfWork unitOfWork, IMapper objectMapper)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _objectMapper = objectMapper;
        }

        public async Task<(IdentityResult identityResult, Fighter? fighter)> RegisterFighterAsync(CreateFighterDto input)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();

            // Create and save Fighter first
            var newFighter = _objectMapper.Map<CreateFighterDto, Fighter>(input);
            await _unitOfWork.Repository<Fighter>().AddAsync(newFighter);
            await _unitOfWork.SaveChangesAsync();

            // Create User with FighterId
            var user = new AppUserEntity
            {
                UserName = input.Email,
                Email = input.Email,
                FighterId = newFighter.Id // Assign the FighterId to the user
            };

            var result = await _userManager.CreateAsync(user, input.Password);
            if (!result.Succeeded)
            {
                await transaction.RollbackAsync();
                return (result, null);
            }

            await transaction.CommitAsync();
            return (IdentityResult.Success, newFighter);
        }
    }

}
