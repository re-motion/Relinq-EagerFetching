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
    /// Modifies the given query model for fetching, adding new <see cref="AdditionalFromClause"/> instances and changing the 
    /// <see cref="SelectClause"/> as needed.
    /// This method is called by <see cref="CreateFetchQueryModel"/> in the process of creating the new fetch query model.
    /// </summary>
    /// <param name="fetchQueryModel">The fetch query model to modify.</param>
    protected abstract void ModifyFetchQueryModel (QueryModel fetchQueryModel);


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

      // clone the original query model, modify it as needed by the fetch request, then copy over the result modifications if needed
      
      var cloneContext = new CloneContext (new ClonedClauseMapping());
      var fetchQueryModel = originalQueryModel.Clone (cloneContext.ClonedClauseMapping);
      var originalFetchSelectClause = (SelectClause) fetchQueryModel.SelectOrGroupClause;

      ModifyFetchQueryModel(fetchQueryModel);

      var newFetchSelectClause = (SelectClause) fetchQueryModel.SelectOrGroupClause;
      if (newFetchSelectClause != originalFetchSelectClause)
      {
        cloneContext.ClonedClauseMapping.AddMapping (originalFetchSelectClause, newFetchSelectClause);

        foreach (var originalResultModifierClause in originalFetchSelectClause.ResultModifications)
        {
          var clonedResultModifierClause = originalResultModifierClause.Clone (cloneContext);
          newFetchSelectClause.ResultModifications.Add (clonedResultModifierClause);
        }
      }
      return fetchQueryModel;
    }

    /// <summary>
    /// Gets an <see cref="Expression"/> that returns the fetched object(s).
    /// </summary>
    /// <param name="selectClauseToFetchFrom">The select clause yielding the objects to apply <see cref="RelatedObjectSelector"/> to in order to
    /// fetch the related object(s).</param>
    /// <returns>An <see cref="Expression"/> that returns the fetched object(s).</returns>
    protected MemberExpression CreateFetchSourceExpression (SelectClause selectClauseToFetchFrom)
    {
      ArgumentUtility.CheckNotNull ("selectClauseToFetchFrom", selectClauseToFetchFrom);

      var selector = selectClauseToFetchFrom.Selector;

      if (!RelationMember.DeclaringType.IsAssignableFrom (selector.Type))
      {
        var message = string.Format ("The given SelectClause contains an invalid selector '{0}'. In order to fetch the relation property "
                                     + "'{1}', the selector must yield objects of type '{2}', but it yields '{3}'.",
                                     selector,
                                     RelationMember.Name,
                                     RelationMember.DeclaringType.FullName,
                                     selector.Type);
        throw new ArgumentException (message, "selectClauseToFetchFrom");
      }
      // for a select clause with a projection of expr, we generate a fetch source expression of expr.RelationMember
      return Expression.MakeMemberAccess (selector, RelationMember);
    }
  }
}