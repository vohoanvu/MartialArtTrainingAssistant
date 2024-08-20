using System.Net;
using System.Text.Json.Serialization;

namespace FighterManager.Server.Helpers
{
    public class Response<T>
    {
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public T? Body { get; set; }
        public int? ErrorCode { get; private set; } = null;
        public string? Type { get; private set; } = null;
        public string? Message { get; private set; } = null;
        public Response()
        {
        }
        public Response(HttpStatusCode statusCode, T? body)
        {
            StatusCode = statusCode;
            Body = body;
        }
        static public Response<T> Create<T>(HttpStatusCode statusCode, T? body)
        {
            return new Response<T>(statusCode, body);
        }

        public Response<T> SetBody(T body)
        {
            this.Body = body;
            return this;
        }
        public Response<T> SetStatusCode(HttpStatusCode statusCode)
        {
            this.StatusCode = statusCode;
            return this;
        }

        public Response<T> SetMessage(string message)
        {
            this.Message = message;
            return this;
        }
    }

    public class ErrorResponseException : Exception
    {
        // Todo: Reduce ErrorResponse class
        public ErrorResponseException(Exception? ex = null) : base(null, innerException: ex)
        {
        }
        public static ErrorResponseException Create(Exception? ex = null)
        {
            return new ErrorResponseException(ex);
        }

        [JsonIgnore]
        public HttpStatusCode StatusCode { get; private set; } = HttpStatusCode.InternalServerError;
        public int? ErrorCode { get; private set; }
        public object? Body { get; set; } = null;
        public string? Type { get; private set; }
        public new string? Message { get; private set; } = "Internal Server Error !!!!";

        public ErrorResponseException SetType(string? type)
        {
            this.Type = type;
            return this;
        }
        public ErrorResponseException SetStatusCode(HttpStatusCode statusCode)
        {
            this.StatusCode = statusCode;
            return this;
        }
        public ErrorResponseException SetErrorCode(int errorCode)
        {
            this.ErrorCode = errorCode;
            return this;
        }
        public ErrorResponseException SetBody(object body)
        {
            this.Body = body;
            return this;
        }
        public ErrorResponseException SetMessage(string message)
        {
            this.Message = message;
            return this;
        }
        public object ToErrorResponse()
        {
            return new
            {
                this.StatusCode,
                this.ErrorCode,
                this.Type,
                this.Message,
                this.Body
            };
        }
    }
}
