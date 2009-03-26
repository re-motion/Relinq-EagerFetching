using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Remotion.Data.Linq.Clauses;
using Remotion.Utilities;

namespace Remotion.Data.Linq.EagerFetching
{
  /// <summary>
  /// Base class for classes representing a property that should be eager-fetched by means of a lambda expression.
  /// </summary>
  public abstract class FetchRequestBase
  {
    private readonly FetchRequestCollection _innerFetchRequestCollection = new FetchRequestCollection ();

    private readonly MemberInfo _relationMember;
    private readonly LambdaExpression _relatedObjectSelector;

    protected FetchRequestBase (LambdaExpression relatedObjectSelector)
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

    /// <summary>
    /// Gets the <see cref="LambdaExpression"/> acting as the selector of the related object(s) to be fetched.
    /// </summary>
    /// <value>The related object selector.</value>
    public LambdaExpression RelatedObjectSelector
    {
      get { return _relatedObjectSelector; }
    }

    /// <summary>
    /// Gets the <see cref="MemberInfo"/> of the relation member whose contained object(s) should be fetched.
    /// </summary>
    /// <value>The relation member.</value>
    public MemberInfo RelationMember
    {
      get { return _relationMember; }
    }

    /// <summary>
    /// Gets the inner fetch requests that were issued for this <see cref="FetchRequestBase"/>.
    /// </summary>
    /// <value>The fetch requests added via <see cref="GetOrAddInnerFetchRequest"/>.</value>
    public IEnumerable<FetchRequestBase> InnerFetchRequests
    {
      get { return _innerFetchRequestCollection.FetchRequests; }
    }

    /// <summary>
    /// Gets or adds an inner eager-fetch request for this <see cref="FetchRequestBase"/>.
    /// </summary>
    /// <param name="relatedObjectSelector">A lambda expression selecting related objects for a given query result object.</param>
    /// <returns>An <see cref="CollectionFetchRequest"/> instance representing the fetch request.</returns>
    public FetchRequestBase GetOrAddInnerFetchRequest (LambdaExpression relatedObjectSelector)
    {
      ArgumentUtility.CheckNotNull ("relatedObjectSelector", relatedObjectSelector);
      return _innerFetchRequestCollection.GetOrAddFetchRequest (relatedObjectSelector);
    }

    /// <summary>
    /// Gets a <see cref="LambdaExpression"/> that takes the same input as the given <paramref name="selectClauseToFetchFrom"/> and returns the
    /// fetched object(s).
    /// </summary>
    /// <param name="selectClauseToFetchFrom">The select clause yielding the objects to apply <see cref="RelatedObjectSelector"/> to in order to
    /// fetch the related object(s).</param>
    /// <returns>A <see cref="LambdaExpression"/> that returns the fetched object(s).</returns>
    public LambdaExpression GetFetchSourceExpression (SelectClause selectClauseToFetchFrom)
    {
      ArgumentUtility.CheckNotNull ("selectClauseToFetchFrom", selectClauseToFetchFrom);

      // if (selectClauseToFetchFrom.ProjectionExpression.Parameters.Count != 1) // TODO 1096: Use this condition after COMMONS-1096
      if (selectClauseToFetchFrom.ProjectionExpression.Parameters.Count < 1)
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
      return Expression.Lambda (
          Expression.MakeMemberAccess (selectClauseToFetchFrom.ProjectionExpression.Body, RelationMember),
          oldSelectParameter);
    }

    /// <summary>
    /// Creates the fetch query model for this <see cref="FetchRequestBase"/> from a given <paramref name="originalQueryModel"/>.
    /// </summary>
    /// <param name="originalQueryModel">The original query model to create a fetch query from.</param>
    /// <returns>
    /// A new <see cref="QueryModel"/> which represents the same query as <paramref name="originalQueryModel"/> but selecting
    /// the objects described by <see cref="RelatedObjectSelector"/> instead of the objects selected by the <paramref name="originalQueryModel"/>.
    /// </returns>
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
      ModifyQueryModelForFetching(fetchQueryModel, originalSelectClause);

      return fetchQueryModel;
    }

    /// <summary>
    /// Modifies the given query model for fetching, adding new <see cref="AdditionalFromClause"/>s and modifying the 
    /// <see cref="QueryModel.SelectOrGroupClause"/> as needed.
    /// </summary>
    /// <param name="fetchQueryModel">The fetch query model to modify.</param>
    /// <param name="originalSelectClause">The original select clause <see cref="RelatedObjectSelector"/> should be applied to.</param>
    protected abstract void ModifyQueryModelForFetching (QueryModel fetchQueryModel, SelectClause originalSelectClause);
  }
}