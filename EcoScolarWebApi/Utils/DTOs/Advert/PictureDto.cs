namespace EcoscolarWebApi.Utils.DTOs.Advert
{
    /// <summary>
    /// DTO used to represent a picture in an advert. 
    /// It contains the label of the picture, which is a string that describes the picture. 
    /// This DTO is used to transfer data between the application and the client, and it is also used to map the data from the database to the application.
    /// </summary>
    /// <param name="label">The label of the picture</param>
    public record PictureDto(string label)
    {
        public static PictureDto FromEntity(Models.Pictures entity) =>
            new PictureDto(label: entity.Label);
    }
}
