using System.Net;
using System.Text.Json.Serialization;

namespace VideoSharing.Server.Helpers
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class Response<T>
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public T? Body { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public int? ErrorCode { get; private set; } = null;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string? Type { get; private set; } = null;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string? Message { get; private set; } = null;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public Response()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
        }
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public Response(HttpStatusCode statusCode, T? body)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            StatusCode = statusCode;
            Body = body;
        }
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        static public Response<T> Create<T>(HttpStatusCode statusCode, T? body)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            return new Response<T>(statusCode, body);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public Response<T> SetBody(T body)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            this.Body = body;
            return this;
        }
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public Response<T> SetStatusCode(HttpStatusCode statusCode)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            this.StatusCode = statusCode;
            return this;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public Response<T> SetMessage(string message)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            this.Message = message;
            return this;
        }
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class ErrorResponseException : Exception
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
        // Todo: Reduce ErrorResponse class
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public ErrorResponseException(Exception? ex = null) : base(null, innerException: ex)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
        }
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static ErrorResponseException Create(Exception? ex = null)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            return new ErrorResponseException(ex);
        }

        [JsonIgnore]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public HttpStatusCode StatusCode { get; private set; } = HttpStatusCode.InternalServerError;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public int? ErrorCode { get; private set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public object? Body { get; set; } = null;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string? Type { get; private set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public new string? Message { get; private set; } = "Internal Server Error !!!!";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public ErrorResponseException SetType(string? type)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            this.Type = type;
            return this;
        }
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public ErrorResponseException SetStatusCode(HttpStatusCode statusCode)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            this.StatusCode = statusCode;
            return this;
        }
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public ErrorResponseException SetErrorCode(int errorCode)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            this.ErrorCode = errorCode;
            return this;
        }
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public ErrorResponseException SetBody(object body)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            this.Body = body;
            return this;
        }
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public ErrorResponseException SetMessage(string message)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            this.Message = message;
            return this;
        }
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public object ToErrorResponse()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
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
