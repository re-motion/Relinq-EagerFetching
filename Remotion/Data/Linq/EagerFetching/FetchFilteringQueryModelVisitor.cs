// This file is part of the re-motion Core Framework (www.re-motion.org)
// Copyright (C) 2005-2009 rubicon informationstechnologie gmbh, www.rubicon.eu
// 
// The re-motion Core Framework is free software; you can redistribute it 
// and/or modify it under the terms of the GNU Lesser General Public License 
// as published by the Free Software Foundation; either version 2.1 of the 
// License, or (at your option) any later version.
// 
// re-motion is distributed in the hope that it will be useful, 
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with re-motion; if not, see http://www.gnu.org/licenses.
// 
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Remotion.Data.Linq.Clauses;
using Remotion.Data.Linq.Utilities;
using System.Linq;

namespace Remotion.Data.Linq.EagerFetching
{
  /// <summary>
  /// Visits a <see cref="QueryModel"/>, removing all <see cref="FetchRequestBase"/> instances from its <see cref="QueryModel.ResultOperators"/>
  /// collection and returning <see cref="FetchQueryModelBuilder"/> objects for them.
  /// </summary>
  public class FetchFilteringQueryModelVisitor : QueryModelVisitorBase
  {
    //TODO: Seems not to be used. Possibly delete?
    public static FetchQueryModelBuilder[] RemoveFetchRequestsFromQueryModel (QueryModel queryModel)
    {
      ArgumentUtility.CheckNotNull ("queryModel", queryModel);

      var visitor = new FetchFilteringQueryModelVisitor ();
      queryModel.Accept (visitor);
      return visitor.FetchQueryModelBuilders.ToArray();
    }

    private readonly List<FetchQueryModelBuilder> _fetchQueryModelBuilders = new List<FetchQueryModelBuilder> ();

    protected FetchFilteringQueryModelVisitor ()
    {
    }

    protected ReadOnlyCollection<FetchQueryModelBuilder> FetchQueryModelBuilders
    {
      get { return _fetchQueryModelBuilders.AsReadOnly (); }
    }

    public override void VisitResultOperator (ResultOperatorBase resultOperator, QueryModel queryModel, int index)
    {
      ArgumentUtility.CheckNotNull ("resultOperator", resultOperator);
      ArgumentUtility.CheckNotNull ("queryModel", queryModel);

      var fetchRequest = resultOperator as FetchRequestBase;
      if (fetchRequest != null)
      {
        queryModel.ResultOperators.RemoveAt (index);
        _fetchQueryModelBuilders.Add (new FetchQueryModelBuilder (fetchRequest, queryModel, index));
      }
    }
  }
}
