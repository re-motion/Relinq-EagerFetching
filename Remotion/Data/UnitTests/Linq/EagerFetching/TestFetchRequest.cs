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
using System.Reflection;
using Remotion.Data.Linq;
using Remotion.Data.Linq.Clauses;
using Remotion.Data.Linq.EagerFetching;
using Remotion.Data.UnitTests.Linq.TestDomain;
using Remotion.Utilities;

namespace Remotion.Data.UnitTests.Linq.EagerFetching
{
  public class TestFetchRequest : FetchRequestBase
  {
    public readonly Expression FakeSelectProjection = Expression.Constant (null, typeof (Student));

    public IBodyClause FakeBodyClauseToAdd = null;

    public TestFetchRequest (MemberInfo relationMember)
        : base (relationMember)
    {
    }

    protected override void ModifyFetchQueryModel (QueryModel fetchQueryModel)
    {
      var selectClause = fetchQueryModel.SelectClause;
      selectClause.Selector = FakeSelectProjection;
      if (FakeBodyClauseToAdd != null)
        fetchQueryModel.BodyClauses.Add (FakeBodyClauseToAdd);
    }

    public new MemberExpression CreateFetchSourceExpression (SelectClause selectClauseToFetchFrom)
    {
      return base.CreateFetchSourceExpression (selectClauseToFetchFrom);
    }

    public override ResultOperatorBase Clone (CloneContext cloneContext)
    {
      ArgumentUtility.CheckNotNull ("cloneContext", cloneContext);

      var clone = new TestFetchRequest (RelationMember);
      foreach (var innerFetchRequest in clone.InnerFetchRequests)
        clone.GetOrAddInnerFetchRequest ((FetchRequestBase) innerFetchRequest.Clone (cloneContext));

      return clone;
    }
  }
}