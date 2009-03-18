// This file is part of the re-motion Core Framework (www.re-motion.org)
// Copyright (C) 2005-2008 rubicon informationstechnologie gmbh, www.rubicon.eu
// 
// The re-motion Core Framework is free software; you can redistribute it 
// and/or modify it under the terms of the GNU Lesser General Public License 
// version 3.0 as published by the Free Software Foundation.
// 
// This framework is distributed in the hope that it will be useful, 
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with this framework; if not, see http://www.gnu.org/licenses.
// 
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Remotion.Data.Linq.EagerFetching;

namespace Remotion.Data.UnitTests.Linq.EagerFetchingTest
{
  [TestFixture]
  public class FetchRequestTest
  {
    [Test]
    public void GetOrAddFetchRequest ()
    {
      FetchRequest<Student> fetchRequest = FetchRequest<Student>.Create<Student> (s => s.Friends);

      Assert.That (fetchRequest.InnerFetchRequests, Is.Empty);

      Expression<Func<Student, IEnumerable<int>>> expectedExpression = s => s.Scores;
      var result = fetchRequest.GetOrAddInnerFetchRequest (expectedExpression);

      Assert.That (result.RelatedObjectSelector, Is.SameAs (expectedExpression));
      Assert.That (fetchRequest.InnerFetchRequests, Is.EqualTo (new[] { result }));
    }
  }
}