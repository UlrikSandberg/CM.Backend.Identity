using System.Collections.Generic;
using System.Threading.Tasks;
using CM.Backend.Identity.AuthorizationServer.Repositories;
using CM.Backend.Identity.AuthorizationServer.Repositories.Models;
using IdentityServer4.Models;
using IdentityServer4.Stores;

namespace CM.Backend.Identity.AuthorizationServer.Helpers
{
    public class PersistedGrantStore : IPersistedGrantStore
    {
        private readonly IGrantRepository _grantRepository;

        public PersistedGrantStore(IGrantRepository grantRepository)
        {
            _grantRepository = grantRepository;
        }

        public async Task StoreAsync(PersistedGrant grant)
        {
            await _grantRepository.StoreAsync(new CMGrantModel
            {
                ClientId = grant.ClientId,
                CreationTime = grant.CreationTime,
                Data = grant.Data,
                Expiration = grant.Expiration,
                Id = grant.Key,
                Key = grant.Key,
                SubjectId = grant.SubjectId,
                Type = grant.Type
            });
        }

        public async Task<PersistedGrant> GetAsync(string key)
        {
            var result = await _grantRepository.GetAsync(key);

            if(result == null)
                return null;

            return ConvertCMGrant(result);
        }

        public async Task<IEnumerable<PersistedGrant>> GetAllAsync(string subjectId)
        {
            var result = await _grantRepository.GetAllAsync(subjectId);

            return ConvertCMGrant(result);
        }

        public async Task RemoveAsync(string key)
        {
            await _grantRepository.RemoveAsync(key);
        }

        public async Task RemoveAllAsync(string subjectId, string clientId)
        {
            await _grantRepository.RemoveAllAsync(subjectId, clientId);
        }

        public async Task RemoveAllAsync(string subjectId, string clientId, string type)
        {
            await _grantRepository.RemoveAllAsync(subjectId, clientId, type);
        }

        private PersistedGrant ConvertCMGrant(CMGrantModel cmGrantModel)
        {
            return new PersistedGrant
            {
                ClientId = cmGrantModel.ClientId,
                CreationTime = cmGrantModel.CreationTime,
                Data = cmGrantModel.Data,
                Expiration = cmGrantModel.Expiration,
                Key = cmGrantModel.Key,
                SubjectId = cmGrantModel.SubjectId,
                Type = cmGrantModel.Type
            };
        }

        private IEnumerable<PersistedGrant> ConvertCMGrant(IEnumerable<CMGrantModel> cmGrantModels)
        {
            var list = new List<PersistedGrant>();

            foreach (var cmGrantModel in cmGrantModels)
            {
                list.Add(new PersistedGrant
                {
                    ClientId = cmGrantModel.ClientId,
                    CreationTime = cmGrantModel.CreationTime,
                    Data = cmGrantModel.Data,
                    Expiration = cmGrantModel.Expiration,
                    Key = cmGrantModel.Key,
                    SubjectId = cmGrantModel.SubjectId,
                    Type = cmGrantModel.Type
                });
            }
            return list;
        }
    }
}