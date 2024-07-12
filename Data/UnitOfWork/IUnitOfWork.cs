
using NewsAggregation.Data.Repository;

namespace NewsAggregation.Data.UnitOfWork
{
    public interface IUnitOfWork
    {
        public INewsAggregationRepository<TEntity> Repository<TEntity>() where TEntity : class;

        bool Complete();
    }
}
