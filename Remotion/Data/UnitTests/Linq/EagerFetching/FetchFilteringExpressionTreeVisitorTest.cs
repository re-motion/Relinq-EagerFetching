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
using System.Linq.Expressions;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Remotion.Data.Linq.EagerFetching;
using System.Linq;
using Remotion.Data.Linq.Parsing;
using Remotion.Data.UnitTests.Linq.Parsing.ExpressionTreeVisitors;

namespace Remotion.Data.UnitTests.Linq.EagerFetching
{
  [TestFixture]
  public class FetchFilteringExpressionTreeVisitorTest
  {
    [Test]
    public void Visit_OrdinaryExpression ()
    {
      var expression = ExpressionHelper.CreateExpression ();

      var result = FetchFilteringExpressionTreeVisitor.Visit (expression);

      Assert.That (result.NewExpression, Is.SameAs (expression));
      Assert.That (result.FetchRequests, Is.Empty);
    }

    [Test]
    public void Visit_FetchManyExpression ()
    {
      var innerExpression = ExpressionHelper.CreateExpression ();
      var relatedObjectSelector = ExpressionHelper.CreateLambdaExpression<Student, IEnumerable<Student>> (s => s.Friends);
      var fetchExpression = new FetchManyExpression (innerExpression, relatedObjectSelector);

      var result = FetchFilteringExpressionTreeVisitor.Visit (fetchExpression);

      Assert.That (result.NewExpression, Is.SameAs (innerExpression));
      Assert.That (result.FetchRequests.Count, Is.EqualTo (1));
      Assert.That (result.FetchRequests[0], Is.InstanceOfType (typeof (FetchManyRequest)));
      Assert.That (result.FetchRequests[0].RelatedObjectSelector, Is.SameAs (relatedObjectSelector));
    }

    [Test]
    public void Visit_FetchOneExpression ()
    {
      var innerExpression = ExpressionHelper.CreateExpression ();
      var relatedObjectSelector = ExpressionHelper.CreateLambdaExpression<Student, IEnumerable<Student>> (s => s.Friends);
      var fetchExpression = new FetchOneExpression (innerExpression, relatedObjectSelector);

      var result = FetchFilteringExpressionTreeVisitor.Visit (fetchExpression);

      Assert.That (result.NewExpression, Is.SameAs (innerExpression));
      Assert.That (result.FetchRequests.Count, Is.EqualTo (1));
      Assert.That (result.FetchRequests[0], Is.InstanceOfType (typeof (FetchOneRequest)));
      Assert.That (result.FetchRequests[0].RelatedObjectSelector, Is.SameAs (relatedObjectSelector));
    }

    [Test]
    public void Visit_FetchExpression_BuriedWithinOtherExpression ()
    {
      var innerExpression = ExpressionHelper.CreateExpression ();
      var relatedObjectSelector = ExpressionHelper.CreateLambdaExpression<Student, IEnumerable<Student>> (s => s.Friends);
      var fetchExpression = new FetchManyExpression (innerExpression, relatedObjectSelector);
      var unaryExpression = Expression.Quote (fetchExpression);

      var result = FetchFilteringExpressionTreeVisitor.Visit (unaryExpression);

      Assert.That (result.NewExpression, Is.InstanceOfType (typeof (UnaryExpression)));
      Assert.That (result.NewExpression.NodeType, Is.EqualTo(ExpressionType.Quote));
      Assert.That (result.NewExpression, Is.Not.SameAs (unaryExpression));
      Assert.That (((UnaryExpression)result.NewExpression).Operand, Is.SameAs (innerExpression));
      Assert.That (result.FetchRequests.Count, Is.EqualTo (1));
      Assert.That (result.FetchRequests[0].RelatedObjectSelector, Is.SameAs (relatedObjectSelector));
    }

    [Test]
    public void Visit_MultipleFetchExpressions ()
    {
      var innerExpression = ExpressionHelper.CreateExpression ();
      var relatedObjectSelector1 = ExpressionHelper.CreateLambdaExpression<Student, IEnumerable<int>> (s => s.Scores);
      var relatedObjectSelector2 = ExpressionHelper.CreateLambdaExpression<Student, IEnumerable<Student>> (s => s.Friends);
      var fetchExpression1 = new FetchManyExpression (innerExpression, relatedObjectSelector1);
      var fetchExpression2 = new FetchManyExpression (fetchExpression1, relatedObjectSelector2);

      var result = FetchFilteringExpressionTreeVisitor.Visit (fetchExpression2);

      Assert.That (result.NewExpression, Is.SameAs (innerExpression));
      Assert.That (result.FetchRequests.Count, Is.EqualTo (2));
      Assert.That (result.FetchRequests.Select (fr => fr.RelatedObjectSelector).ToArray(), 
          Is.EquivalentTo (new Expression[] {relatedObjectSelector1, relatedObjectSelector2}));
    }

