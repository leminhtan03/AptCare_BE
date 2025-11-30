using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.TechniqueDto;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Implements
{
    public class UserTechniqueService : BaseService<UserTechniqueService>, IUserTechniqueService
    {
        public UserTechniqueService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<UserTechniqueService> logger, IMapper mapper) : base(unitOfWork, logger, mapper)
        {
        }

        public async Task<string> CreateAsyns(AssignTechniqueFroTechnicanDto dto)
        {
            try
            {
                if (!await _unitOfWork.GetRepository<Technique>().AnyAsync(predicate: e => e.TechniqueId == dto.TechniqueId))
                    throw new ApplicationException("Chuyên môn không tồn tại .");
                var newTechnicanTechnique = new TechnicianTechnique
                {
                    TechnicianId = dto.TechnicianId,
                    TechniqueId = dto.TechniqueId
                };
                await _unitOfWork.GetRepository<TechnicianTechnique>().InsertAsync(newTechnicanTechnique);
                await _unitOfWork.CommitAsync();
                return "Đã tạo thành công";
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Đã có lỗi xảy ra khi thêm vào" + ex.Message);

            }
        }

        public async Task<ICollection<TechniqueTechnicanResponseDto>> GetTechnicansTechniqueAsyns(int UserId)
        {
            try
            {
                var resutlt = await _unitOfWork.GetRepository<TechnicianTechnique>()
                    .GetListAsync(predicate: e => e.TechnicianId == UserId,
                    include: src => src.Include(e => e.Technique),
                    selector: src => _mapper.Map<TechniqueTechnicanResponseDto>(src)
                );
                return resutlt;
            }
            catch (Exception ex)
            {

                throw new ApplicationException("Đã có lỗi xảy ra khi lấy ra" + ex.Message);

            }
        }

        public Task<string> UpdateAsyns(UpdateTechniqueFroTechnicanDto dto)
        {
            try
            {
                foreach (var item in dto.TechniqueIds)
                {
                    return CreateAsyns(new AssignTechniqueFroTechnicanDto
                    {
                        TechnicianId = dto.TechnicianId,
                        TechniqueId = item
                    });

                }
                return Task.FromResult("Cập nhật thành công");
            }
            catch (Exception ex)
            {
                throw new Exception("Đã có lỗi xảy ra khi cập nhật" + ex.Message);
            }
        }
    }
}
