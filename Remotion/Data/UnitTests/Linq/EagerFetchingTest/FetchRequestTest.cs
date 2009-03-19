// This file is part of the re-motion Core Framework (www.re-motion.org)
// Copyright (C) 2005-2008 rubicon informationstechnologie gmbh, www.rubicon.eu
// 
// The re-motion Core Framework is free software; you can redistribute it 
// and/or modify it under the terms of the GNU Lesser General Public License 
// version 3.0 as published by the Free Software Foundation.
// 
// This framework is distributed in the hope that it will be useful, 
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with this framework; if not, see http://www.gnu.org/licenses.
// 
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Remotion.Data.Linq.Clauses;
using Remotion.Data.Linq.EagerFetching;

namespace Remotion.Data.UnitTests.Linq.EagerFetchingTest
{
  [TestFixture]
  public class FetchRequestTest
  {
    [Test]
    [ExpectedException (typeof (ArgumentException), ExpectedMessage = "A fetch request must be a simple member access expression; 'new [] {1, 2, 3}' "
        + "is a NewArrayExpression instead.\r\nParameter name: relatedObjectSelector")]
    public void Create_InvalidExpression ()
    {
      FetchRequest<int>.Create<Student> (s => new[] { 1, 2, 3 });
    }

    [Test]
    [ExpectedException (typeof (ArgumentException), ExpectedMessage = "A fetch request must be a simple member access expression of the kind "
        + "o => o.Related; 's.OtherStudent.Friends' is too complex.\r\nParameter name: relatedObjectSelector")]
    public void Create_InvalidExpression_MoreThanOneMember ()
    {
      FetchRequest<Student>.Create<Student> (s => s.OtherStudent.Friends);
    }

    [Test]
    public void GetOrAddFetchRequest ()
    {
      FetchRequest<Student> fetchRequest = FetchRequest<Student>.Create<Student> (s => s.Friends);

      Assert.That (fetchRequest.InnerFetchRequests, Is.Empty);

      Expression<Func<Student, IEnumerable<int>>> expectedExpression = s => s.Scores;
      var result = fetchRequest.GetOrAddInnerFetchRequest (expectedExpression);

      Assert.That (result.RelatedObjectSelector, Is.SameAs (expectedExpression));
      Assert.That (fetchRequest.InnerFetchRequests, Is.EqualTo (new[] { result }));
    }

    [Test]
    public void RelationMember ()
    {
      FetchRequest<Student> fetchRequest = FetchRequest<Student>.Create<Student> (s => s.Friends);
      Assert.That (fetchRequest.RelationMember, Is.EqualTo (typeof (Student).GetProperty ("Friends")));
    }

    [Test]
    public void CreateFromClause ()
    {
      // simulate a fetch request for the following: var query = from ... select sd.Student; query.Fetch (s => s.Friends);

      FetchRequest<Student> fetchRequest = FetchRequest<Student>.Create<Student> (s => s.Friends);
      
      var previousClause = ExpressionHelper.CreateClause();
      Expression<Func<Student_Detail, Student>> selectProjection = sd => sd.Student;
      var selectClause = new SelectClause (previousClause, selectProjection);

      var clause = fetchRequest.CreateFromClause (selectClause, "studi");
      Assert.That (clause, Is.Not.Null);
      Assert.That (clause.PreviousClause, Is.SameAs (previousClause));
    }

    [Test]
    [ExpectedException (typeof (ArgumentException), ExpectedMessage = "The given SelectClause contains an invalid projection expression " 
        + "'(sd, i) => sd.Student'. Expected one parameter, but found 2.\r\nParameter name: selectClauseToFetchFrom")]
    public void CreateFromClause_InvalidSelectProjection_WrongParameterCount ()
    {
      FetchRequest<Student> fetchRequest = FetchRequest<Student>.Create<Student> (s => s.Friends);
      var previousClause = ExpressionHelper.CreateClause ();
      Expression<Func<Student_Detail, int, Student>> selectProjection = (sd, i) => sd.Student;
      var selectClause = new SelectClause (previousClause, selectProjection);

      fetchRequest.CreateFromClause (selectClause, "studi");
    }

