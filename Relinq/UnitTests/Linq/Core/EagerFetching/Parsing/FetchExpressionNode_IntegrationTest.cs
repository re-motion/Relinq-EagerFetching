// This file is part of the re-motion Core Framework (www.re-motion.org)
// Copyright (C) 2005-2009 rubicon informationstechnologie gmbh, www.rubicon.eu
// 
// The re-motion Core Framework is free software; you can redistribute it 
// and/or modify it under the terms of the GNU Lesser General Public License 
// as published by the Free Software Foundation; either version 2.1 of the 
// License, or (at your option) any later version.
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
using Remotion.Data.Linq.EagerFetching.Parsing;
using Remotion.Data.Linq.UnitTests.Linq.Core.Parsing.Structure.IntermediateModel;
using Remotion.Data.Linq.UnitTests.Linq.Core.TestDomain;

namespace Remotion.Data.Linq.UnitTests.Linq.Core.EagerFetching.Parsing
{
  [TestFixture]
  public class FetchExpressionNode_IntegrationTest : ExpressionNodeTestBase
  {
    [Test]
    public void IntegrationTest_ApplySeveralRequests ()
    {
      var node1 = new FetchOneExpressionNode (CreateParseInfo(), ExpressionHelper.CreateLambdaExpression<Cook, Cook> (s => s.Substitution));
      var node2 = new ThenFetchManyExpressionNode (
          CreateParseInfo (node1), ExpressionHelper.CreateLambdaExpression<Cook, IEnumerable<Cook>> (s => s.Assistants));
      var node3 = new ThenFetchOneExpressionNode (CreateParseInfo (node2), ExpressionHelper.CreateLambdaExpression<Cook, bool> (s => s.IsStarredCook));
      var node4 = new FetchManyExpressionNode (CreateParseInfo (node3), ExpressionHelper.CreateLambdaExpression<Cook, List<int>> (s => s.Holidays));

      node1.Apply (QueryModel, ClauseGenerationContext);
      node2.Apply (QueryModel, ClauseGenerationContext);
      node3.Apply (QueryModel, ClauseGenerationContext);
      node4.Apply (QueryModel, ClauseGenerationContext);

      Assert.That (QueryModel.ResultOperators.Count, Is.EqualTo (2));

      var fetchRequest1 = ((FetchOneRequest) QueryModel.ResultOperators[0]);
      Assert.That (fetchRequest1.RelationMember, Is.EqualTo (typeof (Cook).GetProperty ("Substitution")));
      Assert.That (fetchRequest1.InnerFetchRequests.Count(), Is.EqualTo (1));

      var fetchRequest2 = ((FetchManyRequest) fetchRequest1.InnerFetchRequests.Single());
      Assert.That (fetchRequest2.RelationMember, Is.EqualTo (typeof (Cook).GetProperty ("Assistants")));
      Assert.That (fetchRequest2.InnerFetchRequests.Count(), Is.EqualTo (1));

      var fetchRequest3 = ((FetchOneRequest) fetchRequest2.InnerFetchRequests.Single());
      Assert.That (fetchRequest3.RelationMember, Is.EqualTo (typeof (Cook).GetProperty ("IsStarredCook")));
      Assert.That (fetchRequest3.InnerFetchRequests.Count(), Is.EqualTo (0));

      var fetchRequest4 = ((FetchManyRequest) QueryModel.ResultOperators[1]);
      Assert.That (fetchRequest4.RelationMember, Is.EqualTo (typeof (Cook).GetProperty ("Holidays")));
      Assert.That (fetchRequest4.InnerFetchRequests.Count(), Is.EqualTo (0));
    }
  }
}
