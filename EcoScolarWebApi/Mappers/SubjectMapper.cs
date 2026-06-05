using EcoScolarWebApi.DTOs.ReferenceData;
using EcoScolarWebApi.Models;
using Riok.Mapperly.Abstractions;

namespace EcoScolarWebApi.Mappers;

[Mapper]
public partial class SubjectMapper
{
    public partial SubjectResponseDTO ToResponse(Subject subject);
    public partial IEnumerable<SubjectResponseDTO> ToResponseList(IEnumerable<Subject> entities);

    [MapperIgnoreTarget(nameof(Subject.SubjectId))]
    public partial Subject ToEntity(SubjectRequestDTO request);
    [MapperIgnoreTarget(nameof(Subject.SubjectId))]
    public partial void UpdateEntity(SubjectRequestDTO request, Subject entity);
}
