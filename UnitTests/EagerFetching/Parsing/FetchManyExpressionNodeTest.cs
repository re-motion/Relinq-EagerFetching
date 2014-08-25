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
using System.Collections.Generic;
using NUnit.Framework;
using Remotion.Linq.Development.UnitTesting;
using Remotion.Linq.EagerFetching;
using Remotion.Linq.EagerFetching.Parsing;
using Remotion.Linq.UnitTests.Parsing.Structure.IntermediateModel;
using Remotion.Linq.UnitTests.TestDomain;

namespace Remotion.Linq.UnitTests.EagerFetching.Parsing
{
  [TestFixture]
  public class FetchManyExpressionNodeTest : ExpressionNodeTestBase
  {
    private FetchManyExpressionNode _node;

    public override void SetUp ()
    {
      base.SetUp ();

      _node = new FetchManyExpressionNode (CreateParseInfo (), ExpressionHelper.CreateLambdaExpression<Cook, IEnumerable<Cook>> (s => s.Assistants));
    }

    [Test]
    public void Apply ()
    {
      _node.Apply (QueryModel, ClauseGenerationContext);

      Assert.That (QueryModel.ResultOperators.Count, Is.EqualTo (1));
      Assert.That (QueryModel.ResultOperators[0], Is.InstanceOf (typeof (FetchManyRequest)));
      Assert.That (((FetchManyRequest) QueryModel.ResultOperators[0]).RelationMember, Is.EqualTo (typeof (Cook).GetProperty ("Assistants")));
    }

    [Test]
    public void Apply_AddsMapping ()
    {
      _node.Apply (QueryModel, ClauseGenerationContext);
      Assert.That (ClauseGenerationContext.GetContextInfo (_node), Is.SameAs (QueryModel.ResultOperators[0]));
    }
    
    [Test]
    public void Apply_WithExistingFetchManyRequest ()
    {
      var request = new FetchManyRequest (_node.RelationMember);
      QueryModel.ResultOperators.Add (request);

      _node.Apply (QueryModel, ClauseGenerationContext);

      Assert.That (QueryModel.ResultOperators.Count, Is.EqualTo (1));
      Assert.That (QueryModel.ResultOperators[0], Is.SameAs (request));
      Assert.That (ClauseGenerationContext.GetContextInfo (_node), Is.SameAs (QueryModel.ResultOperators[0]));
    }
  }
}
