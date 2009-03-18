using System;
using System.Linq.Expressions;

namespace Remotion.Data.Linq.EagerFetching
{
  /// <summary>
  /// Defines a common interface for <see cref="FetchRequest{TRelated}"/> instances representing a relation property that should be eager-fetched.
  /// </summary>
  public interface IFetchRequest
  {
    LambdaExpression RelatedObjectSelector { get; }
  }
}