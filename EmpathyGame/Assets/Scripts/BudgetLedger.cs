using System;
using System.Collections.Generic;

public sealed class BudgetLedger
{
    private readonly HashSet<object> committed = new HashSet<object>();
    public int Total { get; }
    public int Remaining { get; private set; }
    public BudgetLedger(int total) { Total = Remaining = Math.Max(0, total); }
    public bool TryCommit(object document, int amount)
    {
        if (document == null || amount < 0 || amount > Remaining || !committed.Add(document))
            return false;
        Remaining -= amount;
        return true;
    }
}
