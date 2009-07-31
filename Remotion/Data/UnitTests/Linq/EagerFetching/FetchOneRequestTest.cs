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
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Remotion.Data.Linq;
using Remotion.Data.Linq.Clauses;
using Remotion.Data.Linq.EagerFetching;
using Remotion.Data.UnitTests.Linq.Parsing;
using Remotion.Data.UnitTests.Linq.TestDomain;
using System.Reflection;

namespace Remotion.Data.UnitTests.Linq.EagerFetching
{
  [TestFixture]
  public class FetchOneRequestTest
  {
    private MemberInfo _otherStudentMember;
    private FetchOneRequest _otherStudentFetchRequest;
    private IQueryable<Student> _studentFromStudentDetailQuery;
    private QueryModel _studentFromStudentDetailQueryModel;

    [SetUp]
    public void SetUp ()
    {
      _otherStudentMember = typeof (Student).GetProperty ("OtherStudent");
      _otherStudentFetchRequest = new FetchOneRequest (_otherStudentMember);

      _studentFromStudentDetailQuery = (from sd in ExpressionHelper.CreateStudentDetailQueryable ()
                                        select sd.Student);
      _studentFromStudentDetailQueryModel = ExpressionHelper.ParseQuery (_studentFromStudentDetailQuery);
    }

    [Test]
    public void ModifyFetchQueryModel ()
    {
      _otherStudentFetchRequest.ModifyFetchQueryModel (_studentFromStudentDetailQueryModel);

      Assert.That (_studentFromStudentDetailQueryModel.BodyClauses.Count, Is.EqualTo (0), "no additional from clauses added");

      var selectClause = _studentFromStudentDetailQueryModel.SelectClause;
      var expectedExpression = ExpressionHelper.Resolve<Student_Detail, Student> (
          _studentFromStudentDetailQueryModel.MainFromClause, sd => sd.Student.OtherStudent);
      ExpressionTreeComparer.CheckAreEqualTrees (selectClause.Selector, expectedExpression);
    }

    [Test]
    public void Clone ()
    {
      var clone = _otherStudentFetchRequest.Clone (new CloneContext (new QuerySourceMapping ()));

      Assert.That (clone, Is.Not.SameAs (_otherStudentFetchRequest));
      Assert.That (clone, Is.InstanceOfType (typeof (FetchOneRequest)));
      Assert.That (((FetchOneRequest) clone).RelationMember, Is.SameAs (_otherStudentFetchRequest.RelationMember));
      Assert.That (((FetchOneRequest) clone).InnerFetchRequests.ToArray(), Is.Empty);
    }

    [Test]
    public void Clone_WithInnerFetchRequests ()
    {
      var innerRequest = new FetchOneRequest (_otherStudentMember);
      _otherStudentFetchRequest.GetOrAddInnerFetchRequest (innerRequest);

      var clone = _otherStudentFetchRequest.Clone (new CloneContext (new QuerySourceMapping ()));
      var innerClones = ((FetchOneRequest) clone).InnerFetchRequests.ToArray ();
      Assert.That (innerClones.Length, Is.EqualTo (1));
      Assert.That (innerClones[0], Is.InstanceOfType (typeof (FetchOneRequest)));
      Assert.That (innerClones[0], Is.Not.SameAs (innerRequest));
      Assert.That (innerClones[0].RelationMember, Is.SameAs (innerRequest.RelationMember));
    }
  }
}