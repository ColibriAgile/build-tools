using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace BuildTools.Services;

/// <summary>
/// Implementação do serviço JWT compatível com sistema legado (chave "Colibri@Agile").
/// </summary>
/// <remarks>
/// Inicializa uma nova instância da classe <see cref="JwtService"/>.
/// </remarks>
/// <param name="dateTimeProvider">Provedor de data e hora.</param>
public sealed class JwtService(IDateTimeProvider dateTimeProvider) : IJwtService
{
    private const string TOKEN_FIXO = "93cc0ef1-eb78-4dba-acb8-1949a397ad38";
    private const string CHAVE_LEGADA = "Colibri@Agile";
    private const int TAMANHO_CHAVE_MINIMO = 32; // 256 bits

    /// <inheritdoc />
    public string GerarToken()
    {
        // Aplicar Base64 encoding como no Java original
        var chaveOriginal = Encoding.UTF8.GetBytes(CHAVE_LEGADA);
        var chaveBase64 = Convert.ToBase64String(chaveOriginal);
        var chaveBytes = Encoding.UTF8.GetBytes(chaveBase64);
        
        // Fazer padding da chave para 256 bits mínimos
        var chavePadded = FazerPaddingChave(chaveBytes);
        
        var signingKey = new SymmetricSecurityKey(chavePadded);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var agora = dateTimeProvider.UtcNow;
        var expiracao = agora.AddMinutes(15);

        // Usar apenas os claims necessários, como no Java
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, TOKEN_FIXO),
            new Claim(JwtRegisteredClaimNames.Exp, new DateTimeOffset(expiracao).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken
        (
            issuer: null,
            audience: null,
            claims: claims,
            notBefore: null,
            expires: expiracao,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Faz o padding da chave para o tamanho mínimo necessário (256 bits).
    /// </summary>
    /// <param name="chaveOriginal">Chave original em bytes.</param>
    /// <returns>Chave com padding aplicado.</returns>
    private static byte[] FazerPaddingChave(byte[] chaveOriginal)
    {
        if (chaveOriginal.Length >= TAMANHO_CHAVE_MINIMO)
            return chaveOriginal;

        var chavePadded = new byte[TAMANHO_CHAVE_MINIMO];
        Array.Copy(chaveOriginal, chavePadded, chaveOriginal.Length);

        return chavePadded;
    }
}
