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
using System.Linq;
using System.Linq.Expressions;
using Remotion.Data.Linq.Parsing;
using Remotion.Utilities;

namespace Remotion.Data.Linq.EagerFetching
{
  /// <summary>
  /// Analyzes an expression tree for <see cref="FetchExpression"/> and <see cref="ThenFetchExpression"/> instances, removing them from the tree
  /// and returning them as <see cref="FetchRequestBase"/> objects.
  /// </summary>
  public class FetchFilteringExpressionTreeVisitor : ExpressionTreeVisitor
  {
    /// <summary>
    /// Visits the specified expression tree, filtering it for <see cref="FetchExpression"/> and <see cref="ThenFetchExpression"/> instances.
    /// </summary>
    /// <param name="expression">The expression tree to search.</param>
    /// <returns>A <see cref="FetchFilteringResult"/> containing the expression tree with the fetch-related expressions removed as well as a list
    /// of <see cref="FetchRequestBase"/> objects holding the fetch request data.</returns>
    public static FetchFilteringResult Visit (Expression expression)
    {
      ArgumentUtility.CheckNotNull ("expression", expression);

      FetchFilteringExpressionTreeVisitor visitor = new FetchFilteringExpressionTreeVisitor();
      var newExpression = visitor.VisitExpression (expression);
      return new FetchFilteringResult (newExpression, visitor._topLevelFetchRequests.FetchRequests.ToList().AsReadOnly());
    }

    private readonly FetchRequestCollection _topLevelFetchRequests = new FetchRequestCollection();
    private FetchRequestBase _lastFetchRequest;

    private FetchFilteringExpressionTreeVisitor ()
    {
      _topLevelFetchRequests = new FetchRequestCollection();
      _lastFetchRequest = null;
    }

    protected override Expression VisitExpression (Expression expression)
    {
      var fetchExpression = expression as FetchExpression;
      var thenFetchExpression = expression as ThenFetchExpression;

      if (fetchExpression != null)
      {
        var result = VisitExpression (fetchExpression.Operand);
        var fetchRequest = fetchExpression.CreateFetchRequest();
        _lastFetchRequest = _topLevelFetchRequests.GetOrAddFetchRequest (fetchRequest);
        return result;
      }
      else if (thenFetchExpression != null)
      {
        var result = VisitExpression (thenFetchExpression.Operand);

        if (_lastFetchRequest == null)
        {
          throw new ParserException (
              "FetchExpression preceding ThenFetchExpression", thenFetchExpression, "filtering fetch expressions");
        }

        var fetchRequest = thenFetchExpression.CreateFetchRequest();
        _lastFetchRequest = _lastFetchRequest.GetOrAddInnerFetchRequest (fetchRequest);
        return result;
      }
      else
      {
        _lastFetchRequest = null;
        return base.VisitExpression (expression);
      }
    }

    protected override Expression VisitUnknownExpression (Expression expression)
    {
      //ignore
      return expression;
    }
  }
}