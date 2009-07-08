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
using System.Linq.Expressions;
using Remotion.Utilities;

namespace Remotion.Data.Linq.EagerFetching
{
  /// <summary>
  /// Provides a fluent interface to recursively fetch related objects of objects which themselves are eager-fetched.
  /// </summary>
  /// <typeparam name="TQueried">The type of the objects returned by the query.</typeparam>
  /// <typeparam name="TFetch">The type of object from which the recursive fetch operation should be made.</typeparam>
  public class FluentFetchRequest<TQueried, TFetch> : QueryableBase<TQueried>
  {
    public FluentFetchRequest (QueryProviderBase provider, Expression expression)
        : base (provider, expression)
    {
    }

    /// <summary>
    /// Specifies that, when the previous fetch request is executed, the relation indicated by <paramref name="relatedObjectSelector"/> should be 
    /// eagerly fetched, too, if supported by the query provider implementation. The relation must be a collection property.
    /// </summary>
    /// <typeparam name="TRelated">The type of the next related objects to be eager-fetched.</typeparam>
    /// <param name="relatedObjectSelector">A lambda expression selecting the next related objects to be eager-fetched.</param>
    /// <returns>A <see cref="FluentFetchRequest{TFetch, TQueried}"/> object on which further recursive fetch requests can be made. The subsequent fetches start 
    /// from the related objects fetched by the fetch request created by this method.</returns>
    public FluentFetchRequest<TQueried, TRelated> ThenFetchMany<TRelated> (Expression<Func<TFetch, IEnumerable<TRelated>>> relatedObjectSelector)
    {
      ArgumentUtility.CheckNotNull ("relatedObjectSelector", relatedObjectSelector);

      var newExpression = new ThenFetchManyExpression (Expression, relatedObjectSelector);
      return new FluentFetchRequest<TQueried, TRelated> ((QueryProviderBase) Provider, newExpression);
    }

    /// <summary>
    /// Specifies that, when the previous fetch request is executed, the relation indicated by <paramref name="relatedObjectSelector"/> should be 
    /// eagerly fetched, too, if supported by the query provider implementation. The relation must be of cardinality one.
    /// </summary>
    /// <typeparam name="TRelated">The type of the next related object to be eager-fetched.</typeparam>
    /// <param name="relatedObjectSelector">A lambda expression selecting the next related object to be eager-fetched.</param>
    /// <returns>A <see cref="FluentFetchRequest{TFetch, TQueried}"/> object on which further recursive fetch requests can be made. The subsequent fetches start 
    /// from the related object fetched by the fetch request created by this method.</returns>
    public FluentFetchRequest<TQueried, TRelated> ThenFetchOne<TRelated> (Expression<Func<TFetch, TRelated>> relatedObjectSelector)
    {
      ArgumentUtility.CheckNotNull ("relatedObjectSelector", relatedObjectSelector);

      var newExpression = new ThenFetchOneExpression (Expression, relatedObjectSelector);
      return new FluentFetchRequest<TQueried, TRelated> ((QueryProviderBase) Provider, newExpression);
    }
  }
}