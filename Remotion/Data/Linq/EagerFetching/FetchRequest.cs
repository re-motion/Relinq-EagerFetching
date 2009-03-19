// This file is part of the re-motion Core Framework (www.re-motion.org)
// Copyright (C) 2005-2008 rubicon informationstechnologie gmbh, www.rubicon.eu
// 
// The re-motion Core Framework is free software; you can redistribute it 
// and/or modify it under the terms of the GNU Lesser General Public License 
// version 3.0 as published by the Free Software Foundation.
// 
// This framework is distributed in the hope that it will be useful, 
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with this framework; if not, see http://www.gnu.org/licenses.
// 
using System;
using System.Linq.Expressions;
using Remotion.Data.Linq.Clauses;
using Remotion.Utilities;
using System.Collections.Generic;
using System.Reflection;

namespace Remotion.Data.Linq.EagerFetching
{
  /// <summary>
  /// Represents a relation property that should be eager-fetched by means of a lambda expression.
  /// </summary>
  /// <typeparam name="TRelated">The type of the related.</typeparam>
  public class FetchRequest<TRelated> : IFetchRequest
  {
    public static FetchRequest<TRelated> Create<TOriginating> (Expression<Func<TOriginating, IEnumerable<TRelated>>> relatedObjectSelector)
    {
      ArgumentUtility.CheckNotNull ("relatedObjectSelector", relatedObjectSelector);

      return new FetchRequest<TRelated> (relatedObjectSelector);
    }

    private readonly MemberInfo _relationMember;
    private readonly LambdaExpression _relatedObjectSelector;
    private readonly FetchRequestCollection<TRelated> _innerFetchRequestCollection = new FetchRequestCollection<TRelated>();

    private FetchRequest (LambdaExpression relatedObjectSelector)
    {
      ArgumentUtility.CheckNotNull ("relatedObjectSelector", relatedObjectSelector);

      var memberExpression = relatedObjectSelector.Body as MemberExpression;
      if (memberExpression == null)
      {
        var message = string.Format (
            "A fetch request must be a simple member access expression; '{0}' is a {1} instead.",
            relatedObjectSelector.Body,
            relatedObjectSelector.Body.GetType ().Name);
        throw new ArgumentException (message, "relatedObjectSelector");
      }

      if (memberExpression.Expression.NodeType != ExpressionType.Parameter)
      {
        var message = string.Format (
            "A fetch request must be a simple member access expression of the kind o => o.Related; '{0}' is too complex.",
            relatedObjectSelector.Body);
        throw new ArgumentException (message, "relatedObjectSelector");
      }

      _relationMember = memberExpression.Member;
      _relatedObjectSelector = relatedObjectSelector;
    }

    public LambdaExpression RelatedObjectSelector
    {
      get { return _relatedObjectSelector; }
    }

    public MemberInfo RelationMember
    {
      get { return _relationMember; }
    }

    /// <summary>
    /// Gets the inner fetch requests that were issued for this <see cref="FetchRequest{TRelated}"/>.
    /// </summary>
    /// <value>The fetch requests added via <see cref="GetOrAddInnerFetchRequest{TRelated}"/>.</value>
    public IEnumerable<IFetchRequest> InnerFetchRequests
    {
      get { return _innerFetchRequestCollection.FetchRequests; }
    }

    /// <summary>
    /// Gets or adds an inner eager-fetch request for this <see cref="FetchRequest{TRelated}"/>.
    /// </summary>
    /// <typeparam name="TNextRelated">The type of related objects to be fetched.</typeparam>
    /// <param name="relatedObjectSelector">A lambda expression selecting related objects for a given query result object.</param>
    /// <returns>An <see cref="IFetchRequest"/> instance representing the fetch request.</returns>
    public FetchRequest<TNextRelated> GetOrAddInnerFetchRequest<TNextRelated> (Expression<Func<TRelated, IEnumerable<TNextRelated>>> relatedObjectSelector)
    {
      ArgumentUtility.CheckNotNull ("relatedObjectSelector", relatedObjectSelector);
      return _innerFetchRequestCollection.GetOrAddFetchRequest (relatedObjectSelector);
    }

