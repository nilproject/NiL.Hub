using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace NiL.Hub
{
    public interface ISharedInterface<TInterface> : ISharedInterface where TInterface : class
    {
        Task<TResult> Call<TResult>(Expression<Func<TInterface, TResult>> expression, int version = default);

        Task<TResult> Call<TResult>(Expression<Func<TInterface, Task<TResult>>> expression, int version = default);

        Task Call(Expression<Action<TInterface>> expression, int version = default);
    }
}
