using EcoScolarWebApi.DTOs.Users;
using EcoScolarWebApi.Models;
using Riok.Mapperly.Abstractions;

namespace EcoScolarWebApi.Mappers
{
    [Mapper]
    public partial class UserMapper
    {
        [MapProperty(nameof(User.Languages), nameof(UserResponse.Languages))]
        [MapProperty(nameof(User.DateOfBirth), nameof(UserResponse.BirthdayDate))]
        public partial UserResponse ToResponse(User user);

        public partial IEnumerable<UserResponse> ToResponseList(IEnumerable<UserResponse> entities);

        public partial User ToEntity(UserRequest request);
        public partial void UpdateEntity(UserRequest request, User entity);
    }
}
