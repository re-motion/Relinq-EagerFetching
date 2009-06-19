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
using Remotion.Data.Linq.Clauses.Expressions;
using Remotion.Data.Linq.EagerFetching;
using Remotion.Data.UnitTests.Linq.Parsing;

namespace Remotion.Data.UnitTests.Linq.EagerFetching
{
  [TestFixture]
  public class FetchManyRequestTest
  {
    private Expression<Func<Student, IEnumerable<int>>> _scoresFetchExpression;
    private Expression<Func<Student, IEnumerable<Student>>> _friendsFetchExpression;
    private FetchManyRequest _friendsFetchRequest;
    private IQueryable<Student> _studentFromStudentDetailQuery;
    private QueryModel _studentFromStudentDetailQueryModel;

    [SetUp]
    public void SetUp ()
    {
      _scoresFetchExpression = (s => s.Scores);
      _friendsFetchExpression = (s => s.Friends);
      _friendsFetchRequest = new FetchManyRequest (_friendsFetchExpression);

      _studentFromStudentDetailQuery = (from sd in ExpressionHelper.CreateQuerySource_Detail ()
                                        select sd.Student);
      _studentFromStudentDetailQueryModel = ExpressionHelper.ParseQuery (_studentFromStudentDetailQuery);
    }

    [Test]
    [ExpectedException (typeof (ArgumentException), ExpectedMessage = "A fetch many request must yield a list of related objects, but 's => s.ID' "
        + "yields 'System.Int32', which is not enumerable.\r\nParameter name: relatedObjectSelector")]
    public void Create_InvalidExpression_NoEnumerableOfT ()
    {
      new FetchManyRequest ((Expression<Func<Student, int>>) (s => s.ID));
    }

    [Test]
    public void CreateFetchFromClause ()
    {
      // simulate a fetch request for the following: var query = from ... select sd.Student; query.FetchMany (s => s.Friends);

      var previousClause = ExpressionHelper.CreateClause();
      var selectProjection = (MemberExpression) ExpressionHelper.MakeExpression<Student_Detail, Student> (sd => sd.Student);
      var selectClause = new SelectClause (previousClause, selectProjection);

      var clause = _friendsFetchRequest.CreateFetchFromClause (selectClause, "studi");
      Assert.That (clause, Is.Not.Null);
      Assert.That (clause.PreviousClause, Is.SameAs (previousClause));
    }

    [Test]
    public void CreateFetchFromClause_FromExpression ()
    {
      // simulate a fetch request for the following: var query = from ... select sd.Student; query.FetchMany (s => s.Friends);

      var previousClause = ExpressionHelper.CreateClause ();
      var selectProjection = (MemberExpression) ExpressionHelper.MakeExpression<Student_Detail, Student> (sd => sd.Student);
      var selectClause = new SelectClause (previousClause, selectProjection);

      var clause = _friendsFetchRequest.CreateFetchFromClause (selectClause, "studi");

      // expecting: from studi in sd.Student.Friends
      //            fromExpression: sd => sd.Student.Friends

      var memberExpression = (MemberExpression) clause.FromExpression;
      Assert.That (memberExpression.Member, Is.EqualTo (typeof (Student).GetProperty ("Friends")));

      var innerMemberExpression = (MemberExpression) memberExpression.Expression;
      Assert.That (innerMemberExpression.Member, Is.EqualTo (typeof (Student_Detail).GetProperty ("Student")));

      var innermostParameterExpression = (ParameterExpression) innerMemberExpression.Expression;
      Assert.That (innermostParameterExpression.Name, Is.EqualTo ("sd"));
    }

    [Test]
    public void CreateFetchQueryModel ()
    {
      var fetchQueryModel = _friendsFetchRequest.CreateFetchQueryModel (_studentFromStudentDetailQueryModel);
      Assert.That (fetchQueryModel, Is.Not.Null);
      Assert.That (fetchQueryModel, Is.Not.SameAs (_studentFromStudentDetailQueryModel));
    }

