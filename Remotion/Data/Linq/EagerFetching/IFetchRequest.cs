using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Remotion.Data.Linq.Clauses;

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

    /// <summary>
    /// Creates a <see cref="MemberFromClause"/> that represents the <see cref="FetchRequest{TRelated}.RelatedObjectSelector"/>. This can be inserted into a 
    /// <see cref="QueryModel"/> in order to construct an eager-fetch query.
    /// </summary>
    /// <param name="selectClauseToFetchFrom">The <see cref="SelectClause"/> that is used as a template to fetch from. The new 
    /// <see cref="MemberFromClause"/> is created in such a way that it can replace <paramref name="selectClauseToFetchFrom"/>. Its 
    /// <see cref="AdditionalFromClause.ProjectionExpression"/> selects the fetched related objects.</param>
    /// <param name="fromIdentifierName">The name of the <see cref="FromClauseBase.Identifier"/> to use for the new <see cref="MemberFromClause"/>.</param>
    /// <returns>A new <see cref="MemberFromClause"/> representing the <see cref="FetchRequest{TRelated}.RelatedObjectSelector"/>.</returns>
    /// <remarks>
    /// <see cref="FetchRequest{TRelated}.CreateFetchQueryModel"/> uses the <see cref="MemberFromClause"/> returned by this method as follows:
    /// <list type="number">
    ///   <item>It clones the <see cref="QueryModel"/> representing the original query.</item>
    ///   <item>It adds the <see cref="MemberFromClause"/> as the last body clause to the clone.</item>
    ///   <item>It generates a new <see cref="SelectClause"/> and attaches it to the clone.</item>
    /// </list>
    /// </remarks>
    MemberFromClause CreateFetchFromClause (SelectClause selectClauseToFetchFrom, string fromIdentifierName);

    QueryModel CreateFetchQueryModel (QueryModel originalQueryModel);
  }
}