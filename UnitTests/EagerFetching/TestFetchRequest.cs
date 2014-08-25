// Copyright (c) rubicon IT GmbH, www.rubicon.eu
//
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership.  rubicon licenses this file to you under 
// the Apache License, Version 2.0 (the "License"); you may not use this 
// file except in compliance with the License.  You may obtain a copy of the 
// License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.  See the 
// License for the specific language governing permissions and limitations
// under the License.
// 
using System;
using System.Linq.Expressions;
using System.Reflection;
using Remotion.Linq.Clauses;
using Remotion.Linq.EagerFetching;
using Remotion.Utilities;

namespace Remotion.Linq.UnitTests.EagerFetching
{
  public class TestFetchRequest : FetchRequestBase
  {
    public TestFetchRequest (MemberInfo relationMember)
        : base (relationMember)
    {
    }

    public new Expression GetFetchedMemberExpression (Expression source)
    {
      return base.GetFetchedMemberExpression (source);
    }

    protected override void ModifyFetchQueryModel (QueryModel fetchQueryModel)
    {
      // do nothing
    }

    public override ResultOperatorBase Clone (CloneContext cloneContext)
    {
      ArgumentUtility.CheckNotNull ("cloneContext", cloneContext);

      var clone = new TestFetchRequest (RelationMember);
      foreach (var innerFetchRequest in clone.InnerFetchRequests)
        clone.GetOrAddInnerFetchRequest ((FetchRequestBase) innerFetchRequest.Clone (cloneContext));

      return clone;
    }

    public override void TransformExpressions (Func<System.Linq.Expressions.Expression, System.Linq.Expressions.Expression> transformation)
    {
      throw new NotImplementedException ();
    }
  }
}