    [Test]
    public void CreateFetchQueryModel_MemberFromClause ()
    {
      var fetchQueryModel = _friendsFetchRequest.CreateFetchQueryModel (_studentFromStudentDetailQueryModel);

      // expecting:
      // from sd in ExpressionHelper.CreateQuerySource_Detail()
      // from <x> in sd.Student.Friends
      // select <x>

      Assert.That (fetchQueryModel.BodyClauses.Count, Is.EqualTo (1));
      var memberFromClause = (MemberFromClause) fetchQueryModel.BodyClauses.Single ();

      var expectedFromExpression = 
          ExpressionHelper.Resolve<Student_Detail, IEnumerable<Student>> (fetchQueryModel.MainFromClause, sd => sd.Student.Friends);
      
      ExpressionTreeComparer.CheckAreEqualTrees (memberFromClause.FromExpression, expectedFromExpression);
    }

    [Test]
    public void CreateFetchQueryModel_MemberFromClause_PreviousClauseIsClauseInNewQueryModel ()
    {
      var fetchQueryModel = _friendsFetchRequest.CreateFetchQueryModel (_studentFromStudentDetailQueryModel);

      Assert.That (fetchQueryModel.BodyClauses.Count, Is.EqualTo (1));
      var memberFromClause = (MemberFromClause) fetchQueryModel.BodyClauses.Single ();

      Assert.That (memberFromClause.PreviousClause, Is.SameAs (fetchQueryModel.MainFromClause));
    }

    [Test]
    public void CreateFetchQueryModel_SelectClause ()
    {
      var fetchQueryModel = _friendsFetchRequest.CreateFetchQueryModel (_studentFromStudentDetailQueryModel);

      // expecting:
      // from sd in ExpressionHelper.CreateQuerySource_Detail()
      // from <x> in sd.Student.Friends
      // select <x>

      var selectClause = (SelectClause) fetchQueryModel.SelectOrGroupClause;
      var memberFromClause = (MemberFromClause) fetchQueryModel.BodyClauses.Single ();
      Assert.That (((QuerySourceReferenceExpression) selectClause.Selector).ReferencedClause, Is.SameAs (memberFromClause));
      Assert.That (selectClause.PreviousClause, Is.SameAs (memberFromClause));
    }

    [Test]
    public void CreateFetchQueryModel_Twice_MemberFromClause ()
    {
      var fetchQueryModel = _friendsFetchRequest.CreateFetchQueryModel (_studentFromStudentDetailQueryModel);

      var fetchRequest2 = new FetchManyRequest (_scoresFetchExpression);
      var fetchQueryModel2 = fetchRequest2.CreateFetchQueryModel (fetchQueryModel);

      // expecting:
      // from sd in ExpressionHelper.CreateQuerySource_Detail()
      // from <x> in sd.Student.Friends
      // from <y> in <x>.Scores
      // select <y>

      Assert.That (fetchQueryModel2.BodyClauses.Count, Is.EqualTo (2));
      var memberFromClause1 = (MemberFromClause) fetchQueryModel2.BodyClauses.First ();
      var memberFromClause2 = (MemberFromClause) fetchQueryModel2.BodyClauses.Last ();

      Assert.That (memberFromClause1.Identifier.Name, Is.Not.EqualTo (memberFromClause2.Identifier.Name));

      Assert.That (((QuerySourceReferenceExpression) memberFromClause2.MemberExpression.Expression).ReferencedClause, Is.SameAs (memberFromClause1));
      Assert.That (memberFromClause2.MemberExpression.Member, Is.EqualTo (typeof (Student).GetProperty ("Scores")));
    }

    [Test]
    public void CreateFetchQueryModel_Twice_SelectClause ()
    {
      var fetchQueryModel = _friendsFetchRequest.CreateFetchQueryModel (_studentFromStudentDetailQueryModel);

      var fetchRequest2 = new FetchManyRequest (_scoresFetchExpression);
      var fetchQueryModel2 = fetchRequest2.CreateFetchQueryModel (fetchQueryModel);

      // expecting:
      // from sd in ExpressionHelper.CreateQuerySource_Detail()
      // from <x> in sd.Student.Friends
      // from <y> in <x>.Scores
      // select <y>

      var memberFromClause2 = fetchQueryModel2.BodyClauses.Last();
      var selectClause = (SelectClause) fetchQueryModel2.SelectOrGroupClause;
      Assert.That (((QuerySourceReferenceExpression) selectClause.Selector).ReferencedClause, Is.SameAs (memberFromClause2));
    }
  }
}