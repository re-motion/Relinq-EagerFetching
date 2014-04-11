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
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;
using Remotion.Linq.Development.UnitTesting;
using Remotion.Linq.EagerFetching;
using Remotion.Linq.EagerFetching.Parsing;
using Remotion.Linq.Parsing.Structure.IntermediateModel;
using Remotion.Linq.UnitTests.Parsing.Structure.IntermediateModel;
using Remotion.Linq.UnitTests.TestDomain;

namespace Remotion.Linq.UnitTests.EagerFetching.Parsing
{
  [TestFixture]
  public class ThenFetchOneExpressionNodeTest : ExpressionNodeTestBase
  {
    private ThenFetchOneExpressionNode _node;
    
    private TestFetchRequest _sourceFetchRequest;
    private IExpressionNode _sourceFetchRequestNode;

    public override void SetUp ()
    {
      base.SetUp ();

      _sourceFetchRequest = new TestFetchRequest (typeof (Cook).GetProperty ("Substitution"));
      _sourceFetchRequestNode = new MainSourceExpressionNode ("x", Expression.Constant (new Cook[0]));
      ClauseGenerationContext.AddContextInfo (_sourceFetchRequestNode, _sourceFetchRequest);

      QueryModel.ResultOperators.Add (_sourceFetchRequest);

      _node = new ThenFetchOneExpressionNode (CreateParseInfo (_sourceFetchRequestNode), ExpressionHelper.CreateLambdaExpression<Cook, Cook> (s => s.Substitution));
    }

    [Test]
    public void Apply ()
    {
      var queryModel = _node.Apply (QueryModel, ClauseGenerationContext);
      Assert.That (queryModel, Is.SameAs (QueryModel));

      Assert.That (QueryModel.ResultOperators, Is.EqualTo (new[] { _sourceFetchRequest }));
      var innerFetchRequests = _sourceFetchRequest.InnerFetchRequests.ToArray();
      Assert.That (innerFetchRequests.Length, Is.EqualTo (1));
      Assert.That (innerFetchRequests[0], Is.InstanceOf (typeof (FetchOneRequest)));
      Assert.That (innerFetchRequests[0].RelationMember, Is.SameAs (typeof (Cook).GetProperty ("Substitution")));
    }

    [Test]
    public void Apply_AddsMapping ()
    {
      _node.Apply (QueryModel, ClauseGenerationContext);

      var innerFetchRequest = ((FetchRequestBase) QueryModel.ResultOperators[0]).InnerFetchRequests.Single();
      Assert.That (ClauseGenerationContext.GetContextInfo (_node), Is.SameAs (innerFetchRequest));
    }

    [Test]
    public void Apply_AddsMappingForExisting ()
    {
      _node.Apply (QueryModel, ClauseGenerationContext);

      var node = new ThenFetchOneExpressionNode (CreateParseInfo (_sourceFetchRequestNode), ExpressionHelper.CreateLambdaExpression<Cook, Cook> (s => s.Substitution));
      node.Apply (QueryModel, ClauseGenerationContext);

      var innerFetchRequest = ((FetchRequestBase) QueryModel.ResultOperators[0]).InnerFetchRequests.Single ();
      Assert.That (ClauseGenerationContext.GetContextInfo (node), Is.SameAs (innerFetchRequest));
    }

    [Test]
    [ExpectedException (typeof (NotSupportedException), ExpectedMessage = "ThenFetchMany must directly follow another Fetch request.")]
    public void Apply_WithoutPreviousFetchRequest ()
    {
      var node = new ThenFetchOneExpressionNode (CreateParseInfo (), ExpressionHelper.CreateLambdaExpression<Cook, Cook> (s => s.Substitution));
      node.Apply (QueryModel, ClauseGenerationContext);
    }
  }
}
