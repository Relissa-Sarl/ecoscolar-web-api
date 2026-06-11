using EcoScolarWebApi.DTOs.ReferenceData;
using EcoScolarWebApi.Models;
using Riok.Mapperly.Abstractions;

namespace EcoScolarWebApi.Mappers;

[Mapper]
public partial class LocationMapper
{
    public partial LocationResponseDto LocationToLocationResponseDto(Location location);
    public partial IQueryable<LocationResponseDto> ProjectToLocationResponseDto(IQueryable<Location> query);
}