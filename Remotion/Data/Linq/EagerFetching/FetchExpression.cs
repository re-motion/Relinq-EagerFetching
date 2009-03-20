// This file is part of the re-motion Core Framework (www.re-motion.org)
// Copyright (C) 2005-2008 rubicon informationstechnologie gmbh, www.rubicon.eu
// 
// The re-motion Core Framework is free software; you can redistribute it 
// and/or modify it under the terms of the GNU Lesser General Public License 
// version 3.0 as published by the Free Software Foundation.
// 
// This framework is distributed in the hope that it will be useful, 
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with this framework; if not, see http://www.gnu.org/licenses.
// 
using System;
using System.Linq.Expressions;
using Remotion.Utilities;

namespace Remotion.Data.Linq.EagerFetching
{
  /// <summary>
  /// Represents a fetch request.
  /// </summary>
  public class FetchExpression : Expression
  {
    private readonly Expression _operand;
    private readonly LambdaExpression _relatedObjectSelector;

    public FetchExpression (Expression operand, LambdaExpression relatedObjectSelector)
      : base ((ExpressionType) (-1), ArgumentUtility.CheckNotNull ("operand", operand).Type)
    {
      ArgumentUtility.CheckNotNull ("relatedObjectSelector", relatedObjectSelector);

      _operand = operand;
      _relatedObjectSelector = relatedObjectSelector;
    }

    public Expression Operand
    {
      get { return _operand; }
    }

    public LambdaExpression RelatedObjectSelector
    {
      get { return _relatedObjectSelector; }
    }

    public override string ToString ()
    {
      return string.Format ("fetch {0} in {1}", RelatedObjectSelector, Operand);
    }
  }
}