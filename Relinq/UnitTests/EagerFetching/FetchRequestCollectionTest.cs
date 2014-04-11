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
using System.Reflection;
using NUnit.Framework;
using Remotion.Linq.EagerFetching;
using Remotion.Linq.UnitTests.TestDomain;

namespace Remotion.Linq.UnitTests.EagerFetching
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
