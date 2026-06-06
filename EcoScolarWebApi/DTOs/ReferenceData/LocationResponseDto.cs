namespace EcoScolarWebApi.DTOs.ReferenceData;

public record LocationResponseDto(
	int LocationId,
	string PostalCode,
	string City,
	string Region
	);
