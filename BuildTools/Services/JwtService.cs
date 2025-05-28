using System.Security.Claims;

namespace BuildTools.Services;

using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Implementação do serviço JWT seguindo os padrões do sistema Java original.
/// </summary>
public sealed class JwtService : IJwtService
{
    private const string TOKEN_FIXO = "93cc0ef1-eb78-4dba-acb8-1949a397ad38";
    private const string CHAVE_BASE64 = "Q29saWJyaUBBZ2lsZQ=="; // Base64 de "Colibri@Agile"

    /// <summary>
    /// Gera um token JWT com as configurações padrão do sistema.
    /// </summary>
    /// <returns>Token JWT como string.</returns>
    public string GerarToken()
    {
        var chaveBytes = Convert.FromBase64String(CHAVE_BASE64);
        var signingKey = new SymmetricSecurityKey(chaveBytes);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var agora = DateTime.UtcNow;
        var expiracao = agora.AddMinutes(15);

        var claims = new[]
        {
            new Claim("sub", TOKEN_FIXO),
            new Claim("iat", new DateTimeOffset(agora).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim("exp", new DateTimeOffset(expiracao).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken
        (
            issuer: null,
            audience: null,
            claims: claims,
            notBefore: agora,
            expires: expiracao,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
