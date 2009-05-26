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
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Remotion.Data.Linq;
using Remotion.Data.Linq.Clauses;
using Remotion.Data.Linq.EagerFetching;
using Remotion.Data.UnitTests.Linq.Parsing;

namespace Remotion.Data.UnitTests.Linq.EagerFetching
{
  [TestFixture]
  public class FetchRequestBaseTest
  {
    private Expression<Func<Student, IEnumerable<int>>> _scoresFetchExpression;
    private Expression<Func<Student, IEnumerable<Student>>> _friendsFetchExpression;
    private TestFetchRequest _friendsFetchRequest;
    private IQueryable<Student> _studentFromStudentDetailQuery;
    private QueryModel _studentFromStudentDetailQueryModel;

    [SetUp]
    public void SetUp ()
    {
      _scoresFetchExpression = (s => s.Scores);
      _friendsFetchExpression = (s => s.Friends);
      _friendsFetchRequest = new TestFetchRequest (_friendsFetchExpression);
      _studentFromStudentDetailQuery = (from sd in ExpressionHelper.CreateQuerySource_Detail ()
                                        select sd.Student);
      _studentFromStudentDetailQueryModel = ExpressionHelper.ParseQuery (_studentFromStudentDetailQuery);
    }

    [Test]
    [ExpectedException (typeof (ArgumentException), ExpectedMessage = "A fetch request must be a simple member access expression; 'new [] {1, 2, 3}' "
        + "is a NewArrayExpression instead.\r\nParameter name: relatedObjectSelector")]
    public void Create_InvalidExpression ()
    {
      var relatedObjectSelector = ExpressionHelper.CreateLambdaExpression<Student, IEnumerable<int>> (s => new[] { 1, 2, 3 });
      new TestFetchRequest (relatedObjectSelector);
    }

    [Test]
    [ExpectedException (typeof (ArgumentException), ExpectedMessage = "A fetch request must be a simple member access expression of the kind "
        + "o => o.Related; 's.OtherStudent.Friends' is too complex.\r\nParameter name: relatedObjectSelector")]
    public void Create_InvalidExpression_MoreThanOneMember ()
    {
      var relatedObjectSelector = (Expression<Func<Student, IEnumerable<Student>>>) (s => s.OtherStudent.Friends);
      new TestFetchRequest (relatedObjectSelector);
    }

    [Test]
    public void GetOrAddInnerFetchRequest ()
    {
      Assert.That (_friendsFetchRequest.InnerFetchRequests, Is.Empty);

      var result = _friendsFetchRequest.GetOrAddInnerFetchRequest (new FetchManyRequest (_scoresFetchExpression));

      Assert.That (result.RelatedObjectSelector, Is.SameAs (_scoresFetchExpression));
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

      var previousClause = ExpressionHelper.CreateClause ();
      Expression<Func<Student_Detail, Student>> selectProjection = sd => sd.Student;
      var selectClause = new SelectClause (previousClause, selectProjection);

      var fetchSourceExpression = _friendsFetchRequest.CreateFetchSourceExpression (selectClause);
      Assert.That (fetchSourceExpression, Is.Not.Null);
    }

    [Test]
    public void GetFetchSourceExpression_Expression ()
    {
      // simulate a fetch request for the following: var query = from ... select sd.Student; query.FetchMany (s => s.Friends);

      var previousClause = ExpressionHelper.CreateClause ();
      Expression<Func<Student_Detail, Student>> selectProjection = sd => sd.Student;
      var selectClause = new SelectClause (previousClause, selectProjection);

      var fetchSourceExpression = _friendsFetchRequest.CreateFetchSourceExpression (selectClause);

      // expecting: sd => sd.Student.Friends

      Assert.That (fetchSourceExpression.Parameters.Count, Is.EqualTo (1));
      Assert.That (fetchSourceExpression.Parameters[0], Is.SameAs (selectProjection.Parameters[0]));

      var memberExpression = (MemberExpression) fetchSourceExpression.Body;
      Assert.That (memberExpression.Member, Is.EqualTo (typeof (Student).GetProperty ("Friends")));

      var innerMemberExpression = (MemberExpression) memberExpression.Expression;
      Assert.That (innerMemberExpression.Member, Is.EqualTo (typeof (Student_Detail).GetProperty ("Student")));

      var innermostParameterExpression = innerMemberExpression.Expression as ParameterExpression;
      Assert.That (innermostParameterExpression, Is.Not.Null);
      Assert.That (innermostParameterExpression, Is.SameAs (fetchSourceExpression.Parameters[0]));
    }

    [Test]
    [ExpectedException (typeof (ArgumentException), ExpectedMessage = "The given SelectClause contains an invalid projection expression " 
        + "'(sd, i) => sd.Student'. Expected one parameter, but found 2.\r\nParameter name: selectClauseToFetchFrom")]
    [Ignore ("TODO 1096: Enable this as soon as SelectMany clauses don't lead to invalid select projections any longer.")]
    public void GetFetchSourceExpression_InvalidSelectProjection_WrongParameterCount_TooMany ()
    {
      var previousClause = ExpressionHelper.CreateClause ();
      Expression<Func<Student_Detail, int, Student>> selectProjection = (sd, i) => sd.Student;
      var selectClause = new SelectClause (previousClause, selectProjection);

      _friendsFetchRequest.CreateFetchSourceExpression (selectClause);
    }

