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
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Remotion.Data.DomainObjects.Linq;
using Remotion.Data.Linq.EagerFetching;
using Remotion.Data.Linq.EagerFetching.Parsing;
using Remotion.Data.UnitTests.Linq.Parsing.Structure.IntermediateModel;
using Remotion.Data.UnitTests.Linq.TestDomain;

namespace Remotion.Data.UnitTests.Linq.EagerFetching.Parsing
{
  [TestFixture]
  public class FetchOneExpressionNodeTest : ExpressionNodeTestBase
  {
    private FetchOneExpressionNode _node;

    public override void SetUp ()
    {
      base.SetUp ();

      _node = new FetchOneExpressionNode (CreateParseInfo (), ExpressionHelper.CreateLambdaExpression<Student, Student> (s => s.OtherStudent));
    }

    [Test]
    public void Apply ()
    {
      _node.Apply (QueryModel, ClauseGenerationContext);

      Assert.That (QueryModel.ResultOperators.Count, Is.EqualTo (1));
      Assert.That (QueryModel.ResultOperators[0], Is.InstanceOfType (typeof (FetchOneRequest)));
      Assert.That (((FetchOneRequest) QueryModel.ResultOperators[0]).RelationMember, Is.EqualTo (typeof (Student).GetProperty ("OtherStudent")));
    }

    [Test]
    public void Apply_AddsMapping ()
    {
      _node.Apply (QueryModel, ClauseGenerationContext);
      Assert.That (ClauseGenerationContext.GetContextInfo (_node), Is.SameAs (QueryModel.ResultOperators[0]));
    }
  }
}