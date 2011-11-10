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
using System.Reflection;
using NUnit.Framework;
using Remotion.Linq.UnitTests.Linq.Core.TestDomain;
using Remotion.Linq.EagerFetching;

namespace Remotion.Linq.UnitTests.Linq.Core.EagerFetching
{
  [TestFixture]
  public class FetchRequestCollectionTest
  {
    private FetchRequestCollection _collection;
    private MemberInfo _scoresMember;

    [SetUp]
    public void SetUp ()
    {
      _collection = new FetchRequestCollection ();
      _scoresMember = typeof (Cook).GetProperty ("Holidays");
    }

    [Test]
    public void AddFetchRequest ()
    {
      Assert.That (_collection.FetchRequests, Is.Empty);

      var result = _collection.GetOrAddFetchRequest (new FetchManyRequest (_scoresMember));

      Assert.That (result.RelationMember, Is.SameAs (_scoresMember));
      Assert.That (_collection.FetchRequests, Is.EqualTo (new[] { result }));
    }

    [Test]
    public void AddFetchRequest_Twice ()
    {
      Assert.That (_collection.FetchRequests, Is.Empty);
      var result1 = _collection.GetOrAddFetchRequest (new FetchManyRequest (_scoresMember));
      var result2 = _collection.GetOrAddFetchRequest (new FetchManyRequest (_scoresMember));

      Assert.That (result1, Is.SameAs (result2));
      Assert.That (_collection.FetchRequests, Is.EqualTo (new[] { result1 }));
    }
  }
}