    [Test]
    public void Visit_SameFetchExpressionTwice ()
    {
      var innerExpression = ExpressionHelper.CreateExpression ();
      var relatedObjectSelector1 = ExpressionHelper.CreateLambdaExpression<Student, IEnumerable<Student>> (s => s.Friends);
      var relatedObjectSelector2 = ExpressionHelper.CreateLambdaExpression<Student, IEnumerable<Student>> (s => s.Friends);
      var fetchExpression1 = new FetchManyExpression (innerExpression, relatedObjectSelector1);
      var fetchExpression2 = new FetchManyExpression (fetchExpression1, relatedObjectSelector2);

      var result = FetchFilteringExpressionTreeVisitor.Visit (fetchExpression2);

      Assert.That (result.NewExpression, Is.SameAs (innerExpression));
      Assert.That (result.FetchRequests.Count, Is.EqualTo (1));
    }

    [Test]
    [ExpectedException (typeof (ParserException), ExpectedMessage = "Expected FetchExpression preceding ThenFetchExpression for filtering fetch "
        + "expressions, found 'then fetch s => s.Friends in new [] {}' (ThenFetchManyExpression).")]
    public void Visit_ThenFetchExpression_WithoutFetch ()
    {
      var innerExpression = ExpressionHelper.CreateExpression ();
      var relatedObjectSelector = ExpressionHelper.CreateLambdaExpression<Student, IEnumerable<Student>> (s => s.Friends);
      var thenFetchExpression = new ThenFetchManyExpression (innerExpression, relatedObjectSelector);
      
      FetchFilteringExpressionTreeVisitor.Visit (thenFetchExpression);
    }

    [Test]
    [ExpectedException (typeof (ParserException), ExpectedMessage = "Expected FetchExpression preceding ThenFetchExpression for filtering fetch "
        + "expressions, found 'then fetch s => s.Friends in new [] {}' (ThenFetchManyExpression).")]
    public void Visit_ThenFetchExpression_WithOuterFetch ()
    {
      var innerExpression = ExpressionHelper.CreateExpression ();
      var relatedObjectSelector1 = ExpressionHelper.CreateLambdaExpression<Student, IEnumerable<Student>> (s => s.Friends);
      var relatedObjectSelector2 = ExpressionHelper.CreateLambdaExpression<Student, IEnumerable<Student>> (s => s.Friends);
      var thenFetchExpression = new ThenFetchManyExpression (innerExpression, relatedObjectSelector1);
      var fetchExpression = new FetchManyExpression (thenFetchExpression, relatedObjectSelector2);

      FetchFilteringExpressionTreeVisitor.Visit (fetchExpression);
    }

    [Test]
    public void Visit_ThenFetchManyExpression_WithInnerFetch ()
    {
      var innerExpression = ExpressionHelper.CreateExpression ();
      var relatedObjectSelector1 = ExpressionHelper.CreateLambdaExpression<Student, IEnumerable<Student>> (s => s.Friends);
      var relatedObjectSelector2 = ExpressionHelper.CreateLambdaExpression<Student, IEnumerable<Student>> (s => s.Friends);
      var fetchExpression = new FetchManyExpression (innerExpression, relatedObjectSelector1);
      var thenFetchExpression = new ThenFetchManyExpression (fetchExpression, relatedObjectSelector2);

      var result = FetchFilteringExpressionTreeVisitor.Visit (thenFetchExpression);

      Assert.That (result.NewExpression, Is.SameAs (innerExpression));
      Assert.That (result.FetchRequests.Count, Is.EqualTo (1));
      Assert.That (result.FetchRequests[0].RelatedObjectSelector, Is.SameAs (relatedObjectSelector1));
      Assert.That (result.FetchRequests[0].InnerFetchRequests.Count(), Is.EqualTo (1));
      Assert.That (result.FetchRequests[0].InnerFetchRequests.Single().RelatedObjectSelector, Is.SameAs (relatedObjectSelector2));

      var fetchRequestForThenFetchExpression = result.FetchRequests[0].InnerFetchRequests.Single ();
      Assert.That (fetchRequestForThenFetchExpression, Is.InstanceOfType (typeof (FetchManyRequest)));
    }

