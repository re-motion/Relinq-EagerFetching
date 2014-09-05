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
using System.Linq.Expressions;
using Remotion.Linq.Parsing.Structure.IntermediateModel;
using Remotion.Utilities;

namespace Remotion.Linq.EagerFetching.Parsing
{
  /// <summary>
  /// Parses query operators that instruct the LINQ provider to fetch an object-valued relationship starting from another fetch operation. The node 
  /// creates <see cref="FetchOneRequest"/> instances and attaches them to the preceding fetch operation (unless the previous fetch operation already 
  /// has an equivalent fetch request).
  /// </summary>
  /// <remarks>
  /// This class is not automatically configured for any query operator methods. LINQ provider implementations must explicitly provide and register 
  /// these methods in order for <see cref="ThenFetchOneExpressionNode"/> to be used. See also <see cref="FluentFetchRequest{TQueried,TFetch}"/>.
  /// </remarks>
  public class ThenFetchOneExpressionNode : ThenFetchExpressionNodeBase
  {
    public ThenFetchOneExpressionNode (MethodCallExpressionParseInfo parseInfo, LambdaExpression relatedObjectSelector)
        : base (parseInfo, ArgumentUtility.CheckNotNull ("relatedObjectSelector", relatedObjectSelector))
    {
    }

    protected override FetchRequestBase CreateFetchRequest ()
    {
      return new FetchOneRequest (RelationMember);
    }
  }
}
