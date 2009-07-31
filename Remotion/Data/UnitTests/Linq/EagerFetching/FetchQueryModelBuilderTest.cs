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
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Remotion.Data.Linq;
using Remotion.Data.Linq.Clauses.ResultOperators;
using Remotion.Data.Linq.EagerFetching;
using Remotion.Data.UnitTests.Linq.Parsing;
using Remotion.Data.UnitTests.Linq.TestDomain;
using Rhino.Mocks;

namespace Remotion.Data.UnitTests.Linq.EagerFetching
{
  [TestFixture]
  public class FetchQueryModelBuilderTest
  {
    private MemberInfo _friendsMember;
    private MemberInfo _hasDogMember;
    private MemberInfo _scoresMember;
    private MemberInfo _isOldMember;

    private TestFetchRequest _friendsFetchRequest;
    private TestFetchRequest _innerFetchRequest1;
    private TestFetchRequest _innerFetchRequest2;
    private TestFetchRequest _innerInnerFetchRequest;

    private QueryModel _studentFromStudentDetailQueryModel;
    private FetchQueryModelBuilder _friendsFetchQueryModelBuilder;

    [SetUp]
    public void SetUp ()
    {
      _friendsMember = typeof (Student).GetProperty ("Friends");
      _hasDogMember = typeof (Student).GetProperty ("HasDog");
      _scoresMember = typeof (Student).GetProperty ("Scores");
      _isOldMember = typeof (Student).GetProperty ("IsOld");

      _friendsFetchRequest = new TestFetchRequest (_friendsMember);
      _innerFetchRequest1 = new TestFetchRequest (_hasDogMember);
      _friendsFetchRequest.GetOrAddInnerFetchRequest (_innerFetchRequest1);
      _innerFetchRequest2 = new TestFetchRequest (_scoresMember);
      _friendsFetchRequest.GetOrAddInnerFetchRequest (_innerFetchRequest2);
      _innerInnerFetchRequest = new TestFetchRequest (_isOldMember);
      _innerFetchRequest1.GetOrAddInnerFetchRequest (_innerInnerFetchRequest);

      var expression = ExpressionHelper.MakeExpression ( () => (from sd in ExpressionHelper.CreateStudentDetailQueryable ()
                                                                select sd.Student).Take (1)/*.Fetch*/.Distinct().Count());
      _studentFromStudentDetailQueryModel = ExpressionHelper.ParseQuery (expression);
      _friendsFetchQueryModelBuilder = new FetchQueryModelBuilder (_friendsFetchRequest, _studentFromStudentDetailQueryModel, 1);
    }

    [Test]
    public void GetOrCreateFetchQueryModel_ClonesModel ()
    {
      var fetchQueryModel = _friendsFetchQueryModelBuilder.GetOrCreateFetchQueryModel ();
      Assert.That (fetchQueryModel, Is.Not.Null);
      Assert.That (fetchQueryModel, Is.Not.SameAs (_studentFromStudentDetailQueryModel));

      ExpressionTreeComparer.CheckAreEqualTrees (fetchQueryModel.MainFromClause.FromExpression, _studentFromStudentDetailQueryModel.MainFromClause.FromExpression);
      Assert.That (fetchQueryModel.MainFromClause.ItemName, Is.EqualTo (_studentFromStudentDetailQueryModel.MainFromClause.ItemName));
      Assert.That (fetchQueryModel.MainFromClause.ItemType, Is.SameAs (_studentFromStudentDetailQueryModel.MainFromClause.ItemType));
    }

    [Test]
    public void GetOrCreateFetchQueryModel_CallsModifyFetchQueryModel ()
    {
      var fetchRequestMock = MockRepository.GenerateMock<FetchRequestBase> (_friendsMember);

      var builder = new FetchQueryModelBuilder (fetchRequestMock, _studentFromStudentDetailQueryModel, 1);
      builder.GetOrCreateFetchQueryModel ();

      fetchRequestMock.AssertWasCalled (mock => mock.ModifyFetchQueryModel (Arg<QueryModel>.Is.NotNull));
    }

    [Test]
    public void GetOrCreateFetchQueryModel_RemovesResultOperators_StartingFromPosition ()
    {
      var fetchQueryModel = _friendsFetchQueryModelBuilder.GetOrCreateFetchQueryModel ();

      Assert.That (fetchQueryModel.ResultOperators.Count, Is.EqualTo (1));
      Assert.That (fetchQueryModel.ResultOperators[0], Is.InstanceOfType (typeof (TakeResultOperator)));
    }

    [Test]
    public void GetOrCreateFetchQueryModel_CachesResult ()
    {
      var fetchQueryModel1 = _friendsFetchQueryModelBuilder.GetOrCreateFetchQueryModel ();
      var fetchQueryModel2 = _friendsFetchQueryModelBuilder.GetOrCreateFetchQueryModel ();

      Assert.That (fetchQueryModel1, Is.SameAs (fetchQueryModel2));
    }

    [Test]
    public void CreateInnerBuilders ()
    {
      var innerBuilders = _friendsFetchQueryModelBuilder.CreateInnerBuilders ();
      Assert.That (innerBuilders.Length, Is.EqualTo (2));

      Assert.That (innerBuilders[0].QueryModel, Is.SameAs (_friendsFetchQueryModelBuilder.GetOrCreateFetchQueryModel ()));
      Assert.That (innerBuilders[0].ResultOperatorPosition, Is.EqualTo (_friendsFetchQueryModelBuilder.ResultOperatorPosition));

      Assert.That (innerBuilders[1].QueryModel, Is.SameAs (_friendsFetchQueryModelBuilder.GetOrCreateFetchQueryModel ()));
      Assert.That (innerBuilders[1].ResultOperatorPosition, Is.EqualTo (_friendsFetchQueryModelBuilder.ResultOperatorPosition));
    }

    [Test]
    public void CreateInnerBuilders_OnInnerBuilders ()
    {
      var innerBuilders = _friendsFetchQueryModelBuilder.CreateInnerBuilders ();
      var innerInnerBuilders = innerBuilders[0].CreateInnerBuilders ();

      Assert.That (innerInnerBuilders.Length, Is.EqualTo (1));

      Assert.That (innerInnerBuilders[0].QueryModel, Is.SameAs (innerBuilders[0].GetOrCreateFetchQueryModel ()));
      Assert.That (innerInnerBuilders[0].ResultOperatorPosition, Is.EqualTo (_friendsFetchQueryModelBuilder.ResultOperatorPosition));
    }

    [Test]
    public void CreateInnerBuilders_WithoutInnerRequests ()
    {
      var innerBuilders = _friendsFetchQueryModelBuilder.CreateInnerBuilders ();
      var innerInnerBuilders = innerBuilders[1].CreateInnerBuilders ();

      Assert.That (innerInnerBuilders.Length, Is.EqualTo (0));
    }
  }
}