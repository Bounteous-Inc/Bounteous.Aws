# Bounteous.Aws

A comprehensive AWS integration library for .NET 8+ applications that provides essential utilities for building serverless applications, managing AWS services, and handling common AWS patterns. This library simplifies AWS Lambda development, API Gateway integration, SQS messaging, DynamoDB operations, and secrets management.

## 📦 Installation

Install the package via NuGet:

```bash
dotnet add package Bounteous.Aws
```

Or via Package Manager Console:

```powershell
Install-Package Bounteous.Aws
```

## 🚀 Quick Start

### 1. Configure Services

```csharp
using Bounteous.Aws;
using Bounteous.Aws.IoC;
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    // Register AWS services
    services.AddAWSService<IAmazonSQS>();
    services.AddAWSService<IAmazonSecretsManager>();
    services.AddAWSService<IAmazonDynamoDB>();
    
    // Register Bounteous.Aws services
    services.AddSecretProvider(secretConfigCollection);
    services.AddApplicationMonitoringWithSqsSink("https://sqs.us-east-1.amazonaws.com/account/queue");
}
```

### 2. API Gateway Lambda Handler

```csharp
using Bounteous.Aws.Api;
using Amazon.Lambda.APIGatewayEvents;

public class CustomerHandler
{
    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request)
    {
        try
        {
            // Parse request body
            var customerRequest = request.Parse<CreateCustomerRequest>();
            
            // Process business logic
            var customer = await customerService.CreateCustomerAsync(customerRequest);
            
            // Return success response
            return customer.Created();
        }
        catch (ValidationException ex)
        {
            return ex.Message.BadRequest();
        }
        catch (Exception ex)
        {
            return ex.Message.Error();
        }
    }
}
```

### 3. SQS Message Processing

```csharp
using Bounteous.Aws.Sqs;
using Amazon.Lambda.SQSEvents;

public class OrderProcessor : SqsConsumer<OrderMessage>
{
    public OrderProcessor(IAmazonSQS sqsClient, string sqsUrl) 
        : base(sqsClient, sqsUrl, deleteSuccessfulMessages: true)
    {
    }

    protected override Func<OrderMessage, Task<bool>> ExecuteAsync => ProcessOrderAsync;

    private async Task<bool> ProcessOrderAsync(OrderMessage message)
    {
        try
        {
            // Process the order
            await orderService.ProcessOrderAsync(message.OrderId);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to process order {OrderId}", message.OrderId);
            return false;
        }
    }
}
```

## 🏗️ Architecture Overview

Bounteous.Aws provides a comprehensive set of AWS integration utilities:

- **API Gateway Integration**: Simplified request/response handling for Lambda functions
- **SQS Messaging**: Publisher and consumer patterns for reliable message processing
- **Secrets Management**: Secure access to AWS Secrets Manager with caching
- **DynamoDB Support**: Repository patterns for DynamoDB operations
- **Dependency Injection**: AWS service registration and lazy loading patterns
- **Application Monitoring**: Event tracking and monitoring with SQS sinks
- **Lambda Utilities**: Common patterns for serverless application development

## 🔧 Key Features

### API Gateway Integration

The `ApiGatewayProxyRequestExtensions` and `ResponseBuilder` classes provide fluent APIs for handling API Gateway requests and responses:

#### Request Extensions

```csharp
public static class ApiGatewayProxyRequestExtensions
{
    // Parse JSON body to strongly-typed object
    public static T Parse<T>(this APIGatewayProxyRequest request, T defaultValue = null) where T : class;
    
    // Get query string parameters
    public static string GetQueryString(this APIGatewayProxyRequest request, string key);
    
    // Get path parameters
    public static string GetPathParameter(this APIGatewayProxyRequest request, string key);
    
    // Get headers
    public static string GetHeader(this APIGatewayProxyRequest request, string key);
    public static string GetAuthorization(this APIGatewayProxyRequest request);
    
    // Check for keep-warm requests
    public static bool IsKeepWarm(this APIGatewayProxyRequest input);
    
    // Include body in request
    public static APIGatewayProxyRequest IncludeBody<T>(this APIGatewayProxyRequest request, T body);
}
```

