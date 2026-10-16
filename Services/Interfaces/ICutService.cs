using planeProject.Models;
using planeProject.Models.Dtos;
namespace planeProject.Common;

public interface ICutService
{
    Task<HairCut> GetHairCutByIdAsync(int id);
    Task<List<HairCut>> GetAllAsync();
    Task<HairCutDto> AddHairCutAsync(HairCutDto hairCut);
    Task<HairCutDto> UpdateHairCutByIdAsync(int id, HairCutDto hairCut);
    Task<HairCut> DeleteHairCutByIdAsync(int id);
}