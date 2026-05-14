using System.ComponentModel.DataAnnotations;

namespace EcoscolarWebApi.Utils.DTOs.Advert
{
    public record PictureDto(string label)
    {
        public static PictureDto FromEntity(Models.Pictures entity) =>
            new PictureDto(label: entity.Label);
    }
}
