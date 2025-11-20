using AuthGrpc;
using Domain;
using Grpc.Core;
using Repository;

namespace AuthGrpc.Services;

public class AuthService : Auth.AuthBase
{
    private readonly UserRepository _users;
    private readonly OnlineClassRepository _classes;

    public AuthService(UserRepository users, OnlineClassRepository classes)
    {
        _users = users;
        _classes = classes;
    }

    public override Task<ValidateUserResponse> ValidateUser(ValidateUserRequest request, ServerCallContext context)
    {
        var u = _users.GetByUsername(request.Username);
        bool ok = u != null && u.VerificarPassword(request.Password);
        return Task.FromResult(new ValidateUserResponse { Ok = ok, Reason = ok ? "" : "Credenciales inválidas" });
    }

    public override Task<ValidateClassLinkResponse> ValidateClassLink(ValidateClassLinkRequest request, ServerCallContext context)
    {
        bool ok = _classes.GetAll().Any(c => c.Link == request.Link);
        return Task.FromResult(new ValidateClassLinkResponse { Ok = ok, Reason = ok ? "" : "Link no válido" });
    }
}
