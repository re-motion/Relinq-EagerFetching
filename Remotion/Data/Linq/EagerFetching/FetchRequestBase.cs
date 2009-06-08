using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Remotion.Data.Linq.Clauses;
using Remotion.Utilities;
using System.Linq;

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
    /// Modifies the given query model's body clauses for fetching, adding new <see cref="AdditionalFromClause"/>s as needed. This
    /// method is called by <see cref="CreateFetchQueryModel"/> in the process of creating the new fetch query model.
    /// </summary>
    /// <param name="fetchQueryModel">The fetch query model to modify.</param>
    /// <param name="originalSelectClause">The original select clause <see cref="RelatedObjectSelector"/> should be applied to.</param>
    protected abstract void ModifyBodyClausesForFetching (QueryModel fetchQueryModel, SelectClause originalSelectClause);

    /// <summary>
    /// Creates the new select projection expression for the eager fetching query model. This
    /// method is called by <see cref="CreateFetchQueryModel"/> in the process of creating the new query model.
    /// </summary>
    /// <param name="fetchQueryModel">The fetch query model for which to create a new select projection.</param>
    /// <param name="originalSelectClause">The original select clause <see cref="RelatedObjectSelector"/> should be applied to.</param>
    /// <returns>
    /// A new projection expression that selects the related objects as indicated by <see cref="RelatedObjectSelector"/>. This expression
    /// is later set as the projection of the <paramref name="fetchQueryModel"/>'s <see cref="QueryModel.SelectOrGroupClause"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The <see cref="QueryModel.SelectOrGroupClause"/> of the <paramref name="fetchQueryModel"/> passed to this method is a clone of
    /// <paramref name="originalSelectClause"/> and can thus be used as a template for the returned projection expression. Note that
    /// changes made in <see cref="ModifyBodyClausesForFetching"/> might invalidate some of the data contained in the clone. Implementors of this
    /// method need to return a select projection that matches those changes.
    /// </para>
    /// <para>
    /// The returned projection's parameter must match the objects flowing into the select clause from the <paramref name="fetchQueryModel"/>'s last
    /// body clause (or its <see cref="QueryModel.MainFromClause"/> of no body clause exists).
    /// </para>
    /// </remarks>
    protected abstract LambdaExpression CreateSelectProjectionForFetching (QueryModel fetchQueryModel, SelectClause originalSelectClause);
    
    /// <summary>
    /// Gets or adds an inner eager-fetch request for this <see cref="FetchRequestBase"/>.
    /// </summary>
    /// <param name="fetchRequest">The <see cref="FetchRequestBase"/> to be added.</param>
    /// <returns>
    /// <paramref name="fetchRequest"/> or, if another <see cref="FetchRequestBase"/> for the same relation member already existed,
    /// the existing <see cref="FetchRequestBase"/>.
    /// </returns>
    public FetchRequestBase GetOrAddInnerFetchRequest (FetchRequestBase fetchRequest)
    {
      ArgumentUtility.CheckNotNull ("fetchRequest", fetchRequest);
      return _innerFetchRequestCollection.GetOrAddFetchRequest (fetchRequest);
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
      ModifyBodyClausesForFetching(fetchQueryModel, originalSelectClause);

      SelectClause newSelectClause = CreateNewSelectClause(fetchQueryModel, originalSelectClause);
      fetchQueryModel.SelectOrGroupClause = newSelectClause;

      return fetchQueryModel;
    }

    /// <summary>
    /// Gets a <see cref="LambdaExpression"/> that takes the same input as the given <paramref name="selectClauseToFetchFrom"/> and returns the
    /// fetched object(s).
    /// </summary>
    /// <param name="selectClauseToFetchFrom">The select clause yielding the objects to apply <see cref="RelatedObjectSelector"/> to in order to
    /// fetch the related object(s).</param>
    /// <returns>A <see cref="LambdaExpression"/> that returns the fetched object(s).</returns>
    protected LambdaExpression CreateFetchSourceExpression (SelectClause selectClauseToFetchFrom)
    {
      ArgumentUtility.CheckNotNull ("selectClauseToFetchFrom", selectClauseToFetchFrom);

      var selector = selectClauseToFetchFrom.Selector;

      // TODO 1096: Remove
      // This was added to work around an issue with the name resolving mechanism: When a LINQ query ends with an additional from clause directlyy
      // followed by a select clause, the C# does not amit a trailing Select method call. Instead, it simply puts the projection of the select 
      // clause into the ResultSelector of the SelectMany method call. We still add a Select clause to the query model with a generated
      // Selector of the form "<generated>_0 => <generated>_0". A better field resolving mechanism will not depend on the names of the identifiers
      // used in such expressions, but the current mechanism will throw an exception for this identifier. Therefore, we cannot use the identifier
      // for the fetch source expression. Instead, we find the AdditionalFromClause corresponding to the SelectMany method call and use its
      // ResultSelector, which can be resolved correctly by name.
      if (selector.Body is ParameterExpression && ((ParameterExpression) selector.Body).Name.StartsWith ("<generated>_"))
      {
        var additionalFromClause = (AdditionalFromClause) selectClauseToFetchFrom.PreviousClause;
        selector = additionalFromClause.ResultSelector;
      }

      // if (selector.Parameters.Count != 1) // TODO 1096: Use this condition after COMMONS-1096
      if (selector.Parameters.Count < 1)
      {
        var message = string.Format ("The given SelectClause contains an invalid projection expression '{0}'. Expected one parameter, but found {1}.",
                                     selector,
                                     selector.Parameters.Count);
        throw new ArgumentException (message, "selectClauseToFetchFrom");
      }

      var oldSelectParameter = selector.Parameters[0];
      if (!RelationMember.DeclaringType.IsAssignableFrom (selector.Body.Type))
      {
        var message = string.Format ("The given SelectClause contains an invalid projection expression '{0}'. In order to fetch the relation property "
                                     + "'{1}', the projection must yield objects of type '{2}', but it yields '{3}'.",
                                     selector,
                                     RelationMember.Name,
                                     RelationMember.DeclaringType.FullName,
                                     selector.Body.Type);
        throw new ArgumentException (message, "selectClauseToFetchFrom");
      }

      // for a select clause with a projection of x => expr, we generate a fromExpression of x => expr.RelationMember
      return Expression.Lambda (
          Expression.MakeMemberAccess (selector.Body, RelationMember),
          oldSelectParameter);
    }

    private SelectClause CreateNewSelectClause (QueryModel fetchQueryModel, SelectClause originalSelectClause)
    {
      var newSelectProjection = CreateSelectProjectionForFetching (fetchQueryModel, originalSelectClause);
      var previousClauseOfNewClause = fetchQueryModel.BodyClauses.LastOrDefault () ?? (IClause) fetchQueryModel.MainFromClause;
      var newSelectClause = new SelectClause (previousClauseOfNewClause, newSelectProjection);

      foreach (var originalResultModifierClause in originalSelectClause.ResultModifications)
      {
        var clonedResultModifierClause = originalResultModifierClause.Clone (newSelectClause);
        newSelectClause.AddResultModification (clonedResultModifierClause);
      }
      return newSelectClause;
    }
  }
}