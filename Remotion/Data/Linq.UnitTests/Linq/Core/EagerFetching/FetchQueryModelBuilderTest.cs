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
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Remotion.Data.Linq;
using Remotion.Data.Linq.Clauses.Expressions;
using Remotion.Data.Linq.Clauses.ResultOperators;
using Remotion.Data.Linq.EagerFetching;
using Remotion.Data.Linq.UnitTests.Linq.Core.Parsing;
using Remotion.Data.Linq.UnitTests.Linq.Core.TestDomain;
using Rhino.Mocks;

namespace Remotion.Data.Linq.UnitTests.Linq.Core.EagerFetching
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

      var expression = ExpressionHelper.MakeExpression ( () => (from sd in ExpressionHelper.CreateKitchenQueryable ()
                                                                select sd.Cook).Take (1)/*.Fetch*/.Distinct().Count());
      _sourceItemQueryModel = ExpressionHelper.ParseQuery (expression);
      _outerFetchQueryModelBuilder = new FetchQueryModelBuilder (_outerFetchRequest, _sourceItemQueryModel, 1);
    }

    [Test]
    public void GetOrCreateFetchQueryModel_CallsCreateFetchQueryModel ()
    {
      var fetchRequestMock = MockRepository.GenerateMock<FetchRequestBase> (_friendsMember);
      var mockQueryModel = ExpressionHelper.CreateQueryModel_Cook ();
      fetchRequestMock.Expect (mock => mock.CreateFetchQueryModel (Arg<QueryModel>.Is.Anything)).Return (mockQueryModel);

      var builder = new FetchQueryModelBuilder (fetchRequestMock, _sourceItemQueryModel, 1);
      var result = builder.GetOrCreateFetchQueryModel ();

      Assert.That (result, Is.SameAs (mockQueryModel));
    }

    [Test]
    public void GetOrCreateFetchQueryModel_ClonesSourceModel ()
    {
      var fetchQueryModel = _outerFetchQueryModelBuilder.GetOrCreateFetchQueryModel ();
      var newSourceModel = ((SubQueryExpression) fetchQueryModel.MainFromClause.FromExpression).QueryModel;
      Assert.That (newSourceModel, Is.Not.Null);
      Assert.That (newSourceModel, Is.Not.SameAs (_sourceItemQueryModel));

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
      Assert.That (newSourceModel.ResultOperators[0], Is.InstanceOfType (typeof (TakeResultOperator)));
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
