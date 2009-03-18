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
using System.Collections.Generic;

namespace Remotion.Data.Linq.EagerFetching
{
  /// <summary>
  /// Represents a relation property that should be eager-fetched by means of a lambda expression.
  /// </summary>
  /// <typeparam name="TRelated">The type of the related.</typeparam>
  public class FetchRequest<TRelated> : IFetchRequest
  {
    public static FetchRequest<TRelated> Create<TOriginating> (Expression<Func<TOriginating, IEnumerable<TRelated>>> relatedObjectSelector)
    {
      ArgumentUtility.CheckNotNull ("relatedObjectSelector", relatedObjectSelector);
      return new FetchRequest<TRelated> (relatedObjectSelector);
    }

    private readonly LambdaExpression _relatedObjectSelector;
    private readonly FetchRequestCollection<TRelated> _innerFetchRequestCollection = new FetchRequestCollection<TRelated>();

    private FetchRequest (LambdaExpression relatedObjectSelector)
    {
      ArgumentUtility.CheckNotNull ("relatedObjectSelector", relatedObjectSelector);
      _relatedObjectSelector = relatedObjectSelector;
    }

    public LambdaExpression RelatedObjectSelector
    {
      get { return _relatedObjectSelector; }
    }

    /// <summary>
    /// Gets the inner fetch requests that were issued for this <see cref="FetchRequest{TRelated}"/>.
    /// </summary>
    /// <value>The fetch requests added via <see cref="GetOrAddInnerFetchRequest{TRelated}"/>.</value>
    public IEnumerable<IFetchRequest> InnerFetchRequests
    {
      get { return _innerFetchRequestCollection.FetchRequests; }
    }

    /// <summary>
    /// Gets or adds an inner eager-fetch request for this <see cref="FetchRequest{TRelated}"/>.
    /// </summary>
    /// <typeparam name="TNextRelated">The type of related objects to be fetched.</typeparam>
    /// <param name="relatedObjectSelector">A lambda expression selecting related objects for a given query result object.</param>
    /// <returns>An <see cref="IFetchRequest"/> instance representing the fetch request.</returns>
    public FetchRequest<TNextRelated> GetOrAddInnerFetchRequest<TNextRelated> (Expression<Func<TRelated, IEnumerable<TNextRelated>>> relatedObjectSelector)
    {
      ArgumentUtility.CheckNotNull ("relatedObjectSelector", relatedObjectSelector);
      return _innerFetchRequestCollection.GetOrAddFetchRequest (relatedObjectSelector);
    }
  }
}