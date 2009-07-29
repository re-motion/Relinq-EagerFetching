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
using System.Reflection;
using Remotion.Data.Linq.Clauses;
using Remotion.Data.Linq.Clauses.Expressions;
using Remotion.Utilities;

namespace Remotion.Data.Linq.EagerFetching
{
  /// <summary>
  /// Represents a relation collection property that should be eager-fetched by means of a lambda expression.
  /// </summary>
  public class FetchManyRequest : FetchRequestBase
  {
    private readonly Type _relatedObjectType;

    public FetchManyRequest (MemberInfo relationMember)
        : base (ArgumentUtility.CheckNotNull ("relationMember", relationMember))
    {
      var memberType = Utilities.ReflectionUtility.GetFieldOrPropertyType (relationMember);
      _relatedObjectType = ReflectionUtility.GetItemTypeOfIEnumerable (memberType, "relationMember");
    }

    /// <summary>
    /// Modifies the given query model for fetching, adding an <see cref="AdditionalFromClause"/> and changing the <see cref="SelectClause.Selector"/> to 
    /// retrieve the result of the <see cref="AdditionalFromClause"/>.
    /// For example, a fetch request such as <c>FetchMany (x => x.Orders)</c> will be transformed into a <see cref="AdditionalFromClause"/> selecting
    /// <c>y.Orders</c> (where <c>y</c> is what the query model originally selected) and a <see cref="SelectClause"/> selecting the result of the
    /// <see cref="AdditionalFromClause"/>.
    /// This method is called by <see cref="ModifyFetchQueryModel"/> in the process of creating the new fetch query model.
    /// </summary>
    protected override void ModifyFetchQueryModel (QueryModel fetchQueryModel)
    {
      ArgumentUtility.CheckNotNull ("fetchQueryModel", fetchQueryModel);

      var fromExpression = CreateFetchSourceExpression (fetchQueryModel.SelectClause);
      var memberFromClause = new AdditionalFromClause (fetchQueryModel.GetNewName ("#fetch"), _relatedObjectType, fromExpression);
      fetchQueryModel.BodyClauses.Add (memberFromClause);

      var newSelector = new QuerySourceReferenceExpression (memberFromClause);
      var newSelectClause = new SelectClause (newSelector);
      fetchQueryModel.SelectClause = newSelectClause;
    }

    public override ResultOperatorBase Clone (CloneContext cloneContext)
    {
      ArgumentUtility.CheckNotNull ("cloneContext", cloneContext);

      var clone = new FetchManyRequest (RelationMember);
      foreach (var innerFetchRequest in InnerFetchRequests)
        clone.GetOrAddInnerFetchRequest ((FetchRequestBase) innerFetchRequest.Clone (cloneContext));

      return clone;

    }
  }
}