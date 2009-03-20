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
using System.Collections.Generic;
using System.Linq.Expressions;
using Remotion.Data.Linq.Parsing;
using Remotion.Data.Linq.Visitor;
using Remotion.Utilities;
using System.Linq;

namespace Remotion.Data.Linq.EagerFetching
{
  // TODO 1089: Test
  /// <summary>
  /// Analyzes an expression tree for <see cref="FetchExpression"/> and <see cref="ThenFetchExpression"/> instances, removing them in a tree
  /// and returning them via <see cref="TopLevelFetchRequests"/>.
  /// </summary>
  public class FetchFilteringExpressionTreeVisitor : ExpressionTreeVisitor
  {
    private Expression _expressionTreeRoot;
    private FetchRequestCollection _topLevelFetchRequests = new FetchRequestCollection ();
    private FetchRequest _lastFetchRequest;

    public FetchRequest[] TopLevelFetchRequests
    {
      get { return _topLevelFetchRequests.FetchRequests.ToArray(); }
    }

    public Expression Visit (Expression expression)
    {
      ArgumentUtility.CheckNotNull ("expression", expression);

      _expressionTreeRoot = expression;
      _topLevelFetchRequests = new FetchRequestCollection ();
      _lastFetchRequest = null;

      return VisitExpression (expression);
    }

    protected override Expression VisitExpression (Expression expression)
    {
      var fetchExpression = expression as FetchExpression;
      var thenFetchExpression = expression as ThenFetchExpression;

      if (fetchExpression != null)
      {
        // TODO 1089: test that this comes before setting _lastFetchRequest
        // TODO 1089: integration test with two fetches
        var result = VisitExpression (fetchExpression.Operand);
        _lastFetchRequest = _topLevelFetchRequests.GetOrAddFetchRequest (fetchExpression.RelatedObjectSelector);
        return result;
      }
      else if (thenFetchExpression != null)
      {
        // TODO 1089: test that this comes before setting _lastFetchRequest
        var result = VisitExpression (thenFetchExpression.Operand);

        if (_lastFetchRequest == null)
        {
          throw ParserUtility.CreateParserException (
              "FetchExpression preceding ThenFetchExpression", thenFetchExpression, "filtering fetch expressions", _expressionTreeRoot);
        }

        _lastFetchRequest = _lastFetchRequest.GetOrAddInnerFetchRequest (thenFetchExpression.RelatedObjectSelector);
        return result;
      }
      else
      {
        _lastFetchRequest = null;
        return base.VisitExpression (expression);
      }
    }
  }
}