    [Test]
    [ExpectedException (typeof (ArgumentException), ExpectedMessage = "The given SelectClause contains an invalid projection expression "
        + "'() => null'. Expected one parameter, but found 0.\r\nParameter name: selectClauseToFetchFrom")]
    public void GetFetchSourceExpression_InvalidSelectProjection_WrongParameterCount_TooFew ()
    {
      var previousClause = ExpressionHelper.CreateClause ();
      Expression<Func<Student>> selectProjection = () => null;
      var selectClause = new SelectClause (previousClause, selectProjection);

      _friendsFetchRequest.CreateFetchSourceExpression (selectClause);
    }

    [Test]
    [ExpectedException (typeof (ArgumentException), ExpectedMessage = "The given SelectClause contains an invalid projection expression 'sd => sd'. "
        + "In order to fetch the relation property 'Friends', the projection must yield objects of type 'Remotion.Data.UnitTests.Linq.Student', but "
        + "it yields 'Remotion.Data.UnitTests.Linq.Student_Detail'.\r\nParameter name: selectClauseToFetchFrom")]
    public void GetFetchSourceExpression_InvalidSelectProjection_WrongInputType ()
    {
      var previousClause = ExpressionHelper.CreateClause ();
      Expression<Func<Student_Detail, Student_Detail>> selectProjection = sd => sd;
      var selectClause = new SelectClause (previousClause, selectProjection);

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
      // from sd in ExpressionHelper.CreateQuerySource_Detail()
      // (same as in original query model)

      ExpressionTreeComparer.CheckAreEqualTrees (fetchQueryModel.MainFromClause.QuerySource, _studentFromStudentDetailQueryModel.MainFromClause.QuerySource);
      Assert.That (fetchQueryModel.MainFromClause.Identifier, Is.EqualTo (_studentFromStudentDetailQueryModel.MainFromClause.Identifier));
    }

    [Test]
    public void CreateFetchQueryModel_SelectClause ()
    {
      var fetchQueryModel = _friendsFetchRequest.CreateFetchQueryModel (_studentFromStudentDetailQueryModel);
      Assert.That (((SelectClause) fetchQueryModel.SelectOrGroupClause).ProjectionExpression, Is.SameAs (_friendsFetchRequest.FakeSelectProjection));
      Assert.That (fetchQueryModel.SelectOrGroupClause.PreviousClause, Is.SameAs (fetchQueryModel.MainFromClause));
    }

    [Test]
    public void CreateFetchQueryModel_SelectClause_WithBodyClause ()
    {
      _friendsFetchRequest.FakeBodyClauseToAdd = ExpressionHelper.CreateMemberFromClause ();
      var fetchQueryModel = _friendsFetchRequest.CreateFetchQueryModel (_studentFromStudentDetailQueryModel);
      Assert.That (fetchQueryModel.SelectOrGroupClause.PreviousClause, Is.SameAs (_friendsFetchRequest.FakeBodyClauseToAdd));
    }

    [Test]
    public void CreateFetchQueryModel_ResultModifierClausesAreCloned ()
    {
      var selectClause = (SelectClause) _studentFromStudentDetailQueryModel.SelectOrGroupClause;
      var modifier = ExpressionHelper.CreateResultModifierClause (selectClause, selectClause);
      selectClause.AddResultModifierData (modifier);

      var fetchQueryModel = _friendsFetchRequest.CreateFetchQueryModel (_studentFromStudentDetailQueryModel);
      var fetchSelectClause = (SelectClause) fetchQueryModel.SelectOrGroupClause;

      Assert.That (fetchSelectClause.ResultModifierClauses.Count, Is.EqualTo (1));
      Assert.That (fetchSelectClause.ResultModifierClauses[0].ResultModifier, Is.SameAs (modifier.ResultModifier));
    }

    [Test]
    public void CreateFetchQueryModel_ResultModifierClausesAreClonedWithCorrectPreviousClauses ()
    {
      var selectClause = (SelectClause) _studentFromStudentDetailQueryModel.SelectOrGroupClause;
      var modifier1 = ExpressionHelper.CreateResultModifierClause (selectClause, selectClause);
      var modifier2 = ExpressionHelper.CreateResultModifierClause (modifier1, selectClause);
      selectClause.AddResultModifierData (modifier1);
      selectClause.AddResultModifierData (modifier2);

      var fetchQueryModel = _friendsFetchRequest.CreateFetchQueryModel (_studentFromStudentDetailQueryModel);
      var fetchSelectClause = (SelectClause) fetchQueryModel.SelectOrGroupClause;

      Assert.That (fetchSelectClause.ResultModifierClauses.Count, Is.EqualTo (2));
      Assert.That (fetchSelectClause.ResultModifierClauses[0].SelectClause, Is.SameAs (fetchSelectClause));
      Assert.That (fetchSelectClause.ResultModifierClauses[0].PreviousClause, Is.SameAs (fetchSelectClause));
      Assert.That (fetchSelectClause.ResultModifierClauses[1].SelectClause, Is.SameAs (fetchSelectClause));
      Assert.That (fetchSelectClause.ResultModifierClauses[1].PreviousClause, Is.SameAs (fetchSelectClause.ResultModifierClauses[0]));
    }

    [Test]
    [ExpectedException (typeof (NotSupportedException), 
        ExpectedMessage = "Fetch requests only support queries with select clauses, but this query has a GroupClause.")]
    public void CreateFetchQueryModel_GroupClauseNotSupported ()
    {
      var originalQueryModel = 
          new QueryModel (typeof (IQueryable<Student>), ExpressionHelper.CreateMainFromClause (), ExpressionHelper.CreateGroupClause ());

      _friendsFetchRequest.CreateFetchQueryModel (originalQueryModel);
    }
  }
}