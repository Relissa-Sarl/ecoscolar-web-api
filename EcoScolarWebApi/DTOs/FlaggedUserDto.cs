namespace EcoScolarWebApi.DTOs;

public record FlagAdminDto(
    int FlagId,
    string Reason,
    DateTime Date,
    string ReporterId,
    string ReporterNickname,
    string ReporterEmail
);

public record FlaggedUserDto(
    string UserId,
    string Nickname,
    string Email,
    string FirstName,
    string LastName,
    IEnumerable<FlagAdminDto> Flags
);
