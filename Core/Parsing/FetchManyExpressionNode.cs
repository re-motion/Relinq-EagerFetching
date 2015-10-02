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
using Remotion.Linq.Parsing.Structure;
using Remotion.Linq.Parsing.Structure.IntermediateModel;
using Remotion.Utilities;

namespace Remotion.Linq.EagerFetching.Parsing
{
  /// <summary>
  /// Parses query operators that instruct the LINQ provider to fetch a collection-valued relationship starting from the values selected by the query. 
  /// The node creates <see cref="FetchManyRequest"/> instances and adds them to the <see cref="QueryModel"/> as 
  /// <see cref="QueryModel.ResultOperators"/> (unless the <see cref="QueryModel"/> already has an equivalent fetch request).
  /// </summary>
  /// <remarks>
  /// This class is not automatically configured for any query operator methods. LINQ provider implementations must explicitly provide and register 
  /// these  methods with the <see cref="QueryParser"/> in order for <see cref="FetchManyExpressionNode"/> to be used.
  /// <example>
  /// Sample code for using fluent syntax when specifying fetch requests.
  ///   <code>
  ///   public static class EagerFetchingExtensionMethods
  ///   {
  ///     public static FluentFetchRequest&lt;TOriginating, TRelated&gt; FetchMany&lt;TOriginating, TRelated&gt; (
  ///         this IQueryable&lt;TOriginating&gt; query, 
  ///         Expression&lt;Func&lt;TOriginating, IEnumerable&lt;TRelated&gt;&gt;&gt; relatedObjectSelector)
  ///     {
  ///      
  ///       var methodInfo = ((MethodInfo) MethodBase.GetCurrentMethod ()).MakeGenericMethod (typeof (TOriginating), typeof (TRelated));
  ///       return CreateFluentFetchRequest&lt;TOriginating, TRelated&gt; (methodInfo, query, relatedObjectSelector);
  ///     }
  ///  
  ///     private static FluentFetchRequest&lt;TOriginating, TRelated&gt; CreateFluentFetchRequest&lt;TOriginating, TRelated&gt; (
  ///         MethodInfo currentFetchMethod, 
  ///         IQueryable&lt;TOriginating&gt; query, 
  ///         LambdaExpression relatedObjectSelector)
  ///     {
  ///       var queryProvider = (QueryProviderBase) query.Provider;
  ///       var callExpression = Expression.Call (currentFetchMethod, query.Expression, relatedObjectSelector);
  ///       return new FluentFetchRequest&lt;TOriginating, TRelated&gt; (queryProvider, callExpression);
  ///     }
  ///   }
  ///   
  ///   public class FluentFetchRequest&lt;TQueried, TFetch&gt; : QueryableBase&lt;TQueried&gt;
  ///   {
  ///     public FluentFetchRequest (IQueryProvider provider, Expression expression)
  ///         : base (provider, expression)
  ///     {
  ///     }
  ///   }
  ///   
  ///   public IQueryParser CreateQueryParser ()
  ///   {
  ///     var customNodeTypeProvider = new MethodInfoBasedNodeTypeRegistry ();
  ///     customNodeTypeProvider.Register (new[] { typeof (EagerFetchingExtensionMethods).GetMethod ("FetchMany") }, typeof (FetchManyExpressionNode));
  ///     
  ///     var nodeTypeProvider = ExpressionTreeParser.CreateDefaultNodeTypeProvider ();
  ///     nodeTypeProvider.InnerProviders.Insert (0, customNodeTypeProvider);
  ///     
  ///     var transformerRegistry = ExpressionTransformerRegistry.CreateDefault ();
  ///     var processor = ExpressionTreeParser.CreateDefaultProcessor (transformerRegistry);
  ///     var expressionTreeParser = new ExpressionTreeParser (nodeTypeProvider, processor);
  ///     
  ///     return new QueryParser (expressionTreeParser);
  ///   }
  ///   </code>
  /// </example>
  /// <seealso cref="FetchOneExpressionNode"/>
  /// <seealso cref="ThenFetchOneExpressionNode"/>
  /// <seealso cref="ThenFetchManyExpressionNode"/>
  /// </remarks>
  public class FetchManyExpressionNode : OuterFetchExpressionNodeBase
  {
    public FetchManyExpressionNode (MethodCallExpressionParseInfo parseInfo, LambdaExpression relatedObjectSelector)
        : base (parseInfo, ArgumentUtility.CheckNotNull ("relatedObjectSelector", relatedObjectSelector))
    {
    }

    protected override FetchRequestBase CreateFetchRequest ()
    {
      return new FetchManyRequest (RelationMember);
    }
  }
}
