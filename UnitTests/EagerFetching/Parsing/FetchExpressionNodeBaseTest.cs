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
using System.Linq.Expressions;
using NUnit.Framework;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Development.UnitTesting;
using Remotion.Linq.UnitTests.Parsing.Structure.IntermediateModel;
using Remotion.Linq.UnitTests.TestDomain;

namespace Remotion.Linq.UnitTests.EagerFetching.Parsing
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
