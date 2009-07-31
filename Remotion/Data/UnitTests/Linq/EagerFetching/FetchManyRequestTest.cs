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
    private MemberInfo _friendsMember;

    private FetchManyRequest _friendsFetchRequest;
    private IQueryable<Student> _studentFromStudentDetailQuery;
    private QueryModel _studentFromStudentDetailQueryModel;

    [SetUp]
    public void SetUp ()
    {
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
    public void ModifyFetchQueryModel ()
    {
      _friendsFetchRequest.ModifyFetchQueryModel (_studentFromStudentDetailQueryModel);

      // expecting:
      // from sd in ExpressionHelper.CreateStudentDetailQueryable()
      // from <x> in sd.Student.Friends
      // select <x>

      Assert.That (_studentFromStudentDetailQueryModel.BodyClauses.Count, Is.EqualTo (1));
      var memberFromClause = (AdditionalFromClause) _studentFromStudentDetailQueryModel.BodyClauses[0];
      var expectedFromExpression =
          ExpressionHelper.Resolve<Student_Detail, IEnumerable<Student>> (_studentFromStudentDetailQueryModel.MainFromClause, sd => sd.Student.Friends);
      ExpressionTreeComparer.CheckAreEqualTrees (memberFromClause.FromExpression, expectedFromExpression);

      var selectClause = _studentFromStudentDetailQueryModel.SelectClause;
      Assert.That (((QuerySourceReferenceExpression) selectClause.Selector).ReferencedQuerySource, Is.SameAs (memberFromClause));
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