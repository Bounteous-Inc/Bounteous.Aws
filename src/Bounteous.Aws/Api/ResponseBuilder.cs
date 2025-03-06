using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Bounteous.Core.Extensions;

namespace Bounteous.Aws.Api
{
    public static class ResponseBuilder
    {
        private static readonly IDictionary<string,string> Headers = new Dictionary<string, string>
        {
            {"Access-Control-Allow-Origin", "*"},
            {"Access-Control-Allow-Credentials", "true"},
            {"Access-Control-Allow-Headers", "Content-Type,X-Api-Key,Authorization,X-Api-Key,X-Amz-Security-Token"},
            {"Access-Control-Allow-Methods", "POST, GET, OPTIONS, PUT, DELETE, HEAD"},
            {"Content-type", "application/json; charset=UTF-8"}
        };

        private static APIGatewayProxyResponse CreateResponse(this string payload, HttpStatusCode statusCode)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)statusCode,
                Body = payload,
                Headers = Headers
            };
        }

        public static APIGatewayProxyResponse Ok(this string payload)
        {
            return CreateResponse(payload, HttpStatusCode.OK);
        }

        public static APIGatewayProxyResponse Ok<T>(this T payload, JsonSerializerOptions options = null)
        {
            return CreateResponse(payload.ToJson(options), HttpStatusCode.OK);
        }

        public static APIGatewayProxyResponse Created<T>(this T payload, JsonSerializerOptions options = null)
        {
            return CreateResponse(payload.ToJson(options), HttpStatusCode.Created);
        }

        public static APIGatewayProxyResponse Accepted<T>(this T payload, JsonSerializerOptions options = null)
        {
            return CreateResponse(payload.ToJson(options), HttpStatusCode.Accepted);
        }
        
        public static APIGatewayProxyResponse Error(this string payload)
        {
            return CreateResponse(payload, HttpStatusCode.InternalServerError);
        }
        
        public static APIGatewayProxyResponse Error<T>(this T payload, JsonSerializerOptions options = null)
        {
            return CreateResponse(payload.ToJson(options), HttpStatusCode.InternalServerError);
        }

        public static APIGatewayProxyResponse NotFound(this string payload)
        {
            return CreateResponse(payload, HttpStatusCode.NotFound);
        }

        public static APIGatewayProxyResponse NotFound<T>(this T payload, JsonSerializerOptions options = null)
        {
            return CreateResponse(payload.ToJson(options), HttpStatusCode.NotFound);
        }

        public static APIGatewayProxyResponse BadRequest<T>(this T payload, JsonSerializerOptions options = null)
        {
            return CreateResponse(payload.ToJson(options), HttpStatusCode.BadRequest);
        }

        public static APIGatewayProxyResponse BadRequest(this string payload)
        {
            return CreateResponse(payload, HttpStatusCode.BadRequest);
        }

        public static APIGatewayProxyResponse UnAuthorized<T>(this T payload, JsonSerializerOptions options = null)
        {
            return CreateResponse(payload.ToJson(options), HttpStatusCode.Unauthorized);
        }
        
        public static APIGatewayProxyResponse UnAuthorized(this string message)
        {
            return CreateResponse(message, HttpStatusCode.Unauthorized);
        }

        public static APIGatewayProxyResponse Forbidden<T>(this T payload)
        {
            return CreateResponse(payload.ToJson(), HttpStatusCode.Forbidden);
        }

        public static APIGatewayProxyResponse Forbidden(this string message)
        {
            return CreateResponse(message, HttpStatusCode.Forbidden);
        }

        public static APIGatewayProxyResponse Warmed(this object called)
        {
            return Ok(new Ack());
        }
        
        private class Ack
        {
            public Ack()
            {
                WarmedAt = DateTime.UtcNow.ToString("s");
            }
            
            public string WarmedAt { get; set; }
        }
    }
}