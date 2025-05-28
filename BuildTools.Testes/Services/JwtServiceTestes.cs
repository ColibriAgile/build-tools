using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using BuildTools.Services;
using Microsoft.IdentityModel.Tokens;

namespace BuildTools.Testes.Services;

/// <summary>
/// Testes unitários para o serviço JWT.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class JwtServiceTestes
{
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly JwtService _service;    private const string TOKEN_FIXO_ESPERADO = "93cc0ef1-eb78-4dba-acb8-1949a397ad38";
    private const string CHAVE_BASE64_ESPERADA = "Q29saWJyaUBBZ2lsZS1LZXkyNTYtU3VwZXJTZWNyZXRLZXkh";

    public JwtServiceTestes()
        => _service = new JwtService(_dateTimeProvider);

    [Fact]
    public void GerarToken_DeveRetornarTokenJwtValido()
    {
        // Arrange
        var dataFixa = new DateTime(2025, 5, 28, 10, 0, 0, DateTimeKind.Utc);
        _dateTimeProvider.UtcNow.Returns(dataFixa);

        // Act
        var token = _service.GerarToken();

        // Assert
        token.ShouldNotBeNullOrEmpty();

        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).ShouldBeTrue();
    }

    [Fact]
    public void GerarToken_DeveConterClaimsCorretos()
    {
        // Arrange
        var dataFixa = new DateTime(2025, 5, 28, 10, 0, 0, DateTimeKind.Utc);
        _dateTimeProvider.UtcNow.Returns(dataFixa);

        // Act
        var token = _service.GerarToken();

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var subClaim = jwtToken.Claims.FirstOrDefault(static c => c.Type == "sub");
        subClaim.ShouldNotBeNull();
        subClaim.Value.ShouldBe(TOKEN_FIXO_ESPERADO);

        var iatClaim = jwtToken.Claims.FirstOrDefault(static c => c.Type == "iat");
        iatClaim.ShouldNotBeNull();
        var iatEsperado = new DateTimeOffset(dataFixa).ToUnixTimeSeconds();
        iatClaim.Value.ShouldBe(iatEsperado.ToString());

        var expClaim = jwtToken.Claims.FirstOrDefault(static c => c.Type == "exp");
        expClaim.ShouldNotBeNull();
        var expEsperado = new DateTimeOffset(dataFixa.AddMinutes(15)).ToUnixTimeSeconds();
        expClaim.Value.ShouldBe(expEsperado.ToString());
    }

    [Fact]
    public void GerarToken_DeveUsarAlgoritmoHmacSha256()
    {
        // Arrange
        var dataFixa = new DateTime(2025, 5, 28, 10, 0, 0, DateTimeKind.Utc);
        _dateTimeProvider.UtcNow.Returns(dataFixa);

        // Act
        var token = _service.GerarToken();

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Header.Alg.ShouldBe(SecurityAlgorithms.HmacSha256);
    }

    [Fact]
    public void GerarToken_DeveDefinirDataExpiracaoCorreta()
    {
        // Arrange
        var dataFixa = new DateTime(2025, 5, 28, 10, 0, 0, DateTimeKind.Utc);
        _dateTimeProvider.UtcNow.Returns(dataFixa);

        // Act
        var token = _service.GerarToken();

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var dataExpiracaoEsperada = dataFixa.AddMinutes(15);
        jwtToken.ValidTo.ShouldBe(dataExpiracaoEsperada);
    }

    [Fact]
    public void GerarToken_DeveDefinirDataValidadeInicial()
    {
        // Arrange
        var dataFixa = new DateTime(2025, 5, 28, 10, 0, 0, DateTimeKind.Utc);
        _dateTimeProvider.UtcNow.Returns(dataFixa);

        // Act
        var token = _service.GerarToken();

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.ValidFrom.ShouldBe(dataFixa);
    }

    [Fact]
    public void GerarToken_DevePodeverificarAssinaturaComChaveCorreta()
    {
        // Arrange
        var dataFixa = new DateTime(2025, 5, 28, 10, 0, 0, DateTimeKind.Utc);
        _dateTimeProvider.UtcNow.Returns(dataFixa);

        // Act
        var token = _service.GerarToken();

        // Assert
        var chaveBytes = Convert.FromBase64String(CHAVE_BASE64_ESPERADA);
        var signingKey = new SymmetricSecurityKey(chaveBytes);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.Zero
        };

        var handler = new JwtSecurityTokenHandler();

        Should.NotThrow(() => handler.ValidateToken(token, validationParameters, out _));
    }

    [Fact]
    public void GerarToken_ComDatasDiferentes_DeveGerarTokensDiferentes()
    {
        // Arrange
        var data1 = new DateTime(2025, 5, 28, 10, 0, 0, DateTimeKind.Utc);
        var data2 = new DateTime(2025, 5, 28, 11, 0, 0, DateTimeKind.Utc);

        _dateTimeProvider.UtcNow.Returns(data1);
        var token1 = _service.GerarToken();

        _dateTimeProvider.UtcNow.Returns(data2);
        var token2 = _service.GerarToken();

        // Act & Assert
        token1.ShouldNotBe(token2);
    }
}