#### Response Builder

```csharp
public static class ResponseBuilder
{
    // Success responses
    public static APIGatewayProxyResponse Ok<T>(this T payload, JsonSerializerOptions options = null);
    public static APIGatewayProxyResponse Created<T>(this T payload, JsonSerializerOptions options = null);
    public static APIGatewayProxyResponse Accepted<T>(this T payload, JsonSerializerOptions options = null);
    
    // Error responses
    public static APIGatewayProxyResponse Error<T>(this T payload, JsonSerializerOptions options = null);
    public static APIGatewayProxyResponse BadRequest<T>(this T payload, JsonSerializerOptions options = null);
    public static APIGatewayProxyResponse NotFound<T>(this T payload, JsonSerializerOptions options = null);
    public static APIGatewayProxyResponse UnAuthorized<T>(this T payload, JsonSerializerOptions options = null);
    public static APIGatewayProxyResponse Forbidden<T>(this T payload, JsonSerializerOptions options = null);
    
    // Keep-warm response
    public static APIGatewayProxyResponse Warmed(this object called);
}
```

**Usage Examples:**

```csharp
public class UserController
{
    public async Task<APIGatewayProxyResponse> GetUser(APIGatewayProxyRequest request)
    {
        // Handle keep-warm requests
        if (request.IsKeepWarm())
            return new object().Warmed();
        
        // Get user ID from path
        var userId = request.GetPathParameter("userId");
        if (string.IsNullOrEmpty(userId))
            return "User ID is required".BadRequest();
        
        // Get authorization header
        var authToken = request.GetAuthorization();
        if (string.IsNullOrEmpty(authToken))
            return "Authorization required".UnAuthorized();
        
        try
        {
            var user = await userService.GetUserAsync(userId);
            return user.Ok();
        }
        catch (NotFoundException)
        {
            return "User not found".NotFound();
        }
    }
    
    public async Task<APIGatewayProxyResponse> CreateUser(APIGatewayProxyRequest request)
    {
        var createUserRequest = request.Parse<CreateUserRequest>();
        
        try
        {
            var user = await userService.CreateUserAsync(createUserRequest);
            return user.Created();
        }
        catch (ValidationException ex)
        {
            return ex.Message.BadRequest();
        }
    }
}
```

### SQS Messaging

The library provides both publisher and consumer patterns for SQS messaging:

#### SQS Publisher

```csharp
public interface IPublishSqsMessages<T> where T : class
{
    Task<bool> SendMessageAsync(T message);
    Task<bool> SendMessagesAsync(IEnumerable<T> messages);
}

public class SqsPublisher<T> : IPublishSqsMessages<T> where T : class
{
    public SqsPublisher(IAmazonSQS sqsClient, string sqsQueueUrl);
    
    public virtual async Task<bool> SendMessageAsync(T message);
    public virtual async Task<bool> SendMessagesAsync(IEnumerable<T> messages);
}
```

#### SQS Consumer

```csharp
public interface IConsumeSqsMessages<T> where T : class
{
    Task Process(IEnumerable<SQSEvent.SQSMessage> messages);
}

public abstract class SqsConsumer<T> : IConsumeSqsMessages<T> where T : class
{
    protected SqsConsumer(IAmazonSQS sqsClient, string sqsUrl, bool deleteSuccessfulMessages = false);
    
    protected abstract Func<T, Task<bool>> ExecuteAsync { get; }
    
    public async Task Process(IEnumerable<SQSEvent.SQSMessage> messages);
}
```

**Usage Examples:**

