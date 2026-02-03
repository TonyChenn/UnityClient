using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace NCore.Networking
{
    /// <summary>
    /// 用作http请求,小文件下载
    /// </summary>
    public static class WebServer
    {
        #region Get Data
        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// 如果已http开头就是绝对路径，否则是相对路径
        /// <returns></returns>
        public static async Task<UnityWebRequest> Get(string url)
        {
			Debug.Log($"[WebServer] get --- {url}");
            UnityWebRequest request = UnityWebRequest.Get(url);
            await request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
			{
                Debug.LogError($"[WebServer] fail to get ---- {url}");
			}

            return request;
        }
		public static async Task<UnityWebRequest> GetTexture(string url)
		{
			var request = UnityWebRequestTexture.GetTexture(url);
			await request.SendWebRequest();
			if (request.result != UnityWebRequest.Result.Success)
			{
                Debug.LogError($"[WebServer] fail to get texture ---- {url}");
			}
			return request;
		}
        #endregion

        #region Post Data

        public static async Task<UnityWebRequest> Post(string url, Hashtable data, byte[] file = null)
        {
            WWWForm form = new WWWForm();
            foreach (string key in data.Keys)
                form.AddField(key, data[key].ToString());

            if (file != null)
                form.AddBinaryData("file", file);

            var request = UnityWebRequest.Post(url, form);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
                Debug.LogError($"[WebServer] fail to post ---- {url}");

            return request;
        }

        #endregion

        #region Put
        public static async Task<UnityWebRequest> Put(string url, string data)
        {
            return await Put(url,System.Text.Encoding.UTF8.GetBytes(data));
        }
        public static async Task<UnityWebRequest> Put(string url, byte[] data)
        {
            UnityWebRequest request = UnityWebRequest.Put(url, data);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
                Debug.LogError($"[WebServer] fail to put ---- {url}");

            return request;
        }
        #endregion
    }
}
