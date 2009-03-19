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
  /// Holds a number of <see cref="IFetchRequest"/> instances keyed by the <see cref="MemberInfo"/> instances representing the relation members
  /// to be eager-fetched.
  /// </summary>
  /// <typeparam name="TOriginating">The type of the originating objects from which the related objects can be selected.</typeparam>
  public class FetchRequestCollection<TOriginating> : IFetchRequestCollection
  {
    private readonly Dictionary<MemberInfo, IFetchRequest> _fetchRequests = new Dictionary<MemberInfo, IFetchRequest> ();

    public IEnumerable<IFetchRequest> FetchRequests
    {
      get { return _fetchRequests.Values; }
    }

    public FetchRequest<TRelated> GetOrAddFetchRequest<TRelated> (Expression<Func<TOriginating, IEnumerable<TRelated>>> relatedObjectSelector)
    {
      ArgumentUtility.CheckNotNull ("relatedObjectSelector", relatedObjectSelector);

      FetchRequest<TRelated> correspondingFetchRequest = FetchRequest<TRelated>.Create (relatedObjectSelector);
      
      IFetchRequest existingFetchRequest;
      if (_fetchRequests.TryGetValue (correspondingFetchRequest.RelationMember, out existingFetchRequest))
        return (FetchRequest<TRelated>) existingFetchRequest;
      else
      {
        _fetchRequests.Add (correspondingFetchRequest.RelationMember, correspondingFetchRequest);
        return correspondingFetchRequest;
      }
    }

  }
}