namespace EcoscolarWebApi.Models
{
    public class UserLanguage
    {
        public string UserId { get; set; }
        public User User { get; set; }

        public string Label { get; set; }
        public Language Language { get; set; } 

        public string LanguageLevel { get; set; }
    }
}