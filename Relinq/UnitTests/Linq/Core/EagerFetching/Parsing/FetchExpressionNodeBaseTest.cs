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
using System.Linq.Expressions;
using NUnit.Framework;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.UnitTests.Linq.Core.Parsing.Structure.IntermediateModel;
using Remotion.Linq.UnitTests.Linq.Core.TestDomain;

namespace Remotion.Linq.UnitTests.Linq.Core.EagerFetching.Parsing
{
  [TestFixture]
  public class FetchExpressionNodeBaseTest : ExpressionNodeTestBase
  {
    private TestFetchExpressionNodeBase _node;

    public override void SetUp ()
    {
      base.SetUp ();

      _node = new TestFetchExpressionNodeBase (CreateParseInfo (), ExpressionHelper.CreateLambdaExpression<Cook, Cook> (s => s.Substitution));
    }

    [Test]
    public void Initialization ()
    {
      Assert.That (_node.RelationMember, Is.EqualTo (typeof (Cook).GetProperty ("Substitution")));
    }

    [Test]
    public void Initialization_WithConversions ()
    {
// ReSharper disable RedundantCast
// ReSharper disable PossibleInvalidCastException
      var node = new TestFetchExpressionNodeBase (CreateParseInfo (), ExpressionHelper.CreateLambdaExpression<object, Cook> (s => ((Cook) (object) (string) s).Substitution));
// ReSharper restore PossibleInvalidCastException
// ReSharper restore RedundantCast
      Assert.That (node.RelationMember, Is.EqualTo (typeof (Cook).GetProperty ("Substitution")));
    }

    [Test]
    [ExpectedException (typeof (ArgumentException), ExpectedMessage = 
        @"A fetch request must be a simple member access expression; 'new \[\] \{1, 2, 3\}' is a .* instead\.",
        MatchType = MessageMatch.Regex)]
    public void Initialization_InvalidExpression ()
    {
      var relatedObjectSelector = ExpressionHelper.CreateLambdaExpression<Cook, IEnumerable<int>> (s => new[] { 1, 2, 3 });
      new TestFetchExpressionNodeBase (CreateParseInfo (), relatedObjectSelector);
    }

    [Test]
    [ExpectedException (typeof (ArgumentException), ExpectedMessage = "A fetch request must be a simple member access expression of the kind "
                                                                      + "o => o.Related; 's.Substitution.Assistants' is too complex.\r\nParameter name: relatedObjectSelector")]
    public void Initialization_InvalidExpression_MoreThanOneMember ()
    {
      var relatedObjectSelector = (Expression<Func<Cook, IEnumerable<Cook>>>) (s => s.Substitution.Assistants);
      new TestFetchExpressionNodeBase (CreateParseInfo (), relatedObjectSelector);
    }

    [Test]
    public void Resolve ()
    {
      var expression = ExpressionHelper.CreateLambdaExpression<Cook, Cook> (s => s);

      var result = _node.Resolve (expression.Parameters[0], expression.Body, ClauseGenerationContext);
      Assert.That (((QuerySourceReferenceExpression) result).ReferencedQuerySource, Is.SameAs (SourceClause));
    }
  }
}
