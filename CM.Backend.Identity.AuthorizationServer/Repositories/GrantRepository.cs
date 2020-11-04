using System.Collections.Generic;
using System.Threading.Tasks;
using CM.Backend.Identity.AuthorizationServer.Repositories.Helpers;
using CM.Backend.Identity.AuthorizationServer.Repositories.Models;
using IdentityServer4.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CM.Backend.Identity.AuthorizationServer.Repositories
{
    public interface IGrantRepository //This interface reflects the IPersistedGranStore
    {
        Task StoreAsync(CMGrantModel grant);

        Task<CMGrantModel> GetAsync(string key);

        Task<IEnumerable<CMGrantModel>> GetAllAsync(string subjectId);

        Task RemoveAsync(string key);

        Task RemoveAllAsync(string subjectId, string clientId);

        Task RemoveAllAsync(string subjectId, string clientId, string type);
    }
    
    public class GrantRepository : IGrantRepository
    {
        private IMongoClient client;
        private IMongoDatabase database;
        private IMongoCollection<CMGrantModel> defaultCollection;

        private UpdateDefinitionBuilder<CMGrantModel> Update => Builders<CMGrantModel>.Update;
	    
        private string collectionName { get; set; } = "Grants";
        
        public GrantRepository(IOptions<StorageConfigurationSettings> config)
        {
            //Configure database connection
            client = new MongoClient(config.Value.ConnectionString);
            database = client.GetDatabase(config.Value.DefaultReadmodelDatabaseName);
            defaultCollection = database.GetCollection<CMGrantModel>(collectionName);

            //Check if the repository exists
            var filter = new BsonDocument("name", collectionName);
            var collections = database.ListCollections(new ListCollectionsOptions() {Filter = filter});

            if (!collections.Any())
            {
                database.CreateCollection(collectionName);
            }
        } 
        
        
        public async Task StoreAsync(CMGrantModel grant)
        {
            await defaultCollection.InsertOneAsync(grant);
        }

        public async Task<CMGrantModel> GetAsync(string key)
        {
            return await defaultCollection.Find(g => g.Key == key).SingleOrDefaultAsync();
        }

        public async Task<IEnumerable<CMGrantModel>> GetAllAsync(string subjectId)
        {
            return await defaultCollection.Find(g => g.SubjectId == subjectId).ToListAsync();
        }

        public async Task RemoveAsync(string key)
        {
            await defaultCollection.DeleteOneAsync(g => g.Key == key);
        }

        public async Task RemoveAllAsync(string subjectId, string clientId)
        {
            await defaultCollection.DeleteManyAsync(g => g.SubjectId == subjectId && g.ClientId == clientId);
        }

        public async Task RemoveAllAsync(string subjectId, string clientId, string type)
        {
            await defaultCollection.DeleteManyAsync(g =>
                g.SubjectId == subjectId && g.ClientId == clientId && g.Type == type);
        }
    }
}