// This file is part of the re-motion Core Framework (www.re-motion.org)
// Copyright (C) 2005-2009 rubicon informationstechnologie gmbh, www.rubicon.eu
// 
// The re-motion Core Framework is free software; you can redistribute it 
// and/or modify it under the terms of the GNU Lesser General Public License 
// version 3.0 as published by the Free Software Foundation.
// 
// re-motion is distributed in the hope that it will be useful, 
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with re-motion; if not, see http://www.gnu.org/licenses.
// 
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Remotion.Data.Linq.Clauses;
using Remotion.Data.Linq.Clauses.ResultOperators;
using Remotion.Data.Linq.Clauses.StreamedData;
using Remotion.Utilities;

namespace Remotion.Data.Linq.EagerFetching
{
  /// <summary>
  /// Base class for classes representing a property that should be eager-fetched when a query is executed.
  /// </summary>
  public abstract class FetchRequestBase : SequenceTypePreservingResultOperatorBase
  {
    private readonly FetchRequestCollection _innerFetchRequestCollection = new FetchRequestCollection();

    private MemberInfo _relationMember;

    protected FetchRequestBase (MemberInfo relationMember)
    {
      ArgumentUtility.CheckNotNull ("relationMember", relationMember);
      _relationMember = relationMember;
    }

    /// <summary>
    /// Gets the <see cref="MemberInfo"/> of the relation member whose contained object(s) should be fetched.
    /// </summary>
    /// <value>The relation member.</value>
    public MemberInfo RelationMember
    {
      get { return _relationMember; }
      set { _relationMember = ArgumentUtility.CheckNotNull ("value", value); }
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
    public abstract void ModifyFetchQueryModel (QueryModel fetchQueryModel);
    
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

    // TODO 1441: Remove
    /// <summary>
    /// Creates the fetch query model for this <see cref="FetchRequestBase"/> from a given <paramref name="originalQueryModel"/>.
    /// </summary>
    /// <param name="originalQueryModel">The original query model to create a fetch query from.</param>
    /// <returns>
    /// A new <see cref="QueryModel"/> which represents the same query as <paramref name="originalQueryModel"/> but selecting
    /// the objects described by <see cref="RelationMember"/> instead of the objects selected by the <paramref name="originalQueryModel"/>.
    /// </returns>
    public QueryModel CreateFetchQueryModel (QueryModel originalQueryModel)
    {
      ArgumentUtility.CheckNotNull ("originalQueryModel", originalQueryModel);

      // clone the original query model, modify it as needed by the fetch request, then copy over the result operators if needed
      var cloneContext = new CloneContext (new QuerySourceMapping());
      var fetchQueryModel = originalQueryModel.Clone (cloneContext.QuerySourceMapping);

      foreach (var resultOperator in fetchQueryModel.ResultOperators.AsChangeResistantEnumerableWithIndex ())
      {
        if (resultOperator.Value is FetchRequestBase)
          fetchQueryModel.ResultOperators.RemoveAt (resultOperator.Index);
      }

      ModifyFetchQueryModel (fetchQueryModel);

      return fetchQueryModel;
    }

    /// <summary>
    /// Gets an <see cref="Expression"/> that returns the fetched object(s).
    /// </summary>
    /// <param name="selectClauseToFetchFrom">The select clause yielding the objects to apply <see cref="RelationMember"/> to in order to
    /// fetch the related object(s).</param>
    /// <returns>An <see cref="Expression"/> that returns the fetched object(s).</returns>
    protected MemberExpression CreateFetchSourceExpression (SelectClause selectClauseToFetchFrom)
    {
      ArgumentUtility.CheckNotNull ("selectClauseToFetchFrom", selectClauseToFetchFrom);

      var selector = selectClauseToFetchFrom.Selector;

      if (!RelationMember.DeclaringType.IsAssignableFrom (selector.Type))
      {
        var message = string.Format (
            "The given SelectClause contains an invalid selector '{0}'. In order to fetch the relation property "
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

    public override StreamedSequence ExecuteInMemory<T> (StreamedSequence input)
    {
      ArgumentUtility.CheckNotNull ("input", input);
      return input;
    }
  }
}