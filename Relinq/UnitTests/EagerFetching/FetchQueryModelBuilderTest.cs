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
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ResultOperators;
using Remotion.Linq.Development.UnitTesting;
using Remotion.Linq.EagerFetching;
using Remotion.Linq.UnitTests.TestDomain;
using Rhino.Mocks;

namespace Remotion.Linq.UnitTests.EagerFetching
{
  [TestFixture]
  public class FetchQueryModelBuilderTest
  {
    private MemberInfo _friendsMember;
    private MemberInfo _hasDogMember;
    private MemberInfo _scoresMember;
    private MemberInfo _isOldMember;

    private TestFetchRequest _outerFetchRequest;
    private TestFetchRequest _innerFetchRequest1;
    private TestFetchRequest _innerFetchRequest2;
    private TestFetchRequest _innerInnerFetchRequest;

    private QueryModel _sourceItemQueryModel;
    private FetchQueryModelBuilder _outerFetchQueryModelBuilder;

    [SetUp]
    public void SetUp ()
    {
      _friendsMember = typeof (Cook).GetProperty ("Assistants");
      _hasDogMember = typeof (Cook).GetProperty ("IsStarredCook");
      _scoresMember = typeof (Cook).GetProperty ("Holidays");
      _isOldMember = typeof (Cook).GetProperty ("IsFullTimeCook");

      _outerFetchRequest = new TestFetchRequest (_friendsMember);
      _innerFetchRequest1 = new TestFetchRequest (_hasDogMember);
      _outerFetchRequest.GetOrAddInnerFetchRequest (_innerFetchRequest1);
      _innerFetchRequest2 = new TestFetchRequest (_scoresMember);
      _outerFetchRequest.GetOrAddInnerFetchRequest (_innerFetchRequest2);
      _innerInnerFetchRequest = new TestFetchRequest (_isOldMember);
      _innerFetchRequest1.GetOrAddInnerFetchRequest (_innerInnerFetchRequest);

      var expression = ExpressionHelper.MakeExpression ( () => (from sd in ExpressionHelper.CreateQueryable<Kitchen> ()
                                                                select sd.Cook).Take (1)/*.Fetch*/.Distinct().Count());
      _sourceItemQueryModel = ExpressionHelper.ParseQuery (expression);
      _outerFetchQueryModelBuilder = new FetchQueryModelBuilder (_outerFetchRequest, _sourceItemQueryModel, 1);
    }

    [Test]
    public void GetOrCreateFetchQueryModel_CallsCreateFetchQueryModel ()
    {
      var fetchRequestMock = MockRepository.GenerateMock<FetchRequestBase> (_friendsMember);
      var mockQueryModel = ExpressionHelper.CreateQueryModel<Cook> ();
      fetchRequestMock.Expect (mock => mock.CreateFetchQueryModel (Arg<QueryModel>.Is.Anything)).Return (mockQueryModel);

      var builder = new FetchQueryModelBuilder (fetchRequestMock, _sourceItemQueryModel, 1);
      var result = builder.GetOrCreateFetchQueryModel ();

      Assert.That (result, Is.SameAs (mockQueryModel));
    }

    [Test]
    public void GetOrCreateFetchQueryModel_ClonesSourceModel_AndResetsResultTypeOverride ()
    {
      var fetchQueryModel = _outerFetchQueryModelBuilder.GetOrCreateFetchQueryModel ();
      fetchQueryModel.ResultTypeOverride = typeof (List<>);
      var newSourceModel = ((SubQueryExpression) fetchQueryModel.MainFromClause.FromExpression).QueryModel;
      Assert.That (newSourceModel, Is.Not.Null);
      Assert.That (newSourceModel, Is.Not.SameAs (_sourceItemQueryModel));
      Assert.That (newSourceModel.ResultTypeOverride, Is.Null);

      ExpressionTreeComparer.CheckAreEqualTrees (newSourceModel.MainFromClause.FromExpression, _sourceItemQueryModel.MainFromClause.FromExpression);
      Assert.That (newSourceModel.MainFromClause.ItemName, Is.EqualTo (_sourceItemQueryModel.MainFromClause.ItemName));
      Assert.That (newSourceModel.MainFromClause.ItemType, Is.SameAs (_sourceItemQueryModel.MainFromClause.ItemType));
    }

    [Test]
    public void GetOrCreateFetchQueryModel_RemovesResultOperators_StartingFromPosition ()
    {
      var fetchQueryModel = _outerFetchQueryModelBuilder.GetOrCreateFetchQueryModel ();
      var newSourceModel = ((SubQueryExpression) fetchQueryModel.MainFromClause.FromExpression).QueryModel;

      Assert.That (newSourceModel.ResultOperators.Count, Is.EqualTo (1));
      Assert.That (newSourceModel.ResultOperators[0], Is.InstanceOf (typeof (TakeResultOperator)));
    }

    [Test]
    public void GetOrCreateFetchQueryModel_CachesResult ()
    {
      var fetchQueryModel1 = _outerFetchQueryModelBuilder.GetOrCreateFetchQueryModel ();
      var fetchQueryModel2 = _outerFetchQueryModelBuilder.GetOrCreateFetchQueryModel ();

      Assert.That (fetchQueryModel1, Is.SameAs (fetchQueryModel2));
    }

    [Test]
    public void CreateInnerBuilders ()
    {
      var innerBuilders = _outerFetchQueryModelBuilder.CreateInnerBuilders ();
      Assert.That (innerBuilders.Length, Is.EqualTo (2));

      Assert.That (innerBuilders[0].SourceItemQueryModel, Is.SameAs (_outerFetchQueryModelBuilder.GetOrCreateFetchQueryModel ()));
      Assert.That (innerBuilders[0].ResultOperatorPosition, Is.EqualTo (0));

      Assert.That (innerBuilders[1].SourceItemQueryModel, Is.SameAs (_outerFetchQueryModelBuilder.GetOrCreateFetchQueryModel ()));
      Assert.That (innerBuilders[1].ResultOperatorPosition, Is.EqualTo (0));
    }

    [Test]
    public void CreateInnerBuilders_OnInnerBuilders ()
    {
      var innerBuilders = _outerFetchQueryModelBuilder.CreateInnerBuilders ();
      var innerInnerBuilders = innerBuilders[0].CreateInnerBuilders ();

      Assert.That (innerInnerBuilders.Length, Is.EqualTo (1));

      Assert.That (innerInnerBuilders[0].SourceItemQueryModel, Is.SameAs (innerBuilders[0].GetOrCreateFetchQueryModel ()));
      Assert.That (innerInnerBuilders[0].ResultOperatorPosition, Is.EqualTo (0));
    }

    [Test]
    public void CreateInnerBuilders_WithoutInnerRequests ()
    {
      var innerBuilders = _outerFetchQueryModelBuilder.CreateInnerBuilders ();
      var innerInnerBuilders = innerBuilders[1].CreateInnerBuilders ();

      Assert.That (innerInnerBuilders.Length, Is.EqualTo (0));
    }
  }
}
