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
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Remotion.Development.UnitTesting;
using Remotion.Linq.Clauses;
using Remotion.Linq.Development.UnitTesting;
using Remotion.Linq.EagerFetching;
using Remotion.Linq.UnitTests.TestDomain;

namespace Remotion.Linq.UnitTests.EagerFetching
{
  [TestFixture]
  public class FetchOneRequestTest
  {
    private MemberInfo _substitutionMember;
    private FetchOneRequest _substitutionFetchRequest;

    [SetUp]
    public void SetUp ()
    {
      _substitutionMember = typeof (Cook).GetProperty ("Substitution");
      _substitutionFetchRequest = new FetchOneRequest (_substitutionMember);
    }

    [Test]
    public void ModifyFetchQueryModel ()
    {
      var inputFetchQuery = from fetch0 in
                              (from sd in ExpressionHelper.CreateQueryable<Kitchen> () select sd.Cook).Take (1)
                            select fetch0;
      var fetchQueryModel = ExpressionHelper.ParseQuery (inputFetchQuery);

      // expected: from fetch0 in (from sd in ExpressionHelper.CreateQueryable<Kitchen> () select sd.Cook)
      //           select fetch0.Substitution;

      PrivateInvoke.InvokeNonPublicMethod (_substitutionFetchRequest, "ModifyFetchQueryModel", fetchQueryModel);

      var selectClause = fetchQueryModel.SelectClause;
      var expectedExpression = ExpressionHelper.Resolve<Cook, Cook> (fetchQueryModel.MainFromClause, s => s.Substitution);
      ExpressionTreeComparer.CheckAreEqualTrees (selectClause.Selector, expectedExpression);
    }

    [Test]
    public void ModifyFetchQueryModel_WithConversion ()
    {
      var inputFetchQuery = from fetch0 in
                              (from sd in ExpressionHelper.CreateQueryable<Kitchen> () select (object) sd.Cook).Take (1)
                            select fetch0;
      var fetchQueryModel = ExpressionHelper.ParseQuery (inputFetchQuery);

      // expected: from fetch0 in (from sd in ExpressionHelper.CreateQueryable<Kitchen> () select sd.Cook).Take(1)
      //           select ((Cook) fetch0).Substitution;

      PrivateInvoke.InvokeNonPublicMethod (_substitutionFetchRequest, "ModifyFetchQueryModel", fetchQueryModel);

      var selectClause = fetchQueryModel.SelectClause;
      var expectedExpression = ExpressionHelper.Resolve<object, Cook> (fetchQueryModel.MainFromClause, s => ((Cook) s).Substitution);
      ExpressionTreeComparer.CheckAreEqualTrees (selectClause.Selector, expectedExpression);
    }

    [Test]
    public void Clone ()
    {
      var clone = _substitutionFetchRequest.Clone (new CloneContext (new QuerySourceMapping ()));

      Assert.That (clone, Is.Not.SameAs (_substitutionFetchRequest));
      Assert.That (clone, Is.InstanceOf (typeof (FetchOneRequest)));
      Assert.That (((FetchOneRequest) clone).RelationMember, Is.SameAs (_substitutionFetchRequest.RelationMember));
      Assert.That (((FetchOneRequest) clone).InnerFetchRequests.ToArray(), Is.Empty);
    }

    [Test]
    public void Clone_WithInnerFetchRequests ()
    {
      var innerRequest = new FetchOneRequest (_substitutionMember);
      _substitutionFetchRequest.GetOrAddInnerFetchRequest (innerRequest);

      var clone = _substitutionFetchRequest.Clone (new CloneContext (new QuerySourceMapping ()));
      var innerClones = ((FetchOneRequest) clone).InnerFetchRequests.ToArray ();
      Assert.That (innerClones.Length, Is.EqualTo (1));
      Assert.That (innerClones[0], Is.InstanceOf (typeof (FetchOneRequest)));
      Assert.That (innerClones[0], Is.Not.SameAs (innerRequest));
      Assert.That (innerClones[0].RelationMember, Is.SameAs (innerRequest.RelationMember));
    }
  }
}
