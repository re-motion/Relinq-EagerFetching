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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Remotion.Utilities;

namespace Remotion.Data.Linq.EagerFetching
{
  public static class ExtensionMethods
  {
    /// <summary>
    /// Specifies that, when the <paramref name="query"/> is executed, the relation indicated by <paramref name="relatedObjectSelector"/> should be eagerly
    /// fetched if supported by the query provider implementation. The relation must be a collection property.
    /// </summary>
    /// <typeparam name="TOriginating">The type of the originating query result objects.</typeparam>
    /// <typeparam name="TRelated">The type of the related objects to be eager-fetched.</typeparam>
    /// <param name="query">The query for which the fetch request should be made.</param>
    /// <param name="relatedObjectSelector">A lambda expression selecting the related objects to be eager-fetched.</param>
    /// <returns>A <see cref="FluentFetchRequest{TOriginating, TRelated}"/> object on which further fetch requests can be made. The subsequent fetches start from the 
    /// related objects fetched by the original request created by this method.</returns>
    public static FluentFetchRequest<TOriginating, TRelated> FetchMany<TOriginating, TRelated> (
        this IQueryable<TOriginating> query, Expression<Func<TOriginating, IEnumerable<TRelated>>> relatedObjectSelector)
    {
      ArgumentUtility.CheckNotNull ("query", query);
      ArgumentUtility.CheckNotNull ("relatedObjectSelector", relatedObjectSelector);

      var methodInfo = ((MethodInfo) MethodBase.GetCurrentMethod ()).MakeGenericMethod (typeof (TOriginating), typeof (TRelated));
      return CreateFluentFetchRequest<TOriginating, TRelated> (methodInfo, query, relatedObjectSelector);
    }

    /// <summary>
    /// Specifies that, when the <paramref name="query"/> is executed, the relation indicated by <paramref name="relatedObjectSelector"/> should be eagerly
    /// fetched if supported by the query provider implementation. The relation must be of cardinality one.
    /// </summary>
    /// <typeparam name="TOriginating">The type of the originating query result objects.</typeparam>
    /// <typeparam name="TRelated">The type of the related objects to be eager-fetched.</typeparam>
    /// <param name="query">The query for which the fetch request should be made.</param>
    /// <param name="relatedObjectSelector">A lambda expression selecting the related object to be eager-fetched.</param>
    /// <returns>A <see cref="FluentFetchRequest{TOriginating, TRelated}"/> object on which further fetch requests can be made. The subsequent fetches start from the 
    /// related object fetched by the original request created by this method.</returns>
    public static FluentFetchRequest<TOriginating, TRelated> FetchOne<TOriginating, TRelated> (
        this IQueryable<TOriginating> query, Expression<Func<TOriginating, TRelated>> relatedObjectSelector)
    {
      ArgumentUtility.CheckNotNull ("query", query);
      ArgumentUtility.CheckNotNull ("relatedObjectSelector", relatedObjectSelector);

      var methodInfo = ((MethodInfo) MethodBase.GetCurrentMethod ()).MakeGenericMethod (typeof (TOriginating), typeof (TRelated));
      return CreateFluentFetchRequest<TOriginating, TRelated> (methodInfo, query, relatedObjectSelector);
    }

    /// <summary>
    /// Specifies that, when the previous fetch request is executed, the relation indicated by <paramref name="relatedObjectSelector"/> should be 
    /// eagerly fetched, too, if supported by the query provider implementation. The relation must be a collection property.
    /// </summary>
    /// <typeparam name="TRelated">The type of the next related objects to be eager-fetched.</typeparam>
    /// <typeparam name="TQueried">The type of the objects returned by the query.</typeparam>
    /// <typeparam name="TFetch">The type of object from which the recursive fetch operation should be made.</typeparam>
    /// <param name="query">The query for which the fetch request should be made.</param>
    /// <param name="relatedObjectSelector">A lambda expression selecting the next related objects to be eager-fetched.</param>
    /// <returns>A <see cref="FluentFetchRequest{TFetch, TQueried}"/> object on which further recursive fetch requests can be made. The subsequent fetches start 
    /// from the related objects fetched by the fetch request created by this method.</returns>
    public static FluentFetchRequest<TQueried, TRelated> ThenFetchMany<TQueried, TFetch, TRelated> (this FluentFetchRequest<TQueried, TFetch> query, Expression<Func<TFetch, IEnumerable<TRelated>>> relatedObjectSelector)
    {
      ArgumentUtility.CheckNotNull ("query", query);
      ArgumentUtility.CheckNotNull ("relatedObjectSelector", relatedObjectSelector);

      var methodInfo = ((MethodInfo) MethodBase.GetCurrentMethod ()).MakeGenericMethod (typeof (TQueried), typeof (TFetch), typeof (TRelated));
      return CreateFluentFetchRequest<TQueried, TRelated> (methodInfo, query, relatedObjectSelector);
    }

    /// <summary>
    /// Specifies that, when the previous fetch request is executed, the relation indicated by <paramref name="relatedObjectSelector"/> should be 
    /// eagerly fetched, too, if supported by the query provider implementation. The relation must be a collection property.
    /// </summary>
    /// <typeparam name="TRelated">The type of the next related objects to be eager-fetched.</typeparam>
    /// <typeparam name="TQueried">The type of the objects returned by the query.</typeparam>
    /// <typeparam name="TFetch">The type of object from which the recursive fetch operation should be made.</typeparam>
    /// <param name="query">The query for which the fetch request should be made.</param>
    /// <param name="relatedObjectSelector">A lambda expression selecting the next related objects to be eager-fetched.</param>
    /// <returns>A <see cref="FluentFetchRequest{TFetch, TQueried}"/> object on which further recursive fetch requests can be made. The subsequent fetches start 
    /// from the related objects fetched by the fetch request created by this method.</returns>
    public static FluentFetchRequest<TQueried, TRelated> ThenFetchOne<TQueried, TFetch, TRelated> (this FluentFetchRequest<TQueried, TFetch> query, Expression<Func<TFetch, TRelated>> relatedObjectSelector)
    {
      ArgumentUtility.CheckNotNull ("query", query);
      ArgumentUtility.CheckNotNull ("relatedObjectSelector", relatedObjectSelector);
      
      var methodInfo = ((MethodInfo) MethodBase.GetCurrentMethod ()).MakeGenericMethod (typeof (TQueried), typeof (TFetch), typeof (TRelated));
      return CreateFluentFetchRequest<TQueried, TRelated> (methodInfo, query, relatedObjectSelector);
    }

    private static FluentFetchRequest<TOriginating, TRelated> CreateFluentFetchRequest<TOriginating, TRelated> (
        MethodInfo currentFetchMethod, 
        IQueryable<TOriginating> query, 
        LambdaExpression relatedObjectSelector)
    {
      var queryProvider = ArgumentUtility.CheckNotNullAndType<QueryProviderBase> ("query.Provider", query.Provider);
      var callExpression = Expression.Call (currentFetchMethod, query.Expression, relatedObjectSelector);
      return new FluentFetchRequest<TOriginating, TRelated> (queryProvider, callExpression);
    }
  }
}