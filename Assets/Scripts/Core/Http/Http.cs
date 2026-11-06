using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace LiteGame.Core.Http
{
    /// <summary>HTTP 薄封装（UnityWebRequest + Newtonsoft）；主线程调用。
    /// 非 2xx 抛 HttpException，取消抛 OperationCanceledException。</summary>
    public sealed class Http
    {
        private readonly int _timeoutSeconds;

        public Http(int timeoutSeconds = 10)
        {
            if (timeoutSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));
            }

            _timeoutSeconds = timeoutSeconds;
        }

        public UniTask<T> GetAsync<T>(string url, CancellationToken token)
        {
            ValidateUrl(url);
            return SendAsync<T>(UnityWebRequest.Get(url), token);
        }

        public UniTask<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken token)
        {
            ValidateUrl(url);
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(Json.Serialize(body))),
                downloadHandler = new DownloadHandlerBuffer(),
            };
            request.SetRequestHeader("Content-Type", "application/json");
            return SendAsync<TResponse>(request, token);
        }

        private static void ValidateUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("url 不能为空", nameof(url));
            }
        }

        private async UniTask<T> SendAsync<T>(UnityWebRequest request, CancellationToken token)
        {
            request.timeout = _timeoutSeconds;
            try
            {
                await request.SendWebRequest().ToUniTask(cancellationToken: token);
                if (request.result != UnityWebRequest.Result.Success)
                {
                    var body = request.downloadHandler == null ? null : request.downloadHandler.text;
                    throw new HttpException(request.responseCode, request.error, body);
                }

                return Json.Deserialize<T>(request.downloadHandler.text);
            }
            finally
            {
                request.Dispose();
            }
        }
    }
}