    [Test]
    [ExpectedException (typeof (ArgumentException), ExpectedMessage = "The given SelectClause contains an invalid projection expression 'sd => sd'. "
        + "In order to fetch the relation property 'Friends', the projection must yield objects of type 'Remotion.Data.UnitTests.Linq.Student', but "
        + "it yields 'Remotion.Data.UnitTests.Linq.Student_Detail'.\r\nParameter name: selectClauseToFetchFrom")]
    public void CreateFromClause_InvalidSelectProjection_WrongInputType ()
    {
      FetchRequest<Student> fetchRequest = FetchRequest<Student>.Create<Student> (s => s.Friends);
      var previousClause = ExpressionHelper.CreateClause ();
      Expression<Func<Student_Detail, Student_Detail>> selectProjection = sd => sd;
      var selectClause = new SelectClause (previousClause, selectProjection);

      fetchRequest.CreateFromClause (selectClause, "studi");
    }

    [Test]
    public void CreateFromClause_FromExpression ()
    {
      // simulate a fetch request for the following: var query = from ... select sd.Student; query.Fetch (s => s.Friends);

      FetchRequest<Student> fetchRequest = FetchRequest<Student>.Create<Student> (s => s.Friends);

      var previousClause = ExpressionHelper.CreateClause ();
      Expression<Func<Student_Detail, Student>> selectProjection = sd => sd.Student;
      var selectClause = new SelectClause (previousClause, selectProjection);

      var clause = fetchRequest.CreateFromClause (selectClause, "studi");

      // expecting: from studi in sd.Student.Friends
      //            fromExpression: sd => sd.Student.Friends

      Assert.That (clause.FromExpression.Parameters.Count, Is.EqualTo (1));
      Assert.That (clause.FromExpression.Parameters[0], Is.SameAs (selectProjection.Parameters[0]));

      var memberExpression = clause.FromExpression.Body as MemberExpression;
      Assert.That (memberExpression, Is.Not.Null);
      Assert.That (memberExpression.Member, Is.EqualTo (typeof (Student).GetProperty ("Friends")));

      var innerMemberExpression = memberExpression.Expression as MemberExpression;
      Assert.That (innerMemberExpression, Is.Not.Null);
      Assert.That (innerMemberExpression.Member, Is.EqualTo (typeof (Student_Detail).GetProperty ("Student")));

      var innermostParameterExpression = innerMemberExpression.Expression as ParameterExpression;
      Assert.That (innermostParameterExpression, Is.Not.Null);
      Assert.That (innermostParameterExpression, Is.SameAs (clause.FromExpression.Parameters[0]));
    }

    [Test]
    public void CreateFromClause_ProjectionExpression ()
    {
      // simulate a fetch request for the following: var query = from ... select sd.Student; query.Fetch (s => s.Friends);

      FetchRequest<Student> fetchRequest = FetchRequest<Student>.Create<Student> (s => s.Friends);

      var previousClause = ExpressionHelper.CreateClause ();
      Expression<Func<Student_Detail, Student>> selectProjection = sd => sd.Student;
      var selectClause = new SelectClause (previousClause, selectProjection);

      var clause = fetchRequest.CreateFromClause (selectClause, "studi");

      // expecting: from studi in sd.Student.Friends
      //            projectionExpression: (sd, studi) => studi

      Assert.That (clause.ProjectionExpression.Parameters.Count, Is.EqualTo (2));
      Assert.That (clause.ProjectionExpression.Parameters[0], Is.SameAs (selectProjection.Parameters[0]));
      Assert.That (clause.ProjectionExpression.Parameters[1].Name, Is.EqualTo ("studi"));
      Assert.That (clause.ProjectionExpression.Parameters[1].Type, Is.EqualTo (typeof (Student)));

      Assert.That (clause.ProjectionExpression.Body, Is.SameAs (clause.ProjectionExpression.Parameters[1]));
    }

  }
}