using System;
using System.Text;
using UnityEngine.Networking;

namespace UnityEngine.ResourceManagement.Util
{
    /// <summary>
    /// Utility class for extracting information from UnityWebRequests.
    /// </summary>
    public class UnityWebRequestUtilities
    {
        /// <summary>
        /// Determines if a web request resulted in an error.
        /// </summary>
        /// <param name="webReq">The web request.</param>
        /// <param name="result"></param>
        /// <returns>True if a web request resulted in an error.</returns>
        public static bool RequestHasErrors(UnityWebRequest webReq, out UnityWebRequestResult result)
        {
            result = null;
            if (webReq == null || !webReq.isDone)
                return false;

#if UNITY_2020_1_OR_NEWER
            switch (webReq.result)
            {
                case UnityWebRequest.Result.InProgress:
                case UnityWebRequest.Result.Success:
                    return false;
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.ProtocolError:
                case UnityWebRequest.Result.DataProcessingError:
                    result = new UnityWebRequestResult(webReq);
                    return true;
                default:
                    throw new NotImplementedException($"Cannot determine whether UnityWebRequest succeeded or not from result : {webReq.result}");
            }
#else
            var isError = webReq.isHttpError || webReq.isNetworkError;
            if (isError)
                result = new UnityWebRequestResult(webReq);

            return isError;
#endif
        }

        /// <summary>
        /// Indicates if the requested AssetBundle is downloaded.
        /// </summary>
        /// <param name="op">The object returned from sending the web request.</param>
        /// <returns>Returns true if the AssetBundle is downloaded.</returns>
        public static bool IsAssetBundleDownloaded(UnityWebRequestAsyncOperation op)
        {
#if ENABLE_ASYNC_ASSETBUNDLE_UWR
            var handler = (DownloadHandlerAssetBundle)op.webRequest.downloadHandler;
            if (handler != null && handler.autoLoadAssetBundle)
                return handler.isDownloadComplete;
#endif
            return op.isDone;
        }
    }

    /// <summary>
    /// Container class for the result of a unity web request.
    /// </summary>
    public class UnityWebRequestResult
    {
        /// <summary>
        /// Creates a new instance of <see cref="UnityWebRequestResult"/>.
        /// </summary>
        /// <param name="request">The unity web request.</param>
        public UnityWebRequestResult(UnityWebRequest request)
        {
            string error = request.error;
#if UNITY_2020_1_OR_NEWER
            if (request.result == UnityWebRequest.Result.DataProcessingError && request.downloadHandler != null)
            {
                // https://docs.unity3d.com/ScriptReference/Networking.DownloadHandler-error.html
                // When a UnityWebRequest ends with the result, UnityWebRequest.Result.DataProcessingError, the message describing the error is in the download handler
                error = $"{error} : {request.downloadHandler.error}";
            }

            Result = request.result;
#endif
            Error = error;
            ResponseCode = request.responseCode;
            Method = request.method;
            Url = request.url;
        }

        /// <summary>Provides a new string object describing the result.</summary>
        /// <returns>A newly allocated managed string.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();

#if UNITY_2020_1_OR_NEWER
            sb.AppendLine($"{Result} : {Error}");
#else
            if (!string.IsNullOrEmpty(Error))
                sb.AppendLine(Error);
#endif
            if (ResponseCode > 0)
                sb.AppendLine($"ResponseCode : {ResponseCode}, Method : {Method}");
            sb.AppendLine($"url : {Url}");

            return sb.ToString();
        }

        /// <summary>
        /// A string explaining the error that occured.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// The numeric HTTP response code returned by the server, if any.
        /// See <a href="https://docs.unity3d.com/ScriptReference/Networking.UnityWebRequest-responseCode.html">documentation</a> for more details.
        /// </summary>
        public long ResponseCode { get; }

#if UNITY_2020_1_OR_NEWER
        /// <summary>
        /// The outcome of the request.
        /// </summary>
        public UnityWebRequest.Result Result { get; }
#endif
        /// <summary>
        /// The HTTP verb used by this UnityWebRequest, such as GET or POST.
        /// </summary>
        public string Method { get; }

        /// <summary>
        /// The target url of the request.
        /// </summary>
        public string Url { get; }

        /// <summary>
        /// Determines if the web request can be sent again based on its error. 
        /// </summary>
        /// <returns>Returns true if the web request can be sent again.</returns>
        public bool ShouldRetryDownloadError()
        {
            if (string.IsNullOrEmpty(Error))
                return true;

            if (Error == "Request aborted" ||
                Error == "Unable to write data" ||
                Error == "Malformed URL" ||
                Error == "Out of memory" ||
                Error == "Encountered invalid redirect (missing Location header?)" ||
                Error == "Cannot modify request at this time" ||
                Error == "Unsupported Protocol" ||
                Error == "Destination host has an erroneous SSL certificate" ||
                Error == "Unable to load SSL Cipher for verification" ||
                Error == "SSL CA certificate error" ||
                Error == "Unrecognized content-encoding" ||
                Error == "Request already transmitted" ||
                Error == "Invalid HTTP Method" ||
                Error == "Header name contains invalid characters" ||
                Error == "Header value contains invalid characters" ||
                Error == "Cannot override system-specified headers"
#if UNITY_2022_1_OR_NEWER
                || Error == "Insecure connection not allowed"
#endif
               )
                return false;

            /* Errors that can be retried:
                "Unknown Error":
                "No Internet Connection"
                "Backend Initialization Error":
                "Cannot resolve proxy":
                "Cannot resolve destination host":
                "Cannot connect to destination host":
                "Access denied":
                "Generic/unknown HTTP error":
                "Unable to read data":
                "Request timeout":
                "Error during HTTP POST transmission":
                "Unable to complete SSL connection":
                "Redirect limit exceeded":
                "Received no data in response":
                "Destination host does not support SSL":
                "Failed to transmit data":
                "Failed to receive data":
                "Login failed":
                "SSL shutdown failed":
                "Redirect limit is invalid":
                "Not implemented":
                "Data Processing Error, see Download Handler error":
             */
            return true;
        }
    }
}
