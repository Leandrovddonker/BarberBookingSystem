using BarberBookingSystem.Models;
using BarberBookingSystem.Models.Dtos;

namespace BarberBookingSystem.Services.Interfaces;

public interface ICutService
{
    Task<HairCut> GetHairCutByIdAsync(int id);
    Task<List<HairCut>> GetAllAsync();
    Task<HairCutDto> AddHairCutAsync(HairCutDto hairCut);
    Task<HairCutDto> UpdateHairCutByIdAsync(int id, HairCutDto hairCut);
    Task<HairCut> DeleteHairCutByIdAsync(int id);
}