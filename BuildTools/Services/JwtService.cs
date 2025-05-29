using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace BuildTools.Services;

using System.Text;

/// <summary>
/// Implementação do serviço JWT compatível com sistema legado (chave "Colibri@Agile").
/// </summary>
public sealed class JwtService : IJwtService
{
    private const string TOKEN_FIXO = "93cc0ef1-eb78-4dba-acb8-1949a397ad38";
    private const string CHAVE_LEGADA = "Colibri@Agile";

    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="JwtService"/>.
    /// </summary>
    /// <param name="dateTimeProvider">Provedor de data e hora.</param>
    public JwtService(IDateTimeProvider dateTimeProvider)
        => _dateTimeProvider = dateTimeProvider;

    /// <inheritdoc />
    public string GerarToken()
    {
        var chaveOriginal = Encoding.UTF8.GetBytes(CHAVE_LEGADA);
        var chavePadded = PadKeyTo256Bits(chaveOriginal);

        var signingKey = new SymmetricSecurityKey(chavePadded);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var agora = _dateTimeProvider.UtcNow;
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

    /// <summary>
    /// Completa a chave para 256 bits para compatibilidade com o HMAC SHA-256.
    /// </summary>
    /// <param name="key">
    /// A chave original em bytes.
    /// </param>
    /// <returns>
    /// A chave preenchida para 256 bits como um array de bytes.
    /// </returns>
    private static byte[] PadKeyTo256Bits(byte[] key)
    {
        if (key.Length >= 32)
            return key;

        var padded = new byte[32];
        Buffer.BlockCopy(key, 0, padded, 0, key.Length);

        return padded;
    }
}
