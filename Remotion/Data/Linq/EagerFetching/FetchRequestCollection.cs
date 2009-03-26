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
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Remotion.Utilities;

namespace Remotion.Data.Linq.EagerFetching
{
  /// <summary>
  /// Holds a number of <see cref="CollectionFetchRequest"/> instances keyed by the <see cref="MemberInfo"/> instances representing the relation members
  /// to be eager-fetched.
  /// </summary>
  public class FetchRequestCollection
  {
    private readonly Dictionary<MemberInfo, FetchRequestBase> _fetchRequests = new Dictionary<MemberInfo, FetchRequestBase> ();

    public IEnumerable<FetchRequestBase> FetchRequests
    {
      get { return _fetchRequests.Values; }
    }

    public FetchRequestBase GetOrAddFetchRequest (LambdaExpression relatedObjectSelector)
    {
      ArgumentUtility.CheckNotNull ("relatedObjectSelector", relatedObjectSelector);

      var correspondingFetchRequest = new CollectionFetchRequest (relatedObjectSelector);

      FetchRequestBase existingFetchRequest;
      if (_fetchRequests.TryGetValue (correspondingFetchRequest.RelationMember, out existingFetchRequest))
        return existingFetchRequest;
      else
      {
        _fetchRequests.Add (correspondingFetchRequest.RelationMember, correspondingFetchRequest);
        return correspondingFetchRequest;
      }
    }

  }
}