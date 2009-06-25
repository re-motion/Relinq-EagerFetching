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
using System.Linq.Expressions;
using Remotion.Data.Linq.Clauses;
using Remotion.Data.Linq.Clauses.Expressions;
using Remotion.Utilities;
using System.Collections.Generic;

namespace Remotion.Data.Linq.EagerFetching
{
  /// <summary>
  /// Represents a relation collection property that should be eager-fetched by means of a lambda expression.
  /// </summary>
  public class FetchManyRequest : FetchRequestBase
  {
    private readonly Type _relatedObjectType;

    public FetchManyRequest (LambdaExpression relatedObjectSelector)
        : base(relatedObjectSelector)
    {
      try
      {
        _relatedObjectType = ReflectionUtility.GetAscribedGenericArguments (RelatedObjectSelector.Body.Type, typeof (IEnumerable<>))[0];
      }
      catch (ArgumentTypeException ex)
      {
        var message = string.Format (
            "A fetch many request must yield a list of related objects, but '{0}' yields '{1}', which is not enumerable.",
            RelatedObjectSelector, 
            RelatedObjectSelector.Body.Type.FullName);
        throw new ArgumentException (message, "relatedObjectSelector", ex);
      }
    }

    /// <summary>
    /// Creates a <see cref="MemberFromClause"/> that represents the <see cref="FetchRequestBase.RelatedObjectSelector"/>. This can be inserted into a 
    /// <see cref="QueryModel"/> in order to construct an eager-fetch query.
    /// </summary>
    /// <param name="selectClauseToFetchFrom">The <see cref="SelectClause"/> that is used as a template to fetch from. The new 
    /// <see cref="MemberFromClause"/> is created in such a way that it can replace <paramref name="selectClauseToFetchFrom"/>.</param>
    /// <param name="fromItemName">The name to be used for the <see cref="FromClauseBase.ItemName"/> property of the new <see cref="MemberFromClause"/>.</param>
    /// <returns>A new <see cref="MemberFromClause"/> representing the <see cref="FetchRequestBase.RelatedObjectSelector"/>.</returns>
    /// <remarks>
    /// <see cref="FetchRequestBase.CreateFetchQueryModel"/> uses the <see cref="MemberFromClause"/> returned by this method as follows:
    /// <list type="number">
    ///   <item>It clones the <see cref="QueryModel"/> representing the original query.</item>
    ///   <item>It adds the <see cref="MemberFromClause"/> as the last body clause to the clone.</item>
    ///   <item>It generates a new <see cref="SelectClause"/> and attaches it to the clone.</item>
    /// </list>
    /// </remarks>
    public MemberFromClause CreateFetchFromClause (SelectClause selectClauseToFetchFrom, string fromItemName)
    {
      ArgumentUtility.CheckNotNull ("selectClauseToFetchFrom", selectClauseToFetchFrom);
      ArgumentUtility.CheckNotNullOrEmpty ("fromItemName", fromItemName);

      var fromExpression = CreateFetchSourceExpression (selectClauseToFetchFrom);
      return new MemberFromClause (fromItemName, _relatedObjectType, fromExpression);
    }

    /// <summary>
    /// Modifies the given query model for fetching, adding a <see cref="MemberFromClause"/> and changing the <see cref="SelectClause.Selector"/> to 
    /// retrieve the result of the <see cref="MemberFromClause"/>.
    /// For example, a fetch request such as <c>FetchMany (x => x.Orders)</c> will be transformed into a <see cref="MemberFromClause"/> selecting
    /// <c>y.Orders</c> (where <c>y</c> is what the query model originally selected) and a <see cref="SelectClause"/> selecting the result of the
    /// <see cref="MemberFromClause"/>.
    /// This method is called by <see cref="ModifyFetchQueryModel"/> in the process of creating the new fetch query model.
    /// </summary>
    protected override void ModifyFetchQueryModel (QueryModel fetchQueryModel)
    {
      ArgumentUtility.CheckNotNull ("fetchQueryModel", fetchQueryModel);

      var memberFromClause = CreateFetchFromClause ((SelectClause) fetchQueryModel.SelectOrGroupClause, fetchQueryModel.GetNewName ("#fetch"));
      fetchQueryModel.BodyClauses.Add (memberFromClause);

      var newSelector = new QuerySourceReferenceExpression (memberFromClause);
      var newSelectClause = new SelectClause (newSelector);
      fetchQueryModel.SelectOrGroupClause = newSelectClause;
    }
  }
}