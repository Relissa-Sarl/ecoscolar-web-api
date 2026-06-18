using EcoScolarWebApi.DTOs;
using EcoScolarWebApi.Models;
using Riok.Mapperly.Abstractions;

namespace EcoScolarWebApi.Mappers;

[Mapper]
public partial class AbuseReportMapper
{
    [MapperIgnoreSource(nameof(AbuseReport.Reporter))]
    [MapperIgnoreSource(nameof(AbuseReport.TargetComment))]
    [MapperIgnoreSource(nameof(AbuseReport.TargetAdvert))]
    public partial AbuseReportResponseDto ToAbuseReportResponse(AbuseReport report);

    public partial IQueryable<AbuseReportResponseDto> ProjectToAbuseReportResponses(IQueryable<AbuseReport> query);

    [MapProperty(nameof(AbuseReport.Reporter) + "." + nameof(User.Nickname), nameof(AbuseReportAdminDto.ReporterNickname))]
    [MapProperty(nameof(AbuseReport.Reporter) + "." + nameof(User.Email), nameof(AbuseReportAdminDto.ReporterEmail))]
    [MapProperty(nameof(AbuseReport.TargetAdvert) + "." + nameof(Advert.Seller) + "." + nameof(User.Nickname), nameof(AbuseReportAdminDto.SellerNickname))]
    [MapProperty(nameof(AbuseReport.TargetAdvert) + "." + nameof(Advert.Seller) + "." + nameof(User.Email), nameof(AbuseReportAdminDto.SellerEmail))]
    [MapProperty(nameof(AbuseReport.TargetAdvert) + "." + nameof(Advert.Seller) + "." + nameof(User.Id), nameof(AbuseReportAdminDto.SellerId))]
    [MapProperty(nameof(AbuseReport.TargetAdvert) + "." + nameof(Advert.Title), nameof(AbuseReportAdminDto.AdvertTitle))]
    [MapProperty(nameof(AbuseReport.TargetAdvert) + "." + nameof(Advert.Description), nameof(AbuseReportAdminDto.AdvertDescription))]
    [MapProperty(nameof(AbuseReport.TargetAdvert) + "." + nameof(Advert.Price), nameof(AbuseReportAdminDto.AdvertPrice))]
    [MapProperty(nameof(AbuseReport.TargetComment) + "." + nameof(PublicComment.Content), nameof(AbuseReportAdminDto.CommentContent))]
    [MapProperty(nameof(AbuseReport.TargetComment) + "." + nameof(PublicComment.Answer), nameof(AbuseReportAdminDto.CommentAnswer))]
    [MapProperty(nameof(AbuseReport.TargetComment) + "." + nameof(PublicComment.Author) + "." + nameof(User.Nickname), nameof(AbuseReportAdminDto.AuthorNickname))]
    [MapProperty(nameof(AbuseReport.TargetComment) + "." + nameof(PublicComment.Author) + "." + nameof(User.Email), nameof(AbuseReportAdminDto.AuthorEmail))]
    [MapProperty(nameof(AbuseReport.TargetComment) + "." + nameof(PublicComment.Author) + "." + nameof(User.Id), nameof(AbuseReportAdminDto.AuthorId))]
    public partial AbuseReportAdminDto ToAbuseReportAdminDto(AbuseReport report);
    
    public partial IQueryable<AbuseReportAdminDto> ProjectToAbuseReportAdminDtos(IQueryable<AbuseReport> query);
}
