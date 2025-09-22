using Newtonsoft.Json.Linq;

namespace UnityMCP.Editor.Services
{
    /// <summary>
    /// Response helper for consistent API responses following professional MCP patterns
    /// Based on CoplayDev/unity-mcp implementation
    /// </summary>
    public static class Response
    {
        /// <summary>
        /// Create a successful response with optional data
        /// </summary>
        /// <param name="message">Success message</param>
        /// <param name="data">Optional data payload</param>
        /// <returns>JObject with success response</returns>
        public static JObject Success(string message, object data = null)
        {
            var response = new JObject
            {
                ["success"] = true,
                ["message"] = message
            };

            if (data != null)
            {
                if (data is JToken jToken)
                {
                    response["data"] = jToken;
                }
                else
                {
                    response["data"] = JToken.FromObject(data);
                }
            }

            return response;
        }

        /// <summary>
        /// Create an error response
        /// </summary>
        /// <param name="errorCodeOrMessage">Error code or message</param>
        /// <param name="data">Optional error data</param>
        /// <returns>JObject with error response</returns>
        public static JObject Error(string errorCodeOrMessage, object data = null)
        {
            var response = new JObject
            {
                ["success"] = false,
                ["code"] = errorCodeOrMessage,
                ["error"] = errorCodeOrMessage
            };

            if (data != null)
            {
                if (data is JToken jToken)
                {
                    response["data"] = jToken;
                }
                else
                {
                    response["data"] = JToken.FromObject(data);
                }
            }

            return response;
        }

        /// <summary>
        /// Create an error response with structured error object (CoderGamester pattern)
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="errorType">Error type/code</param>
        /// <returns>JObject with structured error response</returns>
        public static JObject CreateErrorResponse(string message, string errorType)
        {
            return new JObject
            {
                ["error"] = new JObject
                {
                    ["type"] = errorType,
                    ["message"] = message
                }
            };
        }
    }
}