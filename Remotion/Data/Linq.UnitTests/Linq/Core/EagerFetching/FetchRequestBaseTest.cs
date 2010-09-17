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
using Remotion.Data.Linq.UnitTests.Linq.Core.TestDomain;
using Remotion.Data.Linq.UnitTests.Linq.Core.TestUtilities;
using Rhino.Mocks;

namespace Remotion.Data.Linq.UnitTests.Linq.Core.EagerFetching
{
  [TestFixture]
  public class FetchRequestBaseTest
  {
    private MemberInfo _holidaysMember;
    private MemberInfo _assistantsMember;

    private TestFetchRequest _assistantsFetchRequest;
    private IQueryable<Cook> _cookFromKitchenQuery;
    private QueryModel _cookFromKitchenQueryModel;

    [SetUp]
    public void SetUp ()
    {
      _holidaysMember = typeof (Cook).GetProperty ("Holidays");
      _assistantsMember = typeof (Cook).GetProperty ("Assistants");
      _assistantsFetchRequest = new TestFetchRequest (_assistantsMember);
      _cookFromKitchenQuery = (from sd in ExpressionHelper.CreateKitchenQueryable ()
                                        select sd.Cook).Take (1);
      _cookFromKitchenQueryModel = ExpressionHelper.ParseQuery (_cookFromKitchenQuery);
    }

    [Test]
    public void GetOrAddInnerFetchRequest ()
    {
      Assert.That (_assistantsFetchRequest.InnerFetchRequests, Is.Empty);

      var result = _assistantsFetchRequest.GetOrAddInnerFetchRequest (new FetchManyRequest (_holidaysMember));

      Assert.That (result.RelationMember, Is.SameAs (_holidaysMember));
      Assert.That (_assistantsFetchRequest.InnerFetchRequests, Is.EqualTo (new[] { result }));
    }

    [Test]
    public void RelationMember ()
    {
      Assert.That (_assistantsFetchRequest.RelationMember, Is.EqualTo (typeof (Cook).GetProperty ("Assistants")));
    }

    [Test]
    public void CreateFetchQueryModel ()
    {
      // expected: from <x> in (from sd in ExpressionHelper.CreateKitchenQueryable() select sd.Cook).Take (1)
      //           select <x>

      var fetchRequestPartialMock = new MockRepository ().PartialMock<FetchRequestBase> (_assistantsMember);
      
      QueryModel modifiedQueryModel = null;
      fetchRequestPartialMock
          .Expect (mock => PrivateInvoke.InvokeNonPublicMethod (mock, "ModifyFetchQueryModel", Arg<QueryModel>.Is.Anything))
          .WhenCalled (mi => modifiedQueryModel = (QueryModel) mi.Arguments[0]);

      fetchRequestPartialMock.Replay ();

      var fetchQueryModel = fetchRequestPartialMock.CreateFetchQueryModel (_cookFromKitchenQueryModel);

      fetchRequestPartialMock.VerifyAllExpectations ();
      Assert.That (modifiedQueryModel, Is.SameAs (fetchQueryModel));

      Assert.That (fetchQueryModel.MainFromClause.FromExpression, Is.InstanceOfType (typeof (SubQueryExpression)));

      var subQueryExpression = (SubQueryExpression) fetchQueryModel.MainFromClause.FromExpression;
      Assert.That (subQueryExpression.QueryModel, Is.SameAs (_cookFromKitchenQueryModel));

      Assert.That (_cookFromKitchenQueryModel.BodyClauses.Count, Is.EqualTo (0));
      Assert.That (((QuerySourceReferenceExpression) fetchQueryModel.SelectClause.Selector).ReferencedQuerySource, 
          Is.SameAs (fetchQueryModel.MainFromClause));

      Assert.That (fetchQueryModel.ResultTypeOverride, Is.Null);
    }

    [Test]
    [ExpectedException (typeof (ArgumentException), ExpectedMessage = "The given source query model selects does not select a sequence, it selects a "
        + "single object of type 'System.Int32'. In order to fetch the relation member 'Assistants', the query must yield a sequence of objects of type "
        + "'Remotion.Data.Linq.UnitTests.Linq.Core.TestDomain.Cook'.\r\nParameter name: sourceItemQueryModel")]
    public void CreateFetchQueryModel_NonSequenceQueryModel ()
    {
      var invalidQueryModel = ExpressionHelper.CreateQueryModel_Cook ();
      invalidQueryModel.ResultOperators.Add (new CountResultOperator ());
      _assistantsFetchRequest.CreateFetchQueryModel (invalidQueryModel);
    }

    [Test]
    [ExpectedException (typeof (ArgumentException), ExpectedMessage = "The given source query model selects items that do not match the fetch "
        + "request. In order to fetch the relation member 'Assistants', the query must yield objects of type "
        + "'Remotion.Data.Linq.UnitTests.Linq.Core.TestDomain.Cook', but it yields 'Remotion.Data.Linq.UnitTests.Linq.Core.TestDomain.Kitchen'.\r\n"
        + "Parameter name: sourceItemQueryModel")]
    public void CreateFetchQueryModel_InvalidItems ()
    {
      var invalidQueryModel = ExpressionHelper.CreateQueryModel (ExpressionHelper.CreateMainFromClause_Kitchen());
      _assistantsFetchRequest.CreateFetchQueryModel (invalidQueryModel);
    }

    [Test]
    public void ExecuteInMemory ()
    {
      var input = new StreamedSequence (new[] { 1, 2, 3 }, new StreamedSequenceInfo (typeof (int[]), Expression.Constant (0)));
      var result = _assistantsFetchRequest.ExecuteInMemory (input);

      Assert.That (result, Is.SameAs (input));
    }
  }
}
