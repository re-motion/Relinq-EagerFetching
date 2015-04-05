// This file is part of the re-linq project (relinq.codeplex.com)
// Copyright (c) rubicon IT GmbH, www.rubicon.eu
// 
// re-linq is free software; you can redistribute it and/or modify it under 
// the terms of the GNU Lesser General Public License as published by the 
// Free Software Foundation; either version 2.1 of the License, 
// or (at your option) any later version.
// 
// re-linq is distributed in the hope that it will be useful, 
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with re-linq; if not, see http://www.gnu.org/licenses.
// 

using System;
using System.Reflection;
using NUnit.Framework;

namespace Remotion.Linq.EagerFetching.UnitTests.Utilities
{
  [TestFixture]
  public class TypeInfoTest
  {
    private class TestType
    {
      public TestType ()
      {
      }
    }

    private class DerivedType : TestType
    {
    }

    [Test]
    public void IsAssignableFrom_WithAssignableType_ReturnsTrue ()
    {
      var type = typeof (TestType);
      var typeInfo = type.GetTypeInfo();
      Assert.That (typeInfo.IsAssignableFrom (typeof (DerivedType).GetTypeInfo()), Is.True);
      Assert.That (typeInfo.IsAssignableFrom (typeof (DerivedType).GetTypeInfo()), Is.EqualTo (type.IsAssignableFrom (typeof (DerivedType))));
    }

    [Test]
    public void IsAssignableFrom_WithNotAssignableType_ReturnsFalse ()
    {
      var type = typeof (DerivedType);
      var typeInfo = type.GetTypeInfo();
      Assert.That (typeInfo.IsAssignableFrom (typeof (TestType).GetTypeInfo()), Is.False);
      Assert.That (typeInfo.IsAssignableFrom (typeof (TestType).GetTypeInfo()), Is.EqualTo (type.IsAssignableFrom (typeof (TestType))));
    }

    [Test]
    public void IsValueType_WithValueType_ReturnsTrue ()
    {
      var type = typeof (int);
      var typeInfo = type.GetTypeInfo();
      Assert.That (typeInfo.IsValueType, Is.True);
      Assert.That (typeInfo.IsValueType, Is.EqualTo (type.IsValueType));
    }

    [Test]
    public void IsValueType_WithReferenceType_ReturnsFalse ()
    {
      var type = typeof (string);
      var typeInfo = type.GetTypeInfo();
      Assert.That (typeInfo.IsValueType, Is.False);
      Assert.That (typeInfo.IsValueType, Is.EqualTo (type.IsValueType));
    }
  }
}