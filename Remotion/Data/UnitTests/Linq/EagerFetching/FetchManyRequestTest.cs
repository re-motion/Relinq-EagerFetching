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
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Remotion.Data.Linq;
using Remotion.Data.Linq.Clauses;
using Remotion.Data.Linq.Clauses.Expressions;
using Remotion.Data.Linq.EagerFetching;
using Remotion.Data.UnitTests.Linq.Parsing;
using Remotion.Data.UnitTests.Linq.TestDomain;
using Remotion.Utilities;

namespace Remotion.Data.UnitTests.Linq.EagerFetching
{
  [TestFixture]
  public class FetchManyRequestTest
  {
    private MemberInfo _scoresMember;
    private MemberInfo _friendsMember;

    private FetchManyRequest _friendsFetchRequest;
    private IQueryable<Student> _studentFromStudentDetailQuery;
    private QueryModel _studentFromStudentDetailQueryModel;

    [SetUp]
    public void SetUp ()
    {
      _scoresMember = typeof (Student).GetProperty ("Scores");
      _friendsMember = typeof (Student).GetProperty ("Friends");
      _friendsFetchRequest = new FetchManyRequest (_friendsMember);

      _studentFromStudentDetailQuery = (from sd in ExpressionHelper.CreateStudentDetailQueryable ()
                                        select sd.Student);
      _studentFromStudentDetailQueryModel = ExpressionHelper.ParseQuery (_studentFromStudentDetailQuery);
    }

    [Test]
    [ExpectedException (typeof (ArgumentTypeException))]
    public void Create_InvalidExpression_NoEnumerableOfT ()
    {
      var idMember = typeof (Student).GetProperty ("ID");
      new FetchManyRequest (idMember);
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
      // from sd in ExpressionHelper.CreateStudentDetailQueryable()
      // from <x> in sd.Student.Friends
      // select <x>

      Assert.That (fetchQueryModel.BodyClauses.Count, Is.EqualTo (1));
      var memberFromClause = (AdditionalFromClause) fetchQueryModel.BodyClauses.Single ();

      var expectedFromExpression = 
          ExpressionHelper.Resolve<Student_Detail, IEnumerable<Student>> (fetchQueryModel.MainFromClause, sd => sd.Student.Friends);
      
      ExpressionTreeComparer.CheckAreEqualTrees (memberFromClause.FromExpression, expectedFromExpression);
    }

    [Test]
    public void CreateFetchQueryModel_SelectClause ()
    {
      var fetchQueryModel = _friendsFetchRequest.CreateFetchQueryModel (_studentFromStudentDetailQueryModel);

      // expecting:
      // from sd in ExpressionHelper.CreateStudentDetailQueryable()
      // from <x> in sd.Student.Friends
      // select <x>

      var selectClause = fetchQueryModel.SelectClause;
      var memberFromClause = (AdditionalFromClause) fetchQueryModel.BodyClauses.Single ();
      Assert.That (((QuerySourceReferenceExpression) selectClause.Selector).ReferencedQuerySource, Is.SameAs (memberFromClause));
    }

    [Test]
    public void CreateFetchQueryModel_Twice_MemberFromClause ()
    {
      var fetchQueryModel = _friendsFetchRequest.CreateFetchQueryModel (_studentFromStudentDetailQueryModel);

      var fetchRequest2 = new FetchManyRequest (_scoresMember);
      var fetchQueryModel2 = fetchRequest2.CreateFetchQueryModel (fetchQueryModel);

      // expecting:
      // from sd in ExpressionHelper.CreateStudentDetailQueryable()
      // from <x> in sd.Student.Friends
      // from <y> in <x>.Scores
      // select <y>

      Assert.That (fetchQueryModel2.BodyClauses.Count, Is.EqualTo (2));
      var memberFromClause1 = (AdditionalFromClause) fetchQueryModel2.BodyClauses[0];
      var memberFromClause2 = (AdditionalFromClause) fetchQueryModel2.BodyClauses[1];

      Assert.That (memberFromClause1.ItemName, Is.Not.EqualTo (memberFromClause2.ItemName));

      var memberFromExpression = (MemberExpression) memberFromClause2.FromExpression;
      Assert.That (((QuerySourceReferenceExpression) memberFromExpression.Expression).ReferencedQuerySource, Is.SameAs (memberFromClause1));
      Assert.That (memberFromExpression.Member, Is.EqualTo (typeof (Student).GetProperty ("Scores")));
    }

    [Test]
    public void CreateFetchQueryModel_Twice_SelectClause ()
    {
      var fetchQueryModel = _friendsFetchRequest.CreateFetchQueryModel (_studentFromStudentDetailQueryModel);

      var fetchRequest2 = new FetchManyRequest (_scoresMember);
      var fetchQueryModel2 = fetchRequest2.CreateFetchQueryModel (fetchQueryModel);

      // expecting:
      // from sd in ExpressionHelper.CreateStudentDetailQueryable()
      // from <x> in sd.Student.Friends
      // from <y> in <x>.Scores
      // select <y>

      var memberFromClause2 = fetchQueryModel2.BodyClauses.Last();
      var selectClause = fetchQueryModel2.SelectClause;
      Assert.That (((QuerySourceReferenceExpression) selectClause.Selector).ReferencedQuerySource, Is.SameAs (memberFromClause2));
    }

    [Test]
    public void Clone ()
    {
      var clone = _friendsFetchRequest.Clone (new CloneContext (new QuerySourceMapping ()));

      Assert.That (clone, Is.Not.SameAs (_friendsFetchRequest));
      Assert.That (clone, Is.InstanceOfType (typeof (FetchManyRequest)));
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
      Assert.That (innerClones[0], Is.InstanceOfType (typeof (FetchManyRequest)));
      Assert.That (innerClones[0].RelationMember, Is.SameAs (innerRequest.RelationMember));
    }
  }
}