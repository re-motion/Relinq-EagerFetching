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
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Remotion.Data.Linq.EagerFetching;
using Remotion.Data.UnitTests.Linq.Parsing.Structure.IntermediateModel;
using Remotion.Data.UnitTests.Linq.TestDomain;

namespace Remotion.Data.UnitTests.Linq.EagerFetching
{
  [TestFixture]
  public class FetchManyExpressionNodeTest : ExpressionNodeTestBase
  {
    private FetchManyExpressionNode _node;

    public override void SetUp ()
    {
      base.SetUp ();

      _node = new FetchManyExpressionNode (CreateParseInfo (), ExpressionHelper.CreateLambdaExpression<Student, IEnumerable<Student>> (s => s.Friends));
    }

    [Test]
    public void SupportedMethod ()
    {
      var method = typeof (ExtensionMethods).GetMethod ("FetchMany");
      Assert.That (FetchManyExpressionNode.SupportedMethods, List.Contains (method));
    }

    [Test]
    public void Apply ()
    {
      _node.Apply (QueryModel, ClauseGenerationContext);

      Assert.That (QueryModel.ResultOperators.Count, Is.EqualTo (1));
      Assert.That (QueryModel.ResultOperators[0], Is.InstanceOfType (typeof (FetchManyRequest)));
      Assert.That (((FetchManyRequest) QueryModel.ResultOperators[0]).RelationMember, Is.EqualTo (typeof (Student).GetProperty ("Friends")));
    }
  }
}