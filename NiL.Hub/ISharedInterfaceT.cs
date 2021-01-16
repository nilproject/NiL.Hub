using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace NiL.Hub
{
    public interface ISharedInterface<TInterface> : ISharedInterface where TInterface : class
    {
        Task<TResult> Call<TResult>(Expression<Func<TInterface, TResult>> expression);
    }
}
