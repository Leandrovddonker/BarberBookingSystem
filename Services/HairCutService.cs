using BarberBookingSystem.Common;
using BarberBookingSystem.Data;
using BarberBookingSystem.Exceptions;
using BarberBookingSystem.Models;
using BarberBookingSystem.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace BarberBookingSystem.Services;

public class HairCutService(BarberSystemDbContext context) : ICutService
{
    public async Task<HairCutDto> AddHairCutAsync(HairCutDto hairCutDto)
    {
        var newHairCut = new HairCut()
        {
            Cut = hairCutDto.Cut,
            Price = hairCutDto.Price,
            DurationMinutes = hairCutDto.DurationMinutes,
        };
        context.Add(newHairCut);
        await context.SaveChangesAsync();
        return new HairCutDto()
        {
            Cut = newHairCut.Cut,
            Price = newHairCut.Price,
            DurationMinutes = newHairCut.DurationMinutes
        };
    }

    public async Task<List<HairCut>> GetAllAsync()
    {
        var allHairCuts = await context.HairCuts.ToListAsync();
        return allHairCuts;
    }

    public async Task<HairCut> GetHairCutByIdAsync(int id)
    {
        var haircut = await context.HairCuts.FirstOrDefaultAsync(c => c.Id == id);
        if (haircut == null)
        {
            throw new NotFoundException("Hair cut not be found");
        }
        return haircut;
    }
 
    public async Task<HairCutDto> UpdateHairCutByIdAsync(int id,HairCutDto hairCut)
    {
        var haircutEntity = await context.HairCuts.FirstOrDefaultAsync(c => c.Id == id);
        if (haircutEntity == null)
        {
            throw new NotFoundException("Hair cut not be found");
        }

        if (!string.IsNullOrEmpty(hairCut.Cut) && hairCut.Cut != "string")
        {
            haircutEntity.Cut = hairCut.Cut;
        }

        if (hairCut.Price > 0)
        {
            haircutEntity.Price = hairCut.Price;
        }

        if (hairCut.DurationMinutes > 0)
        {
            haircutEntity.DurationMinutes = hairCut.DurationMinutes;
        }
        await context.SaveChangesAsync();
        return new HairCutDto
        {
            Cut = hairCut.Cut,
            Price = hairCut.Price,
            DurationMinutes = hairCut.DurationMinutes
        };
    }

    public async Task<HairCut> DeleteHairCutByIdAsync(int Id)
    {
        var hairCutEntity = await context.HairCuts.FirstOrDefaultAsync(c => c.Id == Id);
        if (hairCutEntity == null)
        {
            throw new NotFoundException("Hair cut not be found");
        }
        context.Remove(hairCutEntity);
        await context.SaveChangesAsync();
        return hairCutEntity;
    }
}