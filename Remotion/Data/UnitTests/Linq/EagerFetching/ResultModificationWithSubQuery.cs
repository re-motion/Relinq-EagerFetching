using System;
using System.Collections;
using System.Collections.Generic;
using Remotion.Data.Linq;
using Remotion.Data.Linq.Clauses;
using Remotion.Data.Linq.Clauses.ExecutionStrategies;

namespace Remotion.Data.UnitTests.Linq.EagerFetching
{
  class ResultModificationWithSubQuery : ResultModificationBase
  {
    public readonly QueryModel SubQuery = ExpressionHelper.CreateQueryModel ();

    public ResultModificationWithSubQuery (SelectClause selectClause)
        : base(selectClause, CollectionExecutionStrategy.Instance)
    {
    }

    public override ResultModificationBase Clone (CloneContext cloneContext)
    {
      var clone = new ResultModificationWithSubQuery (cloneContext.ClonedClauseMapping.GetClause<SelectClause> (SelectClause));
      cloneContext.SubQueryRegistry.Add (clone.SubQuery);
      return clone;
    }

    public override IEnumerable ExecuteInMemory<T> (IEnumerable<T> items)
    {
      throw new NotImplementedException();
    }
  }
}