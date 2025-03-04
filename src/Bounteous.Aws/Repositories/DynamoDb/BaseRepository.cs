using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Bounteous.Aws.IoC;
using Bounteous.Core.Extensions;
using DynamoTable = Amazon.DynamoDBv2.DocumentModel;

namespace Bounteous.Aws.Repositories.DynamoDb
{
    public interface IBaseRepository<T> where T : class
    {
        Task<T> FindById(object id);
        Task SaveAsync(T toUpdate);
        Task DeleteAsync(T toDelete);
        Task<IEnumerable<TU>> FindAllAsync<TU>(IEnumerable<ScanCondition> where);
        Task<IEnumerable<TU>> FindAllAsync<TU>();
        Task<TU> FindOneAsync<TU>(ScanCondition where, bool allowNull = true);
    }

    public abstract class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        private static readonly JsonSerializerOptions DynamoDbJsonSerializationOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private ITable Table { get; }
        private readonly ILazyProvider<IAmazonDynamoDB> clientProvider;

        protected BaseRepository(ILazyProvider<IAmazonDynamoDB> clientProvider, string tableName)
        {
            this.clientProvider = clientProvider;
            Table = TableProxy.Create(clientProvider, tableName);
        }

        protected BaseRepository(IAmazonDynamoDB client, string tableName) : this(new LazyProvider<IAmazonDynamoDB>(()=>client), tableName)
        {
        }

        protected BaseRepository(IAmazonDynamoDB client, ITable table)
        {
            clientProvider = new LazyProvider<IAmazonDynamoDB>(()=>client);
            Table = table;
        }

        public async Task<T> FindById(object id)
        {
            using var context = new DynamoDBContext(clientProvider.Create());
            var result = await context.LoadAsync<T>(id, CreateOperationConfig());
            return result;
        }

        public async Task<TU> FindOneAsync<TU>(ScanCondition where, bool allowNull = true)
        {
            return await FindOneAsync<TU>(new[] {where}, allowNull);
        }

        protected async Task<TU> FindOneAsync<TU>(IEnumerable<ScanCondition> where, bool allowNull = true)
        {
            using (var context = new DynamoDBContext(clientProvider.Create()))
            {
                var search = context.ScanAsync<TU>(where, CreateOperationConfig());

                while (!search.IsDone)
                {
                    var entities = await search.GetNextSetAsync();
                    if (entities.Any()) return entities.FirstOrDefault();
                }

                if (allowNull) return default;
            }

            throw new NotFoundException<T>(where);
        }

        public async Task<IEnumerable<TU>> FindAllAsync<TU>(IEnumerable<ScanCondition> where)
        {
            using var context = new DynamoDBContext(clientProvider.Create());
            var results = new List<TU>();
            var search = context.ScanAsync<TU>(where, CreateOperationConfig());

            while (!search.IsDone)
            {
                var entities = await search.GetNextSetAsync();
                if (entities.Any())
                    results.AddRange(entities);
            }

            return results;
        }

        public async Task<IEnumerable<TU>> FindAllAsync<TU>(ScanCondition where)
        {
            return await FindAllAsync<TU>(new[] {where});
        }

        public async Task<IEnumerable<TU>> FindAllAsync<TU>()=>await FindAllAsync<TU>(Enumerable.Empty<ScanCondition>());

        public async Task SaveAsync(T toUpdate)
        {
            var item = DynamoTable.Document.FromJson(toUpdate.ToJson(DynamoDbJsonSerializationOptions));
            await Table.PutItemAsync(item);
        }

        public async Task DeleteAsync(T toDelete)
        {
            await Table.DeleteItemAsync(DynamoTable.Document.FromJson(toDelete.ToJson(DynamoDbJsonSerializationOptions)));
        }

        public async Task<List<Dictionary<string, AttributeValue>>> QueryAsync(QueryRequest request)
        {
            var client = clientProvider.Create();
            request.TableName = Table.TableName;
            var query = await client.QueryAsync(request);
            return query.Items;
        }

        private DynamoDBOperationConfig CreateOperationConfig()=>new DynamoDBOperationConfig {OverrideTableName = Table.TableName};

        protected ScanCondition WhereEquals(string field, string value)=>new ScanCondition(field, DynamoTable.ScanOperator.Equal, value);
    }
}