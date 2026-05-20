namespace EcoscolarWebApi.Utils.DTOs.Adverts
{
    /// <summary>
    /// DTO used to represent a Pictures in an Adverts. 
    /// It contains the label of the Pictures, which is a string that describes the Pictures. 
    /// This DTO is used to transfer data between the application and the client, and it is also used to map the data from the database to the application.
    /// </summary>
    /// <param name="label">The label of the Pictures</param>
    public record PictureDto(string label)
    {
        public static PictureDto FromEntity(Models.Pictures entity) =>
            new PictureDto(label: entity.Label);
    }
}
