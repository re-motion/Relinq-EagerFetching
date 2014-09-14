// This file is part of the re-linq project (relinq.codeplex.com)
// Copyright (c) rubicon IT GmbH, www.rubicon.eu
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
using NUnit.Framework;
using Remotion.Linq.Development.UnitTesting;
using Remotion.Linq.EagerFetching.Parsing;
using Remotion.Linq.EagerFetching.UnitTests.TestDomain;

namespace Remotion.Linq.EagerFetching.UnitTests.Parsing
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