```csharp
// Publisher
public class OrderService
{
    private readonly IPublishSqsMessages<OrderCreatedEvent> _publisher;
    
    public OrderService(IPublishSqsMessages<OrderCreatedEvent> publisher)
    {
        _publisher = publisher;
    }
    
    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        var order = new Order { /* ... */ };
        
        // Save order to database
        await orderRepository.SaveAsync(order);
        
        // Publish event
        var event = new OrderCreatedEvent { OrderId = order.Id };
        await _publisher.SendMessageAsync(event);
        
        return order;
    }
}

// Consumer
public class OrderProcessor : SqsConsumer<OrderCreatedEvent>
{
    public OrderProcessor(IAmazonSQS sqsClient, string sqsUrl) 
        : base(sqsClient, sqsUrl, deleteSuccessfulMessages: true)
    {
    }

    protected override Func<OrderCreatedEvent, Task<bool>> ExecuteAsync => ProcessOrderAsync;

    private async Task<bool> ProcessOrderAsync(OrderCreatedEvent message)
    {
        try
        {
            await inventoryService.ReserveItemsAsync(message.OrderId);
            await paymentService.ProcessPaymentAsync(message.OrderId);
            await shippingService.CreateShipmentAsync(message.OrderId);
            
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to process order {OrderId}", message.OrderId);
            return false;
        }
    }
}
```

### Secrets Management

The library provides secure access to AWS Secrets Manager with caching:

```csharp
public interface ISecretProvider
{
    ISecret GetAwsSecret(string name);
}

public class SecretProvider : ISecretProvider, IDisposable
{
    public SecretProvider(SecretConfigCollection collection, IAmazonSecretsManager manager);
    
    public ISecret GetAwsSecret(string name);
}
```

**Usage Examples:**

```csharp
public class DatabaseService
{
    private readonly ISecretProvider _secretProvider;
    
    public DatabaseService(ISecretProvider secretProvider)
    {
        _secretProvider = secretProvider;
    }
    
    public async Task<string> GetConnectionStringAsync()
    {
        var secret = _secretProvider.GetAwsSecret("database-connection");
        var connectionString = await secret.GetSecretValueAsync();
        return connectionString;
    }
}

// Configuration
var secretConfig = new SecretConfigCollection
{
    Items = new[]
    {
        new SecretConfig { Name = "database-connection", SecretId = "prod/database/connection", Region = "us-east-1" },
        new SecretConfig { Name = "api-key", SecretId = "prod/api/key", Region = "us-east-1" }
    }
};

services.AddSecretProvider(secretConfig);
```

### Dependency Injection Extensions

The library provides convenient extension methods for AWS service registration:

```csharp
public static class IoCExtensions
{
    // Lazy service provider
    public static IServiceCollection AddLazyProviderFor<T>(this IServiceCollection collection);
    
    // Secret provider registration
    public static IServiceCollection AddSecretProvider(this IServiceCollection collection, SecretConfigCollection secretConfigCollection);
    public static IServiceCollection AddLazySecretProvider(this IServiceCollection collection, SecretConfigCollection secretConfigCollection);
    
    // Application monitoring with SQS sink
    public static IServiceCollection AddApplicationMonitoringWithSqsSink(this IServiceCollection collection, string sqsUrl);
    public static IServiceCollection AddApplicationMonitoringWithSqsSink(this IServiceCollection collection, Func<string> sqsUrlFunc);
}
```

**Usage Examples:**

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Add AWS services
    services.AddAWSService<IAmazonSQS>();
    services.AddAWSService<IAmazonSecretsManager>();
    services.AddAWSService<IAmazonDynamoDB>();
    
    // Add lazy providers for expensive services
    services.AddLazyProviderFor<ISecretProvider>();
    
    // Add secret provider
    services.AddSecretProvider(secretConfigCollection);
    
    // Add application monitoring
    services.AddApplicationMonitoringWithSqsSink(() => 
        configuration["Monitoring:SqsUrl"]);
    
    // Add SQS publishers
    services.AddScoped<IPublishSqsMessages<OrderEvent>>(provider =>
        new SqsPublisher<OrderEvent>(
            provider.GetService<IAmazonSQS>(),
            configuration["Sqs:OrderQueueUrl"]));
}
```

## 📚 Usage Examples

### Complete Lambda Function Example

```csharp
using Bounteous.Aws.Api;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

