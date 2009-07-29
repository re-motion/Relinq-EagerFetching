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
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Remotion.Data.Linq;
using Remotion.Data.Linq.Clauses;
using Remotion.Data.Linq.Clauses.StreamedData;
using Remotion.Data.Linq.EagerFetching;
using Remotion.Data.UnitTests.Linq.Parsing;
using Remotion.Data.UnitTests.Linq.TestDomain;

namespace Remotion.Data.UnitTests.Linq.EagerFetching
{
  [TestFixture]
  public class FetchRequestBaseTest
  {
    private MemberInfo _scoresMember;
    private MemberInfo _friendsMember;

    private TestFetchRequest _friendsFetchRequest;
    private IQueryable<Student> _studentFromStudentDetailQuery;
    private QueryModel _studentFromStudentDetailQueryModel;

    [SetUp]
    public void SetUp ()
    {
      _scoresMember = typeof (Student).GetProperty ("Scores");
      _friendsMember = typeof (Student).GetProperty ("Friends");
      _friendsFetchRequest = new TestFetchRequest (_friendsMember);
      _studentFromStudentDetailQuery = (from sd in ExpressionHelper.CreateStudentDetailQueryable ()
                                        select sd.Student);
      _studentFromStudentDetailQueryModel = ExpressionHelper.ParseQuery (_studentFromStudentDetailQuery);
    }

    [Test]
    public void GetOrAddInnerFetchRequest ()
    {
      Assert.That (_friendsFetchRequest.InnerFetchRequests, Is.Empty);

      var result = _friendsFetchRequest.GetOrAddInnerFetchRequest (new FetchManyRequest (_scoresMember));

      Assert.That (result.RelationMember, Is.SameAs (_scoresMember));
      Assert.That (_friendsFetchRequest.InnerFetchRequests, Is.EqualTo (new[] { result }));
    }

    [Test]
    public void RelationMember ()
    {
      Assert.That (_friendsFetchRequest.RelationMember, Is.EqualTo (typeof (Student).GetProperty ("Friends")));
    }

    [Test]
    public void GetFetchSourceExpression ()
    {
      // simulate a fetch request for the following: var query = from ... select sd.Student; query.FetchMany (s => s.Friends);

      var selectProjection = (MemberExpression) ExpressionHelper.MakeExpression<Student_Detail, Student> (sd => sd.Student);
      var selectClause = new SelectClause (selectProjection);

      var fetchSourceExpression = _friendsFetchRequest.CreateFetchSourceExpression (selectClause);
      Assert.That (fetchSourceExpression, Is.Not.Null);
    }

    [Test]
    public void GetFetchSourceExpression_Expression ()
    {
      // simulate a fetch request for the following: var query = from ... select sd.Student; query.FetchMany (s => s.Friends);

      var selectProjection = (MemberExpression) ExpressionHelper.MakeExpression<Student_Detail, Student> (sd => sd.Student);
      var selectClause = new SelectClause (selectProjection);

      var fetchSourceExpression = _friendsFetchRequest.CreateFetchSourceExpression (selectClause);

      // expecting: sd => sd.Student.Friends

      Assert.That (fetchSourceExpression.Member, Is.EqualTo (typeof (Student).GetProperty ("Friends")));

      var innerMemberExpression = (MemberExpression) fetchSourceExpression.Expression;
      Assert.That (innerMemberExpression.Member, Is.EqualTo (typeof (Student_Detail).GetProperty ("Student")));
      Assert.That (innerMemberExpression.Expression, Is.SameAs (selectProjection.Expression));
    }

    [Test]
    [ExpectedException (typeof (ArgumentException), ExpectedMessage = "The given SelectClause contains an invalid selector 'sd'. "
        + "In order to fetch the relation property 'Friends', the selector must yield objects of type 'Remotion.Data.UnitTests.Linq.TestDomain.Student', but "
        + "it yields 'Remotion.Data.UnitTests.Linq.TestDomain.Student_Detail'.\r\nParameter name: selectClauseToFetchFrom")]
    public void GetFetchSourceExpression_InvalidSelectProjection_WrongInputType ()
    {
      var selectProjection = (ParameterExpression) ExpressionHelper.MakeExpression<Student_Detail, Student_Detail> (sd => sd);
      var selectClause = new SelectClause (selectProjection);

      _friendsFetchRequest.CreateFetchSourceExpression (selectClause);
    }

    [Test]
    public void CreateFetchQueryModel ()
    {
      var fetchQueryModel = _friendsFetchRequest.CreateFetchQueryModel (_studentFromStudentDetailQueryModel);
      Assert.That (fetchQueryModel, Is.Not.Null);
      Assert.That (fetchQueryModel, Is.Not.SameAs (_studentFromStudentDetailQueryModel));
    }

    [Test]
    public void CreateFetchQueryModel_MainFromClause ()
    {
      var fetchQueryModel = _friendsFetchRequest.CreateFetchQueryModel (_studentFromStudentDetailQueryModel);

      // expecting:
      // from sd in ExpressionHelper.CreateStudentDetailQueryable()
      // (same as in original query model)

      ExpressionTreeComparer.CheckAreEqualTrees (fetchQueryModel.MainFromClause.FromExpression, _studentFromStudentDetailQueryModel.MainFromClause.FromExpression);
      Assert.That (fetchQueryModel.MainFromClause.ItemName, Is.EqualTo (_studentFromStudentDetailQueryModel.MainFromClause.ItemName));
      Assert.That (fetchQueryModel.MainFromClause.ItemType, Is.SameAs (_studentFromStudentDetailQueryModel.MainFromClause.ItemType));
    }

    [Test]
    public void CreateFetchQueryModel_SelectClause ()
    {
      var fetchQueryModel = _friendsFetchRequest.CreateFetchQueryModel (_studentFromStudentDetailQueryModel);
      Assert.That (fetchQueryModel.SelectClause.Selector, Is.SameAs (_friendsFetchRequest.FakeSelectProjection));
    }

    [Test]
    public void CreateFetchQueryModel_ResultOperatorsAreCloned_WithoutFetch ()
    {
      var resultOperator = ExpressionHelper.CreateResultOperator ();
      _studentFromStudentDetailQueryModel.ResultOperators.Add (resultOperator);
      _studentFromStudentDetailQueryModel.ResultOperators.Add (_friendsFetchRequest);

      var fetchQueryModel = _friendsFetchRequest.CreateFetchQueryModel (_studentFromStudentDetailQueryModel);

      Assert.That (fetchQueryModel.ResultOperators.Count, Is.EqualTo (1));
      Assert.That (fetchQueryModel.ResultOperators[0].GetType (), Is.SameAs (resultOperator.GetType ()));
    }

    [Test]
    public void ExecuteInMemory ()
    {
      var input = new StreamedSequence (new[] { 1, 2, 3 }, new StreamedSequenceInfo (typeof (int[]), Expression.Constant (0)));
      var result = _friendsFetchRequest.ExecuteInMemory (input);

      Assert.That (result, Is.SameAs (input));
    }
  }
}