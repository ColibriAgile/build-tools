using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;

namespace BuildTools.Testes.Helpers;

/// <summary>
/// Handler HTTP personalizado para testes que permite controlar as respostas.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class TestHttpMessageHandler : HttpMessageHandler
{
    private HttpStatusCode _statusCode = HttpStatusCode.OK;
    private string _responseContent = string.Empty;
    private Exception? _exception;

    public HttpRequestMessage? LastRequest { get; private set; }

    public void SetResponse(HttpStatusCode statusCode, string content)
    {
        _statusCode = statusCode;
        _responseContent = content;
        _exception = null;
    }

    public void SetException(Exception exception)
    {
        _exception = exception;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;

        if (_exception != null)
            throw _exception;

        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseContent, Encoding.UTF8, "application/json")
        };

        return Task.FromResult(response);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            LastRequest?.Dispose();

        base.Dispose(disposing);
    }
}