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
using NUnit.Framework;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.ResultOperators;
using Remotion.Linq.Development.UnitTesting;
using Remotion.Linq.EagerFetching;
using Remotion.Linq.UnitTests.TestDomain;

namespace Remotion.Linq.UnitTests.EagerFetching
{
  [TestFixture]
  public class FetchFilteringQueryModelVisitorTest
  {
    private TestFetchFilteringQueryModelVisitor _visitor;
    private QueryModel _queryModel;
    
    private FetchOneRequest _fetchOneRequest;
    private FetchManyRequest _fetchManyRequest;
    private FetchManyRequest _innerFetchManyRequest;

    private DistinctResultOperator _distinctResultOperator;
    private CountResultOperator _countResultOperator;
    
    [SetUp]
    public void SetUp ()
    {
      _visitor = new TestFetchFilteringQueryModelVisitor ();
      _queryModel = ExpressionHelper.CreateQueryModel<Cook> ();

      _distinctResultOperator = new DistinctResultOperator ();
      _countResultOperator = new CountResultOperator ();

      _fetchOneRequest = new FetchOneRequest (typeof (Cook).GetProperty ("Substitution"));
      _fetchManyRequest = new FetchManyRequest (typeof (Cook).GetProperty ("Assistants"));

      _innerFetchManyRequest = new FetchManyRequest (typeof (Cook).GetProperty ("Holidays"));
      _fetchOneRequest.GetOrAddInnerFetchRequest (_innerFetchManyRequest);

      _queryModel.ResultOperators.Add (_distinctResultOperator);
      _queryModel.ResultOperators.Add (_fetchOneRequest);
      _queryModel.ResultOperators.Add (_fetchManyRequest);
      _queryModel.ResultOperators.Add (_countResultOperator);
    }

    [Test]
    public void VisitResultOperator_IgnoresOrdinaryOperator ()
    {
      _visitor.VisitResultOperator (_distinctResultOperator, _queryModel, 0);

      Assert.That (_queryModel.ResultOperators, 
          Is.EqualTo (new ResultOperatorBase[] { _distinctResultOperator, _fetchOneRequest, _fetchManyRequest, _countResultOperator }));
    }

    [Test]
    public void VisitResultOperator_CapturesFetchRequest ()
    {
      _visitor.VisitResultOperator (_fetchOneRequest, _queryModel, 1);

      Assert.That (_queryModel.ResultOperators,
          Is.EqualTo (new ResultOperatorBase[] { _distinctResultOperator, _fetchManyRequest, _countResultOperator }));
      
      Assert.That (_visitor.FetchQueryModelBuilders.Count, Is.EqualTo (1));
      Assert.That (_visitor.FetchQueryModelBuilders[0].FetchRequest, Is.SameAs (_fetchOneRequest));
      Assert.That (_visitor.FetchQueryModelBuilders[0].SourceItemQueryModel, Is.SameAs (_queryModel));
      Assert.That (_visitor.FetchQueryModelBuilders[0].ResultOperatorPosition, Is.EqualTo (1));
    }

    [Test]
    public void IntegrationTest ()
    {
      _visitor.VisitQueryModel (_queryModel);

      Assert.That (_queryModel.ResultOperators, Is.EqualTo (new ResultOperatorBase[] { _distinctResultOperator, _countResultOperator }));
      Assert.That (_visitor.FetchQueryModelBuilders.Count, Is.EqualTo (2));

      Assert.That (_visitor.FetchQueryModelBuilders[0].FetchRequest, Is.SameAs (_fetchOneRequest));
      Assert.That (_visitor.FetchQueryModelBuilders[0].SourceItemQueryModel, Is.SameAs (_queryModel));
      Assert.That (_visitor.FetchQueryModelBuilders[0].ResultOperatorPosition, Is.EqualTo (1)); // Distinct included, Count not

      Assert.That (_visitor.FetchQueryModelBuilders[1].FetchRequest, Is.SameAs (_fetchManyRequest));
      Assert.That (_visitor.FetchQueryModelBuilders[1].SourceItemQueryModel, Is.SameAs (_queryModel));
      Assert.That (_visitor.FetchQueryModelBuilders[1].ResultOperatorPosition, Is.EqualTo (1)); // Distinct included, Count not
    }
  }
}
