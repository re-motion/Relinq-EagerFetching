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
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Remotion.Development.UnitTesting;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Development.UnitTesting;
using Remotion.Linq.EagerFetching;
using Remotion.Linq.UnitTests.TestDomain;

namespace Remotion.Linq.UnitTests.EagerFetching
{
  [TestFixture]
  public class FetchManyRequestTest
  {
    private MemberInfo _friendsMember;

    private FetchManyRequest _friendsFetchRequest;

    [SetUp]
    public void SetUp ()
    {
      _friendsMember = typeof (Cook).GetProperty ("Assistants");
      _friendsFetchRequest = new FetchManyRequest (_friendsMember);
    }

    [Test]
    [ExpectedException (typeof (ArgumentException), ExpectedMessage = 
        "Expected a type implementing IEnumerable<T>, but found 'System.Int32'.\r\nParameter name: relationMember")]
    public void Create_InvalidRelationMember ()
    {
      var idMember = typeof (Cook).GetProperty ("ID");
      new FetchManyRequest (idMember);
    }

    [Test]
    public void ModifyFetchQueryModel ()
    {
      var inputFetchQuery = from fetch0 in (from sd in ExpressionHelper.CreateQueryable<Kitchen> () select sd.Cook).Take (1)
                            select fetch0;
      var fetchQueryModel = ExpressionHelper.ParseQuery (inputFetchQuery);

      // expected: from fetch0 in (from sd in ExpressionHelper.CreateQueryable<Kitchen> () select sd.Cook).Take (1)
      //           from fetch1 in fetch0.Assistants
      //           select fetch1;

      PrivateInvoke.InvokeNonPublicMethod (_friendsFetchRequest, "ModifyFetchQueryModel", fetchQueryModel);

      var additionalFromClause = (AdditionalFromClause) fetchQueryModel.BodyClauses[0];
      var expectedExpression = ExpressionHelper.Resolve<Cook, IEnumerable<Cook>> (fetchQueryModel.MainFromClause, s => s.Assistants);
      ExpressionTreeComparer.CheckAreEqualTrees (expectedExpression, additionalFromClause.FromExpression);

      Assert.That (((QuerySourceReferenceExpression) fetchQueryModel.SelectClause.Selector).ReferencedQuerySource, Is.SameAs (additionalFromClause));
    }

    [Test]
    public void ModifyFetchQueryModel_WithConversion ()
    {
      var inputFetchQuery = from fetch0 in (from sd in ExpressionHelper.CreateQueryable<Kitchen> () select (object) sd.Cook).Take (1)
                            select fetch0;
      var fetchQueryModel = ExpressionHelper.ParseQuery (inputFetchQuery);

      // expected: from fetch0 in (from sd in ExpressionHelper.CreateQueryable<Kitchen> () select (object) sd.Cook).Take (1)
      //           from fetch1 in ((Cook) fetch0).Assistants
      //           select fetch1;

      PrivateInvoke.InvokeNonPublicMethod (_friendsFetchRequest, "ModifyFetchQueryModel", fetchQueryModel);

      var additionalFromClause = (AdditionalFromClause) fetchQueryModel.BodyClauses[0];
      var expectedExpression = ExpressionHelper.Resolve<object, IEnumerable<Cook>> (fetchQueryModel.MainFromClause, s => ((Cook) s).Assistants);
      ExpressionTreeComparer.CheckAreEqualTrees (expectedExpression, additionalFromClause.FromExpression);

      Assert.That (((QuerySourceReferenceExpression) fetchQueryModel.SelectClause.Selector).ReferencedQuerySource, Is.SameAs (additionalFromClause));
    }

    [Test]
    public void Clone ()
    {
      var clone = _friendsFetchRequest.Clone (new CloneContext (new QuerySourceMapping ()));

      Assert.That (clone, Is.Not.SameAs (_friendsFetchRequest));
      Assert.That (clone, Is.InstanceOf (typeof (FetchManyRequest)));
      Assert.That (((FetchManyRequest) clone).RelationMember, Is.SameAs (_friendsFetchRequest.RelationMember));
      Assert.That (((FetchManyRequest) clone).InnerFetchRequests.ToArray (), Is.Empty);
    }

    [Test]
    public void Clone_WithInnerFetchRequests ()
    {
      var innerRequest = new FetchManyRequest (_friendsMember);
      _friendsFetchRequest.GetOrAddInnerFetchRequest (innerRequest);

      var clone = _friendsFetchRequest.Clone (new CloneContext (new QuerySourceMapping ()));
      var innerClones = ((FetchManyRequest) clone).InnerFetchRequests.ToArray ();
      Assert.That (innerClones.Length, Is.EqualTo (1));
      Assert.That (innerClones[0], Is.Not.SameAs (innerRequest));
      Assert.That (innerClones[0], Is.InstanceOf (typeof (FetchManyRequest)));
      Assert.That (innerClones[0].RelationMember, Is.SameAs (innerRequest.RelationMember));
    }
  }
}
