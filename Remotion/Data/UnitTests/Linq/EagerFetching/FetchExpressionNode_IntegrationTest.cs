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
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Remotion.Data.Linq.EagerFetching;
using Remotion.Data.UnitTests.Linq.Parsing.Structure.IntermediateModel;
using Remotion.Data.UnitTests.Linq.TestDomain;

namespace Remotion.Data.UnitTests.Linq.EagerFetching
{
  [TestFixture]
  public class FetchExpressionNode_IntegrationTest : ExpressionNodeTestBase
  {
    [Test]
    public void IntegrationTest_ApplySeveralRequests ()
    {
      var node1 = new FetchOneExpressionNode (CreateParseInfo(), ExpressionHelper.CreateLambdaExpression<Student, Student> (s => s.OtherStudent));
      var node2 = new ThenFetchManyExpressionNode (
          CreateParseInfo (node1), ExpressionHelper.CreateLambdaExpression<Student, IEnumerable<Student>> (s => s.Friends));
      var node3 = new ThenFetchOneExpressionNode (CreateParseInfo (node2), ExpressionHelper.CreateLambdaExpression<Student, bool> (s => s.HasDog));
      var node4 = new FetchManyExpressionNode (CreateParseInfo (node3), ExpressionHelper.CreateLambdaExpression<Student, List<int>> (s => s.Scores));

      node1.Apply (QueryModel, ClauseGenerationContext);
      node2.Apply (QueryModel, ClauseGenerationContext);
      node3.Apply (QueryModel, ClauseGenerationContext);
      node4.Apply (QueryModel, ClauseGenerationContext);

      Assert.That (QueryModel.ResultOperators.Count, Is.EqualTo (2));

      var fetchRequest1 = ((FetchOneRequest) QueryModel.ResultOperators[0]);
      Assert.That (fetchRequest1.RelationMember, Is.EqualTo (typeof (Student).GetProperty ("OtherStudent")));
      Assert.That (fetchRequest1.InnerFetchRequests.Count(), Is.EqualTo (1));

      var fetchRequest2 = ((FetchManyRequest) fetchRequest1.InnerFetchRequests.Single());
      Assert.That (fetchRequest2.RelationMember, Is.EqualTo (typeof (Student).GetProperty ("Friends")));
      Assert.That (fetchRequest2.InnerFetchRequests.Count(), Is.EqualTo (1));

      var fetchRequest3 = ((FetchOneRequest) fetchRequest2.InnerFetchRequests.Single());
      Assert.That (fetchRequest3.RelationMember, Is.EqualTo (typeof (Student).GetProperty ("HasDog")));
      Assert.That (fetchRequest3.InnerFetchRequests.Count(), Is.EqualTo (0));

      var fetchRequest4 = ((FetchManyRequest) QueryModel.ResultOperators[1]);
      Assert.That (fetchRequest4.RelationMember, Is.EqualTo (typeof (Student).GetProperty ("Scores")));
      Assert.That (fetchRequest4.InnerFetchRequests.Count(), Is.EqualTo (0));
    }
  }
}