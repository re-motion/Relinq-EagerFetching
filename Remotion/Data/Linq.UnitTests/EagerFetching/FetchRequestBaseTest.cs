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
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Remotion.Data.Linq.Clauses.Expressions;
using Remotion.Data.Linq.Clauses.ResultOperators;
using Remotion.Data.Linq.Clauses.StreamedData;
using Remotion.Data.Linq.EagerFetching;
using Remotion.Data.Linq.UnitTests.TestDomain;
using Remotion.Data.Linq.UnitTests.TestUtilities;
using Rhino.Mocks;

namespace Remotion.Data.Linq.UnitTests.EagerFetching
{
  [TestFixture]
  public class FetchRequestBaseTest
  {
    private MemberInfo _scoresMember;
    private MemberInfo _friendsMember;

    private TestFetchRequest _friendsFetchRequest;
    private IQueryable<Chef> _studentFromStudentDetailQuery;
    private QueryModel _studentFromStudentDetailQueryModel;

    [SetUp]
    public void SetUp ()
    {
      _scoresMember = typeof (Chef).GetProperty ("Scores");
      _friendsMember = typeof (Chef).GetProperty ("Friends");
      _friendsFetchRequest = new TestFetchRequest (_friendsMember);
      _studentFromStudentDetailQuery = (from sd in ExpressionHelper.CreateStudentDetailQueryable ()
                                        select sd.Chef).Take (1);
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
      Assert.That (_friendsFetchRequest.RelationMember, Is.EqualTo (typeof (Chef).GetProperty ("Friends")));
    }

    [Test]
    public void CreateFetchQueryModel ()
    {
      // expected: from <x> in (from sd in ExpressionHelper.CreateStudentDetailQueryable() select sd.Chef).Take (1)
      //           select <x>

      var fetchRequestPartialMock = new MockRepository ().PartialMock<FetchRequestBase> (_friendsMember);
      
      QueryModel modifiedQueryModel = null;
      fetchRequestPartialMock
          .Expect (mock => PrivateInvoke.InvokeNonPublicMethod (mock, "ModifyFetchQueryModel", Arg<QueryModel>.Is.Anything))
          .WhenCalled (mi => modifiedQueryModel = (QueryModel) mi.Arguments[0]);

      fetchRequestPartialMock.Replay ();

      var fetchQueryModel = fetchRequestPartialMock.CreateFetchQueryModel (_studentFromStudentDetailQueryModel);

      fetchRequestPartialMock.VerifyAllExpectations ();
      Assert.That (modifiedQueryModel, Is.SameAs (fetchQueryModel));

      Assert.That (fetchQueryModel.MainFromClause.FromExpression, Is.InstanceOfType (typeof (SubQueryExpression)));

      var subQueryExpression = (SubQueryExpression) fetchQueryModel.MainFromClause.FromExpression;
      Assert.That (subQueryExpression.QueryModel, Is.SameAs (_studentFromStudentDetailQueryModel));

      Assert.That (_studentFromStudentDetailQueryModel.BodyClauses.Count, Is.EqualTo (0));
      Assert.That (((QuerySourceReferenceExpression) fetchQueryModel.SelectClause.Selector).ReferencedQuerySource, 
          Is.SameAs (fetchQueryModel.MainFromClause));
    }

    [Test]
    [ExpectedException (typeof (ArgumentException), ExpectedMessage = "The given source query model selects does not select a sequence, it selects a "
        + "single object of type 'System.Int32'. In order to fetch the relation member 'Friends', the query must yield a sequence of objects of type "
        + "'Remotion.Data.Linq.UnitTests.TestDomain.Chef'.\r\nParameter name: sourceItemQueryModel")]
    public void CreateFetchQueryModel_NonSequenceQueryModel ()
    {
      var invalidQueryModel = ExpressionHelper.CreateQueryModel_Student ();
      invalidQueryModel.ResultOperators.Add (new CountResultOperator ());
      _friendsFetchRequest.CreateFetchQueryModel (invalidQueryModel);
    }

    [Test]
    [ExpectedException (typeof (ArgumentException), ExpectedMessage = "The given source query model selects items that do not match the fetch "
        + "request. In order to fetch the relation member 'Friends', the query must yield objects of type "
        + "'Remotion.Data.Linq.UnitTests.TestDomain.Chef', but it yields 'Remotion.Data.Linq.UnitTests.TestDomain.Student_Detail'.\r\n"
        + "Parameter name: sourceItemQueryModel")]
    public void CreateFetchQueryModel_InvalidItems ()
    {
      var invalidQueryModel = ExpressionHelper.CreateQueryModel (ExpressionHelper.CreateMainFromClause_Detail());
      _friendsFetchRequest.CreateFetchQueryModel (invalidQueryModel);
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
