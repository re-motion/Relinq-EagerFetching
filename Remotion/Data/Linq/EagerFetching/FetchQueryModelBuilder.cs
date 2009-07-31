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
using Remotion.Data.Linq.Clauses;
using Remotion.Utilities;
using System.Linq;

namespace Remotion.Data.Linq.EagerFetching
{
  /// <summary>
  /// Holds a <see cref="FetchRequestBase"/>, a <see cref="QueryModel"/> for which the fetch request was created, and the position
  /// where the <see cref="FetchRequestBase"/> occurred in the <see cref="Linq.QueryModel.ResultOperators"/> list of the <see cref="QueryModel"/>. From
  /// this information, it builds a new <see cref="QueryModel"/> that represents the <see cref="FetchRequestBase"/> as a query.
  /// </summary>
  /// <remarks>
  /// Use <see cref="FetchFilteringQueryModelVisitor"/> to retrieve the <see cref="FetchQueryModelBuilder"/> instances for a <see cref="QueryModel"/>.
  /// </remarks>
  public class FetchQueryModelBuilder
  {
    private QueryModel _cachedFetchModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="FetchQueryModelBuilder"/> class.
    /// </summary>
    /// <param name="fetchRequest">The fetch request.</param>
    /// <param name="queryModel">The query model for which the <paramref name="fetchRequest"/> was originally defined.</param>
    /// <param name="resultOperatorPosition">The result operator position where the <paramref name="fetchRequest"/> was originally located.
    /// The <see cref="FetchQueryModelBuilder"/> will include all result operators prior to this position into the fetch <see cref="QueryModel"/>,
    /// but it will not include any result operators occurring after (or at) that position.</param>
    public FetchQueryModelBuilder (FetchRequestBase fetchRequest, QueryModel queryModel, int resultOperatorPosition)
    {
      ArgumentUtility.CheckNotNull ("fetchRequest", fetchRequest);
      ArgumentUtility.CheckNotNull ("queryModel", queryModel);

      FetchRequest = fetchRequest;
      QueryModel = queryModel;
      ResultOperatorPosition = resultOperatorPosition;
    }

    public FetchRequestBase FetchRequest { get; private set; }
    public QueryModel QueryModel { get; private set; }
    public int ResultOperatorPosition { get; private set; }

    /// <summary>
    /// Creates the fetch query model for the <see cref="FetchRequestBase"/>, caching the result.
    /// </summary>
    /// <returns>
    /// A new <see cref="QueryModel"/> which represents the same query as <see cref="QueryModel"/> but selecting
    /// the objects described by <see cref="FetchRequestBase.RelationMember"/> instead of the objects selected by the 
    /// <see cref="QueryModel"/>. From the original <see cref="QueryModel"/>, only those result operators are included that occur
    /// prior to <see cref="ResultOperatorPosition"/>.
    /// </returns>
    public QueryModel GetOrCreateFetchQueryModel ()
    {
      if (_cachedFetchModel == null)
      {
        var sourceItemModel = QueryModel.Clone();

        int resultOperatorsToDelete = sourceItemModel.ResultOperators.Count - ResultOperatorPosition;
        for (int i = 0; i < resultOperatorsToDelete; ++i)
          sourceItemModel.ResultOperators.RemoveAt (ResultOperatorPosition);

        _cachedFetchModel = FetchRequest.CreateFetchQueryModel (sourceItemModel);
      }

      return _cachedFetchModel;
    }

    /// <summary>
    /// Creates <see cref="FetchQueryModelBuilder"/> objects for the <see cref="FetchRequestBase.InnerFetchRequests"/> of the 
    /// <see cref="FetchRequest"/>. Inner fetch requests start from the fetch query model of the outer fetch request, and they have
    /// the same <see cref="ResultOperatorPosition"/> as the outer fetch request.
    /// </summary>
    /// <returns>An array of <see cref="FetchQueryModelBuilder"/> objects for the <see cref="FetchRequestBase.InnerFetchRequests"/> of the
    /// <see cref="FetchRequest"/>.</returns>
    public FetchQueryModelBuilder[] CreateInnerBuilders ()
    {
      var innerBuilders = FetchRequest.InnerFetchRequests.Select (
          request => new FetchQueryModelBuilder (request, GetOrCreateFetchQueryModel(), ResultOperatorPosition));
      return innerBuilders.ToArray ();
    }
  }
}