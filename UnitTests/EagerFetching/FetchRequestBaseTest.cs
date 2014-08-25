// Copyright (c) rubicon IT GmbH, www.rubicon.eu
//
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership.  rubicon licenses this file to you under 
// the Apache License, Version 2.0 (the "License"); you may not use this 
// file except in compliance with the License.  You may obtain a copy of the 
// License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.  See the 
// License for the specific language governing permissions and limitations
// under the License.
// 
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;
using Remotion.Development.UnitTesting;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ResultOperators;
using Remotion.Linq.Clauses.StreamedData;
using Remotion.Linq.Development.UnitTesting;
using Remotion.Linq.EagerFetching;
using Remotion.Linq.UnitTests.TestDomain;
using Rhino.Mocks;

namespace Remotion.Linq.UnitTests.EagerFetching
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
      _cookFromKitchenQuery = (from sd in ExpressionHelper.CreateQueryable<Kitchen> ()
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

      Assert.That (fetchQueryModel.MainFromClause.FromExpression, Is.InstanceOf (typeof (SubQueryExpression)));

      var subQueryExpression = (SubQueryExpression) fetchQueryModel.MainFromClause.FromExpression;
      Assert.That (subQueryExpression.QueryModel, Is.SameAs (_cookFromKitchenQueryModel));

      Assert.That (_cookFromKitchenQueryModel.BodyClauses.Count, Is.EqualTo (0));
      Assert.That (((QuerySourceReferenceExpression) fetchQueryModel.SelectClause.Selector).ReferencedQuerySource, 
          Is.SameAs (fetchQueryModel.MainFromClause));

      Assert.That (fetchQueryModel.ResultTypeOverride, Is.Null);
    }

    [Test]
    [ExpectedException (typeof (ArgumentException), ExpectedMessage = 
        "The given source query model cannot be used to fetch the relation member 'Assistants': The query must return a sequence of items, but it "
        + "selects a single object of type 'System.Int32'.\r\nParameter name: sourceItemQueryModel")]
    public void CreateFetchQueryModel_NonSequenceQueryModel ()
    {
      var invalidQueryModel = ExpressionHelper.CreateQueryModel<Cook> ();
      invalidQueryModel.ResultOperators.Add (new CountResultOperator ());
      _assistantsFetchRequest.CreateFetchQueryModel (invalidQueryModel);
    }

    [Test]
    public void CreateFetchQueryModel_NonMatchingItems_Works ()
    {
      var invalidQueryModel = ExpressionHelper.CreateQueryModel (ExpressionHelper.CreateMainFromClause<Kitchen>());
      Assert.That (() => _assistantsFetchRequest.CreateFetchQueryModel (invalidQueryModel), Throws.Nothing);
    }

    [Test]
    public void GetFetchedMemberExpression ()
    {
      var cookSource = ExpressionHelper.CreateExpression (typeof (Cook));
      
      var result = _assistantsFetchRequest.GetFetchedMemberExpression (cookSource);

      var expectedExpression = Expression.MakeMemberAccess (cookSource, _assistantsMember);
      ExpressionTreeComparer.CheckAreEqualTrees (expectedExpression, result);
    }

    [Test]
    public void GetFetchedMemberExpression_ConversionNeeded ()
    {
      var objectSource = ExpressionHelper.CreateExpression (typeof (object));

      var result = _assistantsFetchRequest.GetFetchedMemberExpression (objectSource);

      var expectedExpression = Expression.MakeMemberAccess (Expression.Convert (objectSource, typeof (Cook)), _assistantsMember);
      ExpressionTreeComparer.CheckAreEqualTrees (expectedExpression, result);
    }

    [Test]
    public void ExecuteInMemory ()
    {
      var input = new StreamedSequence (new[] { 1, 2, 3 }, new StreamedSequenceInfo (typeof (int[]), Expression.Constant (0)));
      var result = _assistantsFetchRequest.ExecuteInMemory (input);

      Assert.That (result, Is.SameAs (input));
    }

    [Test]
    public new void ToString ()
    {
      var result = _assistantsFetchRequest.ToString ();

      Assert.That (result, Is.EqualTo ("Fetch (Cook.Assistants)"));
    }

    [Test]
    public void ToString_ThenFetch ()
    {
      var cookMember = typeof (Kitchen).GetProperty ("Cook");
      var outerFetchRequest = new TestFetchRequest (cookMember);
      outerFetchRequest.GetOrAddInnerFetchRequest (_assistantsFetchRequest);
      
      var result = outerFetchRequest.ToString ();

      Assert.That (result, Is.EqualTo ("Fetch (Kitchen.Cook).ThenFetch (Cook.Assistants)"));
    }
  }
}