public class OrderLambda
{
    private readonly IOrderService _orderService;
    
    public OrderLambda(IOrderService orderService)
    {
        _orderService = orderService;
    }
    
    [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            // Handle keep-warm requests
            if (request.IsKeepWarm())
                return new object().Warmed();
            
            // Route based on HTTP method
            return request.HttpMethod switch
            {
                "GET" => await HandleGetOrder(request),
                "POST" => await HandleCreateOrder(request),
                "PUT" => await HandleUpdateOrder(request),
                "DELETE" => await HandleDeleteOrder(request),
                _ => "Method not allowed".BadRequest()
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error processing request: {ex.Message}");
            return ex.Message.Error();
        }
    }
    
    private async Task<APIGatewayProxyResponse> HandleGetOrder(APIGatewayProxyRequest request)
    {
        var orderId = request.GetPathParameter("orderId");
        if (string.IsNullOrEmpty(orderId))
            return "Order ID is required".BadRequest();
        
        var order = await _orderService.GetOrderAsync(orderId);
        return order.Ok();
    }
    
    private async Task<APIGatewayProxyResponse> HandleCreateOrder(APIGatewayProxyRequest request)
    {
        var createOrderRequest = request.Parse<CreateOrderRequest>();
        var order = await _orderService.CreateOrderAsync(createOrderRequest);
        return order.Created();
    }
    
    private async Task<APIGatewayProxyResponse> HandleUpdateOrder(APIGatewayProxyRequest request)
    {
        var orderId = request.GetPathParameter("orderId");
        var updateOrderRequest = request.Parse<UpdateOrderRequest>();
        
        var order = await _orderService.UpdateOrderAsync(orderId, updateOrderRequest);
        return order.Ok();
    }
    
    private async Task<APIGatewayProxyResponse> HandleDeleteOrder(APIGatewayProxyRequest request)
    {
        var orderId = request.GetPathParameter("orderId");
        await _orderService.DeleteOrderAsync(orderId);
        return new { Message = "Order deleted successfully" }.Accepted();
    }
}
```

### SQS Event Processing Example

```csharp
using Bounteous.Aws.Sqs;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.Core;

public class OrderEventHandler : SqsConsumer<OrderEvent>
{
    private readonly IOrderService _orderService;
    
    public OrderEventHandler(IOrderService orderService, IAmazonSQS sqsClient, string sqsUrl) 
        : base(sqsClient, sqsUrl, deleteSuccessfulMessages: true)
    {
        _orderService = orderService;
    }

    protected override Func<OrderEvent, Task<bool>> ExecuteAsync => ProcessOrderEventAsync;

    private async Task<bool> ProcessOrderEventAsync(OrderEvent orderEvent)
    {
        try
        {
            switch (orderEvent.EventType)
            {
                case "OrderCreated":
                    await _orderService.HandleOrderCreatedAsync(orderEvent.OrderId);
                    break;
                case "OrderCancelled":
                    await _orderService.HandleOrderCancelledAsync(orderEvent.OrderId);
                    break;
                case "OrderShipped":
                    await _orderService.HandleOrderShippedAsync(orderEvent.OrderId);
                    break;
                default:
                    Log.Warning("Unknown event type: {EventType}", orderEvent.EventType);
                    return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to process order event {OrderId} of type {EventType}", 
                orderEvent.OrderId, orderEvent.EventType);
            return false;
        }
    }
}