    /// <summary>
    /// Creates a <see cref="MemberFromClause"/> that represents the <see cref="RelatedObjectSelector"/>. This can be inserted into a 
    /// <see cref="QueryModel"/> in order to construct an eager-fetch query.
    /// </summary>
    /// <param name="selectClauseToFetchFrom">The <see cref="SelectClause"/> that is used as a template to fetch from. The new 
    /// <see cref="MemberFromClause"/> is created in such a way that it can replace <paramref name="selectClauseToFetchFrom"/>. Its 
    /// <see cref="AdditionalFromClause.ProjectionExpression"/> selects the fetched related objects.</param>
    /// <param name="fromIdentifierName">The name of the <see cref="FromClauseBase.Identifier"/> to use for the new <see cref="MemberFromClause"/>.</param>
    /// <returns>A new <see cref="MemberFromClause"/> representing the <see cref="RelatedObjectSelector"/>.</returns>
    /// <remarks>
    /// <see cref="CreateFetchQueryModel"/> uses the <see cref="MemberFromClause"/> returned by this method as follows:
    /// <list type="number">
    ///   <item>It clones the <see cref="QueryModel"/> representing the original query.</item>
    ///   <item>It adds the <see cref="MemberFromClause"/> as the last body clause to the clone.</item>
    ///   <item>It generates a new <see cref="SelectClause"/> and attaches it to the clone.</item>
    /// </list>
    /// </remarks>
    public MemberFromClause CreateFetchFromClause (SelectClause selectClauseToFetchFrom, string fromIdentifierName)
    {
      ArgumentUtility.CheckNotNull ("selectClauseToFetchFrom", selectClauseToFetchFrom);
      ArgumentUtility.CheckNotNullOrEmpty ("fromIdentifierName", fromIdentifierName);

      if (selectClauseToFetchFrom.ProjectionExpression.Parameters.Count != 1)
      {
        var message = string.Format ("The given SelectClause contains an invalid projection expression '{0}'. Expected one parameter, but found {1}.", 
            selectClauseToFetchFrom.ProjectionExpression, 
            selectClauseToFetchFrom.ProjectionExpression.Parameters.Count);
        throw new ArgumentException (message, "selectClauseToFetchFrom");
      }

      var oldSelectParameter = selectClauseToFetchFrom.ProjectionExpression.Parameters[0];
      if (!RelationMember.DeclaringType.IsAssignableFrom (selectClauseToFetchFrom.ProjectionExpression.Body.Type))
      {
        var message = string.Format ("The given SelectClause contains an invalid projection expression '{0}'. In order to fetch the relation property " 
            + "'{1}', the projection must yield objects of type '{2}', but it yields '{3}'.",
            selectClauseToFetchFrom.ProjectionExpression,
            RelationMember.Name,
            RelationMember.DeclaringType.FullName,
            selectClauseToFetchFrom.ProjectionExpression.Body.Type);
        throw new ArgumentException (message, "selectClauseToFetchFrom");
      }

      // for a select clause with a projection of x => expr, we generate a fromExpression of x => expr.RelationMember
      var fromExpression = Expression.Lambda (
        Expression.MakeMemberAccess (selectClauseToFetchFrom.ProjectionExpression.Body, RelationMember),
        oldSelectParameter);

      // for a select clause with a projection of x => expr, we generate a projectionExpression of (x, fromIdentifier) => fromIdentifier
      var fromIdentifier = Expression.Parameter (typeof (TRelated), fromIdentifierName);
      var projectionExpression = Expression.Lambda (fromIdentifier, oldSelectParameter, fromIdentifier);

      return new MemberFromClause (selectClauseToFetchFrom.PreviousClause, fromIdentifier, fromExpression, projectionExpression);
    }

    public QueryModel CreateFetchQueryModel (QueryModel originalQueryModel)
    {
      ArgumentUtility.CheckNotNull ("originalQueryModel", originalQueryModel);

      var originalSelectClause = originalQueryModel.SelectOrGroupClause as SelectClause;
      if (originalSelectClause == null)
      {
        var message = string.Format (
            "Fetch requests only support queries with select clauses, but this query has a {0}.",
            originalQueryModel.SelectOrGroupClause.GetType().Name);
        throw new NotSupportedException (message);
      }

      var fetchQueryModel = originalQueryModel.Clone();
      var memberFromClause = CreateFetchFromClause (originalSelectClause, fetchQueryModel.GetUniqueIdentifier ("#fetch"));
      fetchQueryModel.AddBodyClause (memberFromClause);

      var newSelectClause = new SelectClause (memberFromClause, Expression.Lambda (memberFromClause.Identifier, memberFromClause.Identifier));
      fetchQueryModel.SelectOrGroupClause = newSelectClause;
      return fetchQueryModel;
    }
  }
}