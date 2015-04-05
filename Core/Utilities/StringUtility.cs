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
using System.Collections.Generic;

#if NET_3_5
using System.Linq;
#endif

namespace Remotion.Linq.EagerFetching.Utilities
{
  internal static class StringUtility
  {
    public static string Join<T> (string separator, IEnumerable<T> values)
    {
#if !NET_3_5
      return string.Join (separator, values);
#else
      if (typeof (T) == typeof (string))
        return string.Join (separator, values.Cast<string>().ToArray());
      else
        return string.Join (separator, values.Select (v=>v.ToString()).ToArray());
#endif
    }
  }
}