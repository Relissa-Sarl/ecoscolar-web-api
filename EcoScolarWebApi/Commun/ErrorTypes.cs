namespace EcoScolarWebApi.Commun;

public enum ErrorType
{
    None,           // (Success)
    NotFound,       // (404)
    Invalid,        // (400)
    Conflict,       // (409)
    Unauthorized,   // (401)
    Forbidden,      // (403)
    InternalError   // (500)
}
