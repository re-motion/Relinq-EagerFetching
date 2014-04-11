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
using Remotion.Linq.Clauses;
using Remotion.Linq.Parsing;
using Remotion.Linq.Parsing.Structure.IntermediateModel;
using Remotion.Utilities;

namespace Remotion.Linq.EagerFetching.Parsing
{
  /// <summary>
  /// Provides common functionality for <see cref="ThenFetchOneExpressionNode"/> and <see cref="ThenFetchManyExpressionNode"/>.
  /// </summary>
  public abstract class ThenFetchExpressionNodeBase : FetchExpressionNodeBase
  {
    protected ThenFetchExpressionNodeBase (MethodCallExpressionParseInfo parseInfo, LambdaExpression relatedObjectSelector)
        : base(parseInfo, relatedObjectSelector)
    {
    }

    protected abstract FetchRequestBase CreateFetchRequest ();

    protected override ResultOperatorBase CreateResultOperator (ClauseGenerationContext clauseGenerationContext)
    {
      throw new NotImplementedException ("Call ApplyNodeSpecificSemantics instead.");
    }

    protected override QueryModel ApplyNodeSpecificSemantics (QueryModel queryModel, ClauseGenerationContext clauseGenerationContext)
    {
      ArgumentUtility.CheckNotNull ("queryModel", queryModel);

      var previousFetchRequest = clauseGenerationContext.GetContextInfo (Source) as FetchRequestBase;
      if (previousFetchRequest == null)
        throw new NotSupportedException ("ThenFetchMany must directly follow another Fetch request.");

      var innerFetchRequest = CreateFetchRequest();
      innerFetchRequest = previousFetchRequest.GetOrAddInnerFetchRequest (innerFetchRequest);
      // Store a mapping between this node and the innerFetchRequest so that a later ThenFetch... node may add its request to the innerFetchRequest.
      clauseGenerationContext.AddContextInfo (this, innerFetchRequest);

      return queryModel;
    }
  }
}