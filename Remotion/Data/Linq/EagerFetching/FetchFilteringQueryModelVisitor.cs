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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Remotion.Data.Linq.Clauses;
using Remotion.Utilities;

namespace Remotion.Data.Linq.EagerFetching
{
  public class FetchFilteringQueryModelVisitor : QueryModelVisitorBase
  {
    private readonly List<FetchRequestBase> _fetchRequests = new List<FetchRequestBase> ();

    public ReadOnlyCollection<FetchRequestBase> FetchRequests 
    { 
      get { return _fetchRequests.AsReadOnly(); } 
    }

    public override void VisitResultOperator (ResultOperatorBase resultOperator, QueryModel queryModel, int index)
    {
      ArgumentUtility.CheckNotNull ("resultOperator", resultOperator);
      ArgumentUtility.CheckNotNull ("queryModel", queryModel);

      var fetchRequest = resultOperator as FetchRequestBase;
      if (fetchRequest != null)
      {
        queryModel.ResultOperators.RemoveAt (index);
        _fetchRequests.Add (fetchRequest);
      }
    }
  }
}