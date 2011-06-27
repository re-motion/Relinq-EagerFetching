// This file is part of the re-linq project (relinq.codeplex.com)
// Copyright (C) 2005-2009 rubicon informationstechnologie gmbh, www.rubicon.eu
// 
// re-linq is free software; you can redistribute it and/or modify it under 
// the terms of the GNU Lesser General Public License as published by the 
// Free Software Foundation; either version 2.1 of the License, 
// or (at your option) any later version.
// 
// re-linq is distributed in the hope that it will be useful, 
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with re-linq; if not, see http://www.gnu.org/licenses.
// 
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using NUnit.Framework;
using Remotion.Linq.UnitTests.Linq.Core.Parsing.Structure.IntermediateModel;
using Remotion.Linq.UnitTests.Linq.Core.TestDomain;
using System.Linq;
using Remotion.Linq.EagerFetching;
using Remotion.Linq.EagerFetching.Parsing;
using Remotion.Linq.Parsing;
using Remotion.Linq.Parsing.Structure.IntermediateModel;

namespace Remotion.Linq.UnitTests.Linq.Core.EagerFetching.Parsing
{
  [TestFixture]
  public class ThenFetchManyExpressionNodeTest : ExpressionNodeTestBase
  {
    private ThenFetchManyExpressionNode _node;

    private TestFetchRequest _sourceFetchRequest;
    private IExpressionNode _sourceFetchRequestNode;

    public override void SetUp ()
    {
      base.SetUp ();

      _sourceFetchRequest = new TestFetchRequest (typeof (Cook).GetProperty ("Substitution"));
      _sourceFetchRequestNode = new MainSourceExpressionNode ("x", Expression.Constant (new Cook[0]));
      ClauseGenerationContext.AddContextInfo (_sourceFetchRequestNode, _sourceFetchRequest);

      QueryModel.ResultOperators.Add (_sourceFetchRequest);

      _node = new ThenFetchManyExpressionNode (CreateParseInfo (_sourceFetchRequestNode), ExpressionHelper.CreateLambdaExpression<Cook, IEnumerable<Cook>> (s => s.Assistants));
    }

    [Test]
    public void Apply ()
    {
      var queryModel = _node.Apply (QueryModel, ClauseGenerationContext);
      Assert.That (queryModel, Is.SameAs (QueryModel));

      Assert.That (QueryModel.ResultOperators, Is.EqualTo (new[] { _sourceFetchRequest }));
      var innerFetchRequests = _sourceFetchRequest.InnerFetchRequests.ToArray ();
      Assert.That (innerFetchRequests.Length, Is.EqualTo (1));
      Assert.That (innerFetchRequests[0], Is.InstanceOf (typeof (FetchManyRequest)));
      Assert.That (innerFetchRequests[0].RelationMember, Is.SameAs (typeof (Cook).GetProperty ("Assistants")));
    }

    [Test]
    public void Apply_AddsMapping ()
    {
      _node.Apply (QueryModel, ClauseGenerationContext);

      var innerFetchRequest = ((FetchRequestBase) QueryModel.ResultOperators[0]).InnerFetchRequests.Single ();
      Assert.That (ClauseGenerationContext.GetContextInfo (_node), Is.SameAs (innerFetchRequest));
    }

    [Test]
    public void Apply_AddsMappingForExisting ()
    {
      _node.Apply (QueryModel, ClauseGenerationContext);

      var node = new ThenFetchManyExpressionNode (CreateParseInfo (_sourceFetchRequestNode), ExpressionHelper.CreateLambdaExpression<Cook, IEnumerable<Cook>> (s => s.Assistants));
      node.Apply (QueryModel, ClauseGenerationContext);

      var innerFetchRequest = ((FetchRequestBase) QueryModel.ResultOperators[0]).InnerFetchRequests.Single ();
      Assert.That (ClauseGenerationContext.GetContextInfo (node), Is.SameAs (innerFetchRequest));
    }

    [Test]
    [ExpectedException (typeof (ParserException))]
    public void Apply_WithoutPreviousFetchRequest ()
    {
      var node = new ThenFetchManyExpressionNode (CreateParseInfo (), ExpressionHelper.CreateLambdaExpression<Cook, IEnumerable<Cook>> (s => s.Assistants));
      node.Apply (QueryModel, ClauseGenerationContext);
    }
  }
}
