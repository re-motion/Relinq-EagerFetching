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
using Remotion.Utilities;

namespace Remotion.Data.Linq.EagerFetching
{
  /// <summary>
  /// Represents a property holding one object that should be eager-fetched when a query is executed.
  /// </summary>
  public class FetchOneRequest : FetchRequestBase
  {
    public FetchOneRequest (MemberInfo relationMember)
        : base (ArgumentUtility.CheckNotNull ("relationMember", relationMember))
    {
    }

    /// <summary>
    /// Modifies the given query model for fetching, changing the <see cref="SelectClause.Selector"/> to the fetch source expression.
    /// For example, a fetch request such as <c>FetchOne (x => x.Customer)</c> will be transformed into a <see cref="SelectClause"/> selecting
    /// <c>y.Customer</c> (where <c>y</c> is what the query model originally selected).
    /// This method is called by <see cref="ModifyFetchQueryModel"/> in the process of creating the new fetch query model.
    /// </summary>
    protected override void ModifyFetchQueryModel (QueryModel fetchQueryModel)
    {
      ArgumentUtility.CheckNotNull ("fetchQueryModel", fetchQueryModel);

      var newSelectProjection = CreateFetchSourceExpression (fetchQueryModel.SelectClause);
      var selectClause = fetchQueryModel.SelectClause;
      selectClause.Selector = newSelectProjection;
    }

    public override ResultOperatorBase Clone (CloneContext cloneContext)
    {
      ArgumentUtility.CheckNotNull ("cloneContext", cloneContext);
      
      var clone = new FetchOneRequest (RelationMember);
      foreach (var innerFetchRequest in InnerFetchRequests)
        clone.GetOrAddInnerFetchRequest ((FetchRequestBase) innerFetchRequest.Clone (cloneContext));

      return clone;
    }
  }
}