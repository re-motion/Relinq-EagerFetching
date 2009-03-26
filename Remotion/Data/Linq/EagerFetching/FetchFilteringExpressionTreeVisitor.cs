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
  /// Analyzes an expression tree for <see cref="FetchExpression"/> and <see cref="ThenFetchExpression"/> instances, removing them from the tree
  /// and returning them as <see cref="FetchRequestBase"/> objects.
  /// </summary>
  public class FetchFilteringExpressionTreeVisitor : ExpressionTreeVisitor
  {
    private Expression _expressionTreeRoot;
    private FetchRequestCollection _topLevelFetchRequests = new FetchRequestCollection ();
    private FetchRequestBase _lastFetchRequest;
    
    /// <summary>
    /// Visits the specified expression tree, filtering it for <see cref="FetchExpression"/> and <see cref="ThenFetchExpression"/> instances.
    /// </summary>
    /// <param name="expression">The expression tree to search.</param>
    /// <returns>A <see cref="FetchFilteringResult"/> containing the expression tree with the fetch-related expressions removed as well as a list
    /// of <see cref="FetchRequestBase"/> objects holding the fetch request data.</returns>
    public FetchFilteringResult Visit (Expression expression)
    {
      ArgumentUtility.CheckNotNull ("expression", expression);

      _expressionTreeRoot = expression;
      _topLevelFetchRequests = new FetchRequestCollection ();
      _lastFetchRequest = null;

      var newExpression = VisitExpression (expression);
      return new FetchFilteringResult (newExpression, _topLevelFetchRequests.FetchRequests.ToList ().AsReadOnly ());
    }

    protected override Expression VisitExpression (Expression expression)
    {
      var fetchExpression = expression as FetchExpression;
      var thenFetchExpression = expression as ThenFetchExpression;

      if (fetchExpression != null)
      {
        var result = VisitExpression (fetchExpression.Operand);
        _lastFetchRequest = _topLevelFetchRequests.GetOrAddFetchRequest (fetchExpression.RelatedObjectSelector);
        return result;
      }
      else if (thenFetchExpression != null)
      {
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