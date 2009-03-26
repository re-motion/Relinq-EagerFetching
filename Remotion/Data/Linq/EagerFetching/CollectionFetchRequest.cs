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

namespace Remotion.Data.Linq.EagerFetching
{
  /// <summary>
  /// Represents a relation collection property that should be eager-fetched by means of a lambda expression.
  /// </summary>
  public class CollectionFetchRequest : FetchRequestBase
  {
    public CollectionFetchRequest (LambdaExpression relatedObjectSelector)
        : base(relatedObjectSelector)
    {
      // TODO 1115: Test for IEnumerable<T>
    }

    /// <summary>
    /// Creates a <see cref="MemberFromClause"/> that represents the <see cref="FetchRequestBase.RelatedObjectSelector"/>. This can be inserted into a 
    /// <see cref="QueryModel"/> in order to construct an eager-fetch query.
    /// </summary>
    /// <param name="selectClauseToFetchFrom">The <see cref="SelectClause"/> that is used as a template to fetch from. The new 
    /// <see cref="MemberFromClause"/> is created in such a way that it can replace <paramref name="selectClauseToFetchFrom"/>. Its 
    /// <see cref="AdditionalFromClause.ProjectionExpression"/> selects the fetched related objects.</param>
    /// <param name="fromIdentifierName">The name of the <see cref="FromClauseBase.Identifier"/> to use for the new <see cref="MemberFromClause"/>.</param>
    /// <returns>A new <see cref="MemberFromClause"/> representing the <see cref="FetchRequestBase.RelatedObjectSelector"/>.</returns>
    /// <remarks>
    /// <see cref="FetchRequestBase.CreateFetchQueryModel"/> uses the <see cref="MemberFromClause"/> returned by this method as follows:
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

      LambdaExpression fromExpression = GetFetchSourceExpression (selectClauseToFetchFrom);

      // for a select clause with a projection of x => expr, we generate a projectionExpression of (x, fromIdentifier) => fromIdentifier
      var relatedObjectType = ReflectionUtility.GetAscribedGenericArguments (fromExpression.Body.Type, typeof (IEnumerable<>))[0];
      var fromIdentifier = Expression.Parameter (relatedObjectType, fromIdentifierName);
      
      // this SelectMany clause gets the from identifier plus the input to the from expression as its parameter
      var projectionExpression = Expression.Lambda (fromIdentifier, fromExpression.Parameters[0], fromIdentifier);

      return new MemberFromClause (selectClauseToFetchFrom.PreviousClause, fromIdentifier, fromExpression, projectionExpression);
    }

    protected override void ModifyQueryModelForFetching (QueryModel fetchQueryModel, SelectClause originalSelectClause)
    {
      //if (typeof (IEnumerable).IsAssignableFrom (RelatedObjectSelector.Body.Type)) // TODO 1115: Replace if with polymorphism
      {
        var memberFromClause = CreateFetchFromClause (originalSelectClause, fetchQueryModel.GetUniqueIdentifier ("#fetch"));
        fetchQueryModel.AddBodyClause (memberFromClause);

        var newSelectClause = new SelectClause (memberFromClause, Expression.Lambda (memberFromClause.Identifier, memberFromClause.Identifier));

        IClause previousClause = newSelectClause;
        foreach (var originalResultModifierClause in originalSelectClause.ResultModifierClauses)
        {
          var clonedResultModifierClause = originalResultModifierClause.Clone (previousClause, newSelectClause);
          newSelectClause.AddResultModifierData (clonedResultModifierClause);
          previousClause = clonedResultModifierClause;
        }

        fetchQueryModel.SelectOrGroupClause = newSelectClause;
      }
      //else // TODO 1115: move this to another subclass of FetchRequestBase
      //{
      //  var fetchSourceExpression = GetFetchSourceExpression (originalSelectClause);
      //  var newSelectClause = new SelectClause (fetchQueryModel.SelectOrGroupClause.PreviousClause, fetchSourceExpression);
      //  fetchQueryModel.SelectOrGroupClause = newSelectClause;
      //}
    }
  }
}