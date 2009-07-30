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
using System.Linq.Expressions;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Remotion.Data.Linq.EagerFetching;
using Remotion.Data.UnitTests.Linq.TestDomain;
using System.Collections.Generic;

namespace Remotion.Data.UnitTests.Linq.EagerFetching
{
  [TestFixture]
  public class ExtensionMethodsTest
  {
    [Test]
    public void FetchOne ()
    {
      var source = ExpressionHelper.CreateStudentQueryable ();
      Expression<Func<Student, bool>> relatedObjectSelector = s => s.HasDog;
      
      var expression = (MethodCallExpression) source.FetchOne (relatedObjectSelector).Expression;
      
      Assert.That (expression.Arguments.Count, Is.EqualTo (2));
      Assert.That (expression.Arguments[0], Is.SameAs (source.Expression));
      Assert.That (((UnaryExpression) expression.Arguments[1]).Operand, Is.SameAs (relatedObjectSelector));
      Assert.That (expression.Method, 
          Is.EqualTo (typeof (ExtensionMethods).GetMethod ("FetchOne").MakeGenericMethod (typeof (Student), typeof (bool))));
    }

    [Test]
    public void FetchMany ()
    {
      var source = ExpressionHelper.CreateStudentQueryable ();
      Expression<Func<Student, IEnumerable<Student>>> relatedObjectSelector = s => s.Friends;

      var expression = (MethodCallExpression) source.FetchMany (relatedObjectSelector).Expression;

      Assert.That (expression.Arguments.Count, Is.EqualTo (2));
      Assert.That (expression.Arguments[0], Is.SameAs (source.Expression));
      Assert.That (((UnaryExpression) expression.Arguments[1]).Operand, Is.SameAs (relatedObjectSelector));
      Assert.That (expression.Method,
          Is.EqualTo (typeof (ExtensionMethods).GetMethod ("FetchMany").MakeGenericMethod (typeof (Student), typeof (Student))));
    }

    [Test]
    public void ThenFetchOne ()
    {
      var source = ExpressionHelper.CreateStudentDetailQueryable().FetchOne (sd => sd.Student);
      Expression<Func<Student, bool>> relatedObjectSelector = s => s.HasDog;

      var expression = (MethodCallExpression) source.ThenFetchOne (relatedObjectSelector).Expression;

      Assert.That (expression.Arguments.Count, Is.EqualTo (2));
      Assert.That (expression.Arguments[0], Is.SameAs (source.Expression));
      Assert.That (((UnaryExpression) expression.Arguments[1]).Operand, Is.SameAs (relatedObjectSelector));
      Assert.That (expression.Method,
          Is.EqualTo (typeof (ExtensionMethods).GetMethod ("ThenFetchOne").MakeGenericMethod (typeof (Student_Detail), typeof (Student), typeof (bool))));
    }

    [Test]
    public void ThenFetchMany ()
    {
      var source = ExpressionHelper.CreateStudentDetailQueryable ().FetchOne (sd => sd.Student);
      Expression<Func<Student, IEnumerable<Student>>> relatedObjectSelector = s => s.Friends;

      var expression = (MethodCallExpression) source.ThenFetchMany (relatedObjectSelector).Expression;

      Assert.That (expression.Arguments.Count, Is.EqualTo (2));
      Assert.That (expression.Arguments[0], Is.SameAs (source.Expression));
      Assert.That (((UnaryExpression) expression.Arguments[1]).Operand, Is.SameAs (relatedObjectSelector));
      Assert.That (expression.Method,
          Is.EqualTo (typeof (ExtensionMethods).GetMethod ("ThenFetchMany").MakeGenericMethod (typeof (Student_Detail), typeof (Student), typeof (Student))));
    }
  }
}