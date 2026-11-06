using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace LiteGame.Core.Http
{
    /// <summary>HTTP 薄封装（UnityWebRequest + Newtonsoft）；非 2xx 抛 HttpException。</summary>
    public sealed class Http
    {
        private readonly int _timeoutSeconds;

        public Http(int timeoutSeconds = 10)
        {
            _timeoutSeconds = timeoutSeconds;
        }

        public UniTask<T> GetAsync<T>(string url, CancellationToken token)
        {
            return SendAsync<T>(UnityWebRequest.Get(url), token);
        }

        public UniTask<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken token)
        {
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(Json.Serialize(body))),
                downloadHandler = new DownloadHandlerBuffer(),
            };
            request.SetRequestHeader("Content-Type", "application/json");
            return SendAsync<TResponse>(request, token);
        }

        private async UniTask<T> SendAsync<T>(UnityWebRequest request, CancellationToken token)
        {
            request.timeout = _timeoutSeconds;
            try
            {
                await request.SendWebRequest().ToUniTask(cancellationToken: token);
                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new HttpException(request.responseCode, request.error);
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
