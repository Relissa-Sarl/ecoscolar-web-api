using EcoScolarWebApi.DTOs.ReferenceData;
using EcoScolarWebApi.Models;
using Riok.Mapperly.Abstractions;

namespace EcoScolarWebApi.Mappers;
[Mapper]
public partial class LanguageMapper
{
    [MapperIgnoreTarget(nameof(Language.UserLanguages))]
    public partial LanguageResponse ToResponse(Language language);
    public partial IEnumerable<LanguageResponse> ToResponseList(IEnumerable<Language> entities);

    [MapperIgnoreTarget(nameof(Language.UserLanguages))]
    public partial Language ToEntity(LanguageRequest request);
    [MapperIgnoreTarget(nameof(Language.UserLanguages))]
    public partial void UpdateEntity(LanguageRequest request, Language entity);
}