// Lambda handler for SQS events
[LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
public async Task FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
{
    var handler = new OrderEventHandler(orderService, sqsClient, queueUrl);
    await handler.Process(sqsEvent.Records);
}
```

### Application Monitoring Example

```csharp
using Bounteous.Core.Utilities.ApplicationEvents;

public class OrderService
{
    private readonly IMonitorBuilder _monitorBuilder;
    
    public OrderService(IMonitorBuilder monitorBuilder)
    {
        _monitorBuilder = monitorBuilder;
    }
    
    public async Task<Order> ProcessOrderAsync(Order order)
    {
        var monitor = _monitorBuilder
            .ForOperation("ProcessOrder")
            .WithUser(order.CustomerId.ToString())
            .WithDetails($"Processing order {order.Id}")
            .Build();
        
        try
        {
            monitor.StartEvent();
            
            // Process order
            await ValidateOrderAsync(order);
            await ReserveInventoryAsync(order);
            await ProcessPaymentAsync(order);
            await CreateShipmentAsync(order);
            
            monitor.Outcome = Outcome.Successful;
            return order;
        }
        catch (Exception ex)
        {
            monitor.Outcome = Outcome.Failed;
            monitor.FailureCause = ex.Message;
            throw;
        }
        finally
        {
            monitor.StopEvent();
        }
    }
}
```

## 🔧 Configuration Options

### AWS Configuration

```csharp
// appsettings.json
{
  "AWS": {
    "Region": "us-east-1",
    "Profile": "default"
  },
  "Sqs": {
    "OrderQueueUrl": "https://sqs.us-east-1.amazonaws.com/account/order-queue",
    "EventQueueUrl": "https://sqs.us-east-1.amazonaws.com/account/event-queue"
  },
  "Secrets": {
    "DatabaseConnection": "prod/database/connection",
    "ApiKey": "prod/api/key"
  },
  "Monitoring": {
    "SqsUrl": "https://sqs.us-east-1.amazonaws.com/account/monitoring-queue"
  }
}
```

### Service Registration

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // AWS configuration
    services.AddDefaultAWSOptions(configuration.GetAWSOptions());
    
    // AWS services
    services.AddAWSService<IAmazonSQS>();
    services.AddAWSService<IAmazonSecretsManager>();
    services.AddAWSService<IAmazonDynamoDB>();
    
    // Bounteous.Aws services
    services.AddSecretProvider(GetSecretConfig());
    services.AddApplicationMonitoringWithSqsSink(
        () => configuration["Monitoring:SqsUrl"]);
    
    // Application services
    services.AddScoped<IOrderService, OrderService>();
    services.AddScoped<IPublishSqsMessages<OrderEvent>>(provider =>
        new SqsPublisher<OrderEvent>(
            provider.GetService<IAmazonSQS>(),
            configuration["Sqs:OrderQueueUrl"]));
}

private SecretConfigCollection GetSecretConfig()
{
    return new SecretConfigCollection
    {
        Items = new[]
        {
            new SecretConfig 
            { 
                Name = "database-connection", 
                SecretId = configuration["Secrets:DatabaseConnection"], 
                Region = configuration["AWS:Region"] 
            },
            new SecretConfig 
            { 
                Name = "api-key", 
                SecretId = configuration["Secrets:ApiKey"], 
                Region = configuration["AWS:Region"] 
            }
        }
    };
}
```

## 🎯 Target Framework

- **.NET 8.0** and later

## 📋 Dependencies

- **Amazon.Lambda.APIGatewayEvents** (2.7.1) - API Gateway event types
- **Amazon.Lambda.SQSEvents** (2.2.0) - SQS event types
- **AWSSDK.Core** (3.7.402.13) - AWS SDK core
- **AWSSDK.DynamoDBv2** (3.7.405.34) - DynamoDB client
- **AWSSDK.SQS** (3.7.400.107) - SQS client
- **AWSSDK.SecretsManager** (3.7.400.107) - Secrets Manager client
- **AWSSDK.SecretsManager.Caching** (1.0.6) - Secrets caching
- **Bounteous.Core** (0.0.6) - Core utilities and patterns

## 🔗 Related Projects

- [Bounteous.Core](../Bounteous.Core/) - Core utilities and patterns
- [Bounteous.Data](../Bounteous.Data/) - Data access functionality

## 🤝 Contributing

This library is maintained by Xerris Inc. For contributions, please contact the development team.

## 📄 License

See [LICENSE](LICENSE) file for details.

---

*This comprehensive AWS integration library simplifies serverless application development by providing essential utilities for API Gateway, SQS messaging, secrets management, and application monitoring in enterprise .NET applications.*