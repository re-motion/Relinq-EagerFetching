using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Remotion.Data.Linq.EagerFetching
{
  /// <summary>
  /// Defines a common interface for <see cref="FetchRequest{TRelated}"/> instances representing a relation property that should be eager-fetched.
  /// </summary>
  public interface IFetchRequest
  {
    MemberInfo RelationMember { get; }
    LambdaExpression RelatedObjectSelector { get; }

    /// <summary>
    /// Gets the inner fetch requests that were issued for this <see cref="IFetchRequest"/>.
    /// </summary>
    /// <value>The recursive inner fetch requests of this request.</value>
    IEnumerable<IFetchRequest> InnerFetchRequests { get; }
  }
}