// This file is part of the re-motion Core Framework (www.re-motion.org)
// Copyright (C) 2005-2008 rubicon informationstechnologie gmbh, www.rubicon.eu
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
using System.Linq.Expressions;
using Remotion.Data.Linq;
using Remotion.Data.Linq.Clauses;
using Remotion.Data.Linq.EagerFetching;

namespace Remotion.Data.UnitTests.Linq.EagerFetchingTest
{
  public class TestFetchRequest : FetchRequestBase
  {
    public readonly LambdaExpression FakeSelectProjection = Expression.Lambda (Expression.Constant (null, typeof (Student)));

    public IBodyClause FakeBodyClauseToAdd = null;

    public TestFetchRequest (LambdaExpression relatedObjectSelector)
        : base(relatedObjectSelector)
    {
    }

    protected override void ModifyBodyClausesForFetching (QueryModel fetchQueryModel, SelectClause originalSelectClause)
    {
      if (FakeBodyClauseToAdd != null)
      {
        fetchQueryModel.AddBodyClause (FakeBodyClauseToAdd);
      }
    }

    protected override LambdaExpression CreateSelectProjectionForFetching (QueryModel fetchQueryModel, SelectClause originalSelectClause)
    {
      return FakeSelectProjection;
    }

    public new LambdaExpression CreateFetchSourceExpression (SelectClause selectClauseToFetchFrom)
    {
      return base.CreateFetchSourceExpression (selectClauseToFetchFrom);
    }
  }
}