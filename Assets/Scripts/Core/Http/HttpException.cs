using System;

namespace LiteGame.Core.Http
{
    /// <summary>HTTP 失败（非 2xx 或传输错误）；调用方据此映射错误提示。</summary>
    public sealed class HttpException : Exception
    {
        public HttpException(long statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public long StatusCode { get; }
    }
}
