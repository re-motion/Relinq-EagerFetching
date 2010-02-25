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
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Remotion.Data.Linq;
using Remotion.Data.Linq.Clauses;
using Remotion.Data.Linq.Clauses.ResultOperators;
using Remotion.Data.Linq.EagerFetching;
using Remotion.Data.Linq.UnitTests.TestDomain;

namespace Remotion.Data.Linq.UnitTests.EagerFetching
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
      _queryModel = ExpressionHelper.CreateQueryModel_Student ();

      _distinctResultOperator = new DistinctResultOperator ();
      _countResultOperator = new CountResultOperator ();

      _fetchOneRequest = new FetchOneRequest (typeof (Cook).GetProperty ("BuddyCook"));
      _fetchManyRequest = new FetchManyRequest (typeof (Cook).GetProperty ("Friends"));

      _innerFetchManyRequest = new FetchManyRequest (typeof (Cook).GetProperty ("Scores"));
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
