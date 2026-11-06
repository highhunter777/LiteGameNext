using System;

namespace LiteGame.Core.Http
{
    /// <summary>HTTP 失败（非 2xx 或传输错误）；携带状态码与响应体便于定位。</summary>
    public sealed class HttpException : Exception
    {
        public HttpException(long statusCode, string message, string body)
            : base(message)
        {
            StatusCode = statusCode;
            Body = body;
        }

        public long StatusCode { get; }

        /// <summary>响应体（可能为空）。</summary>
        public string Body { get; }
    }
}
