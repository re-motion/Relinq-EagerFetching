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
using Remotion.Data.Linq;
using Remotion.Data.Linq.EagerFetching;

namespace Remotion.Data.UnitTests.Linq.EagerFetchingTest
{
  [TestFixture]
  public class FluentFetchRequestTest
  {
    [Test]
    public void ThenFetchMany ()
    {
      var originatingQuery = ExpressionHelper.CreateQuerySource_Detail();
      var fluentFetchRequest = 
          new FluentFetchRequest<Student_Detail, Student> ((QueryProviderBase) originatingQuery.Provider, originatingQuery.Expression);

      Expression<Func<Student, IEnumerable<int>>> relatedObjectSelector = s => s.Scores;
      var newRequest = fluentFetchRequest.ThenFetchMany (relatedObjectSelector);
      Assert.That (newRequest, Is.InstanceOfType (typeof (FluentFetchRequest<Student_Detail, int>)));
      Assert.That (newRequest.Expression, Is.InstanceOfType (typeof (ThenFetchManyExpression)));
      Assert.That (((ThenFetchExpression) newRequest.Expression).Operand, Is.SameAs (fluentFetchRequest.Expression));
      Assert.That (((ThenFetchExpression) newRequest.Expression).RelatedObjectSelector, Is.SameAs (relatedObjectSelector));
    }

    [Test]
    public void ThenFetchOne ()
    {
      var originatingQuery = ExpressionHelper.CreateQuerySource_Detail ();
      var fluentFetchRequest =
          new FluentFetchRequest<Student_Detail, Student> ((QueryProviderBase) originatingQuery.Provider, originatingQuery.Expression);

      Expression<Func<Student, int>> relatedObjectSelector = s => s.ID;
      var newRequest = fluentFetchRequest.ThenFetchOne (relatedObjectSelector);
      Assert.That (newRequest, Is.InstanceOfType (typeof (FluentFetchRequest<Student_Detail, int>)));
      Assert.That (newRequest.Expression, Is.InstanceOfType (typeof (ThenFetchOneExpression)));
      Assert.That (((ThenFetchExpression) newRequest.Expression).Operand, Is.SameAs (fluentFetchRequest.Expression));
      Assert.That (((ThenFetchExpression) newRequest.Expression).RelatedObjectSelector, Is.SameAs (relatedObjectSelector));
    }
  }
}