using System;
using CM.Backend.Identity.AuthorizationServer.Repositories.Helpers;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using CM.Backend.Identity.AuthorizationServer.Repositories.Models;
using System.Threading.Tasks;

namespace CM.Backend.Identity.AuthorizationServer.Repositories
{
	public interface IPasswordRecoveryRepository
	{
		Task Insert(PasswordRecoveryModel passwordRecoveryModel);
		Task<PasswordRecoveryModel> GetRecoveryProcessFromToken(string token);
		Task InvalidateRecoveryProcess(Guid recoveryProcessId);
		Task InvalidateAllRecoveriesForUser(Guid userId);
	}

	public class PasswordRecoveryRepository : IPasswordRecoveryRepository
    {
		private IMongoClient client;
        private IMongoDatabase database;
		private IMongoCollection<PasswordRecoveryModel> defaultCollection;

		private UpdateDefinitionBuilder<PasswordRecoveryModel> Update => Builders<PasswordRecoveryModel>.Update;

        private string collectionName { get; set; } = "PasswordRecovery";

		public PasswordRecoveryRepository(IOptions<StorageConfigurationSettings> config)
        {
            //Configure database connection
            client = new MongoClient(config.Value.ConnectionString);
            database = client.GetDatabase(config.Value.DefaultReadmodelDatabaseName);
			defaultCollection = database.GetCollection<PasswordRecoveryModel>(collectionName);

            //Check if the repository exists
            var filter = new BsonDocument("name", collectionName);
            var collections = database.ListCollections(new ListCollectionsOptions() { Filter = filter });

            if (!collections.Any())
            {
                database.CreateCollection(collectionName);
            }
        }

		public async Task Insert(PasswordRecoveryModel passwordRecoveryModel)
		{
			await defaultCollection.InsertOneAsync(passwordRecoveryModel);
		}

		public async Task<PasswordRecoveryModel> GetRecoveryProcessFromToken(string token)
		{
			return await defaultCollection.Find(m => m.RecoveryToken == token).SingleOrDefaultAsync();
		}

		public async Task InvalidateRecoveryProcess(Guid recoveryProcessId)
		{
			await defaultCollection.UpdateOneAsync(r => r.Id == recoveryProcessId, Update.Set(r => r.IsActive, false));
		}

	    public async Task InvalidateAllRecoveriesForUser(Guid userId)
	    {
		    await defaultCollection.UpdateManyAsync(r => r.UserId == userId, Update.Set(r => r.IsActive, false));
	    }
    }
}
