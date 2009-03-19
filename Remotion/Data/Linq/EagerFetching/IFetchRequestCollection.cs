using System;
using System.Collections.Generic;

namespace Remotion.Data.Linq.EagerFetching
{
  /// <summary>
  /// Defines a non-generic interface for <see cref="FetchRequestCollection{TOriginating}"/>.
  /// </summary>
  public interface IFetchRequestCollection
  {
    IEnumerable<IFetchRequest> FetchRequests { get; }
  }
}