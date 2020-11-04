using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using CM.Backend.Identity.AuthorizationServer.Repositories.Helpers;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CM.Backend.Identity.AuthorizationServer
{

	public interface IUserRepository
	{
		Task<bool> CheckConnection();
		Task<UserModel> FindUserByEmail(string email);
	    Task<UserModel> FindUserById(Guid userId);
		Task UpdateEmail(Guid userId, string email);
	    Task UpdatePassword(Guid userId, string newPassword);
		Task Insert(UserModel entity);
	    Task<bool> DeleteById(Guid id);
    }
    
	public class UserRepository : IUserRepository
    {    
		private IMongoClient client;
        private IMongoDatabase database;
        private IMongoCollection<UserModel> defaultCollection;

	    private UpdateDefinitionBuilder<UserModel> Update => Builders<UserModel>.Update;
	    
        private string collectionName { get; set; } = "Users";
        
        public UserRepository(IOptions<StorageConfigurationSettings> config)
        {
            //Configure database connection
            client = new MongoClient(config.Value.ConnectionString);
            database = client.GetDatabase(config.Value.DefaultReadmodelDatabaseName);
			defaultCollection = database.GetCollection<UserModel>(collectionName);

            //Check if the repository exists
            var filter = new BsonDocument("name", collectionName);
            var collections = database.ListCollections(new ListCollectionsOptions() {Filter = filter});

            if (!collections.Any())
            {
				database.CreateCollection(collectionName);
            }
        }

	    public async Task<bool> CheckConnection()
	    {
		    return await defaultCollection.EstimatedDocumentCountAsync(new EstimatedDocumentCountOptions
			           {MaxTime = TimeSpan.FromSeconds(10)}) != 0;
	    }

        public async Task<UserModel> FindUserByEmail(string email)
        {
	        return await defaultCollection.Find(e => e.Email == email).SingleOrDefaultAsync();
        }

	    public async Task<UserModel> FindUserById(Guid userId)
	    {
		    return await defaultCollection.Find(u => u.Id == userId).SingleOrDefaultAsync();
	    }

	    public async Task UpdatePassword(Guid userId, string newPassword)
	    {
		    await defaultCollection.UpdateOneAsync(u => u.Id == userId, Update.Set(u => u.Password, newPassword));
	    }

	    public async Task Insert(UserModel entity)
		{
			await defaultCollection.InsertOneAsync(entity);
		}

	    public async Task<bool> DeleteById(Guid id)
	    {
		    return (await defaultCollection.DeleteOneAsync(e => e.Id == id)).IsAcknowledged;
	    }

		public async Task UpdateEmail(Guid userId, string email)
		{
			await defaultCollection.UpdateOneAsync(u => u.Id == userId, Update.Set(u => u.Email, email));
		}
	}
}