    [Test]
    public void Visit_ThenFetchOneExpression_WithInnerFetch ()
    {
      var innerExpression = ExpressionHelper.CreateExpression ();
      var relatedObjectSelector1 = ExpressionHelper.CreateLambdaExpression<Student, IEnumerable<Student>> (s => s.Friends);
      var relatedObjectSelector2 = ExpressionHelper.CreateLambdaExpression<Student, IEnumerable<Student>> (s => s.Friends);
      var fetchExpression = new FetchManyExpression (innerExpression, relatedObjectSelector1);
      var thenFetchExpression = new ThenFetchOneExpression (fetchExpression, relatedObjectSelector2);

      var result = FetchFilteringExpressionTreeVisitor.Visit (thenFetchExpression);

      Assert.That (result.NewExpression, Is.SameAs (innerExpression));
      Assert.That (result.FetchRequests.Count, Is.EqualTo (1));
      Assert.That (result.FetchRequests[0].RelatedObjectSelector, Is.SameAs (relatedObjectSelector1));
      Assert.That (result.FetchRequests[0].InnerFetchRequests.Count (), Is.EqualTo (1));
      Assert.That (result.FetchRequests[0].InnerFetchRequests.Single ().RelatedObjectSelector, Is.SameAs (relatedObjectSelector2));

      var fetchRequestForThenFetchExpression = result.FetchRequests[0].InnerFetchRequests.Single ();
      Assert.That (fetchRequestForThenFetchExpression, Is.InstanceOfType (typeof (FetchOneRequest)));
    }

    [Test]
    public void Visit_ThenFetchExpression_AroundThenFetch ()
    {
      var innerExpression = ExpressionHelper.CreateExpression ();
      var relatedObjectSelector1 = ExpressionHelper.CreateLambdaExpression<Student, IEnumerable<Student>> (s => s.Friends);
      var relatedObjectSelector2 = ExpressionHelper.CreateLambdaExpression<Student, IEnumerable<Student>> (s => s.Friends);
      var relatedObjectSelector3 = ExpressionHelper.CreateLambdaExpression<Student, IEnumerable<Student>> (s => s.Friends);
      var fetchExpression = new FetchManyExpression (innerExpression, relatedObjectSelector1);
      var thenFetchExpression1 = new ThenFetchManyExpression (fetchExpression, relatedObjectSelector2);
      var thenFetchExpression2 = new ThenFetchManyExpression (thenFetchExpression1, relatedObjectSelector3);

      var result = FetchFilteringExpressionTreeVisitor.Visit (thenFetchExpression2);

      Assert.That (result.NewExpression, Is.SameAs (innerExpression));
      Assert.That (result.FetchRequests.Count, Is.EqualTo (1));
      Assert.That (result.FetchRequests[0].RelatedObjectSelector, Is.SameAs (relatedObjectSelector1));
      Assert.That (result.FetchRequests[0].InnerFetchRequests.Count(), Is.EqualTo (1));
      Assert.That (result.FetchRequests[0].InnerFetchRequests.Single ().RelatedObjectSelector, Is.SameAs (relatedObjectSelector2));
      Assert.That (result.FetchRequests[0].InnerFetchRequests.Single ().InnerFetchRequests.Count(), Is.EqualTo (1));
      Assert.That (result.FetchRequests[0].InnerFetchRequests.Single ().InnerFetchRequests.Single().RelatedObjectSelector, Is.SameAs (relatedObjectSelector3));
    }

    [Test]
    public void Visit_ThenFetchExpression_AroundTwoFetches ()
    {
      var innerExpression = ExpressionHelper.CreateExpression ();
      var relatedObjectSelector1 = ExpressionHelper.CreateLambdaExpression<Student, IEnumerable<int>> (sd => sd.Scores);
      var relatedObjectSelector2 = ExpressionHelper.CreateLambdaExpression<Student, IEnumerable<Student>> (s => s.Friends);
      var relatedObjectSelector3 = ExpressionHelper.CreateLambdaExpression<Student, IEnumerable<Student>> (s => s.Friends);
      var fetchExpression1 = new FetchManyExpression (innerExpression, relatedObjectSelector1);
      var fetchExpression2 = new FetchManyExpression (fetchExpression1, relatedObjectSelector2);
      var thenFetchExpression = new ThenFetchManyExpression (fetchExpression2, relatedObjectSelector3);

      var result = FetchFilteringExpressionTreeVisitor.Visit (thenFetchExpression);

      Assert.That (result.NewExpression, Is.SameAs (innerExpression));
      Assert.That (result.FetchRequests.Count, Is.EqualTo (2));
      
      var fetchResultForExpression2 = result.FetchRequests.Where (fr => fr.RelatedObjectSelector == relatedObjectSelector2).Single();
      Assert.That (fetchResultForExpression2.InnerFetchRequests.Count(), Is.EqualTo (1));
      Assert.That (fetchResultForExpression2.InnerFetchRequests.Single().RelatedObjectSelector, Is.SameAs(relatedObjectSelector3));
    }

    [Test]
    public void VisitUnknownExpression_Ignored ()
    {
      var expression = new UnknownExpression (typeof (object));
      var result =  FetchFilteringExpressionTreeVisitor.Visit (expression);

      Assert.That (result.NewExpression, Is.SameAs (expression));
    }
  }
}