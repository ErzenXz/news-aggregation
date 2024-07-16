using NewsAggregation.Data.Repository;
using System.Collections;
using System.Threading.Tasks;

namespace NewsAggregation.Data.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DBContext _dbContext;
        private Hashtable _repositories;

        public UnitOfWork(DBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public bool Complete()
        {
            var numberOfAffectedRows = _dbContext.SaveChanges();
            return numberOfAffectedRows > 0;
        }

        public async Task<bool> CompleteAsync()
        {
            var numberOfAffectedRows = await _dbContext.SaveChangesAsync();
            return numberOfAffectedRows > 0;
        }

        public INewsAggregationRepository<TEntity> Repository<TEntity>() where TEntity : class
        {
            if (_repositories == null)
                _repositories = new Hashtable();

            var type = typeof(TEntity).Name;

            if (!_repositories.Contains(type))
            {
                var repositoryType = typeof(NewsAggregationRepository<>);
                var repositoryInstance = Activator.CreateInstance(repositoryType.MakeGenericType(typeof(TEntity)), _dbContext);
                _repositories.Add(type, repositoryInstance);
            }

            return (INewsAggregationRepository<TEntity>)_repositories[type];
        }
    }
}
