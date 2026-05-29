using EcoScolarWebApi.DTOs.ReferenceData;
using EcoScolarWebApi.Models;
using Riok.Mapperly.Abstractions;

namespace EcoScolarWebApi.Mappers;

[Mapper]
public partial class SubjectMapper
{
    public partial SubjectResponse ToResponse(Subject subject);
    public partial IEnumerable<SubjectResponse> ToResponseList(IEnumerable<Subject> entities);

    [MapperIgnoreTarget(nameof(Subject.SubjectId))]
    public partial Subject ToEntity(SubjectRequest request);
    [MapperIgnoreTarget(nameof(Subject.SubjectId))]
    public partial void UpdateEntity(SubjectRequest request, Subject entity);
}
