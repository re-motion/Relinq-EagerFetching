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
using System.Linq.Expressions;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Remotion.Data.Linq.EagerFetching;

namespace Remotion.Data.UnitTests.Linq.EagerFetching
{
  [TestFixture]
  public class FetchRequestCollectionTest
  {
    private FetchRequestCollection _collection;
    private Expression<Func<Student, IEnumerable<int>>> _scoresFetchExpression;

    [SetUp]
    public void SetUp ()
    {
      _collection = new FetchRequestCollection ();
      _scoresFetchExpression = (s => s.Scores);
    }

    [Test]
    public void AddFetchRequest ()
    {
      Assert.That (_collection.FetchRequests, Is.Empty);

      var result = _collection.GetOrAddFetchRequest (new FetchManyRequest (_scoresFetchExpression));

      Assert.That (result.RelatedObjectSelector, Is.SameAs (_scoresFetchExpression));
      Assert.That (_collection.FetchRequests, Is.EqualTo (new[] { result }));
    }

    [Test]
    public void AddFetchRequest_Twice ()
    {
      Assert.That (_collection.FetchRequests, Is.Empty);
      var result1 = _collection.GetOrAddFetchRequest (new FetchManyRequest (_scoresFetchExpression));
      var result2 = _collection.GetOrAddFetchRequest (new FetchManyRequest (_scoresFetchExpression));

      Assert.That (result1, Is.SameAs (result2));
      Assert.That (_collection.FetchRequests, Is.EqualTo (new[] { result1 }));
    }

    [Test]
    [ExpectedException (typeof (ArgumentException), ExpectedMessage = "A fetch request must be a simple member access expression; 'new [] {1, 2, 3}' "
        + "is a NewArrayExpression instead.\r\nParameter name: relatedObjectSelector")]
    public void AddFetchRequest_InvalidExpression ()
    {
      LambdaExpression relatedObjectSelector = ((Expression<Func<Student, IEnumerable<int>>>) (s => new[] { 1, 2, 3 }));
      _collection.GetOrAddFetchRequest (new FetchManyRequest (relatedObjectSelector));
    }

    [Test]
    [ExpectedException (typeof (ArgumentException), ExpectedMessage = "A fetch request must be a simple member access expression of the kind "
        + "o => o.Related; 's.OtherStudent.Friends' is too complex.\r\nParameter name: relatedObjectSelector")]
    public void AddFetchRequest_InvalidExpression_MoreThanOneMember ()
    {
      LambdaExpression relatedObjectSelector = ((Expression<Func<Student, IEnumerable<Student>>>) (s => s.OtherStudent.Friends));
      _collection.GetOrAddFetchRequest (new FetchManyRequest (relatedObjectSelector));
    }
  }
}